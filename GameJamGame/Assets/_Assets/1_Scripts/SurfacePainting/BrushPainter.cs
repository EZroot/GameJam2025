// BrushPainter.cs
using UnityEngine;

/// Simple mouse input to test painting in Scene/Game view.
/// Put this on any GameObject, assign the PaintSurface2D reference.
public class BrushPainter : MonoBehaviour
{
    public PaintSurface2D surface;
    public int brushRadiusPx = 6;
    public Color brushColor = Color.red;
    public bool drawLines = true;

    Vector3 _prevWorld = Vector3.zero;
    bool _hadPrev;

    /// <summary>
    /// Paint a single step (circle) at the current mouse position.
    /// Will draw a line if the position veers too far from the last painted position.
    /// </summary>
    public void PaintStep(Vector3 position, Color color, int brushRadiusPx = 1)
    {
        if (!surface) return;
        if (drawLines && _hadPrev && Vector2.Distance(_prevWorld, position) < brushRadiusPx) surface.PaintLine(_prevWorld, position, brushRadiusPx, color);
        else surface.PaintCircle(position, brushRadiusPx, color);
            surface.PaintCircle(position, brushRadiusPx, color); // ensure no gaps
        _prevWorld = position; _hadPrev = true;
    }

    //void Update()
    //{
    //    if (!_cam || !surface) return;
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        var w = ScreenToWorld();
    //        surface.PaintCircle(w, brushRadiusPx, brushColor);
    //        _prevWorld = w; _hadPrev = true;
    //    }
    //    else if (Input.GetMouseButton(0))
    //    {
    //        var w = ScreenToWorld();
    //        if (drawLines && _hadPrev) surface.PaintLine(_prevWorld, w, brushRadiusPx, brushColor);
    //        else surface.PaintCircle(w, brushRadiusPx, brushColor);
    //        _prevWorld = w; _hadPrev = true;
    //    }
    //    if (Input.GetMouseButtonUp(0)) _hadPrev = false;

    //    if (Input.GetMouseButton(1)) // RMB = erase
    //    {
    //        var w = ScreenToWorld();
    //        surface.EraseCircle(w, brushRadiusPx);
    //    }
    //}

    //Vector3 ScreenToWorld()
    //{
    //    var m = Input.mousePosition;
    //    m.z = Mathf.Abs(_cam.transform.position.z); // 2D camera looking at XY plane
    //    return _cam.ScreenToWorldPoint(m);
    //}
}
