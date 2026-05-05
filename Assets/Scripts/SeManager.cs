using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class SeManager : MonoBehaviour
{
    private const string PrefabResourcePath = "SeManagerPrefab";
    private const string WaterChangeClipPath = "SE/WaterChange";
    private const string RockLiftClipPath = "SE/RockLift";
    private const string RockDropClipPath = "SE/RockDrop";
    private const string ButtonClickClipPath = "SE/ButtonClick";
    private const string ClearClipPath = "SE/Clear";

    private static SeManager instance;

    [Header("SE Clips")]
    [SerializeField] private AudioClip waterChangeClip;
    [SerializeField] private AudioClip rockLiftClip;
    [SerializeField] private AudioClip rockDropClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip clearClip;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        SeManager prefab = Resources.Load<SeManager>(PrefabResourcePath);
        SeManager manager = prefab != null
            ? Instantiate(prefab)
            : new GameObject(nameof(SeManager)).AddComponent<SeManager>();

        manager.name = nameof(SeManager);
        DontDestroyOnLoad(manager.gameObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        LoadMissingClipsFromResources();
    }

    public static void PlayWaterChange()
    {
        Play(instance != null ? instance.waterChangeClip : null);
    }

    public static void PlayRockLift()
    {
        Play(instance != null ? instance.rockLiftClip : null);
    }

    public static void PlayRockDrop()
    {
        Play(instance != null ? instance.rockDropClip : null);
    }

    public static void PlayButtonClick()
    {
        Play(instance != null ? instance.buttonClickClip : null);
    }

    public static void PlayClear()
    {
        Play(instance != null ? instance.clearClip : null);
    }

    private static void Play(AudioClip clip)
    {
        if (instance == null || clip == null)
        {
            return;
        }

        instance.audioSource.PlayOneShot(clip, instance.volume);
    }

    private void LoadMissingClipsFromResources()
    {
        waterChangeClip = waterChangeClip != null ? waterChangeClip : Resources.Load<AudioClip>(WaterChangeClipPath);
        rockLiftClip = rockLiftClip != null ? rockLiftClip : Resources.Load<AudioClip>(RockLiftClipPath);
        rockDropClip = rockDropClip != null ? rockDropClip : Resources.Load<AudioClip>(RockDropClipPath);
        buttonClickClip = buttonClickClip != null ? buttonClickClip : Resources.Load<AudioClip>(ButtonClickClipPath);
        clearClip = clearClip != null ? clearClip : Resources.Load<AudioClip>(ClearClipPath);
    }
}
