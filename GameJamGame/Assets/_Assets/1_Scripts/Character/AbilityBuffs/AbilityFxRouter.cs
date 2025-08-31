using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AbilityFxRouter : MonoBehaviour
{
    CharacterContext _ctx;
    readonly List<IAbilityEffect> _listeners = new();

    void Awake()
    {
        _ctx = GetComponent<CharacterContext>();
        GetComponentsInChildren(true, _listeners); // cache once
    }

    public void Trigger(string key)
    {
        for (int i = 0; i < _listeners.Count; i++)
            _listeners[i].OnAbility(_ctx, key);
    }
}
