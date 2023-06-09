using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [SerializeField] private EmotesMenu emotesMenu;
    [SerializeField] private ConnectedPlayersMenu connectedPlayersMenu;
    [SerializeField] private ChatMenu chatMenu;
    [SerializeField] private GameplayCanvas gameplayCanvas;
    [SerializeField] private PauseMenu exitMenu;
    [SerializeField] private BackgroundMusicPlayerUIPanel backgroundMusicPlayer;
    [SerializeField] private TimeAndWeatherUI timeWeatherUi;

    [SerializeField]
    private UIMenu currentMenu;
    public UIMenu CurrentMenu { get { return currentMenu; } set { UpdateCurrentMenu(value); } }

    private PlayerInputActions inputActions; //Player input necessary to open the emote wheel

    //--------------PUBLIC ATTRIBUTES--------------
    public EmotesMenu EmotesMenu { get => emotesMenu; }
    public ConnectedPlayersMenu ConnectedPlayersMenu { get => connectedPlayersMenu; }
    public ChatMenu ChatMenu { get => chatMenu; }
    public GlobalChatMenu GlobalChatMenu { get => chatMenu.GlobalChat; }
    public TextChatMenu TextChatMenu { get => chatMenu.PrivateChat; }
    public GameplayCanvas GameplayCanvas { get => gameplayCanvas; }
    public BackgroundMusicPlayerUIPanel BackgroundMusicPlayerUI { get => backgroundMusicPlayer; }
    public TimeAndWeatherUI TimeWeatherUi { get => timeWeatherUi; }



    //--------------PRIVATE ATTRIBUTES--------------

    private bool playerConnected = false;

    private void Awake()
    {
        //New input system 
        inputActions = new PlayerInputActions();
        inputActions.Menus.ShowWheel.performed += ctx => ToggleEmotesMenu();
        inputActions.Menus.ShowConnectedPlayersMenu.performed += ctx => OpenMenu(connectedPlayersMenu, toggle: true); ;
        inputActions.Menus.OpenChatMenu.performed += ctx => OpenChatMenu();
        inputActions.Menus.CloseUIMenu.performed += ctx => CloseUIMenu();
        inputActions.Menus.OpenMusicMenu.performed += ctx => OpenMenu(backgroundMusicPlayer, toggle: true);
        inputActions.Menus.OpenTimeControls.performed += ctx => OpenMenu(TimeWeatherUi, toggle: true);
        inputActions.Enable();
    }

    private void Start()
    {
        connectedPlayersMenu.gameObject.SetActive(false);
        emotesMenu.gameObject.SetActive(false);
        backgroundMusicPlayer.gameObject.SetActive(false);
        Cursor.visible = false;
    }

    private void UpdateCurrentMenu(UIMenu _currentMenu)
    {
        if (currentMenu)
        {
            currentMenu.Exit();
        }

        currentMenu = _currentMenu;

        if (_currentMenu)
        {
            currentMenu.Enter();
            ShowCursor(true);
        }
    }

    private void OnEnable()
    {
        if (inputActions == null) inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    public void InitEmoteWheel()
    {
        emotesMenu.Init();
    }

    public void Init(string userId, string userName)
    {
        playerConnected = true;
        connectedPlayersMenu.Init();
        TextChatMenu.Init(userId, userName);
        GlobalChatMenu.Init();
        chatMenu.gameObject.SetActive(true);

        ServiceManager.EventDispatcher.AddListener<OnPlayerConnectedEvent>(OnPlayerConnected);
        ServiceManager.EventDispatcher.AddListener<OnPlayerDisconnectedEvent>(OnPlayerDisconnected);
        ServiceManager.EventDispatcher.AddListener<OnVoiceCallChangeEvent>(OnVoiceCallChange);
    }

    #region EventDispatcher Listeners
    //Callbacks for eventDispatcher events-----------------------------------------------
    private void OnPlayerConnected(OnPlayerConnectedEvent onPlayerConnectedEvent)
    {
        connectedPlayersMenu.AddPlayerButton(onPlayerConnectedEvent.id, onPlayerConnectedEvent.name, connected: true);
        GlobalChatMenu.PlayersList.AddPlayerEntry(onPlayerConnectedEvent.id, onPlayerConnectedEvent.name);

        if (!onPlayerConnectedEvent.isBot)
        {
            string localId = ServiceManager.LocalPlayer.UserGUID;
            string privateChannelId = ChannelsManager.GetDirectChannelId(localId, onPlayerConnectedEvent.id);
            TextChatMenu.AddChatButton(
                privateChannelId,
                onPlayerConnectedEvent.name,
                ChatChannel.CommunicationType.Private,
                new List<string>() { localId, onPlayerConnectedEvent.id },
                onPlayerConnectedEvent.id);
        }
    }

    private void OnPlayerDisconnected(OnPlayerDisconnectedEvent onPlayerDisconnectedEvent)
    {
        connectedPlayersMenu.RemovePlayerButton(onPlayerDisconnectedEvent.id);
        GlobalChatMenu.PlayersList.RemovePlayerEntry(onPlayerDisconnectedEvent.id);

        if (!onPlayerDisconnectedEvent.isBot)
        {
            string privateChannelId = ChannelsManager.GetDirectChannelId(ServiceManager.LocalPlayer.UserGUID, onPlayerDisconnectedEvent.id);
            TextChatMenu.RemoveChatButton(privateChannelId);
        }
    }

    private void OnVoiceCallChange(OnVoiceCallChangeEvent onVoiceCallChangeEvent)
    {
        if (onVoiceCallChangeEvent.Operation != Mirror.SyncIDictionary<string, VoiceCall>.Operation.OP_SET) return;

        UpdateSpeakerImages(onVoiceCallChangeEvent.CallId, onVoiceCallChangeEvent.VoiceCall);
        ConnectedPlayersMenu.OnVoiceCallChange(onVoiceCallChangeEvent);
    }

    #endregion

    public void Dispose()
    {
        playerConnected = false;

        CurrentMenu = null;
        Cursor.lockState = CursorLockMode.None;
        ServiceManager.EventDispatcher.RemoveListener<OnPlayerConnectedEvent>(OnPlayerConnected);
        ServiceManager.EventDispatcher.RemoveListener<OnPlayerDisconnectedEvent>(OnPlayerDisconnected);
        ServiceManager.EventDispatcher.RemoveListener<OnVoiceCallChangeEvent>(OnVoiceCallChange);
    }

    public void ToggleEmotesMenu()
    {
        OpenMenu(emotesMenu, toggle: true);
    }

    public void OpenChatMenu(bool toggle = true)
    {
        OpenMenu(chatMenu, toggle, disableInput: false);
    }

    private void OpenMenu(UIMenu menu, bool toggle = false, bool disableInput = true)
    {
        if (!playerConnected) return;

        if (EventSystem.current != null
            && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()) return;

        if (currentMenu != menu)
        {
            OpenUIMenu(menu, disableInput);
        }
        else if (toggle)
        {
            CloseUIMenu();
        }
    }

    private void OpenUIMenu(UIMenu menu, bool disableInput = true)
    {
        if (disableInput)
        {
            if (!ServiceManager.LocalPlayerController) return;
            ServiceManager.LocalPlayerController.InputEnabled = false;
        }

        CurrentMenu = menu;
    }

    public void CloseUIMenu()
    {
        if (!ServiceManager.LocalPlayerController) return;
        ServiceManager.LocalPlayerController.InputEnabled = true;

        if (!currentMenu)
        {
            OpenMenu(exitMenu, toggle: true);
            return;
        }
        CurrentMenu = null;
    }

    private void ShowCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

    public void InitPlayersUI()
    {
        foreach (OnlinePlayer onlinePlayer in ServiceManager.Players.ConnectedPlayersValues)
        {
            if (onlinePlayer.PlayerID == ServiceManager.LocalPlayer.UserGUID) continue;

            PlayerUI playerUI = onlinePlayer.GetComponentInChildren<PlayerUI>(true);
            playerUI.Init(onlinePlayer.PlayerName, onlinePlayer.PlayerID);
        }
    }


    public void UpdateSpeakerImages(string key, VoiceCall item)
    {
        // If add, set new channel id. If remove, set global channel id
        string channelId = item.add ? key : ChannelsManager.GLOBAL_CHANNEL_VOICE_ID;

        bool localPlayer = item.lastIdModified == ServiceManager.LocalPlayer.UserGUID;

        SpeakerImage[] speakerImages = FindObjectsOfType<SpeakerImage>(includeInactive: true);

        for (int i = 0; i < speakerImages.Length; i++)
        {
            SpeakerImage speaker = speakerImages[i];

            if (localPlayer)
            {
                // If it's a speaker from a voice call participant
                if (item.Participants.Contains(speaker.PlayerID))
                {
                    if (item.add)
                        speaker.Subscribe(speaker.PlayerID, key);
                    else
                        speaker.Unsubscribe(speaker.PlayerID, key);
                }
            }
            else
            {
                if (speaker.PlayerID == item.lastIdModified)
                {
                    speaker.SetChannelId(channelId);
                }
            }

            speaker.UpdateImageState();
        }
    }

    public void ShowActiveCall(string oldChannelId, ChatChannel newChannel, bool active)
    { 
        TextChatMenu.SetCallButtonState(oldChannelId, CallButton.CallState.Default);
        TextChatMenu.SetCallButtonState(newChannel.GetTextChannelID(), CallButton.CallState.InCall);
        GameplayCanvas.CallCentre.SetActiveCall(active, newChannel);
    }
}
