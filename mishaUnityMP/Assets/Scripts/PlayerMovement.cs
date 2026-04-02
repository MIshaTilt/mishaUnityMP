using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private InputActionAsset _inputAsset;

    private InputAction _moveAction;
    private CharacterController _cc;
    private float _verticalVelocity;
    private PlayerNetwork _playerNetwork;


    private void Awake() 
    {
        _cc = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();

        var playerMap = _inputAsset.FindActionMap("Player");
        _moveAction = playerMap.FindAction("Move");

    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _moveAction.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _moveAction.Disable();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!_playerNetwork.IsAlive.Value) return;

        Vector2 inputDir = _moveAction.ReadValue<Vector2>();

        Vector3 move = (transform.right * inputDir.x + transform.forward * inputDir.y).normalized * _speed;

        _verticalVelocity += _gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);

        if (_cc.isGrounded) 
        {
            _verticalVelocity = 0f;
        }
    }
}