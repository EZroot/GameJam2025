// PooledHandle.cs
using UnityEngine;

[DisallowMultipleComponent]
public class PooledHandle : MonoBehaviour
{
    [HideInInspector] public int key;
    [HideInInspector] public Transform stashParent; // container for inactive
    [HideInInspector] public IPoolService owner;

    public interface ICallbacks
    {
        void OnSpawned();
        void OnDespawned();
    }
}
