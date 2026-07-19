using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Login + lobby UI. Hierarchy is built in the Editor (Canvas drag-and-drop), not at runtime.</summary>
public sealed class MenuLoginUI : MonoBehaviour
{
    private const string PlayerNameKey = "PlayerName";

    [Header("Login")]
    [SerializeField] private GameObject loginCard;
    [SerializeField] private InputField nameField;
    [SerializeField] private Button loginButton;
    [SerializeField] private Text statusText;

    [Header("Lobby")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private Text playerNameText;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Text matchStatus;

    private void Awake()
    {
        EnsureEventSystem();

        if (nameField != null)
        {
            nameField.text = PlayerPrefs.GetString(PlayerNameKey, "");
            nameField.onSubmit.AddListener(_ => Login());
            nameField.Select();
            nameField.ActivateInputField();
        }

        if (loginButton != null)
            loginButton.onClick.AddListener(Login);

        if (findMatchButton != null)
            findMatchButton.onClick.AddListener(FindMatch);

        if (lobbyRoot != null)
            lobbyRoot.SetActive(false);

        if (loginCard != null)
            loginCard.SetActive(true);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(transform);
    }

    private void Login()
    {
        if (nameField == null || statusText == null) return;

        var playerName = nameField.text.Trim();
        if (string.IsNullOrWhiteSpace(playerName))
        {
            statusText.text = "Hãy đặt tên trước khi vào đấu trường.";
            statusText.color = new Color(1f, .56f, .38f);
            nameField.Select();
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        ShowMainMenu(playerName);
    }

    private void ShowMainMenu(string playerName)
    {
        if (loginCard != null)
            loginCard.SetActive(false);

        if (lobbyRoot != null)
            lobbyRoot.SetActive(true);

        if (playerNameText != null)
            playerNameText.text = playerName;

        if (matchStatus != null)
        {
            matchStatus.text = "Sẵn sàng tìm đồng đội";
            matchStatus.color = new Color(.59f, .70f, .77f);
        }
    }

    private void FindMatch()
    {
        if (matchStatus == null) return;
        matchStatus.text = "Đang tìm trận... (chế độ test)";
        matchStatus.color = new Color(.45f, 1f, .70f);
    }
}
