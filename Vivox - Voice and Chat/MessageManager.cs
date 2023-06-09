using UnityEngine;
using Mirror;
using VivoxUnity;
using System.Collections.Generic;

public class MessageManager : NetworkBehaviour
{
    #region Voice Calls

    #region Voice Chat

    [Command]
    public void CmdSendCallStarted(string callerId, List<string> targetIds, string callerName, string textChannelId, string textChannelName)
    {
        foreach (string targetId in targetIds)
        {
            if (targetId == callerId) continue;

            OnlinePlayer target = ServiceManager.Players.GetPlayer(targetId);
            if (target != null) target.GetComponent<MessageManager>().TargetReceiveCallStarted(callerId, callerName, textChannelId, textChannelName);
        }
    }

    [Command]
    public void CmdSendCallCanceled(string callerId, List<string> targetIds)
    {
        foreach (string targetId in targetIds)
        {
            if (targetId == callerId) continue;

            OnlinePlayer target = ServiceManager.Players.GetPlayer(targetId);
            if (target != null) target.GetComponent<MessageManager>().TargetReceiveCallCanceled();
        }
    }

    [Command]
    public void CmdSendCallAccepted(string targetId)
    {
        OnlinePlayer target = ServiceManager.Players.GetPlayer(targetId);
        if (target != null) target.GetComponent<MessageManager>().TargetReceiveCallAccepted();
    }

    [Command]
    public void CmdSendCallDeclined(string targetId)
    {
        OnlinePlayer target = ServiceManager.Players.GetPlayer(targetId);
        if (target != null) target.GetComponent<MessageManager>().TargetReceiveCallDeclined();
    }

    [TargetRpc]
    private void TargetReceiveCallStarted(string callerId, string callerName, string textChannelId, string textChannelName)
    {
        Debug.Log("CallStarted message received");

        ChatChannel textChannel = ServiceManager.VivoxManager.Channels.GetChannel(textChannelId);

        if (textChannel == null)
        {
            ServiceManager.UIManager.TextChatMenu.AddChatButton(textChannelId, textChannelName,
                ChatChannel.GetCommTypeFromChannelId(textChannelId), ChatChannel.GetMembersFromChannelId(textChannelId), callerId);
            textChannel = ServiceManager.VivoxManager.Channels.GetChannel(textChannelId);
        }

        string voiceChannelId = textChannel.GetChannelType() + "_voice_" + textChannel.IDWithoutPrefix();

        ServiceManager.VivoxManager.VoiceChat.SetIncomingCallInfo(callerId, textChannel.MembersList, textChannel.ChannelName, voiceChannelId, ChannelType.NonPositional, textChannelId);
        ServiceManager.UIManager.TextChatMenu.ShowCallNotification(true, callerName, textChannelId);
    }

    [TargetRpc]
    private void TargetReceiveCallCanceled()
    {
        Debug.Log("CallCanceled message received");

        ServiceManager.VivoxManager.VoiceChat.EndIncomingCall();
    }

    [TargetRpc]
    private void TargetReceiveCallAccepted()
    {
        Debug.Log("CallAccepted message received");

        ServiceManager.VivoxManager.VoiceChat.CallAccepted();
    }

    [TargetRpc]
    private void TargetReceiveCallDeclined()
    {
        Debug.Log("CallDeclined message received");

        ServiceManager.VivoxManager.VoiceChat.CallDeclined();
    }

    #endregion

    #region Call Manager

    [Command]
    public void CmdAddVoiceCall(string callId, List<string> members)
    {
        ServiceManager.CallManager.AddVoiceCall(callId, members);
    }

    [Command]
    public void CmdRemoveVoiceCall(string callId)
    {
        ServiceManager.CallManager.RemoveVoiceCall(callId);
    }

    [Command]
    public void CmdAddParticipant(string callId, string participantId, string participantName)
    {
        ServiceManager.CallManager.AddParticipant(callId, participantId);
    }

    [Command]
    public void CmdRemoveParticipant(string callId, string participantId, string participantName)
    {
        ServiceManager.CallManager.RemoveParticipant(callId, participantId);
    }

    [TargetRpc]
    public void TargetSendNotification(string voiceChannelId, string message)
    {
        string textChannelId = voiceChannelId.Replace("voice", "text");
        ChatChannel textChannel = ServiceManager.VivoxManager.Channels.GetChannel(textChannelId);
        ClientChatMessage clientMessage = new ClientChatMessage("", "", message, textChannelId, fromSelf: false, VivoxChatToken.chat_notification);

        // TODO: Shouldn't need this null check, but since groups aren't persistant we cannot assure every player will have the group text chat
        if (textChannel != null)
        {
            textChannel.OnMessageReceived(clientMessage);
        }
    }

    #endregion

    #endregion

    #region Group Chats

    [Command]
    public void CmdSendGroupTextChatCreated(string sourceId, List<string> targetIds, string channelId, string channelName)
    {
        foreach (string id in targetIds)
        {
            if (id == sourceId) continue;

            OnlinePlayer callTarget = ServiceManager.Players.GetPlayer(id);
            if (callTarget != null) callTarget.GetComponent<MessageManager>().TargetReceiveGroupTextChatCreated(channelId, channelName);
        }
    }

    [TargetRpc]
    private void TargetReceiveGroupTextChatCreated(string channelId, string channelName)
    {
        Debug.Log("GroupTextChatCreated message received");

        ServiceManager.UIManager.TextChatMenu.AddChatButton(channelId, channelName, ChatChannel.CommunicationType.Group, ChatChannel.GetMembersFromChannelId(channelId));
    }

    #endregion
}
