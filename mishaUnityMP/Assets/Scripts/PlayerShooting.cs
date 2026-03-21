using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private int _maxAmmo = 10;
    [SerializeField] private InputActionAsset _inputAsset;

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
            // Подписываемся на событие нажатия кнопки
            _shootAction.performed += OnShootPerformed;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _shootAction.Disable();
            _shootAction.performed -= OnShootPerformed;
        }
    }

    // Этот метод вызывается автоматически при нажатии кнопки стрельбы
    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (!_playerNetwork.IsAlive.Value) return;
        // Вызываем серверный метод, передавая позицию и направление
        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpc = default)
    {
        // ВАЛИДАЦИЯ НА СЕРВЕРЕ:
        // 1. Жив ли игрок?
        if (_playerNetwork.HP.Value <= 0 || !_playerNetwork.IsAlive.Value) return;

        // 2. Есть ли патроны?
        if (_currentAmmo <= 0) return;

        // 3. Прошёл ли кулдаун?
        if (Time.time < _lastShotTime + _cooldown) return;

        // Если всё ок - стреляем
        _lastShotTime = Time.time;
        _currentAmmo--;

        // Создаем пулю немного спереди, чтобы не попасть в самого себя при спавне
        var go = Instantiate(_projectilePrefab, pos + dir * 1.5f, Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();
        
        // Спавним пулю в сети и указываем, кто её владелец
        no.SpawnWithOwnership(rpc.Receive.SenderClientId);
    }
}