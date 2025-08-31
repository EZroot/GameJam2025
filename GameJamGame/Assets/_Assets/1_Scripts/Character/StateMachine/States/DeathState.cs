using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Dead")]
public class DeathState : StateScriptableObject
{
    public AudioClip[] m_deathScream;
    public AudioClip[] m_killingBlowSound;

    private CharacterStatemachine m_stateMachine;
    private Rigidbody2D m_rigidbody;
    private Coroutine _smearCo;

    // Smear params
    private float smearDuration = 3f;      // seconds
    private float smearSpacingWU = 0.12f;  // world units per stamp
    private int smearStartRadiusPx = 5;
    private int smearEndRadiusPx = 2;
    private Color deathColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private EffectBlood effectBlood; // To steal blood color

    public override void Enter(CharacterStatemachine statemachine)
    {
        m_stateMachine = statemachine;

        Service.Get<IAudioService>().PlaySound("dead", 0.25f, false);
        effectBlood = m_stateMachine.GetComponentInChildren<EffectBlood>();

        m_rigidbody = m_stateMachine.GetComponent<Rigidbody2D>();
        m_rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;

        var collider = m_stateMachine.GetComponent<Collider2D>();
        collider.enabled = false;

        var prev = m_stateMachine.CharacterContext.PrevHitInfo;

        var paperDoll = m_stateMachine.CharacterContext.PaperDoll;
        foreach (PaperDollContext.BodyPart bp in System.Enum.GetValues(typeof(PaperDollContext.BodyPart)))
            paperDoll.SetColor(bp, deathColor);
        paperDoll.SetAllSortOrder(-10);

        _smearCo = m_stateMachine.StartCoroutine(SmearBlood());

        Service.Get<IUIService>().UIScore.AddScore(100);

        m_stateMachine.StartCoroutine(DeleteSelf());
    }

    IEnumerator DeleteSelf()
    {
        Service.Get<INpcService>().RemoveNpc(m_stateMachine.CharacterContext);
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(m_stateMachine.gameObject);
    }

    public override void Execute() { }

    public override void Exit()
    {
        if (_smearCo != null) { m_stateMachine.StopCoroutine(_smearCo); _smearCo = null; }
        m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        var paperDoll = m_stateMachine.CharacterContext.PaperDoll;
        foreach (PaperDollContext.BodyPart bp in System.Enum.GetValues(typeof(PaperDollContext.BodyPart)))
            paperDoll.SetColor(bp, Color.white);
        paperDoll.ResetToDefaultSortOrder();
    }

    IEnumerator SmearBlood()
    {
        var svc = Service.Get<IPaintSurface2D>();
        if (svc == null) yield break;

        // pick a body anchor to smear from
        var body = m_stateMachine.CharacterContext.PaperDoll.GetBodyPart(PaperDollContext.BodyPart.Body);
        if (!body) yield break;

        float elapsed = 0f;
        Vector3 last = body.position;

        while (elapsed < smearDuration)
        {
            Vector3 cur = body.position;
            Vector3 d = cur - last;
            float dist = d.magnitude;

            // radius eases from start→end over duration
            float t = Mathf.Clamp01(elapsed / smearDuration);
            int r = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(smearStartRadiusPx, smearEndRadiusPx, t)));

            if (dist > 0.0001f)
            {
                Vector3 dir = d / dist;
                // stamp every smearSpacingWU along the path
                for (float p = 0f; p <= dist; p += smearSpacingWU)
                {
                    Vector3 pos = last + dir * p;
                    svc.PaintCircle(pos, r, effectBlood.BloodColor);
                }
                last = cur;
            }
            else
            {
                // still smear when stationary
                svc.PaintCircle(cur, r, effectBlood.BloodColor);
            }

            elapsed += Time.deltaTime;
            yield return null; // or yield return new WaitForFixedUpdate();
        }
    }
}
