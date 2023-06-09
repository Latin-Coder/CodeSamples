using System.Collections.Generic;
using VivoxUnity;

public class VoiceChat
{
    private ChannelsManager Channels => ServiceManager.VivoxManager.Channels;

    public struct CallInfo
    {
        public string callerId;
        public List<string> members; // Target (private) or targets (group)
        public string channelName;
        public string channelId;
        public ChannelType channelType;
        public string textChannelId;
    }

    private CallInfo currentCallInfo;
    private CallInfo incomingCallInfo;

    private bool isCaller;

    private int numResponses;
    private int numResponsesDeclined;

    public bool Calling { get; private set; }

    public enum VoiceCallMessage { Call_Start, Call_Cancel, Call_Accept, Call_Decline, Call_Leave }

    public void InitCall(ChatChannel textChannel)
    {
        string channelId = textChannel.GetChannelType() + "_voice_" + textChannel.IDWithoutPrefix();
        ChannelType channelType = ChannelType.NonPositional;

        isCaller = true;

        currentCallInfo = new CallInfo()
        {
            callerId = ServiceManager.LocalPlayer.UserGUID,
            members = textChannel.MembersList,
            channelId = channelId,
            channelType = channelType,
            textChannelId = textChannel.ID
        };

        if (textChannel.IsPrivateChannel)
        {
            currentCallInfo.channelName = ServiceManager.LocalPlayer.UserName;
        }
        else if (textChannel.IsGroupChannel)
        {
            currentCallInfo.channelName = textChannel.ChannelName;
        }

        VoiceCall voiceCall = ServiceManager.CallManager.GetVoiceCall(channelId);
        if (voiceCall.ID == null)
        {
            ServiceManager.MessageManager.CmdSendCallStarted(
                currentCallInfo.callerId,                       // To send message when accepting / declining call
                currentCallInfo.members,                        // To get target OnlinePlayer object 
                ServiceManager.LocalPlayer.UserName,            // To set Call Notification message
                textChannel.ID,                                 // To get Channel info in target
                currentCallInfo.channelName                     // To create Channel if it doesn't exist
                );

            ServiceManager.MessageManager.CmdAddVoiceCall(channelId, textChannel.MembersList);
            ServiceManager.UIManager.TextChatMenu.SetCallButtonState(textChannel.ID, CallButton.CallState.Calling);
        }
        else
        {
            EnterCall();
        }
    }

    public void CancelCall()
    {
        if (isCaller)
        {
            SendMessageToServer(VoiceCallMessage.Call_Cancel);
            ServiceManager.MessageManager.CmdRemoveVoiceCall(currentCallInfo.channelId);
        }

        EndCall();
    }


    public async void EnterCall()
    {
        if (Channels.CurrentChannel != null)
        {
            //If we are already connected to that channel, we return
            if (Channels.CurrentChannel.ID == currentCallInfo.channelId) { return; }

            if (Channels.CurrentChannel.ID != ChannelsManager.GLOBAL_CHANNEL_VOICE_ID)
            {
                //We leave the previous voice channel
                await Channels.LeaveChannel(Channels.CurrentChannel);
            }
            else
            {
                ServiceManager.UIManager.GlobalChatMenu.SetMuteAll(true);
                Channels.MuteGlobalChannel();
            }
        }
        else
        {
            ServiceManager.EventDispatcher.AddListener<OnVoiceCallChangeEvent>(OnVoiceCallChange);
        }

        ChatChannel textChannel = ServiceManager.VivoxManager.Channels.GetChannel(currentCallInfo.textChannelId);

        ChatChannel oldChannel = Channels.CurrentChannel;
        Channels.SetCurrentChannel(Channels.GetVoiceChat(currentCallInfo.channelId, textChannel.communicationType, currentCallInfo.channelName, "Pavia", currentCallInfo.members));

        ServiceManager.UIManager.ShowActiveCall(oldChannel.GetTextChannelID(), Channels.CurrentChannel, active: true);

        Channels.JoinChannel(Channels.CurrentChannel, currentCallInfo.channelType, connectAudio: true, connectText: false, switchTransmission: true);

        Calling = true;
    }

