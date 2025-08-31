using UnityEngine;

public abstract class StateScriptableObject : ScriptableObject, IState
{
    public abstract void Enter(CharacterStatemachine statemachine);

    public abstract void Execute();

    public abstract void Exit();
}
