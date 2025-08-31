using UnityEngine;

public interface ICharacterAnimation
{
    void Step(float dt, Vector2 move, float speed01, bool aiming);
    void ArmSwingAnimation(bool isMoving, float speed01);
    void WalkingAnimation(bool isMoving, float speed01);
    void HeadBobAnimation(bool isMoving, float speed01);
    void SetArmsEnabled(bool enabled);
    void ResetPose();
}

public class CharacterAnimation : MonoBehaviour, ICharacterAnimation
{
    private CharacterContext m_charContext;
    private CharacterMotor2D m_charMotor;
    private CharacterLookAt2D m_charLookAt;

    [Header("Arm Swing")]
    [SerializeField] float swingAmplitude = 25f;
    [SerializeField] float swingSpeed = 6f;
    [SerializeField] float moveThreshold = 0.01f;

    [Header("Smoothing")]
    [SerializeField] float amplitudeEase = 10f;
    [SerializeField] float dampTimeMoving = 0.06f;
    [SerializeField] float dampTimeIdle = 0.12f;

    [Header("Arm Offsets")]
    [SerializeField] float leftArmOffset = 0f;
    [SerializeField] float rightArmOffset = 0f;

    [Header("Leg Swing (two sprites)")]
    [SerializeField] Transform leftLeg;
    [SerializeField] Transform rightLeg;
    [SerializeField] float legAmplitude = 20f;
    [SerializeField] float leftLegAmplitudeMul = 1f;
    [SerializeField] float rightLegAmplitudeMul = 1f;
    [SerializeField] float legSpeedMul = 1.0f;
    [SerializeField] float leftLegOffsetDeg = 0f;
    [SerializeField] float rightLegOffsetDeg = 0f;
    [SerializeField] float leftLegDampTime = 0.06f;
    [SerializeField] float rightLegDampTime = 0.06f;

    [Header("Optional Body Bob")]
    [SerializeField] Transform bobTarget;
    [SerializeField] float bobHeight = 0.03f;
    [SerializeField] float bobSpeedMul = 2.0f;
    [SerializeField] float bobEase = 10f;
    [SerializeField] float bobDampTime = 0.08f;

    [Header("Control")]
    public bool UseExternalDrive = true;

    Transform leftArm, rightArm;

    float baseLeftArmZ, baseRightArmZ;
    float baseLeftLegZ, baseRightLegZ;

    float swingPhase;
    float currentArmAmplitude;
    float currentBobAmt;

    float velLeftArmZ, velRightArmZ;
    float velLeftLegZ, velRightLegZ;
    float bobVel;

    float bobBaseY;

    bool armsEnabled = true;

    // phase update guard
    int phaseUpdatedFrame = -1;

    void Start()
    {
        m_charContext = GetComponent<CharacterContext>();
        if (!m_charContext) { Debug.LogError("CharacterContext missing"); enabled = false; return; }

        m_charMotor = m_charContext.CharacterMotor2D;
        m_charLookAt = m_charContext.CharacterLookAt2D;

        if (leftArm) baseLeftArmZ = leftArm.localEulerAngles.z;
        if (rightArm) baseRightArmZ = rightArm.localEulerAngles.z;

        if (leftLeg) baseLeftLegZ = leftLeg.localEulerAngles.z;
        if (rightLeg) baseRightLegZ = rightLeg.localEulerAngles.z;

        if (bobTarget) bobBaseY = bobTarget.localPosition.y;
    }

    void LateUpdate()
    {
        if (UseExternalDrive) return;
        if (!m_charMotor) return;

        var move = m_charMotor.MoveInput;
        var speed01 = m_charMotor.Speed01;
        var aiming = m_charLookAt && m_charLookAt.IsAiming;
        Step(Time.deltaTime, move, speed01, aiming);
    }

    // Advance shared phase once per frame regardless of which API is called.
    void UpdatePhaseOnce(float speed01)
    {
        int f = Time.frameCount;
        if (phaseUpdatedFrame == f) return;
        float freq = Mathf.Max(0f, swingSpeed) * Mathf.Clamp01(speed01);
        swingPhase += (Mathf.PI * 2f * freq) * Time.deltaTime;
        phaseUpdatedFrame = f;
    }

    // ---- Public API ----
    public void Step(float dt, Vector2 move, float speed01, bool aiming)
    {
        bool isMoving = move.sqrMagnitude > moveThreshold * moveThreshold;


        UpdatePhaseOnce(speed01);

        if (armsEnabled && !aiming) ArmSwingAnimation(isMoving, speed01);
        WalkingAnimation(isMoving, speed01);
        HeadBobAnimation(isMoving, speed01);
    }

