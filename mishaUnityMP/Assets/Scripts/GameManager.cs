using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public enum GameState
{
    WaitingForPlayers,
    InProgress,
    ShowingResults
}

public class GameManager : NetworkBehaviour
{
    // Синглтон для удобного доступа со стороны UI (клиента)
    public static GameManager Instance;

    [SerializeField] private int _requiredPlayers = 2;
    public int MaxPlayers = 4;
    [SerializeField] private float _matchDuration = 60f;
    [SerializeField] private float _resultsDuration = 5f;

    // Переменные состояния (FishNet V4)
    public readonly SyncVar<GameState> CurrentState = new(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new(0);
    public readonly SyncVar<float> MatchTimer = new(0f);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Подписываемся на события подключения/отключения игроков
        base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
    }

    public override void OnStopServer()
    {
        base.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized) return;

        // Пересчитываем игроков на сервере
        ConnectedPlayers.Value = base.ServerManager.Clients.Count;

        // Если ждем игроков и набрали нужное количество — стартуем
        if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
        {
            StartMatch();
        }
    }

    private void StartMatch()
    {
        // Сброс всех игроков перед матчем (хп, очки, телепорт)
        ResetAllPlayers();

        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.InProgress;
        Debug.Log("[Server] Match started!");
    }

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        // Таймер идет только во время игры
        if (CurrentState.Value == GameState.InProgress)
        {
            MatchTimer.Value -= Time.deltaTime;
            if (MatchTimer.Value <= 0f)
            {
                EndMatch();
            }
        }
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[Server] Match ended! Showing results...");

        // Через N секунд возвращаемся в лобби
        Invoke(nameof(ResetToLobby), _resultsDuration);
    }

    private void ResetToLobby()
    {
        CurrentState.Value = GameState.WaitingForPlayers;
        Debug.Log("[Server] Lobby reset. Waiting for players...");

        // Если пока смотрели результаты, кто-то вышел, проверяем, можем ли сразу начать заново
        if (ConnectedPlayers.Value >= _requiredPlayers)
        {
            StartMatch();
        }
    }

    private void ResetAllPlayers()
    {
        // Ищем точки спавна
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        // Проходимся по всем подключенным клиентам
        foreach (var conn in base.ServerManager.Clients.Values)
        {
            // Берем первый (главный) объект клиента (обычно это игрок)
            if (conn.FirstObject != null)
            {
                PlayerNetwork pn = conn.FirstObject.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    pn.HP.Value = 100;
                    pn.Score.Value = 0;
                    pn.IsAlive.Value = true;
                    pn.Ammo.Value = pn.MaxAmmo;

                    // Возвращаем на случайный спавн
                    if (spawnPoints.Length > 0)
                    {
                        Vector3 newPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
                        // ПРАВИЛЬНАЯ ТЕЛЕПОРТАЦИЯ
                        pn.Teleport(newPos);
                    }
                }
            }
        }
    }
}