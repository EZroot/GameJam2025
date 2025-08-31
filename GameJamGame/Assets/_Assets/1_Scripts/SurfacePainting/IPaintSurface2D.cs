using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public interface IPaintSurface2D : IService
{
    BrushPainter BrushPainter { get; }
    bool WorldToPixel(Vector3 world, out int px, out int py);
    void PaintCircle(Vector3 worldCenter, int radiusPx, Color32 color);
    void PaintLine(Vector3 worldA, Vector3 worldB, int radiusPx, Color32 color);
    void EraseCircle(Vector3 worldCenter, int radiusPx);

}