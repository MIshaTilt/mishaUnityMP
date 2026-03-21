using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _manager;
    private Vector3 _spawnPosition;

    // Этот метод вызовет Менеджер при создании аптечки
    public void Init(PickupManager manager)
    {
        _manager = manager;
        _spawnPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Подбирать предметы разрешено ТОЛЬКО на сервере! Клиенты просто ждут результата.
        if (!base.IsServer) return;

        // Проверяем, игрок ли в нас вошел
        var player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        // Мёртвый игрок не может подбирать предметы
        if (!player.IsAlive.Value) return;

        // Если ХП и так полное - игнорируем, пусть аптечка лежит для других
        if (player.HP.Value >= 100) return;

        // Лечим, но не больше 100 ХП
        player.HP.Value = Mathf.Min(100, player.HP.Value + _healAmount);

        // Сообщаем менеджеру, что нас подобрали, чтобы он запустил таймер респавна
        _manager.OnPickedUp(_spawnPosition);

        // Уничтожаем аптечку в сети
        base.ServerManager.Despawn(gameObject);
    }
}
