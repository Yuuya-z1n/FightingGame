using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// SEの種類
public enum SeCategory
{
    Battle,   // 格闘中(攻撃、ヒット、ガードなど)
    System,   // システム(開始、終了、勝敗ジングルなど)
    Menu      // メニュー(カーソル移動、決定、キャンセルなど)
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

    [Header("BGM登録")]
    [SerializeField] private List<Sound> bgmList = new List<Sound>();

    [Header("SE登録(カテゴリ別)")]
    [SerializeField] private List<Sound> battleSeList = new List<Sound>();
    [SerializeField] private List<Sound> systemSeList = new List<Sound>();
    [SerializeField] private List<Sound> menuSeList = new List<Sound>();

    [Header("シーン別BGM(任意)")]
    [SerializeField] private List<SceneBgm> sceneBgmList = new List<SceneBgm>();

    private readonly Dictionary<string, Sound> bgmDict = new Dictionary<string, Sound>();
    private readonly Dictionary<SeCategory, Dictionary<string, Sound>> seDicts
        = new Dictionary<SeCategory, Dictionary<string, Sound>>();
    private readonly Dictionary<SeCategory, AudioSource> seSources
        = new Dictionary<SeCategory, AudioSource>();
    private readonly Dictionary<SeCategory, float> seVolumes
        = new Dictionary<SeCategory, float>();

    private AudioSource bgmSource;
    private Coroutine bgmRoutine;
    private string currentBgmName;
    private float currentBgmBaseVolume = 1f;

    public float BgmVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        foreach (var s in bgmList)
            if (!string.IsNullOrEmpty(s.name)) bgmDict[s.name] = s;
        BgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1f);

        // SE(カテゴリごとにAudioSource・辞書・音量を用意)
        RegisterSeCategory(SeCategory.Battle, battleSeList);
        RegisterSeCategory(SeCategory.System, systemSeList);
        RegisterSeCategory(SeCategory.Menu, menuSeList);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void RegisterSeCategory(SeCategory category, List<Sound> list)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        seSources[category] = source;

        var dict = new Dictionary<string, Sound>();
        foreach (var s in list)
            if (!string.IsNullOrEmpty(s.name)) dict[s.name] = s;
        seDicts[category] = dict;

        seVolumes[category] = PlayerPrefs.GetFloat("SeVolume_" + category, 1f);
    }

    private void Start()
    {
        PlaySceneBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlaySceneBgm(scene.name);

    private void PlaySceneBgm(string sceneName)
    {
        foreach (var e in sceneBgmList)
        {
            if (e.sceneName == sceneName)
            {
                PlayBGM(e.bgmName);
                return;
            }
        }
    }

    // ---------- BGM ----------

    public void PlayBGM(string name, float fadeTime = 0.5f)
    {
        if (!bgmDict.TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"BGM '{name}' が登録されていません");
            return;
        }
        if (currentBgmName == name && bgmSource.isPlaying) return;

        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(SwitchBgm(sound, fadeTime));
    }

    public void StopBGM(float fadeTime = 0.5f)
    {
        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(FadeOutAndStop(fadeTime));
    }

    private IEnumerator SwitchBgm(Sound next, float fadeTime)
    {
        if (bgmSource.isPlaying)
            yield return FadeTo(0f, fadeTime);

        bgmSource.clip = next.clip;
        currentBgmName = next.name;
        currentBgmBaseVolume = next.volume;
        bgmSource.volume = 0f;
        bgmSource.Play();

        yield return FadeTo(currentBgmBaseVolume * BgmVolume, fadeTime);
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
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        bgmSource.volume = target;
    }

    // ---------- SE ----------

    public void PlaySE(SeCategory category, string name)
    {
        if (!seDicts[category].TryGetValue(name, out var sound))
        {
            Debug.LogWarning($"SE '{name}' は {category} に登録されていません");
            return;
        }
        seSources[category].PlayOneShot(sound.clip, sound.volume * seVolumes[category]);
    }

    // 呼び出しを短く書くためのショートカット
    public void PlayBattleSE(string name) => PlaySE(SeCategory.Battle, name);
    public void PlaySystemSE(string name) => PlaySE(SeCategory.System, name);
    public void PlayMenuSE(string name) => PlaySE(SeCategory.Menu, name);

    // そのカテゴリのSEだけ止める(例: メニューを開いたら格闘SEを止める)
    public void StopSE(SeCategory category) => seSources[category].Stop();

    // ---------- 音量 ----------

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        bgmSource.volume = currentBgmBaseVolume * BgmVolume;
        PlayerPrefs.SetFloat("BgmVolume", BgmVolume);
    }

    public void SetSeVolume(SeCategory category, float value)
    {
        seVolumes[category] = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SeVolume_" + category, seVolumes[category]);
    }

    public float GetSeVolume(SeCategory category) => seVolumes[category];
}