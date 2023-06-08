using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using Pavia.Metaverse.Plots;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance = new ServiceManager();
    private PcaLoader pcaLoaderService;
    private LoginService loginService;
    private AudioService audioService;
    private AudioManager audioManager;
    private GraphicsManager graphicsManager;
    private SkyAndWeatherManager skyAndWeatherManager;
    private PaviaNetworkManager networkManager;
    private ServerManager serverManager;
    private VivoxChatService vivoxManager = new VivoxChatService();
    private CinemachineVirtualCamera mainCamera;
    private UIManager uiManager;
    private ThirdPersonController localPlayerController;
    private LocalPlayerInfo localPlayer;
    private DataManager dataManager;
    private MessageManager messageManager;
    private CallManager callManager;
    private PlayersManager playersManagers;
    private EventDispatcher eventDispatcher;
    private BlockchainService blockchainService;
    private RemoteConfigSession remoteConfigSession;

    //----------------SERVICES----------------

    public static PcaLoader PcaLoaderService => Instance.pcaLoaderService ??= FindObjectOfType<PcaLoader>();    
    public static LoginService LoginService =>Instance.loginService ??= FindObjectOfType<LoginService>();
    public static VivoxChatService VivoxManager => Instance.vivoxManager; //??= FindObjectOfType<VivoxChatService>(true);
    public static PaviaNetworkManager NetworkManager => Instance.networkManager ??= FindObjectOfType<PaviaNetworkManager>(true);
    public static ServerManager ServerManager => Instance.serverManager ??= FindObjectOfType<ServerManager>(true);
    public static GraphicsManager GraphicsManager => Instance.graphicsManager ??= FindObjectOfType<GraphicsManager>(true);
    public static AudioManager AudioManager => Instance.audioManager ??= FindObjectOfType<AudioManager>(true);
    public static SkyAndWeatherManager SkyAndWeatherManager => Instance.skyAndWeatherManager ??= FindObjectOfType<SkyAndWeatherManager>(true);
    public static AudioService AudioService => Instance.audioService ??= FindObjectOfType<AudioService>(true);
    public static CinemachineVirtualCamera MainCamera => Instance.mainCamera ??= FindObjectOfType<CinemachineVirtualCamera>(true);
    public static UIManager UIManager => Instance.uiManager ??= FindObjectOfType<UIManager>(true);
    public static ThirdPersonController LocalPlayerController => Instance.localPlayerController ??= FindObjectOfType<ThirdPersonController>(true);
    public static LocalPlayerInfo LocalPlayer => Instance.localPlayer ??= LocalPlayerController.GetComponent<LocalPlayerInfo>();
    public static DataManager DataManager => Instance.dataManager ??= FindObjectOfType<DataManager>();

    public static MessageManager MessageManager => Instance.messageManager ??= LocalPlayer.GetComponent<MessageManager>();
    public static CallManager CallManager => Instance.callManager ??= FindObjectOfType<CallManager>();
    public static PlayersManager Players => Instance.playersManagers ??= FindObjectOfType<PlayersManager>();
    public static EventDispatcher EventDispatcher => Instance.eventDispatcher ??= new();
    public static RemoteConfigSession RemoteConfigSession => Instance.remoteConfigSession ??= FindObjectOfType<RemoteConfigSession>();
    public static BlockchainService BlockchainService => Instance.blockchainService ??= FindObjectOfType<BlockchainService>();

    //----------------SERVICES----------------

    private void Awake()
    {
            if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        ResetServiceManager();
    }
    public void ResetServiceManager()
    {
        Destroy(gameObject);
    }
}
