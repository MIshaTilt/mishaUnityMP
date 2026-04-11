using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private Slider _respawnSlider;

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
            StartCoroutine(RespawnRoutine(OwnerClientId));
        }
    }

    private IEnumerator RespawnRoutine(ulong clientId)
    {
        float respawnTime = 3f;
        float elapsed = 0f;

        while (elapsed < respawnTime)
        {
            elapsed += Time.deltaTime;
            float progress = 1f - (elapsed / respawnTime);
            UpdateRespawnUIClientRpc(clientId, progress);
            yield return null;
        }

        UpdateRespawnUIClientRpc(clientId, 0f);

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

        if (_respawnSlider != null && IsOwner)
        {
            _respawnSlider.gameObject.SetActive(!next);
        }
    }

    [ClientRpc]
    private void UpdateRespawnUIClientRpc(ulong clientId, float progress)
    {
        if (NetworkManager.LocalClientId == clientId && _respawnSlider != null)
        {
            _respawnSlider.value = progress;
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