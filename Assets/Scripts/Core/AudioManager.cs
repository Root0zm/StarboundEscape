using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                _instance = FindAnyObjectByType<AudioManager>();
#else
                _instance = FindObjectOfType<AudioManager>();
#endif
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource1;
    [SerializeField] private AudioSource bgmSource2;
    [SerializeField] private AudioSource sfx2DSource;

    private AudioSource activeBgmSource;
    private Coroutine crossfadeCoroutine;

    // Dictionary để theo dõi thời gian hồi chiêu của các âm thanh (SFX Throttling)
    private Dictionary<string, float> sfxCooldowns = new Dictionary<string, float>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSources();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSources()
    {
        // Tự tạo AudioSource nếu chưa được gán ngoài Inspector
        if (bgmSource1 == null)
        {
            bgmSource1 = gameObject.AddComponent<AudioSource>();
            bgmSource1.loop = true;
            bgmSource1.playOnAwake = false;
        }
        if (bgmSource2 == null)
        {
            bgmSource2 = gameObject.AddComponent<AudioSource>();
            bgmSource2.loop = true;
            bgmSource2.playOnAwake = false;
        }
        if (sfx2DSource == null)
        {
            sfx2DSource = gameObject.AddComponent<AudioSource>();
            sfx2DSource.loop = false;
            sfx2DSource.playOnAwake = false;
        }

        activeBgmSource = bgmSource1;
    }

    // =================================================================
    // CÁC HÀM QUẢN LÝ NHẠC NỀN (BGM)
    // =================================================================

    /// <summary>
    /// Phát nhạc nền (BGM) mới, tự động chạy hiệu ứng chuyển tiếp (crossfade)
    /// </summary>
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.5f)
    {
        if (clip == null) return;
        if (activeBgmSource != null && activeBgmSource.clip == clip && activeBgmSource.isPlaying) return;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(clip, fadeDuration));
    }

    /// <summary>
    /// Tắt nhạc nền hiện tại
    /// </summary>
    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(FadeOutBGMRoutine(fadeDuration));
    }

    private IEnumerator CrossfadeBGMRoutine(AudioClip newClip, float duration)
    {
        AudioSource oldSource = activeBgmSource;
        AudioSource newSource = (activeBgmSource == bgmSource1) ? bgmSource2 : bgmSource1;

        newSource.clip = newClip;
        newSource.volume = 0f;
        newSource.loop = true;
        newSource.Play();

        activeBgmSource = newSource;

        float timer = 0f;
        float startOldVolume = oldSource != null ? oldSource.volume : 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // Sử dụng unscaledDeltaTime để crossfade hoạt động cả khi pause game
            float t = timer / duration;

            if (oldSource != null && oldSource.isPlaying)
            {
                oldSource.volume = Mathf.Lerp(startOldVolume, 0f, t);
            }
            newSource.volume = Mathf.Lerp(0f, bgmVolume, t);

            yield return null;
        }

        newSource.volume = bgmVolume;
        if (oldSource != null)
        {
            oldSource.Stop();
            oldSource.volume = 0f;
        }

        crossfadeCoroutine = null;
    }

    private IEnumerator FadeOutBGMRoutine(float duration)
    {
        if (activeBgmSource == null || !activeBgmSource.isPlaying) yield break;

        float timer = 0f;
        float startVolume = activeBgmSource.volume;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            activeBgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        activeBgmSource.Stop();
        activeBgmSource.volume = 0f;
        crossfadeCoroutine = null;
    }

    // =================================================================
    // CÁC HÀM HIỆU ỨNG ÂM THANH (SFX)
    // =================================================================

    /// <summary>
    /// Phát hiệu ứng âm thanh 2D (không có chiều không gian, thích hợp cho UI, nhặt đồ)
    /// </summary>
    public void PlaySFX2D(AudioClip clip, float volumeScale = 1.0f, bool randomPitch = false)
    {
        if (clip == null) return;

        // Nếu không cần random pitch và có sfx2DSource, phát bình thường trên AudioSource dùng chung để tối ưu hiệu năng
        if (!randomPitch && sfx2DSource != null)
        {
            sfx2DSource.PlayOneShot(clip, volumeScale * sfxVolume);
            return;
        }

        // Nếu cần random pitch hoặc không có sfx2DSource, tạo đối tượng tạm thời để phát riêng biệt, tránh méo tiếng của các âm thanh khác
        GameObject tempGo = new GameObject("TempSFX2D");
        AudioSource source = tempGo.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f; // Âm thanh 2D
        source.volume = volumeScale * sfxVolume;

        if (randomPitch)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
        }

        source.Play();
        Destroy(tempGo, clip.length + 0.1f);
    }

    /// <summary>
    /// Tắt toàn bộ hiệu ứng âm thanh 2D đang phát (đặc biệt hữu ích để ngắt nhạc thắng/thua khi chơi lại)
    /// </summary>
    public void StopSFX2D()
    {
        if (sfx2DSource != null)
        {
            sfx2DSource.Stop();
        }
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh 3D tại vị trí cụ thể (âm lượng giảm dần theo khoảng cách)
    /// </summary>
    public void PlaySFX3D(AudioClip clip, Vector3 position, float volumeScale = 1.0f, bool randomPitch = false)
    {
        if (clip == null) return;

        GameObject tempGo = new GameObject("TempSFX");
        // Force z to 0 to keep the sound source aligned on the 2D plane
        tempGo.transform.position = new Vector3(position.x, position.y, 0f);

        AudioSource source = tempGo.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1.0f; // 3D Audio
        // Set minDistance to 12f so it is fully audible under standard camera Z-distance (-10f)
        source.minDistance = 12f;
        source.maxDistance = 40f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.volume = volumeScale * sfxVolume;

        if (randomPitch)
        {
            source.pitch = Random.Range(0.9f, 1.1f);
        }

        source.Play();
        Destroy(tempGo, clip.length + 0.1f);
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh 3D kèm giới hạn tần suất phát (chống chồng âm gây ồn)
    /// </summary>
    public void PlaySFX3DThrottled(string key, AudioClip clip, Vector3 position, float cooldown, float volumeScale = 1.0f, bool randomPitch = true)
    {
        if (clip == null) return;

        if (sfxCooldowns.TryGetValue(key, out float nextPlayTime))
        {
            if (Time.time < nextPlayTime)
            {
                return; // Đang trong thời gian cooldown, từ chối phát âm thanh
            }
        }

        // Cập nhật mốc thời gian phát tiếp theo
        sfxCooldowns[key] = Time.time + cooldown;

        // Tiến hành phát âm thanh
        PlaySFX3D(clip, position, volumeScale, randomPitch);
    }
}
