using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Обязательно добавляем для работы с List<>

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    private bool[] _isPointOccupied;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        _isPointOccupied = new bool[_spawnPoints.Length];

        StartCoroutine(PeriodicSpawnRoutine());
    }

    private IEnumerator PeriodicSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_respawnDelay);

            List<int> freeIndices = new List<int>();
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (!_isPointOccupied[i])
                {
                    freeIndices.Add(i);
                }
            }

            if (freeIndices.Count > 0)
            {
                int randomIndex = freeIndices[Random.Range(0, freeIndices.Count)];
                SpawnPickupAtIndex(randomIndex);
            }
        }
    }

    private void SpawnPickupAtIndex(int index)
    {
        _isPointOccupied[index] = true;

        Transform spawnPoint = _spawnPoints[index];
        var go = Instantiate(_healthPickupPrefab, spawnPoint.position, Quaternion.identity);
        
        go.GetComponent<HealthPickup>().Init(this);
        go.GetComponent<NetworkObject>().Spawn();
    }

    public void OnPickedUp(Vector3 position)
    {
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            if (Vector3.Distance(_spawnPoints[i].position, position) < 0.1f)
            {
                _isPointOccupied[i] = false;
                break;
            }
        }
    }
}