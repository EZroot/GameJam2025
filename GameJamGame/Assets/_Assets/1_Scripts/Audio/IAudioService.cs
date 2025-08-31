using UnityEngine;
using static AudioService;

public interface IAudioService : IService
{
    public void SetAudioVisalizerSong(MusicChoice musicChoice);
    public void PlayAudioVisualizer();
    public void PauseAudioVisualizer();
    void PlaySound(string soundKey, float volume = 1f, bool loop = false);
    void PlaySoundRandom(string groupLabel, float volume = 1f, bool loop = false);
}
