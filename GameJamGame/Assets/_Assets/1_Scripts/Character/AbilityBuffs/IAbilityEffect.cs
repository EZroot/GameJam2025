public interface IAbilityEffect
{
    // key = which ability/buff fired, e.g. "speed_boost"
    void OnAbility(CharacterContext ctx, string key);
}
