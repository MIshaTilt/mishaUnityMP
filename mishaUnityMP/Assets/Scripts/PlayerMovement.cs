using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Эта строчка гарантирует, что скрипт не добавится без CharacterController
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
        // Включаем прослушивание кнопок ТОЛЬКО для своего персонажа
        if (IsOwner)
        {
            _moveAction.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Не забываем выключать, чтобы избежать ошибок при удалении объекта
        if (IsOwner)
        {
            _moveAction.Disable();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!_playerNetwork.IsAlive.Value) return;

        // Читаем значения WASD. Это будет Vector2, где X - влево/вправо, Y - вверх/вниз
        Vector2 inputDir = _moveAction.ReadValue<Vector2>();

        // Перекладываем 2D ввод в 3D пространство (X идет в X, а Y идет в Z!)
        Vector3 move = (transform.right * inputDir.x + transform.forward * inputDir.y).normalized * _speed;

        // Гравитация
        _verticalVelocity += _gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        // Двигаем контроллер
        _cc.Move(move * Time.deltaTime);

        // Обнуляем гравитацию, если стоим на земле
        if (_cc.isGrounded) 
        {
            _verticalVelocity = 0f;
        }
    }
}