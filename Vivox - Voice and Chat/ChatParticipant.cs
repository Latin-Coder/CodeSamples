using System.ComponentModel;
using VivoxUnity;
using UnityEngine;

public class ChatParticipant
{
    public string displayName;
    public string id;

    IParticipant participant;

    bool _mutedWantedValue;
    bool _mutedPropertyChanged;

    public delegate void OnPropertyChangeHandler(bool propertyValue);
    public event OnPropertyChangeHandler OnSpeechDetectedChangeEvent;
    public event OnPropertyChangeHandler OnLocalMuteChangeEvent;

    public ChatParticipant(string displayName, string id, IParticipant participant)
    {
        this.displayName = displayName;
        this.id = id;
        this.participant = participant;

        if (participant != null) participant.PropertyChanged += OnPropertyChanged;
    }

    public void AdjustVolume(int volumeAdjustment)
    {
        participant.LocalVolumeAdjustment = volumeAdjustment;
    }

    public void Mute(bool value)
    {
        SetMute(value);
    }

    private void SetMute(bool value)
    {
        if (participant.LocalMute == value) return;

        participant.LocalMute = value;
        _mutedWantedValue = value;
        _mutedPropertyChanged = true;
    }

    public bool IsSpeaking()
    {
        return participant.SpeechDetected;
    }

    public bool IsMuted()
    {
        return participant.LocalMute;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "LocalMute")
        {
            if (_mutedPropertyChanged)
            {
                if (participant.LocalMute != _mutedWantedValue) return;

                _mutedPropertyChanged = false;

                ServiceManager.UIManager.ConnectedPlayersMenu.UpdateUI();

                OnLocalMuteChangeEvent?.Invoke(participant.LocalMute);
            }
        }
        else if (e.PropertyName == "SpeechDetected")
        {
            OnSpeechDetectedChangeEvent?.Invoke(participant.SpeechDetected);
        }
    }
}


