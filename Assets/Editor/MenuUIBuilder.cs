#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds a real Canvas hierarchy into Menu.unity so you can drag-and-drop edit in the Editor.
/// Menu: Tools → DEV MOBA → Build Menu Canvas UI
/// Auto-runs once after scripts compile if the Canvas is missing.
/// </summary>
[InitializeOnLoad]
public static class MenuUIBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string ControllerName = "Menu Login UI";
    private const string CanvasName = "MOBA Login Canvas";
    private const string AutoBakePref = "DEVMOBA_MenuCanvas_AutoBaked_v1";

    private static readonly Color Navy = new Color(0.025f, 0.07f, 0.13f);
    private static readonly Color Panel = new Color(0.035f, 0.12f, 0.20f, 0.96f);
    private static readonly Color Gold = new Color(0.95f, 0.68f, 0.22f);

    static MenuUIBuilder()
    {
        EditorApplication.delayCall += TryAutoBakeOnce;
    }

    private static void TryAutoBakeOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool(AutoBakePref, false)) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null) return;

        try
        {
            BuildAndSave();
            EditorPrefs.SetBool(AutoBakePref, true);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DEV MOBA] Auto-bake Menu Canvas failed: " + ex.Message +
                             "\nChạy thủ công: Tools → DEV MOBA → Build Menu Canvas UI");
        }
    }

    [MenuItem("Tools/DEV MOBA/Build Menu Canvas UI")]
    public static void BuildFromMenu()
    {
        BuildAndSave();
        EditorPrefs.SetBool(AutoBakePref, true);
    }

    /// <summary>Batchmode: Unity.exe -batchmode -quit -projectPath ... -executeMethod MenuUIBuilder.BuildAndSave</summary>
    public static void BuildAndSave()
    {
        var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        var controller = FindOrCreateController();
        ClearGeneratedChildren(controller.transform);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        EnsureEventSystem(controller.transform);
        var canvas = CreateCanvas(controller.transform);
        BuildLoginCard(canvas.transform, font, uiSprite, out var loginCard, out var nameField, out var loginButton, out var statusText);
        BuildLobby(canvas.transform, font, uiSprite, out var lobbyRoot, out var playerNameText, out var findMatchButton, out var matchStatus);

        WireController(controller, loginCard, nameField, loginButton, statusText, lobbyRoot, playerNameText, findMatchButton, matchStatus);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[DEV MOBA] Menu Canvas UI built into Assets/Scenes/Menu.unity — open Scene view and drag-edit freely.");
    }

    private static MenuLoginUI FindOrCreateController()
    {
        var existing = Object.FindFirstObjectByType<MenuLoginUI>();
        if (existing != null) return existing;

        var go = new GameObject(ControllerName);
        Undo.RegisterCreatedObjectUndo(go, "Create Menu Login UI");
        return go.AddComponent<MenuLoginUI>();
    }

    private static void ClearGeneratedChildren(Transform root)
    {
        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child.name == CanvasName || child.name == "EventSystem")
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void EnsureEventSystem(Transform parent)
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetParent(parent, false);
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        var canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void BuildLoginCard(
        Transform canvas,
        Font font,
        Sprite uiSprite,
        out GameObject loginCard,
        out InputField nameField,
        out Button loginButton,
        out Text statusText)
    {
        var background = CreateImage("Background", canvas, Navy, uiSprite);
        Stretch(background.rectTransform);

        var backdropTexture = Resources.Load<Texture2D>("Menu/moba_login_background");
        if (backdropTexture != null)
        {
            var path = AssetDatabase.GetAssetPath(backdropTexture);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                background.sprite = sprite;
            else
                background.sprite = Sprite.Create(backdropTexture, new Rect(0, 0, backdropTexture.width, backdropTexture.height), new Vector2(.5f, .5f));
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
        }

        var shade = CreateImage("Background Shade", canvas, new Color(.005f, .025f, .06f, .42f), uiSprite);
        Stretch(shade.rectTransform);

        var topLine = CreateImage("Gold Horizon", canvas, Gold, uiSprite);
        SetRect(topLine.rectTransform, new Vector2(0, 0.76f), new Vector2(1, 0.76f), Vector2.zero, new Vector2(0, 3));

        var card = CreateImage("Login Card", canvas, Panel, uiSprite);
        loginCard = card.gameObject;
        SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(590, 650));
        AddOutline(card, Gold, 2f);

        var crest = CreateImage("Crest", card.transform, Gold, uiSprite);
        SetRect(crest.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -96), new Vector2(94, 94));
        var crestInner = CreateImage("Crest Core", crest.transform, Navy, uiSprite);
        Stretch(crestInner.rectTransform, new Vector2(12, 12), new Vector2(-12, -12));
        var crestMark = CreateText("Crest Mark", crest.transform, "M", 46, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        Stretch(crestMark.rectTransform);

        var title = CreateText("Title", card.transform, "DEV MOBA", 50, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        SetRect(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -180), new Vector2(500, 64));
        var subtitle = CreateText("Subtitle", card.transform, "ENTER THE ARENA", 17, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        SetRect(subtitle.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -237), new Vector2(500, 32));
        var rule = CreateImage("Title Rule", card.transform, new Color(Gold.r, Gold.g, Gold.b, .7f), uiSprite);
        SetRect(rule.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -278), new Vector2(190, 2));

        var label = CreateText("Name Label", card.transform, "TÊN TRIỆU HỒI SƯ", 15, new Color(.65f, .78f, .86f), TextAnchor.MiddleLeft, FontStyle.Bold, font);
        SetRect(label.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -337), new Vector2(440, 26));

        nameField = CreateInput(card.transform, font, uiSprite);
        SetRect(nameField.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -395), new Vector2(440, 64));

        loginButton = CreateButton(card.transform, "VÀO ĐẤU", font, uiSprite);
        SetRect(loginButton.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -488), new Vector2(440, 70));

        statusText = CreateText("Status", card.transform, "Bản test: nhập tên bất kỳ để vào game", 15, new Color(.59f, .70f, .77f), TextAnchor.MiddleCenter, FontStyle.Normal, font);
        SetRect(statusText.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 65), new Vector2(510, 30));
    }

    private static void BuildLobby(
        Transform canvas,
        Font font,
        Sprite uiSprite,
        out GameObject lobbyRoot,
        out Text playerNameText,
        out Button findMatchButton,
        out Text matchStatus)
    {
        lobbyRoot = new GameObject("Lobby Root", typeof(RectTransform));
        lobbyRoot.transform.SetParent(canvas, false);
        Stretch(lobbyRoot.GetComponent<RectTransform>());
        lobbyRoot.SetActive(false);

        var profile = CreateImage("Player Profile", lobbyRoot.transform, new Color(.02f, .09f, .16f, .92f), uiSprite);
        SetRect(profile.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -55), new Vector2(360, 112));
        AddOutline(profile, new Color(Gold.r, Gold.g, Gold.b, .75f), 1.5f);

        var avatar = CreateImage("Profile Crest", profile.transform, Gold, uiSprite);
        SetRect(avatar.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(58, 0), new Vector2(68, 68));
        var avatarMark = CreateText("Mark", avatar.transform, "M", 30, Navy, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        Stretch(avatarMark.rectTransform);

        playerNameText = CreateText("Player Name", profile.transform, "Player", 23, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, font);
        SetRect(playerNameText.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(112, 16), new Vector2(225, 34));
        var rank = CreateText("Rank", profile.transform, "TÂN BINH  •  CẤP 1", 14, new Color(.63f, .78f, .86f), TextAnchor.MiddleLeft, FontStyle.Bold, font);
        SetRect(rank.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(112, -22), new Vector2(225, 30));

        var header = CreateText("Lobby Title", lobbyRoot.transform, "ĐẤU TRƯỜNG", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        SetRect(header.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -95), new Vector2(620, 65));
        var headerSub = CreateText("Lobby Subtitle", lobbyRoot.transform, "SẴN SÀNG CHINH PHỤC VINH QUANG", 17, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        SetRect(headerSub.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -145), new Vector2(650, 32));

        var queuePanel = CreateImage("Queue Panel", lobbyRoot.transform, new Color(.015f, .055f, .10f, .90f), uiSprite);
        SetRect(queuePanel.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 128), new Vector2(540, 205));
        AddOutline(queuePanel, new Color(Gold.r, Gold.g, Gold.b, .85f), 2f);

        var mode = CreateText("Mode", queuePanel.transform, "ĐẤU THƯỜNG  •  5V5", 17, new Color(.72f, .84f, .90f), TextAnchor.MiddleCenter, FontStyle.Bold, font);
        SetRect(mode.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(450, 32));

        findMatchButton = CreateButton(queuePanel.transform, "TÌM TRẬN", font, uiSprite);
        findMatchButton.gameObject.name = "Find Match Button";
        SetRect(findMatchButton.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -7), new Vector2(430, 74));

        matchStatus = CreateText("Match Status", queuePanel.transform, "Sẵn sàng tìm đồng đội", 15, new Color(.59f, .70f, .77f), TextAnchor.MiddleCenter, FontStyle.Normal, font);
        SetRect(matchStatus.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 23), new Vector2(450, 28));
    }

    private static void WireController(
        MenuLoginUI controller,
        GameObject loginCard,
        InputField nameField,
        Button loginButton,
        Text statusText,
        GameObject lobbyRoot,
        Text playerNameText,
        Button findMatchButton,
        Text matchStatus)
    {
        var so = new SerializedObject(controller);
        so.FindProperty("loginCard").objectReferenceValue = loginCard;
        so.FindProperty("nameField").objectReferenceValue = nameField;
        so.FindProperty("loginButton").objectReferenceValue = loginButton;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("lobbyRoot").objectReferenceValue = lobbyRoot;
        so.FindProperty("playerNameText").objectReferenceValue = playerNameText;
        so.FindProperty("findMatchButton").objectReferenceValue = findMatchButton;
        so.FindProperty("matchStatus").objectReferenceValue = matchStatus;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static InputField CreateInput(Transform parent, Font font, Sprite uiSprite)
    {
        var root = CreateImage("Name Input", parent, new Color(.018f, .055f, .10f, 1), uiSprite);
        AddOutline(root, new Color(.25f, .52f, .62f), 1.5f);
        var field = root.gameObject.AddComponent<InputField>();
        field.targetGraphic = root;
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = 18;

        var placeholder = CreateText("Placeholder", root.transform, "Nhập tên của bạn", 20, new Color(.46f, .60f, .67f), TextAnchor.MiddleLeft, FontStyle.Italic, font);
        Stretch(placeholder.rectTransform, new Vector2(22, 0), new Vector2(-22, 0));
        var inputText = CreateText("Text", root.transform, "", 21, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal, font);
        Stretch(inputText.rectTransform, new Vector2(22, 0), new Vector2(-22, 0));
        field.placeholder = placeholder;
        field.textComponent = inputText;
        return field;
    }

    private static Button CreateButton(Transform parent, string caption, Font font, Sprite uiSprite)
    {
        var image = CreateImage("Login Button", parent, Gold, uiSprite);
        AddOutline(image, new Color(1f, .87f, .52f), 2f);
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, .82f);
        colors.pressedColor = new Color(.78f, .50f, .10f);
        button.colors = colors;
        var text = CreateText("Button Text", image.transform, caption, 25, Navy, TextAnchor.MiddleCenter, FontStyle.Bold, font);
        Stretch(text.rectTransform);
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, Color color, TextAnchor alignment, FontStyle style, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AddOutline(Image image, Color color, float distance)
    {
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, distance);
    }
}
#endif
