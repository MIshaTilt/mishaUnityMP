using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private void Update()
    {
        // Летим вперед
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Урон рассчитывает ТОЛЬКО сервер!
        if (!base.IsServer) return;

        var target = other.GetComponent<PlayerNetwork>();

        // Если попали не в игрока (например, в стену) - игнорируем
        if (target == null) return;

        // Защита: не наносим урон самому себе
        if (target.Owner.ClientId == base.OwnerId) return;

        // Наносим урон
        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        // Уничтожаем пулю в сети
        base.ServerManager.Despawn(gameObject);
    }
}
