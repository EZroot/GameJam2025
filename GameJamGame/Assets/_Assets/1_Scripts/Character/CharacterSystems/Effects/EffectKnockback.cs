using UnityEngine;

public class EffectKnockback : MonoBehaviour, IHitEffect
{
    [SerializeField] float multiplier = 1f;
    [SerializeField] EffectType effectType = EffectType.Hit;
    public Rigidbody2D rb;

    public EffectType EffectType => effectType;

    public void OnHit(CharacterContext ctx, in HitInfo info)
    {
        if (info.knockback <= 0f) return;
        rb.AddForce(info.rayDir.normalized * info.knockback * multiplier, ForceMode2D.Impulse);
    }
}