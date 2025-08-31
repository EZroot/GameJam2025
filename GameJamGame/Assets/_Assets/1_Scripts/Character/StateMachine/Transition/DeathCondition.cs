using UnityEngine;

[CreateAssetMenu(menuName = "AI/Conditions/Death")]
public class DeathCondition : TransitionCondition
{
    public override bool Evaluate(CharacterStatemachine sm)
    {
        if (sm.CharacterContext.CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() <= 0) return true;

        return false;
    }
}
