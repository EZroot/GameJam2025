public interface IState
{
    void Enter(CharacterStatemachine statemachine);
    void Execute();   // called every Update
    void Exit();
}
