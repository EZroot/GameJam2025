using UnityEngine;

public interface ILootCollector
{
    // Implement on player. Return true if accepted.
    bool TryCollect(string lootId, int amount);
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class LootItem2D : MonoBehaviour, PooledHandle.ICallbacks
{
    [Header("Loot")]
    [SerializeField] string lootId = "coin";
    [SerializeField] int amount = 1;

    [Header("Motion")]
    [SerializeField] float linearDrag = 2.5f;     // while flying
    [SerializeField] float angularDrag = 2.5f;    // while flying
    [SerializeField] float settleSpeed = 0.10f;   // m/s
    [SerializeField] float settleSpin = 6f;       // deg/s
    [SerializeField] float settleDelay = 0.05f;   // ignore settle for first frames

    [Header("Pickup")]
    [SerializeField] LayerMask playerMask;        // player layer(s)
    [SerializeField] float pickupDelay = 0.12f;   // no pickup immediately on spawn
    [SerializeField] float magnetRange = 0f;      // 0 = off
    [SerializeField] float magnetSpeed = 6f;      // units/sec

    [Header("Lifetime")]
    [SerializeField] float autoDespawnAfter = 20f; // seconds. 0 = never

    // runtime
    Rigidbody2D rb;
    Collider2D col;
    Transform player;            // optional reference for magnet
    bool settled;
    float tAlive;
    float tSinceSpawn;
    IPoolService pool;
    PooledHandle handle;

    // Cache to avoid GC
    static readonly Collider2D[] _overlap = new Collider2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        handle = GetComponent<PooledHandle>();
        rb.useAutoMass = false;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    // ----------------------------------------------------------------------
    // External API
    // ----------------------------------------------------------------------

    /// <summary>Call right after spawning from pool.</summary>
    public void Setup(Vector2 burstVelocity, float burstTorque, Transform playerRef = null)
    {
        player = playerRef;

        // Flying state
        settled = false;
        tAlive = 0f;
        tSinceSpawn = 0f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = burstVelocity;
        rb.angularVelocity = burstTorque;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        col.isTrigger = false; // collide with world while flying
        col.enabled = true;
    }

    /// <summary>Optional: change payload at runtime before spawn.</summary>
    public void SetPayload(string id, int amt)
    {
        lootId = id;
        amount = amt;
    }

    // ----------------------------------------------------------------------
    // Pool callbacks
    // ----------------------------------------------------------------------

    public void OnSpawned()
    {
        // PoolService already set handle.owner and handle.key.
        pool = handle.owner;
        // If spawner forgot to call Setup, ensure a safe default.
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Setup(Vector2.zero, 0f, player);
        }
    }

    public void OnDespawned()
    {
        // Reset lightweight state
        player = null;
        settled = false;
        tAlive = 0f;
        tSinceSpawn = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
    }

    // ----------------------------------------------------------------------
    // Runtime
    // ----------------------------------------------------------------------

    void Update()
    {
        float dt = Time.deltaTime;
        tAlive += dt;
        tSinceSpawn += dt;

        if (!settled)
        {
            if (tSinceSpawn >= settleDelay)
            {
                if (rb.linearVelocity.sqrMagnitude <= settleSpeed * settleSpeed &&
                    Mathf.Abs(rb.angularVelocity) <= settleSpin)
                {
                    Settle();
                }
            }
        }
        else
        {
            // Magnet
            if (magnetRange > 0f)
            {
                Transform target = player ? player : FindNearestPlayer(transform.position, magnetRange, playerMask);
                if (target)
                {
                    Vector3 dir = (target.position - transform.position);
                    float d = dir.magnitude;
                    if (d > 0.001f)
                    {
                        Vector3 step = (dir / d) * magnetSpeed * dt;
                        if (step.sqrMagnitude > dir.sqrMagnitude) step = dir; // clamp
                        transform.position += step;
                    }
                }
            }
        }

        // Lifetime
        if (autoDespawnAfter > 0f && tAlive >= autoDespawnAfter)
            Despawn();
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        // Optional early settle on first ground touch if moving slowly
        if (!settled && rb.linearVelocity.magnitude < settleSpeed * 1.5f)
            Settle();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!settled) return;
        if (tSinceSpawn < pickupDelay) return;
        if (((1 << other.gameObject.layer) & playerMask) == 0) return;

        // Try handoff
        if (other.TryGetComponent<ILootCollector>(out var collector))
        {
            if (collector.TryCollect(lootId, amount))
            {
                Despawn();
                return;
            }
        }

        // If no collector interface, still despawn as a simple pickup.
        Despawn();
    }

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    void Settle()
    {
        settled = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.isTrigger = true; // pickup via trigger
    }

    void Despawn()
    {
        if (pool != null) pool.Despawn(gameObject);
        else gameObject.SetActive(false);
    }

    static Transform FindNearestPlayer(Vector3 pos, float radius, LayerMask mask)
    {
        int n = Physics2D.OverlapCircleNonAlloc(pos, radius, _overlap, mask);
        float bestD2 = float.PositiveInfinity;
        Transform best = null;
        for (int i = 0; i < n; i++)
        {
            var t = _overlap[i].transform;
            float d2 = (t.position - pos).sqrMagnitude;
            if (d2 < bestD2) { bestD2 = d2; best = t; }
        }
        return best;
    }
}