    public void ArmSwingAnimation(bool isMoving, float speed01)
    {
        if (!leftArm || !rightArm) return;
        UpdatePhaseOnce(speed01);

        float targetAmp = isMoving ? swingAmplitude * Mathf.Clamp01(speed01) : 0f;
        float t = 1f - Mathf.Exp(-amplitudeEase * Time.deltaTime);
        currentArmAmplitude = Mathf.Lerp(currentArmAmplitude, targetAmp, t);

        float wave = Mathf.Sin(swingPhase);

        float targetLeftZ = baseLeftArmZ + leftArmOffset + (wave * currentArmAmplitude);
        float targetRightZ = baseRightArmZ + rightArmOffset + (-wave * currentArmAmplitude);

        float dt = isMoving ? dampTimeMoving : dampTimeIdle;
        float newLeftZ = Mathf.SmoothDampAngle(leftArm.localEulerAngles.z, targetLeftZ, ref velLeftArmZ, dt);
        float newRightZ = Mathf.SmoothDampAngle(rightArm.localEulerAngles.z, targetRightZ, ref velRightArmZ, dt);

        leftArm.localRotation = Quaternion.Euler(0f, 0f, newLeftZ);
        rightArm.localRotation = Quaternion.Euler(0f, 0f, newRightZ);
    }

    public void WalkingAnimation(bool isMoving, float speed01)
    {
        if (!leftLeg || !rightLeg) return;
        UpdatePhaseOnce(speed01);

        float wave = Mathf.Sin(swingPhase * Mathf.Max(0.01f, legSpeedMul));

        float amp = isMoving ? legAmplitude * Mathf.Clamp01(speed01) : 0f;
        float leftAmp = amp * Mathf.Max(0f, leftLegAmplitudeMul);
        float rightAmp = amp * Mathf.Max(0f, rightLegAmplitudeMul);

        float targetLeftZ = baseLeftLegZ + leftLegOffsetDeg + (wave * leftAmp);
        float targetRightZ = baseRightLegZ + rightLegOffsetDeg + (-wave * rightAmp);

        float dtL = isMoving ? leftLegDampTime : Mathf.Max(leftLegDampTime, 0.1f);
        float dtR = isMoving ? rightLegDampTime : Mathf.Max(rightLegDampTime, 0.1f);

        float newLeftZ = Mathf.SmoothDampAngle(leftLeg.localEulerAngles.z, targetLeftZ, ref velLeftLegZ, dtL);
        float newRightZ = Mathf.SmoothDampAngle(rightLeg.localEulerAngles.z, targetRightZ, ref velRightLegZ, dtR);

        leftLeg.localRotation = Quaternion.Euler(0f, 0f, newLeftZ);
        rightLeg.localRotation = Quaternion.Euler(0f, 0f, newRightZ);
    }

    public void HeadBobAnimation(bool isMoving, float speed01)
    {
        if (!bobTarget) return;
        UpdatePhaseOnce(speed01);

        float targetBobAmt = isMoving ? (bobHeight * Mathf.Clamp01(speed01)) : 0f;
        float bt = 1f - Mathf.Exp(-bobEase * Time.deltaTime);
        currentBobAmt = Mathf.Lerp(currentBobAmt, targetBobAmt, bt);

        float bob = Mathf.Cos(swingPhase * Mathf.Max(0.01f, bobSpeedMul)) * currentBobAmt;

        float targetY = bobBaseY + bob;
        float newY = Mathf.SmoothDamp(bobTarget.localPosition.y, targetY, ref bobVel, bobDampTime);

        var lp = bobTarget.localPosition;
        lp.y = newY;
        bobTarget.localPosition = lp;
    }

    public void SetArmsEnabled(bool enabled) => armsEnabled = enabled;

    public void ResetPose()
    {
        if (leftArm) leftArm.localRotation = Quaternion.Euler(0, 0, baseLeftArmZ);
        if (rightArm) rightArm.localRotation = Quaternion.Euler(0, 0, baseRightArmZ);
        if (leftLeg) leftLeg.localRotation = Quaternion.Euler(0, 0, baseLeftLegZ);
        if (rightLeg) rightLeg.localRotation = Quaternion.Euler(0, 0, baseRightLegZ);
        if (bobTarget)
        {
            var lp = bobTarget.localPosition; lp.y = bobBaseY; bobTarget.localPosition = lp;
        }
        currentArmAmplitude = 0f;
        currentBobAmt = 0f;
        velLeftArmZ = velRightArmZ = velLeftLegZ = velRightLegZ = bobVel = 0f;
    }
}
