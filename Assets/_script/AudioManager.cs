using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    // --- 单例模式 ---
    public static AudioManager Instance { get; private set; }

    [Header("音频源 (Audio Sources)")]
    public AudioSource musicSource; // 用于播放背景音乐 (BGM)
    public AudioSource sfxSource;   // 用于播放音效 (SFX)

    [Header("音频剪辑 (Audio Clips)")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;
    public AudioClip buttonClickSFX;
    public AudioClip pickupSFX;
    public AudioClip hitWoodSFX;
    public AudioClip hitReefSFX;

    // --- 音量控制 ---
    private bool isMusicMuted = false;
    private bool isSfxMuted = false;

    void Awake()
    {
        // --- 实现单例模式，并使其跨场景存在 ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 让AudioManager在切换场景时不被销毁
        }
        else
        {
            // 如果场景中已存在一个AudioManager，则销毁这个新的
            Destroy(gameObject);
        }
    }

    // --- 公共的播放方法 ---

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            // 如果要播放的音乐就是当前正在播放的，则不执行任何操作
            return;
        }
        musicSource.clip = clip;
        musicSource.Play();
    }

    // 播放一次性的音效
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // 方便调用的具体音效播放方法
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayPickupSound()
    {
        PlaySFX(pickupSFX);
    }

    public void PlayHitWoodSound()
    {
        PlaySFX(hitWoodSFX);
    }

    public void PlayHitReefSound()
    {
        PlaySFX(hitReefSFX);
    }

    // --- 公共的控制方法 ---

    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;
        musicSource.mute = isMusicMuted;
    }

    public void ToggleSFX()
    {
        isSfxMuted = !isSfxMuted;
        sfxSource.mute = isSfxMuted;
    }

    // 返回当前静音状态，方便UI更新按钮图标
    public bool IsMusicMuted() { return isMusicMuted; }
    public bool IsSfxMuted() { return isSfxMuted; }
}