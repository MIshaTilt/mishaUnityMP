using FishNet.Object;
using UnityEngine;
using TMPro;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private GameObject _billboardCanvas;
    [SerializeField] private TMP_Text _nicknameBillText;
    [SerializeField] private TMP_Text _hpBillText;[SerializeField] private GameObject _hudCanvas;
    [SerializeField] private TMP_Text _nicknameHudText;[SerializeField] private TMP_Text _hpHudText;

    public override void OnStartNetwork()
    {
        _billboardCanvas.SetActive(!base.Owner.IsLocalClient);
        _hudCanvas.SetActive(base.Owner.IsLocalClient);

        _playerNetwork.Nickname.OnChange += OnNicknameChanged;
        _playerNetwork.HP.OnChange += OnHpChanged;

        UpdateUI(_playerNetwork.Nickname.Value, _playerNetwork.HP.Value);
    }

    public override void OnStopNetwork()
    {
        _playerNetwork.Nickname.OnChange -= OnNicknameChanged;
        _playerNetwork.HP.OnChange -= OnHpChanged;
    }

    private void OnNicknameChanged(string old, string newValue, bool asServer) => UpdateUI(newValue, _playerNetwork.HP.Value);
    private void OnHpChanged(int old, int newValue, bool asServer) => UpdateUI(_playerNetwork.Nickname.Value, newValue);

    private void UpdateUI(string nickname, int hp)
    {
        if (_nicknameBillText) _nicknameBillText.text = nickname;
        if (_hpBillText) _hpBillText.text = $"HP: {hp}";
        if (_nicknameHudText) _nicknameHudText.text = nickname;
        if (_hpHudText) _hpHudText.text = $"HP: {hp}";
    }
}