using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // Ник должен быть виден всем клиентам, но менять его может только сервер.
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // HP тоже читает каждый клиент, но изменяется только на сервере.
    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
    true,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    // Ссылка на отображение модельки (капсулы), чтобы ее прятать[SerializeField] private MeshRenderer _meshRenderer;
    private CharacterController _cc;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }


    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Только владелец отправляет на сервер свой локально введенный ник.
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);

            /*Vector3 randomPos = new Vector3(Random.Range(-3f, 3f), 0, 0);
            transform.position = randomPos;*/
        }

        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }


    [ServerRpc(RequireOwnership = false)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        // Сервер нормализует ник и записывает итоговое значение в NetworkVariable.
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next)
    {
        // ТОЛЬКО СЕРВЕР решает, что игрок умер и запускает таймер
        if (!IsServer) return;

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
        TeleportClientRpc(newPos);

        // Воскрешаем
        HP.Value = 100;
        IsAlive.Value = true;
    }


    private void OnIsAliveChanged(bool prev, bool next)
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

    [ClientRpc]
    private void TeleportClientRpc(Vector3 newPos)
    {
        // Хост (сервер) уже переместился в корутине, ему это делать не нужно.
        // Чужие клиенты (не владельцы) просто получат новые координаты через NetworkTransform.
        // А вот ВЛАДЕЛЕЦ-клиент должен сам перенести свой CharacterController!
        if (IsOwner && !IsServer)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = newPos;
            if (_cc != null) _cc.enabled = true;
        }
    }

}