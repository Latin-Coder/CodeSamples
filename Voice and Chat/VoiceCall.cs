using System.Collections.Generic;
using UnityEngine;

public struct VoiceCall
{
    public string ID;
    public List<string> Participants;
    public List<string> Members;

    public string lastIdModified;
    public bool add;

    public VoiceCall(string ID, List<string> members)
    {
        this.ID = ID;
        Members = members;

        Participants = new List<string>();
        lastIdModified = null;
        add = false;
    }

    public void AddParticipant(string participantId)
    {
        if (!Participants.Contains(participantId))
        {
            Participants.Add(participantId);
            lastIdModified = participantId;
            add = true;
        }
    }

    public void RemoveParticipant(string participantId)
    {
        if (Participants.Contains(participantId))
        {
            Participants.Remove(participantId);
            lastIdModified = participantId;
            add = false;
        }
    }

    public bool HasParticipant(string participantId)
    {
        return Participants.Contains(participantId);
    }
}