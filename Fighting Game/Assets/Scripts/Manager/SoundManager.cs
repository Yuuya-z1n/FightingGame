using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public enum SeCategory
{
    Battle,
    System,
    Menu
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class SceneBgm
    {
        public string sceneName;
        public string bgmName;
    }

    [Header("AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup seGroup;

    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SE_VOLUME_PARAM = "SEVolume";

    [Header("BGM登録")]
    [SerializeField] private List<Sound> bgmList = new List<Sound>();

    [Header("SE登録")]
    [SerializeField] private List<Sound> battleSeList = new List<Sound>();
    [SerializeField] private List<Sound> systemSeList = new List<Sound>();
    [SerializeField] private List<Sound> menuSeList = new List<Sound>();

    [Header("シーン別BGM")]
    [SerializeField] private List<SceneBgm> sceneBgmList = new List<SceneBgm>();

    private readonly Dictionary<string, Sound> bgmDict = new Dictionary<string, Sound>();

    private readonly Dictionary<SeCategory, Dictionary<string, Sound>> seDicts
        = new Dictionary<SeCategory, Dictionary<string, Sound>>();

    private readonly Dictionary<SeCategory, AudioSource> seSources
        = new Dictionary<SeCategory, AudioSource>();

    private AudioSource bgmSource;
    private Coroutine bgmRoutine;
    private string currentBgmName;

    public float BgmVolume { get; private set; } = 1f;
    public float SeVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM用AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.outputAudioMixerGroup = bgmGroup;

        foreach (var sound in bgmList)
        {
            if (!string.IsNullOrEmpty(sound.name))
                bgmDict[sound.name] = sound;
        }

        // SE用AudioSource
        RegisterSeCategory(SeCategory.Battle, battleSeList);
        RegisterSeCategory(SeCategory.System, systemSeList);
        RegisterSeCategory(SeCategory.Menu, menuSeList);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        BgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1f);
        SeVolume = PlayerPrefs.GetFloat("SeVolume", 1f);

        SetBgmVolume(BgmVolume);
        SetSeVolume(SeVolume);

        PlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBgm(scene.name);
    }

    // =========================
    // BGM
    // =========================

    private void PlaySceneBgm(string sceneName)
    {
        foreach (var entry in sceneBgmList)
        {
            if (entry.sceneName == sceneName)
            {
                PlayBGM(entry.bgmName);
                return;
            }
        }

        // BGMが設定されていないシーンなら停止
        StopBGM();
    }

    public void PlayBGM(string name, float fadeTime = 0.5f)
    {
        if (!bgmDict.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"BGM '{name}' が登録されていません");
            return;
        }

        if (sound.clip == null)
        {
            Debug.LogWarning($"BGM '{name}' のAudioClipが設定されていません");
            return;
        }

        if (currentBgmName == name && bgmSource.isPlaying)
            return;

        if (bgmRoutine != null)
            StopCoroutine(bgmRoutine);

        bgmRoutine = StartCoroutine(SwitchBgm(sound, fadeTime));
    }

    public void StopBGM(float fadeTime = 0.5f)
    {
        if (bgmRoutine != null)
            StopCoroutine(bgmRoutine);

        if (!bgmSource.isPlaying)
        {
            currentBgmName = null;
            bgmRoutine = null;
            return;
        }

        bgmRoutine = StartCoroutine(FadeOutAndStop(fadeTime));
    }

    private IEnumerator SwitchBgm(Sound next, float fadeTime)
    {
        if (bgmSource.isPlaying)
            yield return FadeTo(0f, fadeTime);

        bgmSource.Stop();
        bgmSource.clip = next.clip;
        bgmSource.volume = next.volume;
        bgmSource.Play();

        currentBgmName = next.name;

        yield return FadeTo(next.volume, fadeTime);

        bgmRoutine = null;
    }

    private IEnumerator FadeOutAndStop(float fadeTime)
    {
        yield return FadeTo(0f, fadeTime);

        bgmSource.Stop();
        currentBgmName = null;
        bgmRoutine = null;
    }

    private IEnumerator FadeTo(float target, float time)
    {
        float start = bgmSource.volume;

        if (time <= 0f)
        {
            bgmSource.volume = target;
            yield break;
        }

        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        bgmSource.volume = target;
    }

    // =========================
    // SE
    // =========================

    private void RegisterSeCategory(SeCategory category, List<Sound> list)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = seGroup;

        seSources[category] = source;

        var dict = new Dictionary<string, Sound>();

        foreach (var sound in list)
        {
            if (!string.IsNullOrEmpty(sound.name))
                dict[sound.name] = sound;
        }

        seDicts[category] = dict;
    }

    public void PlaySE(SeCategory category, string name)
    {
        if (!seDicts.TryGetValue(category, out var dict))
        {
            Debug.LogWarning($"SEカテゴリ '{category}' が登録されていません");
            return;
        }

        if (!dict.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"SE '{name}' は {category} に登録されていません");
            return;
        }

        if (sound.clip == null)
        {
            Debug.LogWarning($"SE '{name}' のAudioClipが設定されていません");
            return;
        }

        seSources[category].PlayOneShot(sound.clip, sound.volume);
    }

    public void PlayBattleSE(string name)
    {
        PlaySE(SeCategory.Battle, name);
    }

    public void PlaySystemSE(string name)
    {
        PlaySE(SeCategory.System, name);
    }

    public void PlayMenuSE(string name)
    {
        PlaySE(SeCategory.Menu, name);
    }

    public void StopSE(SeCategory category)
    {
        if (seSources.TryGetValue(category, out var source))
            source.Stop();
    }

    // =========================
    // 音量調整
    // =========================

    private float LinearToDb(float volume)
    {
        if (volume <= 0.0001f)
            return -80f;

        return Mathf.Log10(volume) * 20f;
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);

        if (audioMixer != null)
            audioMixer.SetFloat(BGM_VOLUME_PARAM, LinearToDb(BgmVolume));

        PlayerPrefs.SetFloat("BgmVolume", BgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSeVolume(float value)
    {
        SeVolume = Mathf.Clamp01(value);

        if (audioMixer != null)
            audioMixer.SetFloat(SE_VOLUME_PARAM, LinearToDb(SeVolume));

        PlayerPrefs.SetFloat("SeVolume", SeVolume);
        PlayerPrefs.Save();
    }

    public float GetSeVolume()
    {
        return SeVolume;
    }

    // 以前のカテゴリ別呼び出しとの互換用
    public void SetSeVolume(SeCategory category, float value)
    {
        SetSeVolume(value);
    }

    public float GetSeVolume(SeCategory category)
    {
        return SeVolume;
    }
}
