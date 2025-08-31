using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIEndScreen : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text m_textScore;
    public TMP_Text m_gameOver;
    public Button m_buttonRestart;

    [Header("FX Settings")]
    [Min(0f)] public float scoreShakeDur = 0.5f;
    [Min(0f)] public float scoreShakeStrength = 20f;   // px
    [Min(1)] public int scoreShakeVibrato = 12;
    [Min(0f)] public float scorePunchDelta = 0.18f;    // additive scale
    [Min(0f)] public float scorePunchDur = 0.25f;

    [Min(0f)] public float overRotateAngle = 8f;       // degrees
    [Min(0f)] public float overRotateDur = 0.65f;      // sec for half-cycle
    [Min(0f)] public float overPulseScale = 1.08f;
    [Min(0f)] public float overPulseDur = 0.28f;

    public string gameSceneName = "02_Game";

    // caches
    Vector3 _scoreBaseScale, _overBaseScale;
    Quaternion _overBaseRot;

    // tweens
    Sequence _scoreSeq;
    Tween _overRotTween, _overPulseTween;

    void Start()
    {
        m_buttonRestart.onClick.AddListener(RestartGame);
    }

    void OnEnable()
    {
        int score = Service.Get<IUIService>().UIScore.Score;
        m_textScore.text = "You popped from power\nScore: " + score;

        // cache bases
        var srt = m_textScore.rectTransform;
        var ort = m_gameOver.rectTransform;
        _scoreBaseScale = srt.localScale;
        _overBaseScale = ort.localScale;
        _overBaseRot = ort.localRotation;

        PlayEnterFX();
    }

    void OnDisable()
    {
        _scoreSeq?.Kill();
        _overRotTween?.Kill();
        _overPulseTween?.Kill();

        // restore transforms
        if (m_textScore) m_textScore.rectTransform.localScale = _scoreBaseScale;
        if (m_gameOver)
        {
            var rt = m_gameOver.rectTransform;
            rt.localScale = _overBaseScale;
            rt.localRotation = _overBaseRot;
        }
    }

    void PlayEnterFX()
    {
        var srt = m_textScore.rectTransform;
        var ort = m_gameOver.rectTransform;

        // score: punch + shake
        _scoreSeq?.Kill();
        _scoreSeq = DOTween.Sequence().SetUpdate(true)
            .Join(srt.DOPunchScale(Vector3.one * scorePunchDelta, scorePunchDur, 8, 0.5f).SetUpdate(true))
            .Join(srt.DOShakeAnchorPos(scoreShakeDur, scoreShakeStrength, scoreShakeVibrato, 90, false, true).SetUpdate(true));

        // game over: rotate back and forth forever + subtle scale yoyo
        _overRotTween?.Kill();
        _overPulseTween?.Kill();

        ort.localRotation = _overBaseRot;
        ort.localScale = _overBaseScale;

        _overRotTween = ort
            .DORotate(new Vector3(0f, 0f, overRotateAngle), overRotateDur, RotateMode.Fast)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        _overPulseTween = ort
            .DOScale(_overBaseScale * overPulseScale, overPulseDur)
            .SetEase(Ease.OutQuad)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    public void RestartGame()
    {
        StartCoroutine(RestartScene());
    }

    IEnumerator RestartScene()
    {
        // unload and reload additively
        yield return SceneManager.UnloadSceneAsync(gameSceneName);
        yield return SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);
        gameObject.SetActive(false);
    }
}
