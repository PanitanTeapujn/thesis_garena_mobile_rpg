using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class PlayerRoomItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI characterText;
    public Image readyIcon;
    public Button kickButton;
    public Image hostIcon;

    [Header("Ready Colors")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.red;

    private PlayerRef playerRef;

    public System.Action<PlayerRef> OnKickClicked;

    public void Setup(PlayerRoomInfo info, bool canKick, PlayerRef player)
    {
        playerRef = player;

        playerNameText.text = info.playerName;
        characterText.text = info.characterType.ToString();
        readyIcon.color = info.isReady ? readyColor : notReadyColor;
        hostIcon.gameObject.SetActive(info.isHost);

        // แสดงปุ่มเตะเฉพาะ host และไม่ใช่ตัวเอง
        kickButton.gameObject.SetActive(canKick && !info.isHost);
        kickButton.onClick.AddListener(() => OnKickClicked?.Invoke(playerRef));
    }
}