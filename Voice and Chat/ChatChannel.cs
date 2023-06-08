using System;
using System.Collections.Generic;
using UnityEngine;

public class ChatChannel
{
    public static readonly int MAX_MESSAGES = 100;

    public enum CommunicationType
    {
        Global,
        Group,
        Private
    }

    public enum MediaType
    {
        Text,
        Voice
    }

    public string ID { get; private set; }
    public string ChannelName { get; private set; }
    public string Moderator { get; set; }
    public List<string> MembersList { get; private set; }
    public string TargetID { get; set; }
    public CommunicationType communicationType { get; private set; }
    public MediaType mediaType { get; private set; }

    public List<ClientChatMessage> messages { get; }

    public Dictionary<string, ChatParticipant> Participants = new Dictionary<string, ChatParticipant>();


    public bool ChatJoinInitialize { get; set; }
    public bool Mute { get; set; }

    public bool IsPrivateChannel => communicationType == CommunicationType.Private;
    public bool IsGroupChannel => communicationType == CommunicationType.Group;
    public bool IsGlobalChannel => communicationType == CommunicationType.Global;
    public bool IsVoiceChannel => mediaType == MediaType.Voice;

    private IChatService chatService;

    public event Action<ClientChatMessage> MessageReceived;
    public event Action<ClientChatMessage> UsersChanged;
    public event Action<bool> ChatMuted;

    public ChatChannel(string id, CommunicationType commType, MediaType mediaType, string channelName, IChatService chatService, string moderator, List<string> members = null, string targetId = null)
    {
        messages = new List<ClientChatMessage>(MAX_MESSAGES);
        communicationType = commType;
        this.mediaType = mediaType;
        ID = id;
        ChannelName = channelName;
        Moderator = moderator;
        MembersList = members;
        TargetID = targetId;
        this.chatService = chatService;
    }

    // Util methods so we don't need to access the chatService object if we have a reference to the ChatChannel

    public void SendMessage(ClientChatMessage chatMessage)
    {
        if (messages.Count >= MAX_MESSAGES)
        {
            messages.RemoveAt(0);
        }

        chatService.SendMessage(this, chatMessage);
    }

    public string GetLastMessage()
    {
        if (messages.Count > 0)
        {
            return messages[messages.Count - 1].message;
        }

        return "";
    }

    public void OnMessageReceived(ClientChatMessage clientMessage)
    {
        messages.Add(clientMessage);
        MessageReceived?.Invoke(clientMessage);

        if (IsGlobalChannel)
        {
            if (ServiceManager.UIManager.GlobalChatMenu.gameObject.activeSelf)
                ServiceManager.UIManager.GlobalChatMenu.AddChatMessage(clientMessage);
        }
        else
        {
            ServiceManager.UIManager.TextChatMenu.AddChatBubble(clientMessage);
        }
    }

    public void OnUserChanged(ClientChatMessage chatNotification)
    {
        messages.Add(chatNotification);
        chatNotification.message = chatNotification.message.Replace(VivoxChatToken.chat_notification.ToString(), "");

        //ServiceManager.UIManager.TextChatMenu.AddChatBubble(chatNotification);
        UsersChanged?.Invoke(chatNotification);
    }

    public void OnChatMuted(bool mute)
    {
        Mute = mute;
        ChatMuted?.Invoke(mute);
    }

    public string GetChannelType()
    {
        return communicationType.ToString().ToLower();
    }

    public string IDWithoutPrefix()
    {
        int index = ID.IndexOf('_') + 1;
        string temp = ID.Substring(index, ID.Length - index); // without comm type
        index = temp.IndexOf('_') + 1;
        temp = temp.Substring(index, temp.Length - index); // without media type

        return temp;
    }

    public string GetTextChannelID()
    {
        if (ID.Contains("text")) return ID;
        else
        {
            string temp = ID;
            return temp.Replace("voice", "text");
        }
    }

    public string GetVoiceChannelID()
    {
        if (ID.Contains("voice")) return ID;
        else
        {
            string temp = ID;
            return temp.Replace("text", "voice");
        }
    }

    public ChatParticipant GetParticipant(string participantId)
    {
        return Participants.GetValueOrDefault(participantId);
    }

    public void AddParticipant(ChatParticipant participant)
    {
        if (!Participants.ContainsKey(participant.id))
        {
            Participants.Add(participant.id, participant);
        }
    }

    public void RemoveParticipant(string participantId)
    {
        if (Participants.ContainsKey(participantId)) Participants.Remove(participantId);
    }

    public static CommunicationType GetCommTypeFromChannelId(string channelId)
    {
        if (channelId.Contains("private")) return CommunicationType.Private;
        else if (channelId.Contains("group")) return CommunicationType.Group;
        else return CommunicationType.Global;
    }

    public static List<string> GetMembersFromChannelId(string channelId)
    {
        string[] splits = channelId.Split('_');

        List<string> targetIds = new List<string>();

        for (int i = 2; i < splits.Length; i++)
        {
            string targetId = splits[i];
            targetIds.Add(targetId);
        }

        return targetIds;
    }
}