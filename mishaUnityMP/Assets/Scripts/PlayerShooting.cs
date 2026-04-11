using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;
    [SerializeField] private InputActionAsset _inputAsset;
    [SerializeField] private TMP_Text _ammoText;

    private InputAction _shootAction;
    private float _lastShotTime;
    private int _currentAmmo;
    private PlayerNetwork _playerNetwork;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
        
        var playerMap = _inputAsset.FindActionMap("Player");
        _shootAction = playerMap.FindAction("Attack");
    }

    public override void OnNetworkSpawn()
    {
        _currentAmmo = _maxAmmo;

        if (IsOwner)
        {
            _shootAction.Enable();
            _shootAction.performed += OnShootPerformed;
        }

        _playerNetwork.IsAlive.OnValueChanged += OnIsAliveChanged;
        UpdateAmmoUI();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _shootAction.Disable();
            _shootAction.performed -= OnShootPerformed;
        }

        _playerNetwork.IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        if (next && IsOwner)
        {
            _currentAmmo = _maxAmmo;
            UpdateAmmoUI();
        }
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (!_playerNetwork.IsAlive.Value) return;
        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    private void UpdateAmmoUI()
    {
        if (_ammoText != null)
        {
            _ammoText.text = $"Ammo: {_currentAmmo}/{_maxAmmo}";
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpc = default)
    {
        if (_playerNetwork.HP.Value <= 0 || !_playerNetwork.IsAlive.Value) return;

        if (_currentAmmo <= 0) return;

        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _currentAmmo--;

        int ammoLeft = _currentAmmo;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.5f, Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();

        no.SpawnWithOwnership(rpc.Receive.SenderClientId);

        ShootResultClientRpc(rpc.Receive.SenderClientId, ammoLeft);
    }

    [ClientRpc]
    private void ShootResultClientRpc(ulong clientId, int ammoLeft)
    {
        if (NetworkManager.LocalClientId == clientId)
        {
            _currentAmmo = ammoLeft;
            UpdateAmmoUI();
        }
    }
}