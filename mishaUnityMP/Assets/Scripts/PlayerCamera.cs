using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, -4f);
    [SerializeField] private float _lookSensitivity = 0.2f;
    [SerializeField] private InputActionAsset _inputAsset;

    private InputAction _lookAction;
    private Camera _cam;
    
    private float _pitch;
    private float _yaw;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        
        _cam = Camera.main;

        var playerMap = _inputAsset.FindActionMap("Player");
        _lookAction = playerMap.FindAction("Look");
        _lookAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _lookAction.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (_cam == null || !IsOwner) return;

        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        _yaw += lookInput.x * _lookSensitivity;
        _pitch -= lookInput.y * _lookSensitivity;
        
        _pitch = Mathf.Clamp(_pitch, -40f, 40f);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private void LateUpdate()
    {
        if (_cam == null || !IsOwner) return;

        Quaternion camRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        
        Vector3 rotatedOffset = camRotation * _offset;

        Vector3 targetPosition = transform.position + Vector3.up * 1.5f;
        _cam.transform.position = targetPosition + rotatedOffset;
        
        _cam.transform.LookAt(targetPosition);
    }
}