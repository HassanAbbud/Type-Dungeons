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
    private AudioSource sfxSource;
    private AudioSource announcerSource;

    private readonly Queue<AudioClip> announcementQueue = new();
    private bool isAnnouncing = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        bgmSource = GetComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        announcerSource = gameObject.AddComponent<AudioSource>();
    }

    public static void PlaySound(SoundType type, float volume = 1.0f, int announcerClipIdx = 0)
    {
        if (instance == null) return;

        switch (type)
        {
            case SoundType.BGM1 or SoundType.BGM2:
                instance.bgmSource.clip = instance.audioClips[(int)type];
                instance.bgmSource.loop = true;
                instance.bgmSource.volume = volume;
                instance.bgmSource.Play();
                break;

            case SoundType.BTN_CLICK:
                AudioClip clickClip = instance.audioClips[(int)type];
                instance.sfxSource.PlayOneShot(clickClip, volume);
                break;

            case SoundType.ANNOUNCER:
                AudioClip announcerClip = instance.announcerClips[announcerClipIdx];
                EnqueueAnnouncement(announcerClip, volume);
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

    private static void EnqueueAnnouncement(AudioClip clip, float volume = 1.0f)
    {
        instance.announcementQueue.Enqueue(clip);

        if (!instance.isAnnouncing)
        {
            instance.StartCoroutine(ProcessAnnouncementQueue(volume));
        }
    }

    private static IEnumerator ProcessAnnouncementQueue(float volume = 1.0f)
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
