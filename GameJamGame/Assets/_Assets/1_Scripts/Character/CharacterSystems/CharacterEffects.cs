using System.Collections.Generic;
using UnityEngine;
public enum Surface2D { Flesh, Metal, Wood, Generic }
public enum EffectType { Hit, Ability }
public struct HitInfo
{
    public Vector2 point;
    public Vector2 normal;
    public Vector2 rayDir;
    public float damage;
    public GameObject instigator;
    public Surface2D surface;
    public float knockback;   // impulse magnitude
}
// IHitEffect.cs
public interface IHitEffect
{
    EffectType EffectType { get; }
    void OnHit(CharacterContext ctx, in HitInfo info);
}


[DisallowMultipleComponent]
public class CharacterEffects : MonoBehaviour
{
    private CharacterContext ctx;
    List<IHitEffect> _effects;

    void Awake()
    {
        _effects = new List<IHitEffect>(GetComponentsInChildren<IHitEffect>(true));
    }

    private void Start()
    {
        ctx = GetComponent<CharacterContext>();
    }

    // Call this from gameplay
    public void OnHit(in HitInfo info)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            if (_effects[i].EffectType == EffectType.Hit)
            {
                _effects[i].OnHit(ctx, info);
            }
        }
    }
}
