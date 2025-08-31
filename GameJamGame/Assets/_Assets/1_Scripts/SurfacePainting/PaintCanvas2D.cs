using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class PaintCanvas2D : MonoBehaviour
{
    [Header("Canvas RT")]
    [SerializeField] int width = 2048;
    [SerializeField] int height = 2048;
    [SerializeField] RenderTextureFormat format = RenderTextureFormat.ARGB32;

    [Header("Brush")]
    [SerializeField] float brushSizePx = 64f;   // diameter in pixels
    [SerializeField] float hardness = 0.7f;     // softer edge < 1
    [SerializeField] float flow = 1.0f;         // 0..1
    [SerializeField] Color brushColor = new Color(0.8f, 0.0f, 0.0f, 0.9f);
    [SerializeField] Texture2D brushTex;        // optional grayscale alpha
    [SerializeField] float spacing = 0.25f;     // fraction of size per step

    RenderTexture _rt;
    Material _brushMat;
    RawImage _img;
    RectTransform _rtf;

    static readonly int ID_Center = Shader.PropertyToID("_Center");
    static readonly int ID_Radius = Shader.PropertyToID("_Radius");
    static readonly int ID_Hardness = Shader.PropertyToID("_Hardness");
    static readonly int ID_Flow = Shader.PropertyToID("_Flow");
    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_Eraser = Shader.PropertyToID("_Eraser");
    static readonly int ID_UseBrush = Shader.PropertyToID("_UseBrushTex");
    static readonly int ID_BrushTex = Shader.PropertyToID("_BrushTex");

    Vector2 _lastUV;
    bool _hasLast;

    void Awake()
    {
        _img = GetComponent<RawImage>();
        _rtf = _img.rectTransform;

        _rt = new RenderTexture(width, height, 0, format)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _rt.Create();

        // clear to transparent
        var active = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = active;

        _img.texture = _rt; // show the canvas

        _brushMat = new Material(Shader.Find("Blit/PaintBrush2D"));
        _brushMat.SetFloat(ID_Hardness, Mathf.Clamp01(hardness));
        _brushMat.SetFloat(ID_Flow, Mathf.Clamp01(flow));
        _brushMat.SetColor(ID_Color, brushColor);
        _brushMat.SetFloat(ID_UseBrush, brushTex ? 1f : 0f);
        if (brushTex) _brushMat.SetTexture(ID_BrushTex, brushTex);
    }

    void Update()
    {
        // LMB draw, RMB erase
        bool paint = Input.GetMouseButton(0);
        bool erase = Input.GetMouseButton(1);
        if (!paint && !erase) { _hasLast = false; return; }

        if (!TryScreenToUV(Input.mousePosition, out var uv)) { _hasLast = false; return; }

        // set per-stamp params
        _brushMat.SetFloat(ID_Eraser, erase ? 1f : 0f);
        _brushMat.SetFloat(ID_Hardness, Mathf.Clamp01(hardness));
        _brushMat.SetFloat(ID_Flow, Mathf.Clamp01(flow));
        _brushMat.SetColor(ID_Color, brushColor);

        float radiusUV = (brushSizePx * 0.5f) / Mathf.Max(_rt.width, _rt.height);

        if (!_hasLast)
        {
            Stamp(uv, radiusUV);
            _lastUV = uv;
            _hasLast = true;
            return;
        }

        // interpolate along the drag path at fixed spacing
        float pxSpacing = Mathf.Max(1f, brushSizePx * Mathf.Clamp01(spacing));
        float uvSpacing = pxSpacing / Mathf.Max(_rt.width, _rt.height);

        Vector2 delta = uv - _lastUV;
        float dist = delta.magnitude;
        int steps = Mathf.FloorToInt(dist / uvSpacing);

        for (int i = 1; i <= Mathf.Max(1, steps); i++)
        {
            Vector2 p = Vector2.Lerp(_lastUV, uv, i / (float)(steps + 1));
            Stamp(p, radiusUV);
        }

        _lastUV = uv;
    }

    void Stamp(Vector2 uv, float radiusUV)
    {
        _brushMat.SetVector(ID_Center, uv);
        _brushMat.SetFloat(ID_Radius, Mathf.Max(1f / Mathf.Max(_rt.width, _rt.height), radiusUV));
        Graphics.Blit(_rt, _rt, _brushMat, 0);
    }

    bool TryScreenToUV(Vector2 screen, out Vector2 uv)
    {
        uv = default;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rtf, screen, null, out var local))
            return false;

        var rect = _rtf.rect; // local space
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        if (u < 0 || u > 1 || v < 0 || v > 1) return false;

        uv = new Vector2(u, v);
        return true;
    }

    // API
    public void SetBrushColor(Color c) => brushColor = c;
    public void SetBrushSizePx(float px) => brushSizePx = Mathf.Max(1f, px);
    public void ClearCanvas(Color? clear = null)
    {
        var active = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(false, true, clear ?? Color.clear);
        RenderTexture.active = active;
    }
}
