using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "AI/States/Follow")]
public class FollowState : StateScriptableObject
{
    private CharacterStatemachine m_stateMachine;

    public float followTimer = 3f;

    private float _timer;
    private Transform _followTarget;

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
    }

    public override void Enter(CharacterStatemachine statemachine)
    {
        m_stateMachine = statemachine;
        Debug.Log("Entered Follow");
        _timer = 0f;
    }

    public override void Execute()
    {
        if(_followTarget == null || Vector2.Distance(_followTarget.position, m_stateMachine.transform.position) > 20)
        {
            Debug.LogWarning("Follow target lost");
            m_stateMachine.ChangeState(m_stateMachine.initialState);
            return;
        }

        if (!m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming)
            m_stateMachine.CharacterContext.CharacterLookAt2D.SetAimMode(true);
        m_stateMachine.CharacterContext.CharacterLookAt2D.SetLookPoint(_followTarget.position);

        Vector2 direction = (_followTarget.position - m_stateMachine.transform.position).normalized;
        m_stateMachine.CharacterContext.CharacterMotor2D.SetMove(direction.x, direction.y);
    }

    public override void Exit()
    {
        Debug.Log("Exited Follow");

        _followTarget = null;
    }
}
