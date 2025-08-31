using System.Collections.Generic;
using UnityEngine;

public class BandScaler2D : MonoBehaviour
{
    [Header("Source")]
    public AudioVisualizer_FlexibleBands visualizer;   // auto-assign if null

    [Header("Band Range (inclusive)")]
    [Min(0)] public int bandStart = 0;
    [Min(0)] public int bandEnd = 2;

    [Header("Response")]
    [Min(0f)] public float gain = 8f;
    [Min(0.01f)] public float exponent = 0.75f;
    [Min(0f)] public float lerpSpeed = 12f;

    [Header("Multipliers (applied to child gfx)")]
    public Vector2 multiplierMinXY = Vector2.one;
    public Vector2 multiplierMaxXY = new(1.4f, 1.4f);

    // cache: root -> gfx child, and its baseline localScale
    readonly Dictionary<Transform, Transform> _gfx = new();
    readonly Dictionary<Transform, Vector3> _base = new();


    void Awake()
    {
        if (!visualizer) visualizer = FindAnyObjectByType<AudioVisualizer_FlexibleBands>();
    }

    void Update()
    {
        if (!visualizer) return;

        float sum = visualizer.GetBandValue(bandStart, bandEnd);
        float v = Mathf.Pow(Mathf.Clamp01(sum * gain), exponent);

        Vector2 mul = new(
            Mathf.Lerp(multiplierMinXY.x, multiplierMaxXY.x, v),
            Mathf.Lerp(multiplierMinXY.y, multiplierMaxXY.y, v)
        );
        Vector3 targetMul = new(mul.x, mul.y, 1f);
        float a = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);

        // NPCs
        foreach (var npc in Service.Get<INpcService>().NpcCollection)
        {
            if (npc.CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() <= 0) continue;
            ApplyToFirstChild(npc.transform, targetMul, a);
        }

        // Player
        var player = Service.Get<IPlayerService>().LocalPlayer;
        if (player && player.CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() > 0) ApplyToFirstChild(player.transform, targetMul, a);
    }

    void ApplyToFirstChild(Transform root, Vector3 targetMul, float a)
    {
        if (!root) return;

        // get first child as GFX container
        if (!_gfx.TryGetValue(root, out var gfx) || !gfx)
        {
            if (root.childCount == 0) return;
            gfx = root.GetChild(0);
            _gfx[root] = gfx;
            // remember artist baseline once
            _base[gfx] = gfx.localScale;
        }

        if (!_base.TryGetValue(gfx, out var baseScale))
        {
            baseScale = gfx.localScale;
            _base[gfx] = baseScale;
        }

        // current multiplier = current / base
        var cur = gfx.localScale;
        var curMul = new Vector3(
            SafeDiv(cur.x, baseScale.x),
            SafeDiv(cur.y, baseScale.y),
            SafeDiv(cur.z, baseScale.z == 0f ? 1f : baseScale.z)
        );

        // lerp the multiplier, not the absolute scale
        var newMul = Vector3.Lerp(curMul, targetMul, a);
        gfx.localScale = new Vector3(
            baseScale.x * newMul.x,
            baseScale.y * newMul.y,
            baseScale.z * newMul.z
        );
    }

    static float SafeDiv(float a, float b) => b != 0f ? a / b : a;

    void OnValidate()
    {
        if (bandEnd < bandStart) bandEnd = bandStart;
    }
}
