using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Idle")]
public class IdleState : StateScriptableObject
{
    private CharacterStatemachine m_stateMachine;

    public float idleTime = 2f;
    private float _timer;


    public override void Enter(CharacterStatemachine statemachine)
    {
        m_stateMachine = statemachine;
        _timer = 0f;
    }


    public override void Execute()
    {
    }


    public override void Exit()
    {

    }
}
