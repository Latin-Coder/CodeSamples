using Mirror;
using System.Collections.Generic;

public class CallManager : NetworkBehaviour
{
    public readonly SyncDictionary<string, VoiceCall> VoiceCalls = new SyncDictionary<string, VoiceCall>();

    public readonly SyncDictionary<string, string> PlayersVoiceCalls = new SyncDictionary<string, string>();

    private void Awake()
    {
        VoiceCalls.Callback += VoiceCalls_Callback;
    }

    private struct CallbackInfo
    {
        public string channelId;
        public SyncIDictionary<string, VoiceCall>.Operation operation;
        public string participantId;
        public bool add;

        public bool Equals(CallbackInfo info)
        {
            return channelId == info.channelId && operation == info.operation && participantId == info.participantId && add == info.add;
        }
    }

    private CallbackInfo lastCallbackInfo;

    private void VoiceCalls_Callback(SyncIDictionary<string, VoiceCall>.Operation op, string key, VoiceCall item)
    {
        if (isClient)
        {
            CallbackInfo callbackInfo = new CallbackInfo() { channelId = key, operation = op, participantId = item.lastIdModified, add = item.add };

            if (callbackInfo.Equals(lastCallbackInfo)) return;

            lastCallbackInfo = callbackInfo;

            ServiceManager.EventDispatcher.TriggerEvent(new OnVoiceCallChangeEvent(key, item, op));

            //ServiceManager.UIManager.TextChatMenu.Conversation.UpdateCallPanel(key, (item.ID != null) ? item.Participants : new List<string>());
        }
        else
        {
            SendCallNotification(op, key, item);
        }
    }

    private void SendCallNotification(SyncIDictionary<string, VoiceCall>.Operation op, string key, VoiceCall item)
    {
        string message = "";

        switch (op)
        {
            case SyncIDictionary<string, VoiceCall>.Operation.OP_ADD:
                message += "Call started.";
                break;

            case SyncIDictionary<string, VoiceCall>.Operation.OP_REMOVE:
                message += "Call ended.";
                break;

            case SyncIDictionary<string, VoiceCall>.Operation.OP_SET:
                ServiceManager.Players.ConnectedPlayers.TryGetValue(item.lastIdModified, out PlayerInfo info);

                if (item.add) message += info.Name + " has joined the call.";
                else message += info.Name + " has left the call.";

                break;
        }

        foreach (string targetId in item.Members)
        {
            OnlinePlayer targetPlayer = ServiceManager.Players.GetPlayer(targetId);
            targetPlayer?.GetComponent<MessageManager>().TargetSendNotification(key, message);
        }
    }

    public void AddVoiceCall(string callId, List<string> members)
    {
        VoiceCall voiceCall = new VoiceCall(callId, members);
        VoiceCalls.Add(callId, voiceCall);
    }

    public void RemoveVoiceCall(string callId)
    {
        VoiceCalls.Remove(callId);
    }

    public void AddParticipant(string callId, string participantId)
    {
        VoiceCalls.TryGetValue(callId, out VoiceCall voiceCall);
        if (voiceCall.ID != null)
        {
            voiceCall.AddParticipant(participantId);
            VoiceCalls[callId] = voiceCall;
            PlayersVoiceCalls[participantId] = callId;
        }
    }

    public void RemoveParticipant(string callId, string participantId)
    {
        VoiceCalls.TryGetValue(callId, out VoiceCall voiceCall);
        if (voiceCall.ID != null)
        {
            voiceCall.RemoveParticipant(participantId);
            VoiceCalls[callId] = voiceCall;
            PlayersVoiceCalls[participantId] = ChannelsManager.GLOBAL_CHANNEL_VOICE_ID;

            if (voiceCall.Participants.Count == 0)
            {
                VoiceCalls.Remove(callId);
            }
        }
    }

    public VoiceCall GetVoiceCall(string callId)
    {
        VoiceCalls.TryGetValue(callId, out VoiceCall voiceCall);
        return voiceCall;
    }

    public void AddPlayer(string playerId)
    {
        PlayersVoiceCalls.TryAdd(playerId, ChannelsManager.GLOBAL_CHANNEL_VOICE_ID);
    }

    public void RemovePlayer(string playerId)
    {
        if (PlayersVoiceCalls.ContainsKey(playerId)) PlayersVoiceCalls.Remove(playerId);
    }
}
