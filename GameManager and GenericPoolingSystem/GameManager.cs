using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager>
{

    [Header("Racers Config")]
    [SerializeField]
    private int initialNumberOfRacers = 100;

    [SerializeField]
    private List<Racer> racerPrefabList;
    private List<Racer> racerList = new List<Racer>();
    [Tooltip("Sphere radius of spawn volume")]
    public int radiusSpawnZone = 100;
    private Vector3 spawnPoint = Vector3.zero;
    [SerializeField]
    private Transform spawnedObjectsParent;

    [Space]
    [Header("Buttons")]
    [SerializeField]
    private Button resetButton;
    [SerializeField]
    private Button moveMainGameObject;

    [Space]
    [Header("MainGameObject")]
    [SerializeField]
    private MainObject mainObjectPrefab;
    private MainObject mainObject;
    private Collider mainCollider;
    [SerializeField]
    [Range(1f, 100f)]
    private float mainObjectSpeed;
    [SerializeField]
    private List<Transform> movementPoints;
    private Rigidbody mainObjectRigidBody;

    [Space]
    [Header("Collisions")]
    [SerializeField]
    private CollisionEventChannel collisionEventChannel;
    public CollisionEventChannel CollisionChannel { get => collisionEventChannel; private set => collisionEventChannel = value; }
    [SerializeField]
    private ParticleSystem explosionParticleSystemPrefab;
    private ParticleSystem explosionParticleSystem;
    private bool collisionDetected;

    private AudioSource audioSource;
    private Coroutine movementCoroutine;
    private PoolSystem<Racer> poolingSystem = new();

    private void Awake()
    {
        audioSource = Camera.main.GetComponent<AudioSource>();
        collisionEventChannel.onCollisionEnterEvent += RefreshCollisionState;
    }

    private void OnDestroy()
    {
        collisionEventChannel.onCollisionEnterEvent -= RefreshCollisionState;
    }
    private void Start()
    {

        InitMainObject();
        InitRacers();

        moveMainGameObject.onClick.AddListener(() => movementCoroutine = StartCoroutine(MoveMainObject(movementPoints)));
        resetButton.onClick.AddListener(Reset);
    }

    private void RefreshCollisionState(Collider collider)
    {
        if (collider)
        {

            collisionDetected = true;
            //To reduce physics callbacks
            mainCollider.enabled = false;
        }
    }

    private void Reset()
    {
        InitMainObject();
        InitRacers();
    }

    private void InitMainObject()
    {
        if (!mainObject)
        {
            mainObject = Instantiate(mainObjectPrefab);
            mainObjectRigidBody = mainObject.GetComponent<Rigidbody>();
            mainCollider = mainObject.GetComponent<Collider>();
            FindObjectOfType<CinemachineVirtualCamera>().LookAt = mainObject.transform;
        }

        if (!explosionParticleSystem)
        {
            explosionParticleSystem = Instantiate(explosionParticleSystemPrefab);
        }
        explosionParticleSystem.Stop();

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        mainCollider.enabled = true;
        mainObjectRigidBody.position = Vector3.zero;
        collisionDetected = false;
    }
    private void InitRacers()
    {
        poolingSystem.ClearItems(racerList);
        racerList.Clear();
        for (int i = 0; i < initialNumberOfRacers; i++)
        {
            spawnPoint = Random.insideUnitSphere * radiusSpawnZone;
            Racer racer = poolingSystem.PoolAddElement(racerPrefabList[Random.Range(0, racerPrefabList.Count)], spawnPoint, Quaternion.Euler(-90, 90, 180), spawnedObjectsParent);
            racerList.Add(racer);
        }
    }

    private IEnumerator MoveMainObject(List<Transform> destination)
    {
        if (movementCoroutine != null)
        {
            InitMainObject();
        }

        for (int i = 0; i < destination.Count; i++)
        {
            Vector3 initialPosition = mainObjectRigidBody.position;
            Vector3 lastPosition = initialPosition;
            Vector3 direction = destination[i].position - initialPosition;
            float maxDistance = direction.magnitude;
            float distanceTravelled = 0;

            while (distanceTravelled < maxDistance)
            {
                lastPosition += mainObjectSpeed * Time.deltaTime * direction.normalized;
                distanceTravelled = (lastPosition - initialPosition).magnitude;

                if (distanceTravelled < maxDistance)
                {
                    mainObjectRigidBody.MovePosition(lastPosition);
                }
                else
                {
                    //To avoid distanceTravelled exceed maximum distance (greater deviation for higher speeds)
                    mainObjectRigidBody.MovePosition(destination[i].position);
                    break;
                }
                yield return null;
            }
        }
        OnMovementFinished();
    }

    private void OnMovementFinished()
    {
        if (collisionDetected)
        {
            explosionParticleSystem.transform.position = mainObjectRigidBody.position;
            explosionParticleSystem.Play();
            audioSource.Play();
            Destroy(mainObject.gameObject);
        }
    }
}
