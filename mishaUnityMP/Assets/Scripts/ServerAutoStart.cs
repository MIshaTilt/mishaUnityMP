using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Application.isBatchMode возвращает true, когда мы запускаем игру 
        // через консоль без графики (флаги -batchmode -nographics)
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Обнаружен консольный режим. Запускаем сервер...");
            InstanceFinder.ServerManager.StartConnection();
        }
    }
}