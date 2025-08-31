// RopeIntestine2D.cs
// Lightweight 2D "guts" rope using Verlet + distance constraints.
// One end can be pinned to a Transform (zombie). Detach/reattach supported.
// Optimized for many instances: no physics components, no GC in play.

using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeIntestine2D : MonoBehaviour
{
    [Header("Attach")]
    [SerializeField] Transform attach;          // zombie bone or sprite pivot
    [SerializeField] bool startAttached = true;

    [Header("Rope")]
    [SerializeField] public int segmentCount = 20;     // total particles
    [SerializeField] public float segmentLength = 0.08f;
    [SerializeField] public int solverIterations = 4;  // 3–6 is enough
    [SerializeField] public float slack = 0.0f;        // >0 for slight sag without stretch
    [SerializeField] public Vector2 initialDirection = new Vector2(1, -0.2f);

    [Header("Blood Painting")]
    [SerializeField] public bool paintBlood = true;    // paint blood at tail
    [SerializeField] public Color bloodColor = new Color(0.35f, 0, 0, 0.75f);

    [Header("Forces")]
    [SerializeField] public Vector2 gravity = new Vector2(0, -9.81f);
    [SerializeField, Range(0.90f, 0.999f)] public float damping = 0.985f;

    [Header("Tearing/Detach")]
    [SerializeField] public float breakStretch = 1.8f; // if any segment > this * length → Detach()

    [Header("Rendering")]
    [SerializeField] public float width = 0.05f;

    // buffers
    Vector2[] _pos;
    Vector2[] _prev;
    LineRenderer _lr;
    bool _attached;

    // Cached dt^2 for Verlet
    float _dt2;

    bool _stopSimulation = false; // If true, dont do simulation

    public Vector2 RopeEndPosition => _pos[segmentCount - 1];
    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = segmentCount;
        _lr.widthMultiplier = width;
        _lr.useWorldSpace = true;
        _lr.alignment = LineAlignment.View; // billboard

        _pos = new Vector2[segmentCount];
        _prev = new Vector2[segmentCount];
    }

    void OnEnable()
    {
        ResetRope();
        _attached = startAttached && attach != null;
    }

    void FixedUpdate()
    {
        if (_stopSimulation) return;

        float dt = Time.fixedDeltaTime;
        _dt2 = dt * dt;

        var paintSurface = Service.Get<IPaintSurface2D>();
        // 1) Integrate (Verlet)
        for (int i = 0; i < segmentCount; i++)
        {
            // Pin head if attached
            if (i == 0 && _attached && attach)
            {
                Vector2 p = attach.position;
                _prev[0] = p;    // zero velocity at pin
                _pos[0] = p;
                continue;
            }

            Vector2 p0 = _pos[i];
            Vector2 v = (p0 - _prev[i]) * damping;
            Vector2 p1 = p0 + v + gravity * _dt2;
            _prev[i] = p0;
            _pos[i] = p1;

            // if were at the last segment
            if (i == segmentCount - 1 && paintBlood)
            {
                paintSurface.PaintCircle(_pos[i], 1, bloodColor);
            }
        }

        // 2) Satisfy distance constraints
        float targetLen = segmentLength * (1f + slack);
        for (int it = 0; it < solverIterations; it++)
        {
            for (int i = 1; i < segmentCount; i++)
            {
                Satisfy(i - 1, i, targetLen);
            }
        }

        // 3) Optional tear check → detach head
        if (_attached)
        {
            for (int i = 1; i < segmentCount; i++)
            {
                float dist = (_pos[i] - _pos[i - 1]).magnitude;
                if (dist > breakStretch * segmentLength) { Detach(); break; }
            }
        }

        // 4) Render
        for (int i = 0; i < segmentCount; i++)
            _lr.SetPosition(i, new Vector3(_pos[i].x, _pos[i].y, 0));

        // If not attached, and rope is mostly still, stop simulation
        if (!_attached)
        {
            Vector2 headV = _pos[0] - _prev[0];
            if (headV.sqrMagnitude < 0.0001f)
            {
                // check tail too
                Vector2 tailV = _pos[segmentCount - 1] - _prev[segmentCount - 1];
                if (tailV.sqrMagnitude < 0.0001f)
                {
                    _stopSimulation = true; // stop sim
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Satisfy(int a, int b, float rest)
    {
        Vector2 pa = _pos[a];
        Vector2 pb = _pos[b];
        Vector2 d = pb - pa;
        float m = d.magnitude;
        if (m < 1e-6f) return;

        float diff = (m - rest) / m;
        // Head pinned gets weight 0 when attached
        float wa = (a == 0 && _attached) ? 0f : 0.5f;
        float wb = 1f - wa; // keep total 1 for symmetry

        Vector2 corr = d * diff;
        _pos[a] += corr * wa;
        _pos[b] -= corr * wb;
    }

    public void Attach(Transform t)
    {
        attach = t;
        _attached = attach != null;
        if (_attached)
        {
            // snap head to attach to avoid pop
            Vector2 p = attach.position;
            _pos[0] = _prev[0] = p;
        }
    }

    public void Detach()
    {
        _attached = false;
        gravity = Vector2.zero;
        // preserve current velocity at head
        // nothing else needed
    }

    public void ResetRope()
    {
        Vector2 head = attach ? (Vector2)attach.position : (Vector2)transform.position;
        Vector2 dir = initialDirection.normalized;
        if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 p = head + dir * (segmentLength * i);
            _pos[i] = _prev[i] = p;
        }

        // seed LR
        _lr.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
            _lr.SetPosition(i, new Vector3(_pos[i].x, _pos[i].y, 0));
    }

    // Optional: shorten or lengthen at runtime without realloc
    public void SetSegmentCount(int newCount)
    {
        newCount = Mathf.Clamp(newCount, 2, _pos.Length);
        _lr.positionCount = newCount;
        // we still simulate arrays up to newCount only
        segmentCount = newCount;
    }

    // Cheap wind or tug
    public void Nudge(Vector2 impulsePerSegment)
    {
        for (int i = 1; i < segmentCount; i++)
            _prev[i] -= impulsePerSegment; // adds velocity
    }
}
