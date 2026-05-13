using FishNet.Object;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _manager;
    private Vector3 _spawnPosition;
    private bool _isPickedUp = false; // Защита от двойного подбора в один кадр

    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, игрок ли в нас вошел
        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Если это НАШ локальный игрок, отправляем запрос серверу!
        if (player.IsOwner)
        {
            TryPickupServerRpc(player);
        }
    }

    // RequireOwnership = false разрешает любому клиенту вызвать этот метод у аптечки
    [ServerRpc(RequireOwnership = false)]
    private void TryPickupServerRpc(PlayerNetwork player)
    {
        // 1. Если кто-то другой уже успел съесть эту аптечку долю секунды назад — игнорируем
        if (_isPickedUp) return;

        // 2. Проверяем, жив ли игрок
        if (player == null || !player.IsAlive.Value) return;

        // 3. Если ХП и так полное - игнорируем
        if (player.HP.Value >= 100) return;

        // 4. (Опционально) Защита от читеров: проверяем, реально ли игрок стоит рядом с аптечкой
        if (Vector3.Distance(transform.position, player.transform.position) > 3f) return;

        // --- Если все проверки пройдены ---

        // Помечаем как собранную
        _isPickedUp = true;

        // Лечим
        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);

        // Запускаем таймер респавна у менеджера
        if (_manager != null) _manager.OnPickedUp(_spawnPosition);

        // Уничтожаем аптечку в сети
        base.ServerManager.Despawn(gameObject);
    }
}