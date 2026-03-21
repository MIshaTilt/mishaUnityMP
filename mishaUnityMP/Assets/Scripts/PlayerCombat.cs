using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 5f;
    [SerializeField] private InputActionAsset _inputAsset;

    private InputAction _attackAction;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        Debug.Log("Init");
    }

    private void Awake()
    {
        if (_inputAsset != null)
        {
            var playerMap = _inputAsset.FindActionMap("Player");
            if (playerMap != null)
            {
                _attackAction = playerMap.FindAction("Attack");
            }
        }
    }

    public override void OnStartNetwork()
    {
        // Включаем прослушивание кнопок ТОЛЬКО для своего персонажа
        if (base.Owner.IsLocalClient)
        {
            _attackAction.Enable();
        }
    }

    public override void OnStopNetwork()
    {
        // Не забываем выключать, чтобы избежать ошибок при удалении объекта
        if (base.IsOwner)
        {
            _attackAction.Disable();
        }
    }

    private void Update()
    {
        // Проверяем: наш ли это объект?
        if (!base.IsOwner) return;

        // Используем новый Input System для проверки клика
        // Мы используем Mouse.current, так как это стандарт для мыши
        if (_attackAction != null && _attackAction.WasPerformedThisFrame())
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, _attackRange))
        {
            PlayerNetwork targetNetwork = hit.collider.GetComponent<PlayerNetwork>();

            if (targetNetwork != null)
            {
                Debug.Log($"[Combat] Нашел цель: {targetNetwork.name}. Попытка выстрела...");
                TryAttack(targetNetwork);
            }
            else
            {
                Debug.Log("[Combat] Луч прошел мимо или не нашел PlayerNetwork");
            }
        }
    }

    public void TryAttack(PlayerNetwork target)
    {
        if (!base.IsOwner || target == null) return;
        Debug.Log("TryAttack");
        
        NetworkObject targetObject = target.GetComponent<NetworkObject>();
        if (targetObject != null)
            DealDamageServerRpc(targetObject.ObjectId, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(int targetObjectId, int damage)
    {
        if (!base.ServerManager.Objects.Spawned.TryGetValue(targetObjectId, out NetworkObject targetObject))
            return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();

        if (targetPlayer == null || targetPlayer == _playerNetwork) return;

        int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
        targetPlayer.HP.Value = nextHp;
    }
}
