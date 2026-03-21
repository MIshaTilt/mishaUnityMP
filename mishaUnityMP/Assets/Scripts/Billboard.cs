using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        // Находим локальную камеру этого игрока
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // Если камеры почему-то нет (например, при загрузке), ничего не делаем
        if (_mainCamera == null) return;

        // Поворачиваем Канвас так, чтобы он смотрел ровно в ту же сторону, 
        // что и камера. Это предотвратит "отзеркаливание" текста!
        transform.forward = _mainCamera.transform.forward;
    }
}