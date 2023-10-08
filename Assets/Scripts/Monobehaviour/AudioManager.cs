using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioClip[] audioClips;
    private int currentTrackIndex = 0;
    private int nextTrackIndex = 1;
    public float fadeOutDuration = 1.0f;
    public float fadeInDuration = 1.0f;
    private bool isFadingOut = false;
    private bool isFadingIn = false;
    private float currentVolume = 0.0f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!isFadingOut && !audioSource.isPlaying)
        {
            isFadingOut = true;
            StartCoroutine(FadeOutCurrentTrack());
        }
    }

    public void SetVolume(float value)
    {
        audioSource.volume = value / 100f;
    }

    IEnumerator FadeOutCurrentTrack()
    {
        float startTime = Time.time;
        float endTime = startTime + fadeOutDuration;

        while (Time.time < endTime)
        {
            float progress = (Time.time - startTime) / fadeOutDuration;
            currentVolume = Mathf.Lerp(1.0f, 0.0f, progress);
            audioSource.volume = currentVolume;
            yield return null;
        }

        audioSource.Stop();

        // Swap track indices and audio sources
        currentTrackIndex = nextTrackIndex;
        nextTrackIndex = (nextTrackIndex + 1) % audioClips.Length;

        audioSource.clip = audioClips[currentTrackIndex];
        audioSource.clip = audioClips[nextTrackIndex];

        audioSource.volume = 0.0f;
        audioSource.Play();

        isFadingOut = false;

        // Start fading in the new track
        StartCoroutine(FadeInNextTrack());
    }

    IEnumerator FadeInNextTrack()
    {
        float startTime = Time.time;
        float endTime = startTime + fadeInDuration;

        while (Time.time < endTime)
        {
            float progress = (Time.time - startTime) / fadeInDuration;
            currentVolume = Mathf.Lerp(0.0f, 1.0f, progress);
            audioSource.volume = currentVolume;
            yield return null;
        }

        audioSource.volume = 1.0f;
        isFadingIn = false;
    }
}
