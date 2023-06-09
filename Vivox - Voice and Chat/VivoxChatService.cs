using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using VivoxUnity;

public class VivoxChatService : IChatService
{
    public ILoginSession LoginSession;

    private string _server = "https://mt1s.www.vivox.com/api2";
    private string _domain = "mt1s.vivox.com";
    private string _tokenIssuer = "pavia3558-vi72-dev";
    private string _tokenKey = "know691";

    private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);
    private Client client = new Client();
    private AccountId _accountId;
    private bool disposedValue;

    public ChannelsManager Channels = new ChannelsManager();
    public VoiceChat VoiceChat = new VoiceChat();

    public LoginState LoginState { get; private set; }

    public bool IsMuted { get => client.AudioInputDevices.Muted; }
    public List<string> mutedPlayers = new List<string>();

    private Uri _serverUri
    {
        get => new Uri(_server);

        set { _server = value.ToString(); }
    }

    public delegate void LoginStatusChangedHandler();

    public event LoginStatusChangedHandler OnUserLoggedInEvent;

    public VivoxChatService()
    {
        OnUserLoggedInEvent += Channels.ConnectToGlobalChannel;
    }

    //Login
    public async UniTask<bool> Connect(string userId, string userName)
    {
        ServiceManager.LocalPlayer.UserGUID = userId;
        ServiceManager.LocalPlayer.UserName = userName;

        Channels.Clear();

        if (client == null) //If was previously logged in
        {
            client = new Client();
            disposedValue = false;
        }

        if (!client.Initialized)
        {
            client.Initialize();
        }

        _accountId = new AccountId(_tokenIssuer, userId, _domain, displayname: userName);
        LoginSession = client.GetLoginSession(_accountId);
        LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;

        Channels.SetVivoxConfig(LoginSession, _domain, _tokenIssuer, _tokenKey);

        bool loginEnd = false;
        LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                LoginSession.EndLogin(ar);
            }
            catch (Exception e)
            {
                // Handle error 
                VivoxLogError(nameof(e));
                // Unbind if we failed to login.
                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                return;
            }

            loginEnd = true;
        });

        await UniTask.WaitUntil(() => loginEnd);

        LoginSession.DirectedMessages.AfterItemAdded += Channels.OnDirectedMessageReceived;

        return LoginState == LoginState.LoggedIn;
    }

    private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (propertyChangedEventArgs.PropertyName != "State") return;

        ILoginSession loginSession = (ILoginSession)sender;
        LoginState = loginSession.State;
        VivoxLog("Detecting login session change: " + LoginState.ToString());

        switch (LoginState)
        {
            case LoginState.LoggedIn:
                {
                    VivoxLog("Connected and logged in.");
                    OnUserLoggedInEvent?.Invoke();
                    break;
                }
            case LoginState.LoggedOut:
                {
                    VivoxLog("Logged out");
                    LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                    client.Uninitialize();
                    break;
                }
            default:
                break;
        }
    }

    public async void LeaveVivox()
    {
        ServiceManager.UIManager.Dispose();
        LoginSession.Logout();
        await UniTask.WaitUntil(() => LoginState == LoginState.LoggedOut);
        ServiceManager.NetworkManager.StopClient();
    }

    public void Set3DPosition(Transform listener)
    {
        if (LoginSession == null || LoginSession.State != LoginState.LoggedIn || Channels.GlobalChannelSession.AudioState != ConnectionState.Connected
            || Channels.GlobalChannelSession.Channel.Type != ChannelType.Positional) return;

        Channels.GlobalChannelSession.Set3DPosition(listener.position, listener.position, Camera.main.transform.forward, Camera.main.transform.up);
    }

    public void Dispose()
    {
        OnUserLoggedInEvent -= Channels.ConnectToGlobalChannel;

        Client.Cleanup();
        if (client != null)
        {
            VivoxLog("Uninitializing client.");
            client.Uninitialize();
            client = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue && disposing)
        {
            Client.Cleanup();
            if (client != null)
            {
                VivoxLog("Uninitializing client.");
                client.Uninitialize();
                client = null;
            }

            disposedValue = true;
        }
    }

    private void VivoxLogError(string msg)
    {
        Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
    }

    private void VivoxLog(string msg)
    {
        Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
    }

    public async UniTask SendMessage(ChatChannel channel, ClientChatMessage message) { await Channels.SendMessage(channel, message); }

    public void ToggleMuteParticipant(string participantId)
    {
        ToggleMuteParticipant(participantId, Channels.CurrentChannel);
    }

    public void ToggleMuteParticipant(string participantId, ChatChannel channel)
    {
        if (participantId == ServiceManager.LocalPlayer.UserGUID)
        {
            ToggleMuteSelf();
            return;
        }

        bool participantMuted = IsParticipantMuted(participantId);

        if (channel.Participants.ContainsKey(participantId))
        {
            if (participantMuted) RemoveMutedPlayer(participantId);
            else AddMutedPlayer(participantId);

            ChatParticipant participant = channel.GetParticipant(participantId);
            participant.Mute(!participantMuted);
        }
    }

    public bool IsParticipantMuted(string participantId, ChatChannel channel = null)
    {
        if (participantId == ServiceManager.LocalPlayer.UserGUID)
        {
            return IsSelfMuted();
        }

        if (channel == null) channel = Channels.CurrentChannel;

        ChatParticipant participant = channel.GetParticipant(participantId);
        if (participant == null) return true;

        return participant.IsMuted();
    }

    public void AddMutedPlayer(string participantId) { if (!mutedPlayers.Contains(participantId)) mutedPlayers.Add(participantId); }
    public void RemoveMutedPlayer(string participantId) { if (mutedPlayers.Contains(participantId)) mutedPlayers.Remove(participantId); }
    public bool IsMutedPlayer(string playerId) { return mutedPlayers.Contains(playerId); }

    public void ToggleMuteSelf()
    {
        client.AudioInputDevices.Muted = !client.AudioInputDevices.Muted;
    }

    public bool IsSelfMuted()
    {
        return client.AudioInputDevices.Muted;
    }

    public void AdjustParticipantVolume(string channelId, string participantId, int volumeAdjustment)
    {
        ChatParticipant participant = GetParticipant(channelId, participantId);
        if (participant != null) participant.AdjustVolume(volumeAdjustment);
    }

    public bool IsSpeaking(string channelId, string participantId)
    {
        ChatParticipant participant = GetParticipant(channelId, participantId);
        return (participant != null) ? participant.IsSpeaking() : false;
    }

    private ChatParticipant GetParticipant(string channelId, string participantId)
    {
        ChatChannel channel = Channels.GetChannel(channelId);
        if (channel != null)
        {
            ChatParticipant participant = channel.GetParticipant(participantId);
            if (participant != null)
            {
                return participant;
            }
        }
        return null;
    }
}

public enum VivoxChatToken
{
    chat_message = 0,
    chat_notification = 1,
    chat_group_invitation = 2,
    message_notification = 3,
    chat_private_invitation = 4
}
