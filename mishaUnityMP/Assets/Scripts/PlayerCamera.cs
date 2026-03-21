using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, -4f); // Камера за спиной
    [SerializeField] private float _lookSensitivity = 0.2f; // Чувствительность мыши
    [SerializeField] private InputActionAsset _inputAsset;

    private InputAction _lookAction;
    private Camera _cam;
    
    // Переменные для хранения текущего угла поворота
    private float _pitch; // Вверх-вниз
    private float _yaw;   // Влево-вправо

    public override void OnStartNetwork()
    {
        if (!base.Owner.IsLocalClient)
        {
            enabled = false;
            return;
        }
        
        _cam = Camera.main;

        // Находим экшен Look
        var playerMap = _inputAsset.FindActionMap("Player");
        _lookAction = playerMap.FindAction("Look");
        _lookAction.Enable();

        // Прячем и блокируем курсор мыши в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStopNetwork()
    {
        if (IsOwner)
        {
            _lookAction.Disable();
            // Возвращаем курсор, когда выходим из игры
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (_cam == null || !IsOwner) return;

        // Читаем движение мыши (Delta)
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        // Прибавляем движение мыши к углам поворота
        _yaw += lookInput.x * _lookSensitivity;
        _pitch -= lookInput.y * _lookSensitivity;
        
        // Ограничиваем угол вверх-вниз, чтобы камера не переворачивалась (сальто)
        _pitch = Mathf.Clamp(_pitch, -40f, 40f);

        // Вращаем САМОГО ИГРОКА влево-вправо. 
        // NetworkTransform автоматически отправит этот поворот на сервер!
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private void LateUpdate()
    {
        if (_cam == null || !IsOwner) return;

        // Вычисляем поворот камеры (вверх-вниз + влево-вправо)
        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        
        // Вращаем наш оффсет (позицию за спиной) вокруг игрока
        Vector3 rotatedOffset = camRotation * _offset;

        // Ставим камеру в новую точку (чуть выше центра игрока)
        Vector3 targetPosition = transform.position + Vector3.up * 1.5f;
        _cam.transform.position = targetPosition + rotatedOffset;
        
        // Заставляем камеру смотреть в сторону головы игрока
        _cam.transform.LookAt(targetPosition);
    }
}