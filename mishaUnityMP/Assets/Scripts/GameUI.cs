using FishNet.Object;
using TMPro;
using UnityEngine;

public class GameUI : NetworkBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _hudPanel;
    [SerializeField] private GameObject _resultsPanel;

    [Header("Text Elements")]
    [SerializeField] private TMP_Text _lobbyText; // "Ожидание: 1/2"
    [SerializeField] private TMP_Text _timerText; // "Осталось: 59"
    [SerializeField] private TMP_Text _resultsText; // Итоги

    private void Update()
    {
        // Ждем, пока GameManager не появится в сети
        if (GameManager.Instance == null || !GameManager.Instance.IsNetworked) return;

        // Получаем текущие значения с сервера
        GameState state = GameManager.Instance.CurrentState.Value;
        
        // Переключаем панели в зависимости от состояния игры
        _lobbyPanel.SetActive(state == GameState.WaitingForPlayers);
        _hudPanel.SetActive(state == GameState.InProgress);
        _resultsPanel.SetActive(state == GameState.ShowingResults);

        // Обновляем тексты
        switch (state)
        {
            case GameState.WaitingForPlayers:
                _lobbyText.text = $"Ожидание игроков: {GameManager.Instance.ConnectedPlayers.Value} / {GameManager.Instance.MaxPlayers}";
                break;


            case GameState.InProgress:
                _timerText.text = $"Осталось: {Mathf.CeilToInt(GameManager.Instance.MatchTimer.Value)} сек";
                break;

            case GameState.ShowingResults:
                UpdateResultsText();
                break;
        }
    }

    private void UpdateResultsText()
    {
        // Ищем всех игроков на сцене, чтобы собрать статистику
        PlayerNetwork[] players = FindObjectsOfType<PlayerNetwork>();
        string res = "Результаты матча:\n\n";

        foreach (var p in players)
        {
            res += $"{p.Nickname.Value} - {p.Score.Value} фрагов\n";
        }

        _resultsText.text = res;
    }
}