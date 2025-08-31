using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    public Button PlayGame;
    public Button QuitGame;
    public Slider AudioSlider;
    [SerializeField] RectTransform Title;     // <-- assign your title text RectTransform

    [Header("Title FX")]
    [SerializeField] float idleAngle = 8f;    // deg left/right
    [SerializeField] float idleHalfPeriod = 1.1f;
    [SerializeField] float shakeAngle = 25f;  // deg peak
    [SerializeField] float shakeDuration = 0.35f;
    [SerializeField] Vector2 shakeIntervalRange = new Vector2(2.5f, 4.5f);

    [Header("Audio")]
    public AudioMixer audioMixer;
    const string MixerParam = "MusicVolume";
    const string PrefKey = "music_vol_01";
    const float MinDb = -80f;

    Tween _idleTw;
    Coroutine _shakeLoop;

    void Start()
    {
        Service.Get<IAudioService>().SetAudioVisalizerSong(AudioService.MusicChoice.MenuMusic);
        Service.Get<IAudioService>().PlayAudioVisualizer();

        PlayGame.onClick.AddListener(() => Service.Get<IGameService>().StartGame());
        QuitGame.onClick.AddListener(Application.Quit);

        float v01 = PlayerPrefs.GetFloat(PrefKey, 0.8f);
        AudioSlider.SetValueWithoutNotify(v01);
        ApplyVolume(v01);
        AudioSlider.onValueChanged.AddListener(ApplyVolume);

        StartTitleFX();
    }

    void OnDisable()
    {
        AudioSlider.onValueChanged.RemoveListener(ApplyVolume);
        _idleTw?.Kill();
        if (_shakeLoop != null) StopCoroutine(_shakeLoop);
        if (Title) { Title.localRotation = Quaternion.identity; Title.localScale = Vector3.one; }
    }

    void ApplyVolume(float value01)
    {
        float dB = value01 > 0.0001f ? Mathf.Log10(Mathf.Clamp01(value01)) * 20f : MinDb;
        audioMixer.SetFloat(MixerParam, dB);
        PlayerPrefs.SetFloat(PrefKey, value01);
    }

    // ---- Title FX ----
    void StartTitleFX()
    {
        if (!Title) return;

        Title.localRotation = Quaternion.identity;

        // idle back-and-forth rotate forever
        _idleTw = Title
            .DORotate(new Vector3(0, 0, idleAngle), idleHalfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // periodic shake on top
        _shakeLoop = StartCoroutine(CoShakeLoop());
    }

    IEnumerator CoShakeLoop()
    {
        while (true)
        {
            float wait = Random.Range(shakeIntervalRange.x, shakeIntervalRange.y);
            yield return new WaitForSecondsRealtime(wait);

            if (!Title) yield break;

            Title.DOShakeRotation(
                    shakeDuration,
                    new Vector3(0, 0, shakeAngle),
                    vibrato: 18,
                    randomness: 90,
                    fadeOut: true)
                .SetUpdate(true);
        }
    }
}
