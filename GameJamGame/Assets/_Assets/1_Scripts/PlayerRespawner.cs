using UnityEngine;

public class PlayerRespawner : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;

    public void SpawnNewPlayer()
    {
        Instantiate(PlayerPrefab, transform.position, Quaternion.identity);
    }
}
