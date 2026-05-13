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
        if (!base.IsServerInitialized) return;

        var target = other.GetComponent<PlayerNetwork>();

        // Если попали не в игрока, или в себя, или игрок УЖЕ мертв - игнорируем
        if (target == null || target.Owner.ClientId == base.OwnerId || target.HP.Value <= 0) return;

        // Вычисляем новое ХП
        int newHp = Mathf.Max(0, target.HP.Value - _damage);
        target.HP.Value = newHp;

        // Если этим выстрелом мы убили врага
        if (newHp == 0)
        {
            // ПРАВИЛЬНЫЙ ПОИСК ВЛАДЕЛЬЦА ПУЛИ:
            // Берем соединение владельца пули и обращаемся к его главному объекту (Игроку)
            if (base.Owner != null && base.Owner.FirstObject != null)
            {
                var attacker = base.Owner.FirstObject.GetComponent<PlayerNetwork>();
                if (attacker != null)
                {
                    attacker.Score.Value++;
                    Debug.Log($"[Server] Игрок {attacker.Nickname.Value} убил {target.Nickname.Value}. Счёт: {attacker.Score.Value}");
                }
            }
        }

        // Уничтожаем пулю в сети
        base.ServerManager.Despawn(gameObject);
    }
}
