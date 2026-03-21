using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;

    [Header("UI над головой (для других)")]
    [SerializeField] private GameObject _billboardCanvas;
    [SerializeField] private TMP_Text _nicknameBillText;
    [SerializeField] private TMP_Text _hpBillText;

    [Header("HUD (для владельца)")]
    [SerializeField] private GameObject _hudCanvas;
    [SerializeField] private TMP_Text _nicknameHudText;
    [SerializeField] private TMP_Text _hpHudText;

    public override void OnStartNetwork()
    {
        // 1. Управление видимостью целых канвасов
        _billboardCanvas.SetActive(!base.Owner.IsLocalClient);
        _hudCanvas.SetActive(base.Owner.IsLocalClient);

        // 2. Подписка на изменения SyncVar
        _playerNetwork.Nickname.OnChange += OnNicknameChanged;
        _playerNetwork.HP.OnChange += OnHpChanged;

        // 3. Первичное обновление
        UpdateUI(_playerNetwork.Nickname.Value, _playerNetwork.HP.Value);
    }

    public override void OnStopNetwork()
    {
        _playerNetwork.Nickname.OnChange -= OnNicknameChanged;
        _playerNetwork.HP.OnChange -= OnHpChanged;
    }

    private void OnNicknameChanged(string old, string newValue, bool asServer)
    {
        UpdateUI(newValue, _playerNetwork.HP.Value);
    }

    private void OnHpChanged(int old, int newValue, bool asServer)
    {
        UpdateUI(_playerNetwork.Nickname.Value, newValue);
    }

    private void UpdateUI(string nickname, int hp)
    {
        // Обновляем текст в билборде (над головой)
        _nicknameBillText.text = nickname;
        _hpBillText.text = $"HP: {hp}";

        // Обновляем текст в HUD (экранный)
        _nicknameHudText.text = nickname;
        _hpHudText.text = $"HP: {hp}";
    }
}
