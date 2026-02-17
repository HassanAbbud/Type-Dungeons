using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    BGM1,
    BGM2,
    BTN_CLICK,
    ANNOUNCER
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] audioClips;
    [SerializeField]
    private AudioClip[] announcerClips;
    private static SoundManager instance;

    private AudioSource bgmSource;
    private static float bgmVolume = 0.5f;
    private AudioSource sfxSource;
    private static float sfxVolume = 1.0f;
    private AudioSource announcerSource;
    private static float announcerVolume = 0.7f;

    private readonly Queue<AudioClip> announcementQueue = new();
    private bool isAnnouncing = false;

    public static float Scale { get; set; } = 0.5f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = GetComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        announcerSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        //
    }

    public static void PlaySound(SoundType type, int announcerClipIdx = 0)
    {
        if (instance == null) return;


        switch (type)
        {
            case SoundType.BGM1 or SoundType.BGM2:
                instance.bgmSource.clip = instance.audioClips[(int)type];
                instance.bgmSource.loop = true;
                instance.bgmSource.volume = bgmVolume * Scale;
                instance.bgmSource.Play();
                break;

            case SoundType.BTN_CLICK:
                AudioClip clickClip = instance.audioClips[(int)type];
                instance.sfxSource.PlayOneShot(clickClip, sfxVolume * Scale);
                break;

            case SoundType.ANNOUNCER:
                AudioClip announcerClip = instance.announcerClips[announcerClipIdx];
                EnqueueAnnouncement(announcerClip, announcerVolume * Scale);
                break;
            default:
                break;
        }
    }

    public static void StopSound(SoundType type)
    {
        if (instance == null) return;

        switch (type)
        {
            case SoundType.BGM1 or SoundType.BGM2:
                if (instance.bgmSource.isPlaying && instance.bgmSource.loop)
                {
                    instance.bgmSource.Stop();
                    instance.bgmSource.loop = false;
                    instance.bgmSource.clip = null;
                }
                break;

            case SoundType.ANNOUNCER:
                instance.announcerSource.Stop();
                instance.announcementQueue.Clear();
                break;

            default:
                instance.sfxSource.Stop();
                break;
        }
    }

    public static void UpdateBGMVolume(float value)
    {
        if (instance == null) return;

        if (instance.bgmSource != null && instance.bgmSource.isPlaying)
        {
            instance.bgmSource.volume = bgmVolume * value;
        }
    }

    private static void EnqueueAnnouncement(AudioClip clip, float volume)
    {
        instance.announcementQueue.Enqueue(clip);

        if (!instance.isAnnouncing)
        {
            instance.StartCoroutine(ProcessAnnouncementQueue(volume));
        }
    }

    private static IEnumerator ProcessAnnouncementQueue(float volume)
    {
        instance.isAnnouncing = true;

        while (instance.announcementQueue.Count > 0)
        {
            AudioClip clip = instance.announcementQueue.Dequeue();
            instance.announcerSource.PlayOneShot(clip, volume);

            yield return new WaitForSeconds(clip.length);
        }

        instance.isAnnouncing = false;
    }

}
