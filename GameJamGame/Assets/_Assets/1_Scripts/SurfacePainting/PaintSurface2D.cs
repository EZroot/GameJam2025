// PaintSurface2D.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Attach to a GameObject with a SpriteRenderer. Scale it to cover your ground.
/// This creates a writable Texture2D you can paint pixels onto at runtime.
[RequireComponent(typeof(SpriteRenderer))]
public class PaintSurface2D : MonoBehaviour, IPaintSurface2D
{
    public BrushPainter m_brushPainter;
    [Header("Canvas")]
    public int pixelsWidth = 2048;
    public int pixelsHeight = 2048;
    public float worldWidth = 32f;   // world units covered by the canvas
    public float worldHeight = 32f;

    [Header("Texture")]
    public TextureFormat texFormat = TextureFormat.RGBA32;
    public FilterMode filter = FilterMode.Point;
    public Color clearColor = new Color(0, 0, 0, 0);

    Texture2D _tex;
    SpriteRenderer _sr;
    Color32[] _clearRow;
    float _pxPerUnitX, _pxPerUnitY;  // pixels per world unit

    public Texture2D Texture => _tex;
    public BrushPainter BrushPainter => m_brushPainter;
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        _tex = new Texture2D(pixelsWidth, pixelsHeight, texFormat, false, false);
        _tex.filterMode = filter;
        _tex.wrapMode = TextureWrapMode.Clamp;

        // Clear
        _clearRow = new Color32[pixelsWidth];
        for (int x = 0; x < pixelsWidth; x++) _clearRow[x] = clearColor;
        for (int y = 0; y < pixelsHeight; y++) _tex.SetPixels32(0, y, pixelsWidth, 1, _clearRow);
        _tex.Apply(false);

        // Create sprite covering worldWidth x worldHeight
        var pivot = new Vector2(0.5f, 0.5f);
        var rect = new Rect(0, 0, pixelsWidth, pixelsHeight);
        var pixelsPerUnit = pixelsWidth / worldWidth; // maintain aspect via width
        var sprite = Sprite.Create(_tex, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);

        _sr.sprite = sprite;
        transform.localScale = Vector3.one; // sprite pixel density controls size

        _pxPerUnitX = pixelsWidth / worldWidth;
        _pxPerUnitY = pixelsHeight / worldHeight;
    }

    // World → pixel coordinates within texture. Returns false if outside.
    public bool WorldToPixel(Vector3 world, out int px, out int py)
    {
        // Transform world to local coordinates of this surface
        var local = transform.InverseTransformPoint(world);
        // Map local XY in [-worldW/2,+worldW/2] to [0 .. pixels]
        float lx = local.x + worldWidth * 0.5f;
        float ly = local.y + worldHeight * 0.5f;

        px = Mathf.FloorToInt(lx * _pxPerUnitX);
        py = Mathf.FloorToInt(ly * _pxPerUnitY);

        return px >= 0 && px < pixelsWidth && py >= 0 && py < pixelsHeight;
    }

    // Solid circle brush
    public void PaintCircle(Vector3 worldCenter, int radiusPx, Color32 color)
    {
        if (!WorldToPixel(worldCenter, out int cx, out int cy)) return;

        int r2 = radiusPx * radiusPx;
        int minX = Mathf.Max(cx - radiusPx, 0);
        int maxX = Mathf.Min(cx + radiusPx, pixelsWidth - 1);
        int minY = Mathf.Max(cy - radiusPx, 0);
        int maxY = Mathf.Min(cy + radiusPx, pixelsHeight - 1);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 <= r2)
                    _tex.SetPixel(x, y, color);
            }
        }
        _tex.Apply(false);
    }

    // Bresenham line with circular stamp
    public void PaintLine(Vector3 worldA, Vector3 worldB, int radiusPx, Color32 color)
    {
        if (!WorldToPixel(worldA, out int x0, out int y0)) return;
        if (!WorldToPixel(worldB, out int x1, out int y1)) return;

        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            PaintCirclePixel(x0, y0, radiusPx, color, apply: false);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        _tex.Apply(false);
    }

    void PaintCirclePixel(int cx, int cy, int r, Color32 c, bool apply)
    {
        int r2 = r * r;
        int minX = Mathf.Max(cx - r, 0);
        int maxX = Mathf.Min(cx + r, pixelsWidth - 1);
        int minY = Mathf.Max(cy - r, 0);
        int maxY = Mathf.Min(cy + r, pixelsHeight - 1);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cx; // bug guard
        }

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int dy2 = dy * dy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy2 <= r2)
                    _tex.SetPixel(x, y, c);
            }
        }
        if (apply) _tex.Apply(false);
    }

    // Optional: erase by painting transparent
    public void EraseCircle(Vector3 worldCenter, int radiusPx) =>
        PaintCircle(worldCenter, radiusPx, new Color32(0, 0, 0, 0));

    // --- Gizmos ---
    [Header("Gizmos")]
    public bool gizmoShow = true;
    public bool gizmoShowPixelGrid = false;   // off unless your texture is small
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.9f);
    [Range(0.01f, 1f)] public float gizmoGridStridePx = 16f; // draw every N pixels

    void OnDrawGizmos()
    {
        if (!gizmoShow) return;

        // Draw in local space so size tracks worldWidth/worldHeight and rotation
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Outline
        Gizmos.color = gizmoColor;
        Vector3 half = new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, 0f);

        Vector3 bl = new Vector3(-half.x, -half.y, 0f);
        Vector3 tl = new Vector3(-half.x, half.y, 0f);
        Vector3 br = new Vector3(half.x, -half.y, 0f);
        Vector3 tr = new Vector3(half.x, half.y, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        // Pixel grid (optional, throttled by gizmoGridStridePx)
        if (gizmoShowPixelGrid && pixelsWidth > 0 && pixelsHeight > 0)
        {
            float stepX = worldWidth / pixelsWidth;
            float stepY = worldHeight / pixelsHeight;

            int strideX = Mathf.Max(1, Mathf.RoundToInt(gizmoGridStridePx));
            int strideY = Mathf.Max(1, Mathf.RoundToInt(gizmoGridStridePx));

            Color gridC = gizmoColor; gridC.a *= 0.35f;
            Gizmos.color = gridC;

            // Vertical lines
            for (int px = 0; px <= pixelsWidth; px += strideX)
            {
                float x = -half.x + px * stepX;
                Gizmos.DrawLine(new Vector3(x, -half.y, 0f), new Vector3(x, half.y, 0f));
            }
            // Horizontal lines
            for (int py = 0; py <= pixelsHeight; py += strideY)
            {
                float y = -half.y + py * stepY;
                Gizmos.DrawLine(new Vector3(-half.x, y, 0f), new Vector3(half.x, y, 0f));
            }
        }

        Gizmos.matrix = prev;
    }

}
