using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] Transform target;
    [SerializeField] float followSmoothTime = 0.08f;

    [Header("Deadzone (world units)")]
    [SerializeField] Vector2 deadzoneSize = new Vector2(3f, 2f);

    [Header("Mouse Lookahead")]
    [SerializeField] float lookaheadMax = 2.0f;
    [SerializeField] float lookaheadSmoothTime = 0.06f;
    [SerializeField] bool lookaheadOnlyWhenMoving = false;

    [Header("Pixel Snapping")]
    [SerializeField] bool snapToPixelGrid = true;
    [SerializeField] int pixelsPerUnit = 64;

    [Header("Shake v2 (trauma + impulse)")]
    [SerializeField] float posPixels = 3f;          // base translation amplitude in pixels
    [SerializeField] float rotDegrees = 6f;         // base Z-rot amplitude in degrees
    [SerializeField] float noiseFrequency = 28f;    // hops/sec (band-limited)
    [SerializeField] float traumaDecayPerSecond = 1.8f;
    [SerializeField] float impulseDecayPerSecond = 8f;
    [SerializeField] bool enableRotation = false;   // disable if using pixel-perfect rotation-sensitive setup
    [SerializeField] bool useUnscaledTime = true;

    // ---- runtime ----
    [SerializeField] Camera _cam;
    [SerializeField] PixelPerfectCamera _pixelPerfectCamera;
    int _pixelPPUOrigin;
    Coroutine _zoomRoutine = null;
    Vector3 _velocity;
    Vector2 _lookVel;
    Vector2 _lookaheadCurrent;
    float _zLock;

    // trauma noise seeds
    int _seedX, _seedY, _seedR;
    float _t;                 // time accumulator
    float _trauma;            // 0..1
    Vector2 _impulse;         // world-units kick that damps out
    float _rotImpulse;        // degrees kick that damps out

    public void SetCameraTarget(Transform target)
    {
        this.target = target;
    }

    void Awake()
    {
        _pixelPPUOrigin = _pixelPerfectCamera.assetsPPU;

        if (!_cam) _cam = Camera.main;
        _zLock = transform.position.z;

        _seedX = Random.Range(1, 1 << 20);
        _seedY = Random.Range(1, 1 << 20);
        _seedR = Random.Range(1, 1 << 20);
    }

    void FixedUpdate()
    {
        if (!target) return;

        // 1) Keep target inside deadzone
        Vector3 camPos = transform.position;
        Vector3 targ = target.position;

        Vector2 delta = Vector2.zero;
        Vector2 diff = (Vector2)(targ - camPos);
        Vector2 half = deadzoneSize * 0.5f;

        if (diff.x > half.x) delta.x = diff.x - half.x;
        if (diff.x < -half.x) delta.x = diff.x + half.x;
        if (diff.y > half.y) delta.y = diff.y - half.y;
        if (diff.y < -half.y) delta.y = diff.y + half.y;

        Vector3 desired = camPos + (Vector3)delta;

        // 2) Mouse lookahead
        Vector2 lookTarget = Vector2.zero;
        if (_cam)
        {
            Vector3 mouseWS = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 toMouse = (Vector2)(mouseWS - targ);
            if (lookaheadOnlyWhenMoving)
            {
                if (delta.sqrMagnitude > 0.0001f)
                    lookTarget = Vector2.ClampMagnitude(toMouse, lookaheadMax);
            }
            else
            {
                lookTarget = Vector2.ClampMagnitude(toMouse, lookaheadMax);
            }
        }

        _lookaheadCurrent = Vector2.SmoothDamp(_lookaheadCurrent, lookTarget, ref _lookVel, lookaheadSmoothTime);
        desired += (Vector3)_lookaheadCurrent;

        // 3) Smooth follow
        camPos = Vector3.SmoothDamp(camPos, desired, ref _velocity, followSmoothTime);

        // 4) Shake v2
        Vector3 shakePos; float shakeRot;
        ComputeShake(out shakePos, out shakeRot);

        // 5) Apply
        Vector3 finalPos = camPos + shakePos;
        finalPos.z = _zLock;

        if (snapToPixelGrid && pixelsPerUnit > 0)
            finalPos = SnapToPixel(finalPos, pixelsPerUnit);

        transform.position = finalPos;

        if (enableRotation)
            transform.rotation = Quaternion.Euler(0f, 0f, shakeRot);
    }

    // ---- Camera Zoom ----
    public void ZoomToPPU(int PPU)
    {
        if(_zoomRoutine != null) StopCoroutine(_zoomRoutine);
        _zoomRoutine = StartCoroutine(CoZoomCameraToPPU(PPU));
    }

    public int PPU => _pixelPerfectCamera.assetsPPU;

    IEnumerator CoZoomCameraToPPU(int PPU)
    {
        if (!_pixelPerfectCamera) yield break;
        int startPPU = _pixelPerfectCamera.assetsPPU;
        float duration = 0.3f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            u = u * u * (3f - 2f * u); // smoothstep
            int ppu = Mathf.RoundToInt(Mathf.Lerp(startPPU, PPU, u));
            _pixelPerfectCamera.assetsPPU = ppu;
            yield return null;
        }
        _pixelPerfectCamera.assetsPPU = PPU;
    }

    // ---- Shake API ----

    /// Add non-directional shake energy. 0.3 small, 0.6 medium, 1.0 big.
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
    }

    // CameraController
    public void SetTrauma01(float value) => _trauma = Mathf.Max(_trauma, Mathf.Clamp01(value));


    /// Add a directional kick in world space plus optional roll.
    /// pixels: translation magnitude in screen pixels
    /// rot: degrees around Z
    public void Kick(Vector2 worldDir, float pixels = 2f, float rot = 0f)
    {
        float wu = PixelsToWorld(pixels);
        if (wu > 0f && worldDir.sqrMagnitude > 0f)
            _impulse += worldDir.normalized * wu;
        _rotImpulse += rot;
    }

    /// Backward-compat wrapper for your old call.
    public void Shake(float pixels = 3, float duration = 0.15f, float frequency = 3f)
    {
        // Map to trauma + optional short kick.
        if (pixels > 0f) Kick(Random.insideUnitCircle.normalized, pixels * 0.5f, 0f);
        AddTrauma(Mathf.Clamp01(duration > 0f ? duration : 0.18f));
        if (frequency > 0f) noiseFrequency = frequency;
    }

    // ---- internals ----

    void ComputeShake(out Vector3 pos, out float rotDeg)
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _t += dt;

        // decay trauma
        if (_trauma > 0f)
        {
            _trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * dt);
        }

        // decay impulses (critically damped-ish)
        float impulseDecay = Mathf.Exp(-impulseDecayPerSecond * dt);
        _impulse *= impulseDecay;
        _rotImpulse *= impulseDecay;

        // intensity curve: square gives strong start, smooth tail
        float s = _trauma * _trauma;

        // band-limited value noise (no Perlin crawl)
        float nxf = SmoothValueNoise(_t, noiseFrequency, _seedX);
        float nyf = SmoothValueNoise(_t + 17.13f, noiseFrequency * 0.97f, _seedY);
        float nrf = SmoothValueNoise(_t + 31.7f, noiseFrequency * 0.91f, _seedR);

        float posWU = PixelsToWorld(posPixels);
        Vector2 noisePos = new Vector2(nxf, nyf) * posWU * s;
        float noiseRot = nrf * rotDegrees * s;

        pos = new Vector3(noisePos.x, noisePos.y, 0f) + new Vector3(_impulse.x, _impulse.y, 0f);
        rotDeg = noiseRot + _rotImpulse;
    }

    static float SmoothValueNoise(float t, float freq, int seed)
    {
        // hop between random values at 'freq' with smoothstep interpolation
        float tf = t * Mathf.Max(0.0001f, freq);
        int k = Mathf.FloorToInt(tf);
        float a = Hash01(k + seed);
        float b = Hash01(k + 1 + seed);
        float u = tf - k;
        u = u * u * (3f - 2f * u); // smoothstep
        return Mathf.Lerp(a, b, u) * 2f - 1f; // [-1,1]
    }

    static float Hash01(int n)
    {
        // simple, stable integer hash → [0,1)
        unchecked
        {
            n = (n ^ 61) ^ (n >> 16);
            n = n + (n << 3);
            n = n ^ (n >> 4);
            n = n * 0x27d4eb2d;
            n = n ^ (n >> 15);
        }
        // convert to [0,1)
        return (n & 0x7fffffff) / 2147483648f;
    }

    float PixelsToWorld(float pixels)
    {
        return Mathf.Max(0f, pixels) / Mathf.Max(1, pixelsPerUnit);
    }

    static Vector3 SnapToPixel(Vector3 worldPos, int ppu)
    {
        float step = 1f / Mathf.Max(1, ppu);
        worldPos.x = Mathf.Round(worldPos.x / step) * step;
        worldPos.y = Mathf.Round(worldPos.y / step) * step;
        return worldPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 c = Application.isPlaying ? (Vector3)transform.position : transform.position;
        Gizmos.DrawWireCube(c, new Vector3(deadzoneSize.x, deadzoneSize.y, 0f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(c, lookaheadMax);
    }
#endif
}