    public async void LeaveCall()
    {
        if (Channels.CurrentChannel != null)
        {
            await Channels.LeaveChannel(Channels.CurrentChannel);
        }

        Channels.SetCurrentChannel(Channels.GlobalChannel);

        ServiceManager.UIManager.GlobalChatMenu.SetMuteAll(false);
        Channels.UnmuteGlobalChannel();

        EndCall();
    }

    #region Incoming Call

    public void SetIncomingCallInfo(string callerId, List<string> targetIds, string channelName, string channelId, ChannelType channelType, string textChannelId)
    {
        incomingCallInfo = new CallInfo()
        {
            callerId = callerId,
            members = targetIds,
            channelName = channelName,
            channelId = channelId,
            channelType = channelType,
            textChannelId = textChannelId
        };
    }

    public void CallDeclined()
    {
        ChatChannel textChannel = ServiceManager.VivoxManager.Channels.GetChannel(currentCallInfo.textChannelId);

        if (isCaller)
        {
            if (textChannel.IsPrivateChannel)
            {
                ServiceManager.MessageManager.CmdRemoveVoiceCall(currentCallInfo.channelId);
                EndCall();
            }
            else if (textChannel.IsGroupChannel)
            {
                numResponses++;
                numResponsesDeclined++;
                if (numResponsesDeclined == currentCallInfo.members.Count - 1)
                {
                    ServiceManager.MessageManager.CmdRemoveVoiceCall(currentCallInfo.channelId);
                    EndCall();
                }
            }
        }
    }

    public void DeclineCall()
    {
        SendMessageToServer(VoiceCallMessage.Call_Decline);
        EndIncomingCall();
    }

    public void CallAccepted()
    {
        numResponses++;
        EnterCall();
    }

    public void AcceptCall()
    {
        SendMessageToServer(VoiceCallMessage.Call_Accept);

        currentCallInfo = incomingCallInfo;
        incomingCallInfo = new CallInfo();

        EnterCall();
    }

    public void EndIncomingCall()
    {
        ServiceManager.UIManager.TextChatMenu.ShowCallNotification(false);

        ServiceManager.EventDispatcher.RemoveListener<OnVoiceCallChangeEvent>(OnVoiceCallChange);

        incomingCallInfo = new CallInfo();
    }

    #endregion

    public void EndCall()
    {
        ServiceManager.UIManager.TextChatMenu.ShowCallNotification(false);
        ServiceManager.UIManager.TextChatMenu.SetCallButtonState(currentCallInfo.textChannelId, CallButton.CallState.Default);
        ServiceManager.UIManager.GameplayCanvas.CallCentre.SetActiveCall(false);

        ServiceManager.EventDispatcher.RemoveListener<OnVoiceCallChangeEvent>(OnVoiceCallChange);

        numResponses = 0;
        numResponsesDeclined = 0;

        currentCallInfo = new CallInfo();
        isCaller = false;
        Calling = false;
    }

    public void SendMessageToServer(VoiceCallMessage message)
    {
        switch (message)
        {
            case VoiceCallMessage.Call_Cancel:
                ServiceManager.MessageManager.CmdSendCallCanceled(currentCallInfo.callerId, currentCallInfo.members);
                break;

            case VoiceCallMessage.Call_Accept:
                ServiceManager.MessageManager.CmdSendCallAccepted(incomingCallInfo.callerId);
                break;

            case VoiceCallMessage.Call_Decline:
                ServiceManager.MessageManager.CmdSendCallDeclined(incomingCallInfo.callerId);
                break;
        }
    }


    private void OnVoiceCallChange(OnVoiceCallChangeEvent e)
    {
        switch (e.Operation)
        {
            case Mirror.SyncIDictionary<string, VoiceCall>.Operation.OP_REMOVE:

                if (e.CallId == incomingCallInfo.channelId)
                {
                    EndIncomingCall();
                }
                break;

            case Mirror.SyncIDictionary<string, VoiceCall>.Operation.OP_SET:

                ChatChannel currentChannel = ServiceManager.VivoxManager.Channels.CurrentChannel;

                if (e.CallId != currentChannel.ID || !currentChannel.IsPrivateChannel) return;
                if (e.VoiceCall.lastIdModified == ServiceManager.LocalPlayer.UserGUID) return;

                if (!e.VoiceCall.add && e.VoiceCall.Participants.Count == 1)
                {
                    LeaveCall();
                }

                break;
        }
    }
}