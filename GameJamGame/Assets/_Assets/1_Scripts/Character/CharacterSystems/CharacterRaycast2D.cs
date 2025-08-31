using UnityEngine;

public class CharacterRaycast2D : MonoBehaviour
{
    CharacterContext _ctx;
    private void Start()
    {
        _ctx = GetComponent<CharacterContext>();
    }

    // try get a 2d raycast hit and output the hit2d information
    public void RaycastHit2D(Vector2 origin, Vector2 direction, float distance, LayerMask layerMask)
    {
        var ragdollMask = LayerMask.GetMask("Ragdoll");
        int combinedMask = layerMask.value | ragdollMask;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, combinedMask);

        //if we hit ourselves, retry with ray origin moved outside of the collider
        if (hit.collider && hit.collider.transform.IsChildOf(transform))
        {
            Vector2 offset = hit.normal * 0.1f; // Move the origin slightly away from the collider
            hit = Physics2D.Raycast(origin + offset, direction, distance, combinedMask);
        }

        if (!hit) return;

        Debug.DrawLine(origin, origin + direction * distance, Color.red, 2f);

        var raycastHit = hit.collider.GetComponent<IRaycastHit>();
        if(raycastHit != null)
            raycastHit.OnRaycastHit(hit, direction, _ctx.CharacterSkills.GetSkill(SkillType.Combat).GetCurrentWorkValue());

        // If the hit is on a ragdoll layer, apply knockback
        if (IsInLayerMask(hit.collider.gameObject.layer, ragdollMask))
        {
            // Prefer attachedRigidbody; falls back to GetComponent if needed
            Rigidbody2D rb = hit.rigidbody ? hit.rigidbody : hit.collider.attachedRigidbody;
            if (!rb) rb = hit.collider.GetComponent<Rigidbody2D>();

            if (rb)
            {
                // Apply impulse at the contact point for nicer spin
                rb.AddForceAtPosition(direction * 2.5f, hit.point, ForceMode2D.Impulse);
            }
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
