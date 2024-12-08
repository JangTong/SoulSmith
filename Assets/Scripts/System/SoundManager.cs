using System;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]); // Swap
        }
    }
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public List<AudioClip> bgmClips;
    public List<AudioClip> sfxClips;

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private int currentBgmIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        foreach (var clip in sfxClips)
        {
            if (!sfxDictionary.ContainsKey(clip.name))
            {
                sfxDictionary.Add(clip.name, clip);
            }
        }
    }

    private void Start()
    {
        bgmClips.Shuffle(); // 게임 실행 시 리스트 셔플
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmClips.Count == 0) return;

        bgmSource.clip = bgmClips[currentBgmIndex];
        bgmSource.Play();
        StartCoroutine(WaitForTrackToEnd());
    }

    private System.Collections.IEnumerator WaitForTrackToEnd()
    {
        while (bgmSource.isPlaying)
        {
            yield return null; // 현재 트랙 재생 중이면 대기
        }

        currentBgmIndex++;

        // 마지막 트랙까지 재생한 경우
        if (currentBgmIndex >= bgmClips.Count)
        {
            currentBgmIndex = 0;
            bgmClips.Shuffle(); // 한 사이클 종료 시 리스트 셔플
        }

        PlayBGM(); // 다음 트랙 재생
    }

    public void PlaySFX(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out var clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SFX {sfxName}를 찾을 수 없습니다.");
        }
    }

    public void PlaySoundAtPosition(string sfxName, Vector3 position)
    {
        if (sfxDictionary.TryGetValue(sfxName, out var clip))
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
        else
        {
            Debug.LogWarning($"SFX {sfxName}를 찾을 수 없습니다.");
        }
    }
}
