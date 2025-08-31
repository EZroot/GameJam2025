using UnityEngine;

[CreateAssetMenu(menuName = "AI/Conditions/TimerElapsed")]
public class TimerCondition : TransitionCondition
{
    public float threshold;
    private float _elapsed;
    public override bool Evaluate(CharacterStatemachine sm)
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= threshold)
        {
            _elapsed = 0f; // reset timer after threshold is met
            return true;
        }
        return false;
    }
}