using System.Collections;
using UnityEngine;

public class EffectBlood : MonoBehaviour, IHitEffect
{
    [Header("Streak shape")]
    [SerializeField] int frames = 10;
    [SerializeField] float stepWorld = 0.15f;     // distance per stamp
    [SerializeField] int startRadiusPx = 3;
    [SerializeField] int endRadiusPx = 1;
    [SerializeField] Color color = new Color(0.35f, 0, 0, 0.75f);

    [Header("Arc")]
    [SerializeField] int streakCount = 5;         // how many streaks per hit
    [SerializeField] float arcHalfDeg = 15f;      // ± angle
    [SerializeField] bool evenlySpaced = false;    // else random within arc

    [SerializeField] EffectType effectType = EffectType.Hit;

    public Color BloodColor => color;

    public EffectType EffectType => effectType;

    public void OnHit(CharacterContext ctx, in HitInfo info)
    {
        // base direction (fallbacks if rayDir missing)
        Vector3 baseDir = info.rayDir.sqrMagnitude > 0 ? (Vector3)info.rayDir.normalized
                         : (info.normal.sqrMagnitude > 0 ? (Vector3)info.normal.normalized
                         : Vector3.right);

        Vector3 hit = (Vector3)info.point + baseDir * 0.01f; // tiny nudge

        for (int s = 0; s < Random.Range(1,streakCount); s++)
        {
            float angle = evenlySpaced
                ? Mathf.Lerp(-arcHalfDeg, arcHalfDeg, (streakCount == 1 ? 0.5f : (float)s / (streakCount - 1)))
                : Random.Range(-arcHalfDeg, arcHalfDeg);

            Vector3 dir = RotateAroundZ(baseDir, angle).normalized;
            StartCoroutine(CoStreak(hit, dir));
        }
    }

    IEnumerator CoStreak(Vector3 startWorld, Vector3 dirWorld)
    {
        var svc = Service.Get<IPaintSurface2D>();
        if (svc == null) yield break;

        Vector3 pos = transform.position; // small offset like your code
        Vector3 step = dirWorld * stepWorld;
        pos += step * 2; // Head start

        var wait = new WaitForFixedUpdate();
        for (int i = 0; i < frames; i++)
        {
            pos += step;

            float t = (i + 1f) / frames;
            int r = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(startRadiusPx, endRadiusPx, t)));

            svc.PaintCircle(pos, r, color);
            yield return wait;
        }
    }

    static Vector3 RotateAroundZ(Vector3 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
    }
}
