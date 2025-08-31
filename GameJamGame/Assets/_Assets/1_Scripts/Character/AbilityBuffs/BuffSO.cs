// BuffSO.cs
using UnityEngine;

public enum StackMode { RefreshDuration, AdditiveStacks, IgnoreIfPresent }

[CreateAssetMenu(menuName = "Buffs/Buff")]
public class BuffSO : ScriptableObject
{
    [Header("Identity")]
    public string id = "speed_boost";

    [Header("Timing")]
    public float duration = 4f;
    public StackMode stacking = StackMode.RefreshDuration;
    public int maxStacks = 1;

    [Header("Stats per stack")]
    [Range(0f, 5f)] public float moveSpeedMul = 1.15f; // 1 = no change
    [Range(0f, 5f)] public float damageMul = 1.00f;
    [Range(1f, 5f)] public float sizeMul = 1f;

    [Header("Visuals per stack")]
    public Color glowColor = new Color(1f, 0.9f, 0.5f, 1f);
    [Range(0f, 5f)] public float glowIntensity = 0.6f; // summed or clamped by runtime
}
