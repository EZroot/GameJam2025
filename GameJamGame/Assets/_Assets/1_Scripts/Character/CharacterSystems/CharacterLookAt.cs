using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CharacterLookAt2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera cam;
    [SerializeField] Transform flipRoot;
    [SerializeField] CharacterContext ctx;

    [Header("Aim")]
    [SerializeField] bool aimMode = false;  // toggle this externally when aiming
    [SerializeField] Vector2 lookOffset = new Vector2(0f, 0.5f);
    [SerializeField] float headOffsetDeg = 90f;
    [SerializeField] float armOffsetDeg = 90f;

    [Header("Debug")]
    [SerializeField] bool debugGizmos = true;
    [SerializeField] float gizmoSize = 0.08f;
    [SerializeField] Color gizmoColor = Color.red;
    [SerializeField] float headLookLerp = 10f;
    [SerializeField] float bodyLookLerp = 8f;
    Transform head, body;
    Vector3 lookPointWS;

    public bool IsAiming => aimMode;
    public Vector3 LookPoint => lookPointWS;

    public enum AimFlagType
    {
        None = 0,
        Head = 1 << 0,
    }
    public AimFlagType AimFlags = AimFlagType.Head;

    void Awake()
    {
        if (!flipRoot) flipRoot = transform;
        if (!ctx) ctx = GetComponent<CharacterContext>();

        head = ctx.PaperDoll.Head ? ctx.PaperDoll.Head.transform : null;
        body = ctx.PaperDoll.Body ? ctx.PaperDoll.Body.transform : null;

        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!cam || !aimMode) return;

        //Aim based on flags
        if ((AimFlags & AimFlagType.Head) != 0 && head)
        {
            Aim(head, headOffsetDeg, headLookLerp);
            Aim(body, headOffsetDeg, bodyLookLerp);
        }
    }

    public void SetLookPoint(Vector3 point)
    {
        lookPointWS = point;
    }
    
    public void SetLookPointToMouse()
    {
        lookPointWS = MouseWorld(cam, flipRoot.position.z);
    }

    Vector3 MouseWorld(Camera c, float zPlane)
    {
        Rect r = c.pixelRect;
        Vector3 mp = Input.mousePosition;
        mp.x = Mathf.Clamp(mp.x - r.x, 0f, r.width);
        mp.y = Mathf.Clamp(mp.y - r.y, 0f, r.height);

        float dz = zPlane - c.transform.position.z;
        Vector3 worldPos = c.ScreenToWorldPoint(new Vector3(mp.x + r.x, mp.y + r.y, dz));
        worldPos.z = zPlane;
        return worldPos;
    }


    void Aim(Transform part, float offsetDeg, float lerpSpeed = 10f)
    {
        if (!part) return;

        Vector2 tgtLS = flipRoot.InverseTransformPoint(lookPointWS + (Vector3)lookOffset);
        Vector2 partLS = flipRoot.InverseTransformPoint(part.position);
        Vector2 dir = tgtLS - partLS;
        if (dir.sqrMagnitude < 1e-6f) return;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offsetDeg;
        var e = part.localEulerAngles;
        e.z = Mathf.LerpAngle(e.z, ang, Time.deltaTime * lerpSpeed);
        part.localEulerAngles = e;
    }

    void OnDrawGizmos()
    {
        if (!debugGizmos || !aimMode) return;

        Camera c = cam;
        if (!c) c = Camera.main;
        if (!c) return;

        float z = flipRoot ? flipRoot.position.z : 0f;
        Vector3 p = Application.isPlaying ? lookPointWS : MouseWorld(c, z);

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(p, gizmoSize);

        if (head) Gizmos.DrawLine(head.position, p);
        if(body) Gizmos.DrawLine(body.position, p);
    }

    // public API toggle
    public void SetAimMode(bool state, AimFlagType aimFlag = AimFlagType.Head)
    {
        state = true;
        this.AimFlags = aimFlag;
        aimMode = state;
        if(aimMode == false)
        {
            //Reset rotation
            if (head) head.localEulerAngles = Vector3.zero;
            if (body) body.localEulerAngles = Vector3.zero;
        }
    }
}
