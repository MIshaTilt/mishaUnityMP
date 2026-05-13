using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    // В FishNet V4 это правильный синтаксис!
    public readonly SyncVar<string> Nickname = new();
    public readonly SyncVar<int> HP = new(100);
    public readonly SyncVar<bool> IsAlive = new(true);
    public readonly SyncVar<int> Score = new(0);
    public int MaxAmmo = 10;
    public readonly SyncVar<int> Ammo = new(10);


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
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
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
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (asServer && next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        if (_meshRenderer != null) _meshRenderer.enabled = next;
        if (_cc != null) _cc.enabled = next;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        Vector3 newPos = Vector3.zero;

        if (spawnPoints.Length > 0)
        {
            newPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        }

        if (_cc != null) _cc.enabled = false;
        transform.position = newPos;
        if (_cc != null) _cc.enabled = true;

        TeleportObserversRpc(newPos);
        HP.Value = 100;
        IsAlive.Value = true;
        Ammo.Value = MaxAmmo;
    }

    [ObserversRpc]
    private void TeleportObserversRpc(Vector3 newPos)
    {
        if (base.IsServerInitialized) return;

        if (_cc != null) _cc.enabled = false;
        transform.position = newPos;
        if (_cc != null) _cc.enabled = true;
    }


    public void Teleport(Vector3 newPos)
    {
        // 1. Отключаем CC на сервере, чтобы Unity разрешил изменить позицию
        if (_cc != null) _cc.enabled = false;
        transform.position = newPos;
        if (_cc != null) _cc.enabled = true;

        // 2. Вызываем RPC, чтобы телепортировать локального клиента (владельца)
        TeleportObserversRpc(newPos);
    }

}