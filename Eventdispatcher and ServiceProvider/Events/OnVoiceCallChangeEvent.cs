using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnVoiceCallChangeEvent
{
    public string CallId;
    public VoiceCall VoiceCall;
    public SyncIDictionary<string, VoiceCall>.Operation Operation;

    public OnVoiceCallChangeEvent(string callId, VoiceCall voiceCall, SyncIDictionary<string, VoiceCall>.Operation operation)
    {
        CallId = callId;
        VoiceCall = voiceCall;
        Operation = operation;
    }
}
