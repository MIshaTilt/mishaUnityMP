using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // Обязательно добавляем это

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

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _attackAction.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _attackAction.Disable();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

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
        if (!IsOwner || target == null) return;
        Debug.Log("TryAttack");
        DealDamageServerRpc(target.NetworkObjectId, _damage);
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
            return;

        PlayerNetwork targetPlayer = targetObject.GetComponent<PlayerNetwork>();
        
        if (targetPlayer == null || targetPlayer == _playerNetwork) return;

        int nextHp = Mathf.Max(0, targetPlayer.HP.Value - damage);
        targetPlayer.HP.Value = nextHp;
    }
}