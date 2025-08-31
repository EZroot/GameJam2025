using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioService : MonoBehaviour, IAudioService
{
    public AudioSource m_audioSource;
    public AudioDefinition[] m_audioDefinitions;
    public AudioVisualizer_FlexibleBands m_audioVisualizer;

    public AudioClip gameSong;
    public AudioClip mainMenuSong;
    
    private Dictionary<string, List<AudioDefinition>> m_audioSortedByGroup = new();

    private void Awake()
    {
        // populate our dictionary with clips that have the same grouplabel
        m_audioSortedByGroup.Clear();
        foreach (var audioDef in m_audioDefinitions)
        {
            if (!m_audioSortedByGroup.ContainsKey(audioDef.GroupLabel))
            {
                m_audioSortedByGroup[audioDef.GroupLabel] = new List<AudioDefinition>();
            }
            m_audioSortedByGroup[audioDef.GroupLabel].Add(audioDef);
        }
    }

    public enum MusicChoice { MenuMusic, GameMusic }
    public void SetAudioVisalizerSong(MusicChoice musicChoice)
    {
        switch(musicChoice)
        {
            case MusicChoice.MenuMusic:
                if(m_audioVisualizer.audioSource.clip == mainMenuSong) return;
                m_audioVisualizer.audioSource.clip = mainMenuSong;
                break;
                case MusicChoice.GameMusic:
                if (m_audioVisualizer.audioSource.clip == gameSong) return;
                m_audioVisualizer.audioSource.clip = gameSong;
                break;
        }
    }
    public void PlayAudioVisualizer()
    {
        if(m_audioVisualizer.audioSource.isPlaying) return;
        m_audioVisualizer.audioSource.Play();
    }

    public void PauseAudioVisualizer()
    {
        m_audioVisualizer.audioSource.Stop();
    }

    public void PlaySound(string soundKey, float volume = 1, bool loop = false)
    {
        if (m_audioDefinitions == null || m_audioDefinitions.First(x => x.Key == soundKey) == null) return;
        var clip = m_audioDefinitions.First(x => x.Key == soundKey).Clip;
        if(clip == null)
        {
            Debug.LogError($"Audio clip for key '{soundKey}' not found!");
            return;
        }

        if (loop)
        {
            m_audioSource.clip = clip;
            m_audioSource.loop = true;
            m_audioSource.volume = volume;
            m_audioSource.Play();
            return;
        }

        m_audioSource.PlayOneShot(clip, volume);
    }

    public void PlaySoundRandom(string groupLabel, float volume = 1f, bool loop = false)
    {
        if (!m_audioSortedByGroup.ContainsKey(groupLabel))
        {
            Debug.LogError($"Audio group '{groupLabel}' not found!");
            return;
        }
        var audioDefs = m_audioSortedByGroup[groupLabel];
        if (audioDefs.Count == 0)
        {
            Debug.LogError($"No audio definitions found for group '{groupLabel}'!");
            return;
        }
        var randomDef = audioDefs[Random.Range(0, audioDefs.Count)];
        PlaySound(randomDef.Key, volume, loop);
    }
}

[System.Serializable]
public class AudioDefinition
{
    public string Key;
    public string GroupLabel;
    public AudioClip Clip;
    public bool Loop;
    public float Volume = 1f;
    public AudioDefinition(string key, AudioClip clip, bool loop = false, float volume = 1f, string groupLabel = "default")
    {
        Key = key;
        Clip = clip;
        Loop = loop;
        Volume = volume;
        GroupLabel = groupLabel;
    }
}