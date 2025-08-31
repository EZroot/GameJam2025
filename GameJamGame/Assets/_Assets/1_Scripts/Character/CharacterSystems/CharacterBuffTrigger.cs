using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityBuff
{
    Rage,
    Speed,
    Heal
}

[System.Serializable]
public class BuffTrigger
{
    public ApplyBuffAbilitySO AbilitySO;
    public AbilityBuff AbilityBuff;
}

public class CharacterBuffTrigger : MonoBehaviour
{
    [SerializeField] BuffTrigger[] _buffAbilityCollection;
    CharacterContext _ctx; 
    Dictionary<AbilityBuff, ApplyBuffAbilitySO> _buffDict;

    void Awake()
    {
        _ctx = GetComponent<CharacterContext>();
    }

    private void Start()
    {
        _buffDict = new Dictionary<AbilityBuff, ApplyBuffAbilitySO>();
        foreach (var buff in _buffAbilityCollection)
        {
            if (buff.AbilitySO != null && !_buffDict.ContainsKey(buff.AbilityBuff))
            {
                _buffDict.Add(buff.AbilityBuff, buff.AbilitySO);
            }
        }
    }

    public void TriggerBuff(AbilityBuff buffType, bool effectPlayerCamera = false)
    {
        if (_buffDict.TryGetValue(buffType, out var buffSO))
        {
            if (!effectPlayerCamera)
            {
                if (_routine == null)
                {
                    _routine = StartCoroutine(CoCamSet());
                }
            }
            _ctx.CharacterSpeech.Say("Power increased!");

            buffSO.Use(gameObject);
        }
        else
        {
            Debug.LogError("No buff found for type " + buffType);
        }
    }

    Coroutine _routine;
    int prevPpu;
    IEnumerator CoCamSet()
    {
        var prevPpu = _ctx.CameraController.PPU;
        yield return null;
        if (prevPpu > 6)
        {
            _ctx.CameraController.ZoomToPPU(prevPpu - 2);
            yield return new WaitForSeconds(1f);
        }
        _routine = null;
    }

}
