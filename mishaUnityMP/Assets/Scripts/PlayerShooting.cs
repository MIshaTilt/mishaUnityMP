using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;
    [SerializeField] private InputActionAsset _inputAsset;

    private InputAction _shootAction;
    private float _lastShotTime;
    private PlayerNetwork _playerNetwork;
    
    // Таймер для отсчета 2 секунд
    private float _ammoRegenTimer;

    private void Awake()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();

        var playerMap = _inputAsset.FindActionMap("Player");
        _shootAction = playerMap.FindAction("Attack");
    }

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient)
        {
            _shootAction.Enable();
            _shootAction.performed += OnShootPerformed;
        }
    }

    public override void OnStopNetwork()
    {
        if (base.IsOwner)
        {
            _shootAction.Disable();
            _shootAction.performed -= OnShootPerformed;
        }
    }

    private void Update()
    {
        // Регенерацию патронов доверяем ТОЛЬКО серверу!
        if (!base.IsServerInitialized) return;

        // Если игрок жив и патронов меньше максимума
        if (_playerNetwork.IsAlive.Value && _playerNetwork.Ammo.Value < _playerNetwork.MaxAmmo)
        {
            _ammoRegenTimer += Time.deltaTime; // Копим время
            
            if (_ammoRegenTimer >= 2f) // Прошло 2 секунды
            {
                _playerNetwork.Ammo.Value++;
                _ammoRegenTimer = 0f; // Сбрасываем таймер для следующего патрона
            }
        }
        else
        {
            // Если патроны полные или игрок мертв, таймер не идет
            _ammoRegenTimer = 0f;
        }
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (!_playerNetwork.IsAlive.Value) return;

        if (GameManager.Instance == null || GameManager.Instance.CurrentState.Value != GameState.InProgress) 
            return;

        ShootServerRpc(_firePoint.position, _firePoint.forward);
    }


    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState.Value != GameState.InProgress) 
            return;

        if (_playerNetwork.HP.Value <= 0 || !_playerNetwork.IsAlive.Value) return;
        if (_playerNetwork.Ammo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _playerNetwork.Ammo.Value--;
        _ammoRegenTimer = 0f; 

        var go = Instantiate(_projectilePrefab, pos + dir * 1.5f, Quaternion.LookRotation(dir));
        base.ServerManager.Spawn(go);
        go.GetComponent<NetworkObject>().GiveOwnership(base.Owner);
    }

}