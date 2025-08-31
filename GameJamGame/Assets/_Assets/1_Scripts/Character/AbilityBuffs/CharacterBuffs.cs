// CharacterBuffs.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterBuffs : MonoBehaviour
{
    class Active
    {
        public BuffSO data;
        public int stacks;
        public float endTime; // realtime
    }

    readonly Dictionary<string, Active> _active = new(8);
    private CharacterContext _ctx;
    private Coroutine _coGrowRoutine = null;
    private CharacterGlow _glow;

    void Awake()
    {
        _ctx = GetComponent<CharacterContext>();
        _glow = GetComponent<CharacterGlow>();
    }

    void Update()
    {
        float now = Time.realtimeSinceStartup;
        bool changed = false;

        // expire
        var toRemove = new List<string>();
        foreach (var kv in _active)
            if (kv.Value.endTime <= now) toRemove.Add(kv.Key);
        if (toRemove.Count > 0) { changed = true; foreach (var id in toRemove) _active.Remove(id); }

        if (changed) Recompute();
    }

    public void Apply(BuffSO buff)
    {
        Debug.Log("[Buff] Apply");
        if (!buff) return;
        float now = Time.realtimeSinceStartup;

        if (_active.TryGetValue(buff.id, out var a))
        {
            switch (buff.stacking)
            {
                case StackMode.RefreshDuration:
                    a.endTime = now + buff.duration;
                    break;
                case StackMode.AdditiveStacks:
                    a.stacks = Mathf.Min(buff.maxStacks, a.stacks + 1);
                    a.endTime = now + buff.duration;
                    break;
                case StackMode.IgnoreIfPresent:
                    // do nothing
                    break;
            }
        }
        else
        {
            _active[buff.id] = new Active
            {
                data = buff,
                stacks = 1,
                endTime = now + buff.duration
            };
        }
        Debug.Log("[Buff] Recomputing");

        Recompute();
    }

    public bool Has(string id) => _active.ContainsKey(id);

    void Recompute()
    {
        float moveMul = 1f;
        float dmgMul = 1f;
        Vector3 growMul = transform.localScale;

        // Visuals aggregate
        Color glowCol = Color.black;
        float glowInt = 0f;

        Debug.Log("[Buff] Calcculating");

        foreach (var kv in _active)
        {
            var a = kv.Value;
            int s = Mathf.Max(1, a.stacks);

            // multiplicative stacking for multipliers
            moveMul *= Mathf.Pow(a.data.moveSpeedMul, s);
            dmgMul *= Mathf.Pow(a.data.damageMul, s);
            growMul *= Mathf.Pow(a.data.sizeMul, s);

            // visuals: sum intensity, average color contribution
            glowCol += a.data.glowColor * s;
            glowInt += a.data.glowIntensity * s;
        }

        Debug.Log("[Buff] GrowmUl: " + growMul);
        _ctx.CharacterSkills.GetSkill(SkillType.Speed).SetSkillBuff(moveMul);
        _ctx.CharacterSkills.GetSkill(SkillType.Combat).SetSkillBuff(dmgMul);

        if (_coGrowRoutine != null) StopCoroutine(_coGrowRoutine);
        
        _coGrowRoutine = StartCoroutine(CoGrowScale(growMul));
        // if we wanted to apply a light or something, this was the old way
        if (_glow)
        {
            if (_active.Count == 0) _glow.SetGlow(Color.black, 0f);
            else
            {
                // normalize color a bit
                glowCol /= Mathf.Max(1, _active.Count);
                _glow.SetGlow(glowCol, Mathf.Clamp(glowInt, 0f, 3f));
            }
        }
    }

    IEnumerator CoGrowScale(Vector3 newScale)
    {
        // grow from curr scale to newscale in 0.2s
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, newScale, t);
            yield return null;
        }
        transform.localScale = newScale;
        Debug.Log("GrowScale: " + newScale);
    }
}
