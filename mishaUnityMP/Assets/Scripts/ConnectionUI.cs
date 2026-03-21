using TMPro;
using FishNet.Managing;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private NetworkManager _networkManager;

    // Сохраняем ник локально до появления сетевого объекта игрока.
    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        // 1. Запускаем сервер
        if (_networkManager.ServerManager.StartConnection())
        {
            // 2. Если сервер успешно запустился, подключаем локальный клиент
            _networkManager.ClientManager.StartConnection();
            _menuPanel.SetActive(false);
        }
    }

    public void StartAsClient()
    {
        SaveNickname();
        // Клиент только подключается к уже запущенному хосту/серверу.
        _networkManager.ClientManager.StartConnection();
        _menuPanel.SetActive(false);
    }

    private void SaveNickname()
    {
        // Нормализуем ввод, чтобы сервер не получил пустую строку.
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }
}