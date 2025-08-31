using System.Collections;
using UnityEngine;

public class EffectTimeStop : MonoBehaviour, IHitEffect
{
    [Header("Hit Stop")]
    [SerializeField] float duration = 0.06f;      // hold time in realtime seconds
    [SerializeField] float scaleDuring = 0.12f;   // 0.06–0.2 feels snappy
    [SerializeField] float easeOut = 0.06f;       // smooth restore
    [SerializeField] bool onlyOnKill = true;     // set true to trigger only on death
    [SerializeField] EffectType effectType = EffectType.Hit;
    bool _running;
    float _baseFixed;
    float _endRT;         
    float _targetScale = 1f;
    public EffectType EffectType => effectType;

    void Awake()
    {
        _baseFixed = Time.fixedDeltaTime;
    }

    public void OnHit(CharacterContext ctx, in HitInfo info)
    {
        if (onlyOnKill && ctx.CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() > 0) return;

        float s = Mathf.Clamp(scaleDuring, 0.06f, 1f);
        float now = Time.realtimeSinceStartup;

        // extend window and take the strongest slow scale
        _endRT = Mathf.Max(_endRT, now + Mathf.Max(0f, duration));
        _targetScale = Mathf.Min(_targetScale, s);

        if (!_running) StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        _running = true;
        Apply(_targetScale);

        while (Time.realtimeSinceStartup < _endRT)
            yield return null; // unscaled wait

        // ease back to 1 using unscaled time
        float start = _targetScale;
        float t = 0f;
        float eo = Mathf.Max(1e-4f, easeOut);
        while (t < eo)
        {
            t += Time.unscaledDeltaTime;
            Apply(Mathf.Lerp(start, 1f, t / eo));
            yield return null;
        }

        Apply(1f);
        _targetScale = 1f;
        _running = false;
    }

    void Apply(float s)
    {
        Time.timeScale = s;
        Time.fixedDeltaTime = _baseFixed * s;
    }

    void OnDisable()
    {
        if (_running)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixed;
            _running = false;
            _targetScale = 1f;
        }
    }
}
