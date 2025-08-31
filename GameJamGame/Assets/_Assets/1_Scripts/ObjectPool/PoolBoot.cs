using UnityEngine;

public enum PoolKey { Rope }

public class PoolBoot : MonoBehaviour
{
    IPoolService pools;
    [SerializeField] GameObject ropePrefab; 
    void Start()
    {
        pools = Service.Get<IPoolService>();
        pools.Register(PoolKey.Rope, ropePrefab, prewarm: 32);
    }
}
