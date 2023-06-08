using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VivoxUnity;

public class ChannelsManager
{
    private ILoginSession LoginSession;
    public IChannelSession GlobalChannelSession;

    private string _domain;
    private string _tokenIssuer;
    private string _tokenKey;

    private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);

    private ChatChannel globalChannel_voice;
    private ChatChannel globalChannel_text;

    private ChatChannel currentVoiceChannel;

    private Dictionary<string, ChatChannel> activeChannels = new Dictionary<string, ChatChannel>();

    public ChatChannel GlobalChannel { get => globalChannel_voice; }
    public ChatChannel CurrentChannel { get => currentVoiceChannel; }

    private const string GLOBAL_CHANNEL_TEXT_ID = "global_text_GlobalChannel";
    public const string GLOBAL_CHANNEL_VOICE_ID = "global_voice_GlobalChannel";

    public delegate void CurrentChannelChangeHandler();
    public event CurrentChannelChangeHandler OnCurrentChannelChangeEvent;

    public void SetVivoxConfig(ILoginSession loginSession, string domain, string tokenIssuer, string tokenKey)
    {
        LoginSession = loginSession;
        _domain = domain;
        _tokenIssuer = tokenIssuer;
        _tokenKey = tokenKey;
    }

    #region Channels

    public void ConnectToGlobalChannel()
    {
        //Global voice
        globalChannel_voice = GetVoiceChat(GLOBAL_CHANNEL_VOICE_ID, ChatChannel.CommunicationType.Global, "Global_channel", "Pavia");
        currentVoiceChannel = globalChannel_voice;

        Channel3DProperties properties = new Channel3DProperties(
            audibleDistance: 32, // To avoid sound being abruptly cut off, set at minimum of 32 x ConversationalDistance / AudioFadeIntensityByDistance
            conversationalDistance: 1,
            audioFadeModel: AudioFadeModel.InverseByDistance,
            audioFadeIntensityByDistanceaudio: 1.0f
            );

        JoinChannel(currentVoiceChannel, ChannelType.Positional, connectAudio: true, connectText: false, switchTransmission: true, properties);

        GlobalChannelSession = GetChannelSessionByID(globalChannel_voice.ID);

        //Global text
        globalChannel_text = GetTextChat(GLOBAL_CHANNEL_TEXT_ID, ChatChannel.CommunicationType.Global, "Global_channel", "Pavia");

        //Config chat script (UI)
        ServiceManager.UIManager.GlobalChatMenu.SetCurrentChannel(globalChannel_text);
        ServiceManager.UIManager.InitPlayersUI();
    }

    public void JoinChannel(ChatChannel channel, ChannelType channelType, bool connectAudio, bool connectText, bool switchTransmission, Channel3DProperties properties = null)
    {
        if (ServiceManager.VivoxManager.LoginState != LoginState.LoggedIn) return;

        ChannelId channelId = new ChannelId(_tokenIssuer, channel.ID, _domain, channelType, properties);
        IChannelSession channelSession = LoginSession.GetChannelSession(channelId);

        channelSession.Participants.AfterKeyAdded += OnParticipantJoin;
        channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
        channelSession.MessageLog.AfterItemAdded += OnChannelMessageReceived;

        channelSession.BeginConnect(connectAudio, connectText, switchTransmission, channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
        {
            try
            {
                channelSession.EndConnect(ar);
                activeChannels.TryAdd(channel.ID, channel);
            }
            catch (Exception e)
            {
                // Handle error 
                VivoxLogError($"Could not connect to voice channel: {e.Message}");
                return;
            }
        });
    }

    public async UniTask LeaveChannel(ChatChannel chatChannelToLeave)
    {
        IChannelSession channelSession = GetChannelSessionByID(chatChannelToLeave.ID);

        if (channelSession != null)
        {
            var request = channelSession.Disconnect();

            await UniTask.WaitUntil(() => request.IsCompleted);

            activeChannels.Remove(chatChannelToLeave.ID);
        }
    }

    public ChatChannel GetChannel(string channelId)
    {
        activeChannels.TryGetValue(channelId, out ChatChannel channel);
        return channel;
    }

    public ChatChannel GetDirectChannel(string userId, string targetId)
    {
        string channelId = GetDirectChannelId(userId, targetId);

        if (activeChannels.TryGetValue(channelId, out ChatChannel channel))
        {
            return channel;
        }

        return null;
    }

    public static string GetDirectChannelId(string userId, string targetId)
    {
        string[] participantsIds = { userId, targetId };
        Array.Sort(participantsIds);
        return "private_text_" + participantsIds[0] + "_" + participantsIds[1];
    }

    private IChannelSession GetChannelSessionByID(string chatCommunicationID)
    {
        return LoginSession.ChannelSessions.FirstOrDefault(n => n.Key.Name == chatCommunicationID);
    }

    public bool ChatSessionExists(string chatCommunicationID)
    {
        return activeChannels.ContainsKey(chatCommunicationID);
        //return GetChannelSessionByID(chatCommunicationID) != null;
    }

    public bool IsCurrentChannel(string channelId)
    {
        return currentVoiceChannel.ID.Contains(channelId);
    }

    public void Clear()
    {
        activeChannels.Clear();
    }

    #endregion

    #region Voice calls

    public void SetCurrentChannel(ChatChannel channel)
    {
        currentVoiceChannel = channel;
        OnCurrentChannelChangeEvent?.Invoke();
    }

    public void MuteCurrentChannel(bool mute)
    {
        foreach (ChatParticipant participant in currentVoiceChannel.Participants.Values)
        {
            if (participant.id != ServiceManager.LocalPlayer.UserGUID)
                participant.Mute(mute);
        }
    }

    public void MuteGlobalChannel()
    {
        foreach (ChatParticipant participant in globalChannel_voice.Participants.Values)
        {
            if (participant.id != ServiceManager.LocalPlayer.UserGUID)
                participant.Mute(true);
        }
    }

    public void UnmuteGlobalChannel()
    {
        foreach (ChatParticipant participant in globalChannel_voice.Participants.Values)
        {
            if (participant.id != ServiceManager.LocalPlayer.UserGUID && !ServiceManager.VivoxManager.IsMutedPlayer(participant.id))
                participant.Mute(false);
        }

        LoginSession.SetTransmissionMode(TransmissionMode.Single, GlobalChannelSession.Channel);
    }

    public ChatChannel GetVoiceChat(string channelId, ChatChannel.CommunicationType comType, string channelName, string moderator, List<string> targetIds = null)
    {
        if (ChatSessionExists(channelId))
        {
            ChatChannel channel = GetChannel(channelId);
            return channel;
        }

        ChatChannel chatChannel = new ChatChannel(channelId, comType, ChatChannel.MediaType.Voice, channelName, ServiceManager.VivoxManager, moderator, targetIds);
        return chatChannel;
    }

    #endregion

    #region Text chatting

    public ChatChannel GetTextChat(string channelId, ChatChannel.CommunicationType comType = ChatChannel.CommunicationType.Private,
        string channelName = null, string moderator = null, List<string> members = null, string targetId = null)
    {
        if (ChatSessionExists(channelId))
        {
            ChatChannel channel = GetChannel(channelId);
            return channel;
        }

        ChatChannel chatChannel = new ChatChannel(channelId, comType, ChatChannel.MediaType.Text, channelName, ServiceManager.VivoxManager, moderator, members, targetId);

        if (comType == ChatChannel.CommunicationType.Global)
        {
            JoinChannel(chatChannel, ChannelType.NonPositional, connectAudio: false, connectText: true, switchTransmission: false);
        }
        else if (comType == ChatChannel.CommunicationType.Group)
        {
            JoinChannel(chatChannel, ChannelType.NonPositional, connectAudio: false, connectText: true, switchTransmission: false);
        }
        else if (comType == ChatChannel.CommunicationType.Private)
        {
            ChatParticipant localParticipant = new ChatParticipant(ServiceManager.LocalPlayer.UserName, ServiceManager.LocalPlayer.UserGUID, null);
            ChatParticipant targetParticipant = new ChatParticipant(channelName, targetId, null);

            chatChannel.AddParticipant(localParticipant);
            chatChannel.AddParticipant(targetParticipant);

            activeChannels.Add(channelId, chatChannel);
        }

        return chatChannel;
    }

    public async UniTask SendMessage(ChatChannel channel, ClientChatMessage message)
    {
        if (channel.IsPrivateChannel)
        {
            await SendDirectMessage(channel.TargetID, message.message); //ChannelId = targetId (user)
            return;
        }

        IChannelSession channelSession = GetChannelSessionByID(channel.ID);
        var sendTask = channelSession.BeginSendText(message.message, (ar) =>
        {
            try
            {
                channelSession.EndSendText(ar);
            }
            catch (Exception e)
            {
                // Handle error 
                VivoxLogError(nameof(e));
                return;
            }
        });

        while (!sendTask.IsCompleted)
        {
            await UniTask.NextFrame();
        }
    }

    public void SendLocalNotification(ChatChannel channel, string message)
    {
        channel.OnMessageReceived(new ClientChatMessage("", "", message, channel.ID, fromSelf: true, VivoxChatToken.chat_notification));
    }

    public UniTask SendDirectMessage(string userId, string message, string localMessage = null)
    {
        bool completedSend = false;
        AccountId targetId = new AccountId(_tokenIssuer, userId, _domain);
        LoginSession.BeginSendDirectedMessage(targetId, message, ar =>
        {
            try
            {
                LoginSession.EndSendDirectedMessage(ar);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            completedSend = true;

            if (localMessage != null) message = localMessage;

            //Add message to chatChannel.messages
            var _directChannel = GetDirectChannel(ServiceManager.LocalPlayer.UserGUID, targetId.Name);
            _directChannel.OnMessageReceived(new ClientChatMessage(ServiceManager.LocalPlayer.UserGUID, ServiceManager.LocalPlayer.UserName,
                message, _directChannel.ID, fromSelf: true));
        });

        return UniTask.WaitUntil(() => completedSend);
    }

    public void OnDirectedMessageReceived(object sender, QueueItemAddedEventArgs<IDirectedTextMessage> itemAdded)
    {
        IReadOnlyQueue<IDirectedTextMessage> directedMessages = (IReadOnlyQueue<IDirectedTextMessage>)sender;
        while (directedMessages.Count > 0)
        {
            IDirectedTextMessage _message = directedMessages.Dequeue();
            //Debug.Log($"From {_message.Sender.Name}: {_message.Message} | To: {ServiceManager.LocalPlayer.UserName}");

            var _directChannel = GetDirectChannel(ServiceManager.LocalPlayer.UserGUID, _message.Sender.Name);
            _directChannel.OnMessageReceived(new ClientChatMessage(_message.Sender.Name, _message.Sender.DisplayName, _message.Message, _directChannel.ID, fromSelf: false));
        }
    }

    #endregion

    #region Events

    private void OnParticipantJoin(object sender, KeyEventArg<string> keyEventArg)
    {
        ValidateArgs(new object[] { sender, keyEventArg });

        VivoxUnity.IReadOnlyDictionary<string, IParticipant> source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;

        IParticipant participant = source[keyEventArg.Key];
        IChannelSession channelSession = participant.ParentChannelSession;

        activeChannels.TryGetValue(channelSession.Key.Name, out ChatChannel channel);
        ChatParticipant chatParticipant = new ChatParticipant(participant.Account.DisplayName, participant.Account.Name, participant);
        channel.AddParticipant(chatParticipant);

        if (participant.IsSelf)
        {
            VivoxLog($"Subscribing to: {channelSession.Key.Name}");

            if (channel.IsVoiceChannel && !channel.IsGlobalChannel)
            {
                ServiceManager.MessageManager.CmdAddParticipant(channel.ID, participant.Account.Name, participant.Account.DisplayName);
            }
        }
        else
        {
            if (channel.IsVoiceChannel)
            {
                if (channel.IsGlobalChannel)
                {
                    OnlinePlayer player = ServiceManager.Players.GetPlayer(chatParticipant.id);
                    if (player != null)
                    {
                        player.ui.Label.Speaker.Init(player.PlayerID);
                    }
                }

                // If participant is muted
                // If user is in private call and participant joins global channel
                if (ServiceManager.VivoxManager.IsParticipantMuted(participant.Account.Name) ||
                    (channel.IsGlobalChannel && ServiceManager.VivoxManager.Channels.CurrentChannel.IsPrivateChannel))
                {
                    chatParticipant.Mute(true);
                }
            }
        }
    }

    private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
    {
        ValidateArgs(new object[] { sender, keyEventArg });

        // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        VivoxUnity.IReadOnlyDictionary<string, IParticipant> source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        // Look up the participant via the key.
        IParticipant participant = source[keyEventArg.Key];
        //ChannelId channel = participant.ParentChannelSession.Key;
        IChannelSession channelSession = participant.ParentChannelSession;

        activeChannels.TryGetValue(channelSession.Key.Name, out ChatChannel channel);
        channel.RemoveParticipant(participant.Account.Name);

        if (participant.IsSelf)
        {
            VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
            // Now that we are disconnected, unsubscribe.
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterKeyAdded -= OnParticipantJoin;
            channelSession.MessageLog.AfterItemAdded -= OnChannelMessageReceived;

            // Remove session.
            LoginSession.DeleteChannelSession(channelSession.Channel);

            if (channel.IsVoiceChannel && !channel.IsGlobalChannel)
            {
                ServiceManager.MessageManager.CmdRemoveParticipant(channel.ID, participant.Account.Name, participant.Account.DisplayName);
            }
        }
        else
        {
            if (channel.IsVoiceChannel)
            {
                if (channel.IsPrivateChannel && channel.ID == ServiceManager.VivoxManager.Channels.CurrentChannel.ID && channel.Participants.Count == 1)
                {
                    ServiceManager.VivoxManager.VoiceChat.LeaveCall();
                }
            }
        }
    }

    private void OnChannelMessageReceived(object sender, QueueItemAddedEventArgs<IChannelTextMessage> queueItemAddedEventArgs)
    {
        string channelID = queueItemAddedEventArgs.Value.ChannelSession.Channel.Name;
        string senderID = queueItemAddedEventArgs.Value.Sender.Name;
        string senderUsername = queueItemAddedEventArgs.Value.Sender.DisplayName;
        string message = queueItemAddedEventArgs.Value.Message;
        bool self = queueItemAddedEventArgs.Value.FromSelf;

        if (activeChannels.ContainsKey(channelID))
        {
            ClientChatMessage clientMessage;
            if (message.StartsWith('$'))
            {
                message = message.Substring(1, message.Length - 1);
                clientMessage = new ClientChatMessage("", "", message, channelID, self, VivoxChatToken.chat_notification);
            }
            else
            {
                clientMessage = new ClientChatMessage(senderID, senderUsername, message, channelID, self, VivoxChatToken.chat_message);
            }

            activeChannels[channelID].OnMessageReceived(clientMessage);

            //if (clientMessage.ChatToken == VivoxChatToken.chat_message || clientMessage.ChatToken == VivoxChatToken.chat_notification)
            //{
            //    activeChannels[channelID].OnMessageReceived(clientMessage);
            //}
            //else if (clientMessage.ChatToken == VivoxChatToken.chat_notification && self)
            //{
            //    activeChannels[channelID].OnUserChanged(clientMessage);
            //}
        }
    }

    #endregion

    #region Utils

    private void VivoxLogError(string msg)
    {
        Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
    }

    private void VivoxLog(string msg)
    {
        Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
    }

    private static void ValidateArgs(object[] objs)
    {
        foreach (object obj in objs)
        {
            if (obj == null)
                throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
        }
    }

    #endregion
}
