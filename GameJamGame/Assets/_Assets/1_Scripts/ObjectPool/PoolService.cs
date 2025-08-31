// PoolService.cs
using System.Collections.Generic;
using UnityEngine;

public class PoolService : MonoBehaviour, IPoolService
{
    class Pool
    {
        public GameObject prefab;
        public Queue<GameObject> q = new Queue<GameObject>(32);
        public Transform container;
    }

    readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>(32);
    Transform _root;

    Transform Root
    {
        get
        {
            if (!_root)
            {
                var go = new GameObject("[PoolService]");
                go.transform.SetParent(transform, false);
                _root = go.transform;
            }
            return _root;
        }
    }

    // -------- Register / Unregister ----------

    public void Register(int key, GameObject prefab, int prewarm = 0, Transform container = null)
    {
        if (_pools.ContainsKey(key)) return;

        var p = new Pool
        {
            prefab = prefab,
            container = container ? container : CreateContainer(prefab.name, key)
        };
        _pools.Add(key, p);

        if (prewarm > 0) Prewarm(key, prewarm);
    }

    public bool IsRegistered(int key) => _pools.ContainsKey(key);

    public void Unregister(int key, bool destroyInstances = true)
    {
        if (!_pools.TryGetValue(key, out var p)) return;

        if (destroyInstances)
        {
            while (p.q.Count > 0)
            {
                var go = p.q.Dequeue();
                if (go) Destroy(go);
            }
            if (p.container) Destroy(p.container.gameObject);
        }

        _pools.Remove(key);
    }

    // --------------- Spawn -------------------

    public GameObject Spawn(int key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var go = DequeueOrCreate(key);
        var t = go.transform;

        t.SetPositionAndRotation(position, rotation);
        if (parent) t.SetParent(parent, worldPositionStays: true);

        go.SetActive(true);

        // callback
        var handle = go.GetComponent<PooledHandle>();
        if (handle && go.TryGetComponent<PooledHandle.ICallbacks>(out var cb)) cb.OnSpawned();

        return go;
    }

    public T Spawn<T>(int key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        => Spawn(key, position, rotation, parent).GetComponent<T>();

    // --------------- Despawn -----------------

    public void Despawn(GameObject instance)
    {
        if (!instance) return;

        var handle = instance.GetComponent<PooledHandle>();
        if (!handle || !_pools.ContainsKey(handle.key))
        {
            // Not from pool. Just disable.
            instance.SetActive(false);
            return;
        }

        // callback
        if (instance.TryGetComponent<PooledHandle.ICallbacks>(out var cb)) cb.OnDespawned();

        var t = instance.transform;
        t.SetParent(handle.stashParent, worldPositionStays: false);
        instance.SetActive(false);

        _pools[handle.key].q.Enqueue(instance);
    }

    // --------------- Utilities ----------------

    public int AvailableCount(int key) => _pools.TryGetValue(key, out var p) ? p.q.Count : 0;

    public void Prewarm(int key, int count)
    {
        if (!_pools.TryGetValue(key, out var p)) return;
        int need = count - p.q.Count;
        for (int i = 0; i < need; i++)
        {
            var go = CreateInstance(key, p);
            go.SetActive(false);
            p.q.Enqueue(go);
        }
    }

    // --------------- Enum helpers --------------

    public void Register<TEnum>(TEnum key, GameObject prefab, int prewarm = 0, Transform container = null) where TEnum : System.Enum
        => Register(key.GetHashCode(), prefab, prewarm, container);

    public GameObject Spawn<TEnum>(TEnum key, Vector3 position, Quaternion rotation, Transform parent = null) where TEnum : System.Enum
        => Spawn(key.GetHashCode(), position, rotation, parent);

    public T Spawn<T, TEnum>(TEnum key, Vector3 position, Quaternion rotation, Transform parent = null)
        where T : Component where TEnum : System.Enum
        => Spawn<T>(key.GetHashCode(), position, rotation, parent);

    public int AvailableCount<TEnum>(TEnum key) where TEnum : System.Enum
        => AvailableCount(key.GetHashCode());

    // --------------- Internals -----------------

    Transform CreateContainer(string prefabName, int key)
    {
        var go = new GameObject($"[{key}] {prefabName} Pool");
        var t = go.transform;
        t.SetParent(Root, false);
        return t;
    }

    GameObject DequeueOrCreate(int key)
    {
        if (!_pools.TryGetValue(key, out var p))
            throw new System.Exception($"Pool key {key} not registered.");

        if (p.q.Count > 0)
            return p.q.Dequeue();

        return CreateInstance(key, p);
    }

    GameObject CreateInstance(int key, Pool p)
    {
        var go = Instantiate(p.prefab, p.container);
        var handle = go.GetComponent<PooledHandle>();
        if (!handle) handle = go.AddComponent<PooledHandle>();
        handle.key = key;
        handle.stashParent = p.container;
        handle.owner = this;
        go.SetActive(false);
        return go;
    }
}
