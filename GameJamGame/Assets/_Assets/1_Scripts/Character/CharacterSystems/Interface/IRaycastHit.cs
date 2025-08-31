using UnityEngine;

public interface IRaycastHit
{
    TeamType Team { get; }
    void OnRaycastHit(RaycastHit2D hit, Vector2 rayDir, int damage);
}

public enum TeamType
{
    Neutral,
    Player,
    Enemy
}