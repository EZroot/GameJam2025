using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Apply Buff")]
public class ApplyBuffAbilitySO : ScriptableObject
{
    public string id = "apply_speed";
    public BuffSO buff;
    public string fxKeyOverride; // optional; defaults to buff.id

    public void Use(GameObject owner)
    {
        var buffs = owner.GetComponent<CharacterBuffs>();
        var router = owner.GetComponent<AbilityFxRouter>();
        if (buffs && buff) buffs.Apply(buff);

        var key = string.IsNullOrEmpty(fxKeyOverride) ? buff?.id : fxKeyOverride;
        if (!string.IsNullOrEmpty(key) && router) router.Trigger(key);
    }
}
