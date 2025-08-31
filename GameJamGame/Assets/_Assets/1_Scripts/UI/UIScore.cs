using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIScore : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text comboText;
    [SerializeField] TMP_Text textWave;
    [SerializeField] TMP_Text overflowText;
    [SerializeField] Slider overflowSlider;

    [Header("Formatting")]
    [SerializeField, Min(1)] int scorePadLength = 12;
    [SerializeField] string comboPrefix = "x";

    [Header("Score Juice")]
    [SerializeField] float scorePunchBase = 1.12f;
    [SerializeField] float scorePunchPerCombo = 0.04f;
    [SerializeField] float scorePunchDuration = 0.25f;
    [SerializeField] float scoreShakeDuration = 0.25f;
    [SerializeField] float scoreShakeStrength = 10f;
    [SerializeField] int scoreShakeVibrato = 10;
    [SerializeField] Color scoreFlashColor = new(1f, 0.95f, 0.4f, 1f);

    [Header("Combo Juice")]
    [SerializeField] float comboPopMin = 1.05f;
    [SerializeField] float comboPopPerStep = 0.06f;
    [SerializeField] float comboPopMax = 1.8f;
    [SerializeField] float comboPopDuration = 0.18f;
    [SerializeField] Gradient comboColorByValue;

    [Header("Combo System")]
    [SerializeField, Min(0.05f)] float comboWindow = 1.0f;
    [SerializeField, Min(1)] int comboMax = 99;
    [SerializeField] bool dropToOneOnTimeout = true;

    [Header("Beat Juice")]
    [SerializeField] RectTransform scorePulseRoot;
    [SerializeField] RectTransform comboPulseRoot;
    [SerializeField] RectTransform overflowTextPulseRoot;
    [SerializeField] RectTransform overflowSliderPulseRoot;
    [SerializeField] float beatPunchMin = 0.06f;
    [SerializeField] float beatPunchMax = 0.14f;
    [SerializeField] float beatDuration = 0.12f;
    [SerializeField] Color beatFlashColor = new(1f, 0.85f, 0.25f, 1f);
    [SerializeField] float beatFlashIn = 0.04f;
    [SerializeField] float beatFlashOut = 0.10f;

    [Header("Overflow Meter")]
    [SerializeField, Min(1)] int overflowThreshold = 1000;   // points per overflow
    [SerializeField] float overflowFillTime = 0.2f;          // tween time for bar fill
    [SerializeField] Ease overflowEase = Ease.OutCubic;
    [SerializeField] float overflowHugeScale = 2.4f;         // overflow text spike
    [SerializeField] float overflowIn = 0.12f;
    [SerializeField] float overflowOut = 0.20f;

    public event Action OnOverflowed;

    int _score;
    int _combo = 1;

    Vector3 _scoreBaseScale, _comboBaseScale, _overflowTextBaseScale;
    Vector2 _comboBaseAnchoredPos;
    Color _scoreBaseColor, _comboBaseColor;

    Sequence _scoreSeq, _comboSeq, _overflowSeq;

    float _comboExpireAt = -1f;
    int _overflowProgress; // 0..overflowThreshold (in points)

    bool _initialized;

    public int Score => _score;

    void Awake()
    {
        if (!scoreText || !comboText) { Debug.LogError("UIScore: assign texts."); enabled = false; return; }

        _scoreBaseScale = scoreText.rectTransform.localScale;
        _comboBaseScale = comboText.rectTransform.localScale;
        _overflowTextBaseScale = overflowText ? overflowText.rectTransform.localScale : Vector3.one;
        _comboBaseAnchoredPos = comboText.rectTransform.anchoredPosition;
        _scoreBaseColor = scoreText.color;
        _comboBaseColor = comboText.color;

        if (!scorePulseRoot) scorePulseRoot = scoreText.rectTransform;
        if (!comboPulseRoot) comboPulseRoot = comboText.rectTransform;
        if (!overflowTextPulseRoot && overflowText) overflowTextPulseRoot = overflowText.rectTransform;
        if (!overflowSliderPulseRoot && overflowSlider)
            overflowSliderPulseRoot = overflowSlider.fillRect ? overflowSlider.fillRect : (RectTransform)overflowSlider.transform;

        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        scoreText.text = _score.ToString().PadLeft(scorePadLength, '0');
        comboText.text = comboPrefix + _combo;

        if (overflowSlider) { overflowSlider.minValue = 0f; overflowSlider.maxValue = 1f; overflowSlider.value = 0f; }
        if (overflowText) overflowText.text = overflowText.text; // leave as-is

        _initialized = true;
    }

    void Update()
    {
        if (!_initialized) return;

        if (dropToOneOnTimeout && _combo > 1 && Time.unscaledTime > _comboExpireAt)
        {
            _combo = 1;
            AnimateCombo(_combo);
        }
    }

    public void SetWaveText(string text)
    {
        if (textWave)
            textWave.text = text;
    }
    public void AddScore(int points)
    {
        if (points <= 0) return;

        bool chain = Time.unscaledTime <= _comboExpireAt;
        _combo = Mathf.Clamp(chain ? _combo + 1 : 1, 1, comboMax);
        _comboExpireAt = Time.unscaledTime + comboWindow;

        int delta = points * _combo;
        int prev = _score;
        _score += delta;

        AnimateScore(prev, _score, _combo);
        AnimateCombo(_combo);
        AddOverflow(delta);
    }

    public void AddScore(int points, int _ignoredExternalMultiplier) => AddScore(points);

    void AddOverflow(int deltaPoints)
    {
        if (!overflowSlider) return;

        _overflowSeq?.Kill(true);

        int before = _overflowProgress;
        int after = before + Mathf.Max(0, deltaPoints);

        int overflowCount = after / overflowThreshold;
        int remainder = after % overflowThreshold;

        float startNorm = Mathf.Clamp01(before / (float)overflowThreshold);
        float endNormNoOverflow = Mathf.Clamp01(after / (float)overflowThreshold);
        float remainderNorm = remainder / (float)overflowThreshold;

        var seq = DOTween.Sequence().SetUpdate(true);

        if (overflowCount == 0)
        {
            // simple fill
            seq.Append(DOTween.To(() => overflowSlider.value, v => overflowSlider.value = v, endNormNoOverflow, overflowFillTime).SetEase(overflowEase));
        }
        else
        {
            // fill to 1
            seq.Append(DOTween.To(() => overflowSlider.value, v => overflowSlider.value = v, 1f, overflowFillTime * (1f - startNorm)).SetEase(overflowEase));

            // fire events (for multiple overflows in one add)
            seq.AppendCallback(() =>
            {
                for (int i = 0; i < overflowCount; i++) OnOverflowed?.Invoke();
                // big spike on overflow text
                if (overflowText)
                {
                    var rt = overflowText.rectTransform;
                    rt.DOKill();
                    DOTween.Sequence().SetUpdate(true)
                        .Append(rt.DOScale(_overflowTextBaseScale * overflowHugeScale, overflowIn).SetEase(Ease.OutBack, 2.5f))
                        .Append(rt.DOScale(_overflowTextBaseScale, overflowOut).SetEase(Ease.OutQuad));
                }
            });

            // reset bar instantly
            seq.AppendCallback(() => overflowSlider.value = 0f);

            // fill remainder
            if (remainder > 0)
                seq.Append(DOTween.To(() => overflowSlider.value, v => overflowSlider.value = v, remainderNorm, overflowFillTime).SetEase(overflowEase));
        }

        _overflowProgress = remainder;
        _overflowSeq = seq;
    }

    void AnimateScore(int fromValue, int toValue, int combo)
    {
        _scoreSeq?.Kill(true);

        Tween val = DOTween.To(
            () => fromValue,
            v => scoreText.text = v.ToString().PadLeft(scorePadLength, '0'),
            toValue,
            Mathf.Clamp(0.35f + 0.02f * Mathf.Log10(Mathf.Max(1, toValue - fromValue)), 0.2f, 0.9f)
        ).SetEase(Ease.OutCubic).SetUpdate(true);

        float punch = Mathf.Min(scorePunchBase + scorePunchPerCombo * (combo - 1), 2.2f);

        Tween flashOut = scoreText.DOColor(scoreFlashColor, 0.07f).SetUpdate(true);
        Tween flashBack = scoreText.DOColor(_scoreBaseColor, 0.20f).SetEase(Ease.OutQuad).SetUpdate(true);

        _scoreSeq = DOTween.Sequence()
            .Join(val)
            .Join(scoreText.rectTransform.DOPunchScale(Vector3.one * (punch - 1f), scorePunchDuration, 8, 0.5f).SetUpdate(true))
            .Join(scoreText.rectTransform.DOShakeAnchorPos(scoreShakeDuration, scoreShakeStrength * Mathf.Clamp(combo, 1, 8), scoreShakeVibrato, 90, false, true).SetUpdate(true))
            .Insert(0f, flashOut)
            .Insert(0.07f, flashBack)
            .OnKill(() => scoreText.rectTransform.localScale = _scoreBaseScale);
    }

    void AnimateCombo(int combo)
    {
        _comboSeq?.Kill(true);

        comboText.text = comboPrefix + combo;
        if (comboColorByValue != null && comboColorByValue.colorKeys.Length > 0)
        {
            float t = Mathf.InverseLerp(1f, 50f, combo);
            comboText.color = comboColorByValue.Evaluate(t);
        }
        else comboText.color = _comboBaseColor;

        float target = Mathf.Clamp(comboPopMin + comboPopPerStep * (combo - 1), comboPopMin, comboPopMax);

        _comboSeq = DOTween.Sequence().SetUpdate(true)
            .Append(comboText.rectTransform.DOScale(_comboBaseScale * target, comboPopDuration).SetEase(Ease.OutBack, 1.6f))
            .Append(comboText.rectTransform.DOScale(_comboBaseScale, 0.18f).SetEase(Ease.OutQuad))
            .OnKill(() =>
            {
                comboText.rectTransform.localScale = _comboBaseScale;
                comboText.rectTransform.anchoredPosition = _comboBaseAnchoredPos;
                comboText.rectTransform.localRotation = Quaternion.identity;
            });
    }

    public void OnBeat(float strength01)
    {
        strength01 = Mathf.Clamp01(strength01);
        var delta = Vector3.one * Mathf.Lerp(beatPunchMin, beatPunchMax, strength01);

        PunchBlendable(scorePulseRoot, delta, beatDuration);
        PunchBlendable(comboPulseRoot, delta, beatDuration);
        if (overflowTextPulseRoot) PunchBlendable(overflowTextPulseRoot, delta, beatDuration);
        if (overflowSliderPulseRoot) PunchBlendable(overflowSliderPulseRoot, delta, beatDuration);

        DOTween.Sequence().SetUpdate(true)
            .Append(scoreText.DOColor(beatFlashColor, beatFlashIn))
            .Append(scoreText.DOColor(_scoreBaseColor, beatFlashOut));
    }

    static void PunchBlendable(RectTransform rt, Vector3 delta, float dur)
    {
        if (!rt) return;
        DOTween.Sequence().SetUpdate(true)
            .Join(rt.DOBlendableScaleBy(delta, dur * 0.5f).SetEase(Ease.OutQuad))
            .Append(rt.DOBlendableScaleBy(-delta, dur * 0.5f).SetEase(Ease.InQuad));
    }

    public void ResetUI(int score = 0)
    {
        _scoreSeq?.Kill(true);
        _comboSeq?.Kill(true);
        _overflowSeq?.Kill(true);

        _score = score;
        _combo = 1;
        _overflowProgress = 0;

        scoreText.rectTransform.localScale = _scoreBaseScale;
        comboText.rectTransform.localScale = _comboBaseScale;
        scoreText.color = _scoreBaseColor;
        comboText.color = _comboBaseColor;
        scoreText.text = _score.ToString().PadLeft(scorePadLength, '0');
        comboText.text = comboPrefix + _combo;

        if (overflowSlider) overflowSlider.value = 0f;
        if (overflowText) overflowText.rectTransform.localScale = _overflowTextBaseScale;

        _comboExpireAt = -1f;
    }

    public float ComboTime01 =>
        (_combo > 1 && _comboExpireAt > 0f)
            ? Mathf.Clamp01((_comboExpireAt - Time.unscaledTime) / comboWindow)
            : 0f;
}
