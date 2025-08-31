using UnityEngine;

public class WeaponRecoil2D : MonoBehaviour
{
    [Header("Kickback")]
    [SerializeField] float kickUnits = 0.1f;      // local-units pushed back per shot
    [SerializeField] float returnSharpness = 16f; // higher = faster return

    [Header("Muzzle rise")]
    [SerializeField] float recoilDeg = 5f;        // degrees per shot
    [SerializeField] float settleSharpness = 14f; // higher = faster return

    Transform pivot;
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    Vector3 posOffset;   // local offset applied to pivot
    float rotOffsetDeg;  // z-rotation offset

    void Awake()
    {
        pivot = transform;
        baseLocalPos = pivot.localPosition;
        baseLocalRot = pivot.localRotation;
    }

    // aimDir = world-space aim direction (normalized or not)
    public void Fire(Vector2 aimDirWS, float strength = 1f)
    {
        // convert world aim to this local space
        Vector3 aimLS = pivot.InverseTransformDirection(new Vector3(aimDirWS.x, aimDirWS.y, 0f)).normalized;

        // kick back along -aim
        posOffset += (-aimLS) * (kickUnits * Mathf.Max(0.0001f, strength));

        // rotate around Z with sign matching aim side
        float sign = Mathf.Sign(Vector3.Cross(Vector3.right, new Vector3(aimDirWS.x, aimDirWS.y, 0f)).z);
        rotOffsetDeg += recoilDeg * strength * sign;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // exponential decay to zero
        float posLerp = 1f - Mathf.Exp(-returnSharpness * dt);
        float rotLerp = 1f - Mathf.Exp(-settleSharpness * dt);

        posOffset = Vector3.Lerp(posOffset, Vector3.zero, posLerp);
        rotOffsetDeg = Mathf.Lerp(rotOffsetDeg, 0f, rotLerp);

        // apply
        pivot.localPosition = baseLocalPos + posOffset;
        pivot.localRotation = baseLocalRot * Quaternion.Euler(0f, 0f, rotOffsetDeg);
    }
}
