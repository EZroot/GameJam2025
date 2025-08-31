using UnityEngine;

public class CharacterMotor2D : MonoBehaviour
{
    [Header("Speeds")]
    [Min(0f)] public float acceleration = 60f;   // how fast you reach speed
    [Min(0f)] public float deceleration = 80f;   // how fast you stop when letting go
    [Min(0f)] public float turnBoost = 120f;     // extra accel when reversing direction

    [Header("Input")]
    [Range(0f, 1f)] public float inputSmoothing = 0.08f; // 0 = raw, 0.1 = subtle smoothing
    [Range(0f, 0.5f)] public float deadZone = 0.08f;

    [Header("Misc")]
    public bool useRawAxis = true; // true = GetAxisRaw (snappier), false = GetAxis

    [Header("Use Rigidbody. Not needed for AI")]
    public bool useRigidbody;

    CharacterContext _ctx;
    Rigidbody2D rb;
    Vector2 smoothedInput;     // low-pass filtered input
    Vector2 desiredVel;        // target velocity based on input
    Vector2 currentVel;        // cached rb.velocity for calculations
    Vector3 prevTransformPos;

    public float Speed01 => useRigidbody ? Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.0001f, GetSkillSpeed())) : Mathf.Clamp01(currentVel.magnitude / Mathf.Max(0.0001f, GetSkillSpeed()));
    public Vector2 Velocity => rb.linearVelocity;
    public Vector2 MoveInput => smoothedInput;

    void Awake()
    {
        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
        }
    }

    private void Start()
    {
        _ctx = GetComponent<CharacterContext>();
        prevTransformPos = transform.position;
    }

    public void SetMove(float xAxis, float yAxis)
    {
        // 1) Read input
        float ix = xAxis;
        float iy = yAxis;
        Vector2 raw = new Vector2(ix, iy);

        // 2) Dead zone
        if (raw.sqrMagnitude < deadZone * deadZone) raw = Vector2.zero;
        else raw = raw.normalized * Mathf.Min(1f, raw.magnitude); // keep circular

        // 3) Simple low-pass smoothing for small hand jitter without adding lag
        if (inputSmoothing > 0f)
            smoothedInput = Vector2.Lerp(smoothedInput, raw, 1f - Mathf.Pow(1f - 0.72f, Time.unscaledDeltaTime / Mathf.Max(0.0001f, inputSmoothing)));
        else
            smoothedInput = raw;

        // 4) Desired velocity
        desiredVel = smoothedInput * GetSkillSpeed();
    }

    void FixedUpdate()
    {
        if (useRigidbody)
        {
            currentVel = rb.linearVelocity;
        }
        else
        {
            currentVel = (transform.position - prevTransformPos) / Time.fixedDeltaTime;
            prevTransformPos = transform.position;
        }

        // Choose accel rate: stronger when changing direction for �stiff� feel
        float accelRate;
        if (desiredVel == Vector2.zero)
        {
            // No input: brake hard to a stop
            accelRate = deceleration;
        }
        else
        {
            bool reversing =
                currentVel.sqrMagnitude > 0.0001f &&
                Vector2.Dot(currentVel.normalized, desiredVel.normalized) < 0f;

            accelRate = reversing ? turnBoost : acceleration;
        }

        // Move velocity toward target
        Vector2 newVel = Vector2.MoveTowards(currentVel, desiredVel, accelRate * Time.fixedDeltaTime);

        // Tiny snap to zero to avoid micro drift
        if (desiredVel == Vector2.zero && newVel.magnitude < 0.05f) newVel = Vector2.zero;

        if (useRigidbody)
        {
            rb.linearVelocity = newVel;
        }
        else
        {
            transform.Translate(newVel * Time.fixedDeltaTime, Space.World);
        }
    }

    private float GetSkillSpeed()
    {
        int pts = _ctx.CharacterSkills?.GetSkill(SkillType.Speed)?.GetCurrentWorkValue() ?? 0;

        const float baseSpeed = 1.0f;  // speed at 0 pts
        const float capSpeed = 5.0f;  // hard ceiling
        const float knee = 12f;   // pts where you reach ~50% of (cap-base)

        // Michaelis–Menten style: t in [0,1), diminishing returns
        float t = pts <= 0 ? 0f : (pts / (pts + knee));
        return Mathf.Lerp(baseSpeed, capSpeed, t);
    }

}
