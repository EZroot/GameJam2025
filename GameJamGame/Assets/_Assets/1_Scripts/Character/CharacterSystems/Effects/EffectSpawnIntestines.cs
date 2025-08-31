// EffectSpawnIntestines.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EffectSpawnIntestines : MonoBehaviour, IHitEffect, IAbilityEffect
{
    public bool spawnImmediatelyForFun = false;
    [Header("Pool")]
    [SerializeField] PoolKey poolKey = PoolKey.Rope;
    [SerializeField] GameObject fallbackPrefab; 
    [SerializeField] float spawnChance = 0.1f;
    [SerializeField] bool destroyOnLifetimeEnd = false;
    [SerializeField] EffectType effectType = EffectType.Hit;

    [Header("Attach")]
    [SerializeField] string attachBoneName = "Spine";
    [SerializeField] Transform explicitAttach;

    [Header("Intestines & Blood Colors")]
    [SerializeField] Color intestinesColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    [SerializeField] Color bloodColor = new Color(0.35f, 0, 0, 0.75f);
    [SerializeField] float colorIntensity = 1.0f;
    [SerializeField] bool paintBloodByIntestines = true;

    [Header("Rope Setup")]
    [SerializeField] int count = 3;
    [SerializeField] Vector2 segmentsRange = new Vector2(12, 22);
    [SerializeField] float segmentLength = 0.08f;
    [SerializeField] int solverIterations = 4;
    [SerializeField] float slack = 0.05f;
    [SerializeField] float breakStretch = 10f;
    [SerializeField] float widthOverride = 0.05f;
    [SerializeField] bool uniformWidth = true;

    [Header("Gravity")]
    [SerializeField] public Vector2 gravity = new Vector2(0, -9.81f);
    [SerializeField, Range(0.90f, 0.999f)] public float damping = 0.985f;
    [SerializeField] public bool useCircularGravity = false;

    [Header("Flow")]
    [SerializeField] public Vector2 gravityFlowInfluencer = Vector2.zero; // world-space wind accel

    [Header("Lifecycle")]
    [SerializeField] bool onlyOnKill = true;
    [SerializeField] float minLifetime = 2.5f;
    [SerializeField] float maxLifetime = 5.0f;
    [SerializeField] bool detachOnLifetimeEnd = true;
    [SerializeField] float fadeOut = 0.35f;

    [Header("Kick")]
    [SerializeField] float impulse = 0.25f;
    [SerializeField] float impulseVariance = 0.15f;

    [Header("Ability Trigger")]
    [SerializeField] bool triggerOnAbility = false;
    [SerializeField] string listenKey = "speed_proc";

    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock _mpb;
    public EffectType EffectType => effectType;

    // Cache ropes
    // For game jam cuz fk it
    [Header("Rope Damage Effects")]
    public bool DoDamageOnRopeEnds = false;
    public int RopeEndDamage = 10;
    public bool shouldPaintOnTryHit = false;
    public Color RopeOnTryHitColor = Color.blue;
    public int TryHitColorRadius = 2;
    public Color RopeOnDidHitColor = Color.blueViolet;
    public int DidHitColorRadius = 4;
    public float RopeDamageCooldown = 0.5f;
    public float RopeDamageRadius = 0.25f;
    private float _ropeDamageCooldownCounter = 0f;

    List<RopeIntestine2D> _ropeCollection = new List<RopeIntestine2D>();

    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        if(spawnImmediatelyForFun)
        {
            SpawnIntestinesBatch(FindAnyObjectByType<CharacterContext>(), null);
        }
    }


    public float changeTimer = 0.25f;
    public float changeCounter = 0f;
    float flipper = 1f;

    void Update()
    {
        Debug.Log("SCALE: "+(1f + transform.localScale.magnitude));

        if (!DoDamageOnRopeEnds || spawnImmediatelyForFun) return;
        var mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
        //changeCounter += Time.deltaTime;
        //if (changeCounter > changeTimer)
        //{
        //    changeCounter = 0f;
        //    flipper *= -1f;
        //}

        foreach (var rope in _ropeCollection)
        {

            rope.gravity = ((rope.transform.position - mousePos) * flipper).normalized * 10f;
        }

        _ropeDamageCooldownCounter -= Time.deltaTime;
        if(_ropeDamageCooldownCounter > 0f) return;
        _ropeDamageCooldownCounter = RopeDamageCooldown;

        // Middle of player hit
        var hits = Physics2D.OverlapCircleAll(transform.position, transform.localScale.magnitude + RopeDamageRadius * 2, LayerMask.GetMask("Character"));
        foreach (var hit in hits)
        {
            var raycastHit = hit.GetComponent<IRaycastHit>();
            var dir = (hit.transform.position - transform.position);
            if (raycastHit != null)
            {
                raycastHit.OnRaycastHit(new RaycastHit2D { point = transform.position, normal = -dir.normalized }, dir, RopeEndDamage);
                Service.Get<IPaintSurface2D>().PaintCircle(hit.transform.position, DidHitColorRadius + Mathf.FloorToInt(transform.localScale.magnitude + RopeDamageRadius * 2), RopeOnDidHitColor);
            }
        }

        //// Rope end hits
        //foreach (var rope in _ropeCollection)
        //{
        //    var didHit = false;
        //    var position = rope.RopeEndPosition;
        //    var h = Physics2D.OverlapCircleAll(position, RopeDamageRadius, LayerMask.GetMask("Character"));
        //    foreach(var hit in h)
        //    {
        //        var raycastHit = hit.GetComponent<IRaycastHit>();
        //        var dir = (position - (Vector2)transform.position);
        //        if (raycastHit != null)
        //        {
        //            raycastHit.OnRaycastHit(new RaycastHit2D { point = position, normal = -dir.normalized }, dir, RopeEndDamage);
        //            Service.Get<IPaintSurface2D>().PaintCircle(position, DidHitColorRadius, RopeOnDidHitColor);
        //            didHit = true;
        //        }
        //    } 
        //    if (!didHit && shouldPaintOnTryHit)
        //    {
        //        Service.Get<IPaintSurface2D>().PaintCircle(position, TryHitColorRadius, RopeOnTryHitColor);
        //    }
        //}

    }


    public void OnHit(CharacterContext ctx, in HitInfo info)
    {
        if (onlyOnKill && ctx.CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() > 0) return;
        if (Random.value > spawnChance) return;
        SpawnIntestinesBatch(ctx, info.normal);
    }

    public void OnAbility(CharacterContext ctx, string key)
    {
        if (!triggerOnAbility || key != listenKey) return;
        Debug.Log("[EffectSpawnIntestines] OnAbility!!");
        SpawnIntestinesBatch(ctx, null);
    }

    void SpawnIntestinesBatch(CharacterContext ctx, Vector2? hitNormal)
    {
        Transform attach = explicitAttach ? explicitAttach : FindChildByName(ctx.transform, attachBoneName) ?? ctx.transform;
        var _pools = Service.Get<IPoolService>();
        float step = Mathf.PI * 2f / count;
        float baseG = gravity.sqrMagnitude > 0f ? gravity.magnitude : 9.81f;

        for (int i = 0; i < count; i++)
        {
            var go = _pools.Spawn(poolKey, ctx.transform.position, Quaternion.identity);
            if (!go) continue;

            float angle = -i * step;
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var rope = go.GetComponent<RopeIntestine2D>();
            if (!rope) rope = go.AddComponent<RopeIntestine2D>();

            _ropeCollection.Add(rope);
            // For sorting group purposes we do this
            rope.transform.SetParent(attach);

            rope.Attach(attach);
            rope.SetSegmentCount(Random.Range((int)segmentsRange.x, (int)segmentsRange.y + 1));
            rope.bloodColor = bloodColor;
            rope.paintBlood = paintBloodByIntestines;
            rope.enabled = true;

            var lr = rope.GetComponent<LineRenderer>();
            lr.startWidth = widthOverride * (1f + transform.localScale.magnitude);
            lr.endWidth = uniformWidth ? widthOverride * (1f + transform.localScale.magnitude) : 0f;
            lr.startColor = intestinesColor;
            lr.endColor = intestinesColor;
            lr.widthMultiplier = Random.Range(0.035f, 0.06f);
            lr.positionCount = Mathf.Max(lr.positionCount, 2);

            _mpb ??= new MaterialPropertyBlock();
            lr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ID_Color, Color.white * colorIntensity);
            lr.SetPropertyBlock(_mpb);

            Vector2 dir = (hitNormal.HasValue && hitNormal.Value != Vector2.zero)
                ? -hitNormal.Value
                : Random.insideUnitCircle.normalized;
            Vector2 jitter = Random.insideUnitCircle * 0.5f;
            float mag = Mathf.Max(0f, impulse + Random.Range(-impulseVariance, impulseVariance));
            rope.Nudge((dir + jitter * 0.25f).normalized * mag);

            // Gravity with wind bias
            Vector2 gVec = useCircularGravity ? (outward * baseG) : gravity;
            rope.gravity = gVec + gravityFlowInfluencer;

            rope.slack = slack;
            rope.damping = damping;
            rope.breakStretch = breakStretch;
            rope.segmentLength = segmentLength * (1f + transform.localScale.magnitude);
            rope.segmentCount = Mathf.Clamp(lr.positionCount, 2, 256);
            rope.solverIterations = solverIterations;

            StartCoroutine(CoAutoDespawn(go, rope, Random.Range(minLifetime, maxLifetime)));
        }
    }

    IEnumerator CoAutoDespawn(GameObject go, RopeIntestine2D rope, float life)
    {
        yield return new WaitForSeconds(life);
        if (detachOnLifetimeEnd && rope) rope.Detach();
        if(destroyOnLifetimeEnd)
        {
            rope.transform.SetParent(null);
            _ropeCollection.Remove(rope);
            var pool = Service.Get<IPoolService>();
            pool.Despawn(go);
        }
    }

    static Transform FindChildByName(Transform root, string name)
    {
        if (string.IsNullOrEmpty(name) || !root) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
