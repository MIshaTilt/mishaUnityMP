using Unity.Netcode;
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

    public override void OnNetworkSpawn()
    {
        _billboardCanvas.SetActive(!IsOwner);
        _hudCanvas.SetActive(IsOwner);

        _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged += OnHpChanged;

        UpdateUI(_playerNetwork.Nickname.Value.ToString(), _playerNetwork.HP.Value);
    }

    public override void OnNetworkDespawn()
    {
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
    }

    private void OnNicknameChanged(FixedString32Bytes old, FixedString32Bytes newValue)
    {
        UpdateUI(newValue.ToString(), _playerNetwork.HP.Value);
    }

    private void OnHpChanged(int old, int newValue)
    {
        UpdateUI(_playerNetwork.Nickname.Value.ToString(), newValue);
    }

    private void UpdateUI(string nickname, int hp)
    {
        _nicknameBillText.text = nickname;
        _hpBillText.text = $"HP: {hp}";

        _nicknameHudText.text = nickname;
        _hpHudText.text = $"HP: {hp}";
    }
}