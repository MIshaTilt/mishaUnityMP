using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

public struct MoveData : IReplicateData
{
    public float Horizontal;
    public float Vertical;

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}

public struct ReconcileData : IReconcileData
{
    public Vector3 Position;
    public float VerticalVelocity;

    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;[SerializeField] private InputActionAsset _inputAsset;

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

    public override void OnStartNetwork()
    {
        base.TimeManager.OnTick += OnTick;

        if (base.Owner.IsLocalClient)
        {
            _moveAction.Enable();
        }
    }

    public override void OnStopNetwork()
    {
        if (base.TimeManager != null) base.TimeManager.OnTick -= OnTick;
        if (base.IsOwner) _moveAction.Disable();
    }

    private void OnTick()
    {
        if (!_playerNetwork.IsAlive.Value) return;

        if (base.IsOwner)
        {
            Vector2 inputDir = _moveAction.ReadValue<Vector2>();
            MoveData md = new MoveData { Horizontal = inputDir.x, Vertical = inputDir.y };
            Replicate(md); 
        }
        else
        {
            Replicate(default);
        }
    }

    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        Vector3 moveDir = (transform.right * md.Horizontal + transform.forward * md.Vertical).normalized * _speed;
        _verticalVelocity += _gravity * (float)base.TimeManager.TickDelta;
        moveDir.y = _verticalVelocity;

        _cc.Move(moveDir * (float)base.TimeManager.TickDelta);

        if (_cc.isGrounded) _verticalVelocity = 0f;
    }

    // НОВОЕ В FISHNET V4: Метод-создатель слепка данных для отката (ошибка была из-за его отсутствия)
    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position,
            VerticalVelocity = _verticalVelocity
        };
        Reconcile(rd); 
    }[Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        _cc.enabled = false;
        transform.position = rd.Position;
        _verticalVelocity = rd.VerticalVelocity;
        _cc.enabled = true;
    }
}