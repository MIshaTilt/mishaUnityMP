using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Collections;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Ник должен быть виден всем клиентам, но менять его может только сервер.
    public readonly SyncVar<string> Nickname = new();

    // HP тоже читает каждый клиент, но изменяется только на сервере.
    public readonly SyncVar<int> HP = new(100);

    public readonly SyncVar<bool> IsAlive = new(true);

    // Ссылка на отображение модельки (капсулы), чтобы ее прятать
    private CharacterController _cc;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Подписка на изменения SyncVar (это безопасно делать здесь)
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
    }

    // Добавляем этот метод для клиентской инициализации
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Здесь проверка IsOwner разрешена и отработает корректно
        if (base.IsOwner)
        {
            // Только владелец отправляет на сервер свой локально введенный ник.
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        HP.OnChange -= OnHpChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
    }


    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        // Сервер нормализует ник и записывает итоговое значение в SyncVar.
        Debug.Log($"Сервер получил ник: {nickname}");
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        // ТОЛЬКО СЕРВЕР решает, что игрок умер и запускает таймер
        if (!asServer) return;

        // Если ХП упало до 0 или ниже, а игрок был жив
        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // Ждем 3 секунды
        yield return new WaitForSeconds(3f);

        // Ищем все точки спавна на сцене по тегу
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        Vector3 newPos = Vector3.zero; // Позиция по умолчанию

        if (spawnPoints.Length > 0)
        {
            // Берем случайную точку
            newPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        }

        // 1. Сервер перемещает объект у себя
        if (_cc != null) _cc.enabled = false;
        transform.position = newPos;
        if (_cc != null) _cc.enabled = true;

        // 2. Сервер приказывает владельцу-клиенту тоже переместиться!
        TeleportObserversRpc(newPos);

        // Воскрешаем
        HP.Value = 100;
        IsAlive.Value = true;
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        // Этот код выполнится у ВСЕХ клиентов автоматически
        // Прячем или показываем капсулу
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = next;
        }

        // Выключаем коллизию, чтобы пули пролетали сквозь "труп"
        if (_cc != null)
        {
            _cc.enabled = next;
        }
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 newPos)
    {
        // Хост (сервер) уже переместился в корутине, ему это делать не нужно.
        // Чужие клиенты (не владельцы) просто получат новые координаты через NetworkTransform.
        // А вот ВЛАДЕЛЕЦ-клиент должен сам перенести свой CharacterController!
        if (base.IsOwner && !IsServer)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = newPos;
            if (_cc != null) _cc.enabled = true;
        }
    }
}
