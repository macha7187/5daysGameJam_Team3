using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public sealed class BgmManager : MonoBehaviour
{
    private const string PrefabResourcePath = "BgmManagerPrefab";
    private const string StageScenePrefix = "Stage";

    private static BgmManager instance;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip titleClip;
    [SerializeField] private AudioClip[] stageClips;

    private AudioSource audioSource;
    private AudioClip currentClip;
    private int lastStageClipIndex = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        BgmManager prefab = Resources.Load<BgmManager>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"BGM manager prefab was not found. Create Resources/{PrefabResourcePath}.prefab.");
            return;
        }

        BgmManager manager = Instantiate(prefab);
        manager.name = nameof(BgmManager);
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
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        if (IsTitleScene(sceneName))
        {
            if (titleClip == null)
            {
                Debug.LogWarning("Title BGM clip is not assigned on BgmManagerPrefab.");
            }

            Play(titleClip);
            return;
        }

        if (IsStageScene(sceneName))
        {
            if (stageClips == null || stageClips.Length == 0)
            {
                Debug.LogWarning("Stage BGM clips are not assigned on BgmManagerPrefab.");
            }

            Play(GetRandomStageClip());
            return;
        }

        Stop();
    }

    private static bool IsTitleScene(string sceneName)
    {
        return sceneName == "Title"
            || sceneName == "TitleScene"
            || (sceneName.Length >= 2 && sceneName[0] == 'T' && int.TryParse(sceneName.Substring(1), out _));
    }

    private static bool IsStageScene(string sceneName)
    {
        return sceneName.Length > StageScenePrefix.Length
            && sceneName.StartsWith(StageScenePrefix)
            && int.TryParse(sceneName.Substring(StageScenePrefix.Length), out _);
    }

    private AudioClip GetRandomStageClip()
    {
        if (stageClips == null || stageClips.Length == 0)
        {
            return null;
        }

        if (stageClips.Length == 1)
        {
            lastStageClipIndex = 0;
            return stageClips[0];
        }

        int index;
        do
        {
            index = Random.Range(0, stageClips.Length);
        }
        while (index == lastStageClipIndex);

        lastStageClipIndex = index;
        return stageClips[index];
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Stop();
            return;
        }

        if (currentClip == clip && audioSource.isPlaying)
        {
            return;
        }

        currentClip = clip;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void Stop()
    {
        currentClip = null;
        audioSource.Stop();
        audioSource.clip = null;
    }
}
