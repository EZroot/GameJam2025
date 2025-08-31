using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Wander")]
public class WanderState : StateScriptableObject
{
    private CharacterStatemachine m_stateMachine;

    public float wanderRadius = 5f;
    public float wanderTimer = 3f;

    private float spotPlayerDistance = 5.5f;
    private float _timer;
    private Vector3 _moveTarget;

    public override void Enter(CharacterStatemachine statemachine)
    {
        Debug.Log("Entered Wanderstat");
        m_stateMachine = statemachine;
        _timer = 0f;

        m_stateMachine.CharacterContext.CharacterSkills.OnDamaged += OnDamaged;

        var otherCol = Physics2D.OverlapCircleAll(m_stateMachine.transform.position, 30f, LayerMask.GetMask("PlayerCharacter"));
        foreach (var other in otherCol)
        {
            if (other && other.transform != m_stateMachine.transform)
            {
                IRaycastHit raycastHit = other.GetComponent<IRaycastHit>();
                if (raycastHit != null)
                {
                    if (raycastHit.Team != m_stateMachine.CharacterContext.Team)
                    {
                        Follow(other.transform);
                        break;
                    }
                }
            }
        }
    }

    public override void Execute()
    {
        _timer += Time.deltaTime;
        if (_timer >= wanderTimer)
        {
            _moveTarget = m_stateMachine.transform.position + Random.insideUnitSphere * wanderRadius;
            _timer = 0f;
        }

        //if a character is close, set lookat target 
        // TODO: OPTIMIZE THIS 
        var otherCol = Physics2D.OverlapCircleAll(m_stateMachine.transform.position, spotPlayerDistance, LayerMask.GetMask("PlayerCharacter"));
        foreach (var other in otherCol)
        {
            if (other && other.transform != m_stateMachine.transform)
            {
                IRaycastHit raycastHit = other.GetComponent<IRaycastHit>();
                if (raycastHit != null)
                {
                    if(raycastHit.Team != m_stateMachine.CharacterContext.Team)
                    {
                        if (!m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming)
                            m_stateMachine.CharacterContext.CharacterLookAt2D.SetAimMode(true);
                        m_stateMachine.CharacterContext.CharacterLookAt2D.SetLookPoint(other.transform.position);
                        break;
                    }
                }
            }
        }



        Vector2 direction = (_moveTarget - m_stateMachine.transform.position).normalized;

        // Ensure aiming
        if (!m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming)
            m_stateMachine.CharacterContext.CharacterLookAt2D.SetAimMode(true);
        m_stateMachine.CharacterContext.CharacterLookAt2D.SetLookPoint(direction);

        m_stateMachine.CharacterContext.CharacterMotor2D.SetMove(direction.x, direction.y);
        m_stateMachine.CharacterContext.CharacterAnimation.Step(Time.deltaTime, direction, m_stateMachine.CharacterContext.CharacterMotor2D.Speed01, m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming);
    }

    public override void Exit()
    {
        Debug.Log("Exited wandered");

        m_stateMachine.CharacterContext.CharacterSkills.OnDamaged -= OnDamaged;
    }

    private void OnDamaged(int dmg)
    {
        Debug.Log("OnDamage - Trying to follow player");
        var player = Service.Get<IPlayerService>().LocalPlayer;
        Follow(player.transform);
    }

    IEnumerator DelayFollow(float delay, Collider2D other)
    {
        yield return new WaitForSeconds(delay);
        Follow(other.transform);
    }

    void Follow(Transform target)
    {
        var followState = m_stateMachine.GetStateInstance<FollowState>();
        followState.SetFollowTarget(target);
        m_stateMachine.ChangeState(followState);
    }
}
