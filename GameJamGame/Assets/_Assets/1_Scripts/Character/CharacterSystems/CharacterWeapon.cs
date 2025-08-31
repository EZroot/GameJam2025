using System.Collections;
using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    private CharacterContext m_context;
    private CameraController cam;

    [Header("Recoil/Camera")]
    [SerializeField] float recoilStrength = 1f;

    [Header("Tracer")]
    [SerializeField] float _tracerWidth = 0.035f;
    [SerializeField] float tracerLength = 1.2f;
    [SerializeField] float tracerSpeed = 120f;
    [SerializeField] float tracerRetractSpeed = 160f;   // tail speed when catching up
    [SerializeField] float tracerFade = 0.0f;           // optional fade after retract
    [SerializeField] float maxDistance = 10f;
    [SerializeField] Material tracerMaterial;
    [SerializeField] string sortingLayerName = "FX";
    [SerializeField] int sortingOrder = 0;

    [Header("Tracer Appearance")]
    [SerializeField] bool tracerUseGradient = false;
    [SerializeField] Gradient tracerGradient;
    [SerializeField] Color tracerStartColor = Color.white;
    [SerializeField] Color tracerEndColor = Color.white;
    [SerializeField] bool tracerUseWidthCurve = false;
    [SerializeField] AnimationCurve tracerWidthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] bool tracerTaperTail = true; // start thinner than head when no width curve
    [SerializeField] LineTextureMode tracerTextureMode = LineTextureMode.Stretch;
    [SerializeField] LineAlignment tracerAlignment = LineAlignment.View;
    [SerializeField] int tracerNumCapVertices = 4;
    [SerializeField] int tracerNumCornerVertices = 0;

    [Header("Masks")]
    [SerializeField] LayerMask stopMask;    // blocks visuals (World | Character | etc.)
    [SerializeField] LayerMask damageMask;  // passed into your RaycastHit2D

    LineRenderer lr;
    Coroutine tracerCo;
    float tracerWidth;

    void Awake()
    {
        m_context = GetComponent<CharacterContext>();

        lr = gameObject.AddComponent<LineRenderer>();
        lr.enabled = false;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        if (tracerMaterial) lr.material = tracerMaterial;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;

        ApplyTracerAppearance();
    }

    void OnValidate()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (lr) ApplyTracerAppearance();
    }

    void OnDisable()
    {
        if (tracerCo != null) StopCoroutine(tracerCo);
        tracerCo = null;
        if (lr) lr.enabled = false;
    }

    void ApplyTracerAppearance()
    {
        tracerWidth = _tracerWidth * (Mathf.Clamp(transform.localScale.magnitude, 1, 10));

        // Width
        if (tracerUseWidthCurve)
        {
            lr.widthCurve = tracerWidthCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
            lr.widthMultiplier = tracerWidth * transform.localScale.magnitude;
            lr.startWidth = tracerWidth; // not used by curve at runtime but keeps Inspector sane
            lr.endWidth = tracerWidth;
        }
        else
        {
            lr.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            lr.widthMultiplier = 1f * transform.localScale.magnitude;
            lr.startWidth = tracerTaperTail ? 0f : tracerWidth;
            lr.endWidth = tracerWidth;
        }

        // Color
        if (tracerUseGradient && tracerGradient != null)
        {
            lr.colorGradient = tracerGradient;
        }
        else
        {
            lr.startColor = tracerStartColor;
            lr.endColor = tracerEndColor;
        }

        // Geometry
        lr.numCapVertices = Mathf.Max(0, tracerNumCapVertices);
        lr.numCornerVertices = Mathf.Max(0, tracerNumCornerVertices);
        lr.textureMode = tracerTextureMode;
        lr.alignment = tracerAlignment;
    }

    public void Attack(CharacterStatemachine sm)
    {
        var look = sm.CharacterContext.CharacterLookAt2D;
        Vector3 origin = look.transform.position;
        Vector2 dir = (look.LookPoint - origin).normalized;

        // Add camera shake and recoil based on combat skill
        var combatPower = sm.CharacterContext.CharacterSkills.GetSkill(SkillType.Combat).GetCurrentWorkValue();
        var t = Mathf.InverseLerp(10f, 50f, combatPower);

        if (!cam) cam = FindAnyObjectByType<CameraController>();
        if (cam)
        {
            //normalize combat power between a min and max
            var trauma = Mathf.Lerp(0.1f, 1.5f, t);
            //cam.AddTrauma(0.75f); cam.Kick(Vector2.zero, 16, 0);
            cam.SetTrauma01(trauma); 
            //cam.Kick(Vector2.zero, kickPixels, 0);

            Service.Get<IUIService>().UIScore.OnBeat(trauma);

        }

        // pre-fire raycast origin (matches your later call)
        Vector3 castOrigin = origin + (Vector3)(dir * 0.5f);

        // Precast to clamp distance to first blocker
        RaycastHit2D pre = Physics2D.Raycast(castOrigin, dir, maxDistance, stopMask);
        float endDist = pre ? pre.distance : maxDistance; // 'pre.distance' is from castOrigin

        if (tracerCo != null) StopCoroutine(tracerCo);
        tracerCo = StartCoroutine(CoTracerAndHit(sm, origin, dir, endDist));
    }

    IEnumerator CoTracerAndHit(CharacterStatemachine sm, Vector3 origin, Vector2 dir, float endDist)
    {
        lr.enabled = true;
        ApplyTracerAppearance();
        float headDist = 0f;

        // Head advances to impact
        while (headDist < endDist)
        {
            headDist = Mathf.Min(endDist, headDist + tracerSpeed * Time.deltaTime);
            float tailDist = Mathf.Max(0f, headDist - tracerLength);

            lr.SetPosition(0, origin + (Vector3)dir * tailDist);
            lr.SetPosition(1, origin + (Vector3)dir * headDist);
            yield return null;
        }

        // Do hitscan with the same origin and clamped distance
        sm.CharacterContext.CharacterRaycast2D.RaycastHit2D(
            origin + (Vector3)(dir * 0.5f),
            dir,
            endDist + 1,
            damageMask
        );

        // Tail catches up to head (no fade). Bullet "enters" the collider.
        float tail = Mathf.Max(0f, headDist - tracerLength);
        Vector3 headPos = origin + (Vector3)dir * headDist;

        while (tail < headDist)
        {
            tail = Mathf.Min(headDist, tail + tracerRetractSpeed * Time.deltaTime);
            lr.SetPosition(0, origin + (Vector3)dir * tail);
            lr.SetPosition(1, headPos);
            yield return null;
        }

        // Optional quick fade after retract if tracerFade > 0
        if (tracerFade > 0f)
        {
            float t = 0f;
            float w0 = lr.widthMultiplier;
            // If not using curve, widthMultiplier = 1, so scale start/end widths instead
            float sStart = lr.startWidth;
            float sEnd = lr.endWidth;

            while (t < tracerFade)
            {
                float k = 1f - t / tracerFade;
                if (tracerUseWidthCurve)
                    lr.widthMultiplier = w0 * k;
                else
                {
                    lr.startWidth = sStart * k;
                    lr.endWidth = sEnd * k;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        lr.enabled = false;
        tracerCo = null;

        // Restore widths if we scaled them
        if (!tracerUseWidthCurve)
        {
            lr.startWidth = tracerTaperTail ? 0f : tracerWidth;
            lr.endWidth = tracerWidth;
        }
        else
        {
            lr.widthMultiplier = tracerWidth;
        }
    }
}
