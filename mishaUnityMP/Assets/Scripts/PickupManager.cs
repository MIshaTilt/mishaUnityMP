using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickupManager : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 10f;

    // Массив, который запоминает, занята ли конкретная точка (true = занята, false = свободна)
    private bool[] _isPointOccupied;

    public override void OnStartNetwork()
    {
        // Менеджер работает ТОЛЬКО на сервере.
        if (!base.IsServer) return;

        // Инициализируем массив размером с количество наших точек спавна.
        // По умолчанию все значения в bool-массиве равны false (все точки свободны).
        _isPointOccupied = new bool[_spawnPoints.Length];

        StartCoroutine(PeriodicSpawnRoutine());
    }

    private IEnumerator PeriodicSpawnRoutine()
    {
        while (true)
        {
            // Ждем время респавна
            yield return new WaitForSeconds(_respawnDelay);

            // Собираем в список индексы всех СВОБОДНЫХ точек
            List<int> freeIndices = new List<int>();
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (!_isPointOccupied[i])
                {
                    freeIndices.Add(i);
                }
            }

            // Если есть хотя бы одна свободная точка — выбираем из них
            if (freeIndices.Count > 0)
            {
                // Берем случайный индекс ИМЕННО ИЗ СПИСКА СВОБОДНЫХ
                int randomIndex = freeIndices[Random.Range(0, freeIndices.Count)];
                SpawnPickupAtIndex(randomIndex);
            }
        }
    }

    private void SpawnPickupAtIndex(int index)
    {
        // Сразу помечаем эту точку как занятую, чтобы сюда больше не спавнило
        _isPointOccupied[index] = true;

        Transform spawnPoint = _spawnPoints[index];
        var go = Instantiate(_healthPickupPrefab, spawnPoint.position, Quaternion.identity);

        go.GetComponent<HealthPickup>().Init(this);
        base.ServerManager.Spawn(go);
    }

    public void OnPickedUp(Vector3 position)
    {
        // Когда игрок съедает аптечку, она передает нам свои координаты.
        // Ищем в нашем массиве точек ту самую, чтобы снова сделать её свободной.
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            // Сравниваем дистанцию (с небольшой погрешностью, т.к. это float)
            if (Vector3.Distance(_spawnPoints[i].position, position) < 0.1f)
            {
                _isPointOccupied[i] = false; // Освобождаем точку!
                break; // Точку нашли, дальше цикл крутить не нужно
            }
        }
    }
}
