using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
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
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next)
    {
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
        yield return new WaitForSeconds(3f);

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        Vector3 newPos = Vector3.zero; // Позиция по умолчанию
        
        if (spawnPoints.Length > 0)
        {
            newPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        }

        if (_cc != null) _cc.enabled = false;
        transform.position = newPos;
        if (_cc != null) _cc.enabled = true;

        TeleportClientRpc(newPos);

        HP.Value = 100;
        IsAlive.Value = true;
    }


    private void OnIsAliveChanged(bool prev, bool next)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = next;
        }

        if (_cc != null)
        {
            _cc.enabled = next;
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 newPos)
    {
        if (IsOwner && !IsServer)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = newPos;
            if (_cc != null) _cc.enabled = true;
        }
    }

}