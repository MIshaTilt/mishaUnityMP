using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    // Настройки положения камеры относительно игрока (сзади и сверху)
    [SerializeField] private Vector3 _offset = new Vector3(0f, 4f, -6f);

    private Camera _cam;

    public override void OnNetworkSpawn()
    {
        // Если это чужой игрок, мы просто выключаем этот скрипт, 
        // чтобы камера не пыталась за ним следить.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        
        // Находим главную камеру на сцене
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        
        // Плавно (или жестко, как здесь) перемещаем камеру за игроком
        _cam.transform.position = transform.position + _offset;
        
        // Заставляем камеру смотреть на игрока
        _cam.transform.LookAt(transform.position);
    }
}