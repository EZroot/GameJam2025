// IPoolService.cs
using UnityEngine;

public interface IPoolService : IService
{
    // Register / setup
    void Register(int key, GameObject prefab, int prewarm = 0, Transform container = null);
    bool IsRegistered(int key);
    void Unregister(int key, bool destroyInstances = true);

    // Spawn
    GameObject Spawn(int key, Vector3 position, Quaternion rotation, Transform parent = null);
    T Spawn<T>(int key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component;

    // Despawn
    void Despawn(GameObject instance);

    // Utilities
    int AvailableCount(int key);
    void Prewarm(int key, int count);

    // Enum convenience
    void Register<TEnum>(TEnum key, GameObject prefab, int prewarm = 0, Transform container = null) where TEnum : System.Enum;
    GameObject Spawn<TEnum>(TEnum key, Vector3 position, Quaternion rotation, Transform parent = null) where TEnum : System.Enum;
    T Spawn<T, TEnum>(TEnum key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component where TEnum : System.Enum;
    int AvailableCount<TEnum>(TEnum key) where TEnum : System.Enum;
}
