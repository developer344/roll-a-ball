using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// The Moon Hunter start menu, in a pixel-art style: a starry night with a spinning pixel moon,
// the title, the goal, a Play button that loads the game and a Controls button that explains how to play.
// The whole menu is built at runtime, so the StartMenu scene only needs this one component.
public class StartMenu : MonoBehaviour
{
    // The game scene the Play button loads.
    public string gameScenePath = "Assets/Scenes/MiniGame.unity";

    // Font for all the menu text, and the texture wrapped around the moon.
    public TMP_FontAsset font;
    public Texture moonTexture;

    // Music looped on the menu. It stops when Play loads the game scene and this menu is unloaded.
    public AudioClip menuMusic;
    public float musicVolume = 0.5f;

    // Colors used throughout the menu.
    public Color skyColor = new Color(0.02f, 0.03f, 0.09f);
    public Color titleColor = new Color(1.0f, 0.93f, 0.6f);
    public Color buttonColor = new Color(0.16f, 0.2f, 0.42f);
    public Color buttonHoverColor = new Color(0.3f, 0.36f, 0.7f);
    public Color outlineColor = new Color(0.02f, 0.02f, 0.06f);

    // Size of one "pixel" of the pixel-art style, in canvas units (1920x1080 reference).
    const float Pixel = 6.0f;

    // Star brightness steps, for a blinky retro twinkle instead of a smooth fade.
    static readonly float[] TwinkleLevels = { 0.25f, 0.6f, 1.0f };

    private readonly List<Image> stars = new List<Image>();
    private readonly List<float> starPhases = new List<float>();
    private GameObject mainPanel;
    private GameObject controlsPanel;
    private Button playButton;
    private Button backButton;

    void Start()
    {
        // In case we came back here from a frozen end screen.
        Time.timeScale = 1.0f;

        PlayMusic();

        EnsureEventSystem();
        Transform canvas = CreateCanvas();

        Image sky = CreateRect<Image>("Sky", canvas, Vector2.zero, Vector2.zero, true);
        sky.color = skyColor;
        CreateStars(canvas, 90);

        // Main menu: moon, title, goal and buttons.
        mainPanel = CreateRect<RectTransform>("MainPanel", canvas, Vector2.zero, Vector2.zero, true).gameObject;
        Transform main = mainPanel.transform;

        // The moon texture is 64 pixels wide, shown at 6x so each pixel is a visible block.
        RawImage moon = CreateRect<RawImage>("Moon", main, new Vector2(0, 280), new Vector2(64 * Pixel, 64 * Pixel));
        moon.raycastTarget = false;
        moon.gameObject.AddComponent<PixelMoon>().sourceTexture = moonTexture;

        CreateShadowedText("Title", main, "MOON HUNTER", 96, titleColor, new Vector2(0, 30), new Vector2(1800, 140), 2);
        CreateShadowedText("Goal", main, "Collect all 12 moons to win!", 30, Color.white, new Vector2(0, -70), new Vector2(1600, 60), 1);

        playButton = CreateButton("PlayButton", main, "PLAY", new Vector2(0, -200), PlayGame);
        CreateButton("ControlsButton", main, "CONTROLS", new Vector2(0, -330), ShowControls);

        // Controls screen, hidden until the Controls button is pressed.
        controlsPanel = CreateRect<RectTransform>("ControlsPanel", canvas, Vector2.zero, Vector2.zero, true).gameObject;
        BuildControls(controlsPanel.transform);
        controlsPanel.SetActive(false);

        Select(playButton);
    }

    void Update()
    {
        // Each star blinks between a few brightness steps at its own pace.
        for (int i = 0; i < stars.Count; i++)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (1.0f + starPhases[i] % 2.0f) + starPhases[i]);
            int level = Mathf.Min((int)(wave * TwinkleLevels.Length), TwinkleLevels.Length - 1);
            Color color = stars[i].color;
            color.a = TwinkleLevels[level];
            stars[i].color = color;
        }
    }

    void BuildControls(Transform panel)
    {
        Image dim = CreateRect<Image>("Dim", panel, Vector2.zero, Vector2.zero, true);
        dim.color = new Color(0.0f, 0.0f, 0.0f, 0.7f);

        CreateShadowedText("Heading", panel, "CONTROLS", 72, titleColor, new Vector2(0, 400), new Vector2(1600, 110), 2);

        // W A S D keys, laid out like on a keyboard.
        CreateKey(panel, "W", new Vector2(0, 250));
        CreateKey(panel, "A", new Vector2(-115, 135));
        CreateKey(panel, "S", new Vector2(0, 135));
        CreateKey(panel, "D", new Vector2(115, 135));

        string help =
            "<color=#FFEE99>MOVE</color>\n" +
            "W A S D  /  Arrow Keys  /  Left Stick\n\n" +
            "<color=#FFEE99>GOAL</color>\n" +
            "Roll into all 12 moons to collect them\n\n" +
            "<color=#FF7777>DANGER</color>\n" +
            "Don't get caught by the enemy!\n" +
            "It starts chasing as soon as you move.";
        TextMeshProUGUI helpText = CreateText("Help", panel, help, 28, Color.white, new Vector2(0, -130), new Vector2(1700, 420));
        helpText.lineSpacing = 20;

        backButton = CreateButton("BackButton", panel, "BACK", new Vector2(0, -420), ShowMain);
    }

    void PlayMusic()
    {
        // Fall back to Assets/Sounds/Resources/start_screen_soundtrack.mp3 when nothing is set in the Inspector.
        if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("start_screen_soundtrack");
        if (menuMusic == null) return;
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = menuMusic;
        source.loop = true;
        source.volume = musicVolume;
        source.Play();
    }

    void PlayGame()
    {
#if UNITY_EDITOR
        // Works in the editor even if the scene isn't in the Build Settings.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(gameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(gameScenePath);
#endif
    }

    void ShowControls()
    {
        mainPanel.SetActive(false);
        controlsPanel.SetActive(true);
        Select(backButton);
    }

    void ShowMain()
    {
        controlsPanel.SetActive(false);
        mainPanel.SetActive(true);
        Select(playButton);
    }

    // Highlights a button so the menu also works with the keyboard (arrows + Enter) or a gamepad.
    static void Select(Button button)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        // A module created from code has no input actions yet; give it the standard UI ones.
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    Transform CreateCanvas()
    {
        GameObject canvasObject = new GameObject("MenuCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject.transform;
    }

    void CreateStars(Transform canvas, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // One or (rarely) two pixels big, snapped to the pixel grid.
            float size = Random.value < 0.15f ? Pixel * 2.0f : Pixel;
            Vector2 position = new Vector2(Mathf.Round(Random.Range(-960.0f, 960.0f) / Pixel) * Pixel,
                                           Mathf.Round(Random.Range(-540.0f, 540.0f) / Pixel) * Pixel);
            Image star = CreateRect<Image>("Star", canvas, position, new Vector2(size, size));
            star.color = new Color(1.0f, 1.0f, Random.Range(0.8f, 1.0f));
            star.raycastTarget = false;
            stars.Add(star);
            starPhases.Add(Random.Range(0.0f, 10.0f));
        }
    }

    // A pixel-art keycap: dark outline, light face and a darker bottom edge.
    void CreateKey(Transform parent, string letter, Vector2 position)
    {
        Image outline = CreateRect<Image>("Key" + letter, parent, position, new Vector2(102, 102));
        outline.color = outlineColor;
        Image face = CreateRect<Image>("Face", outline.transform, Vector2.zero, new Vector2(102 - Pixel * 2, 102 - Pixel * 2));
        face.color = new Color(0.9f, 0.9f, 0.95f);
        Image edge = CreateRect<Image>("Edge", face.transform, new Vector2(0, -45 + Pixel * 1.5f), new Vector2(102 - Pixel * 2, Pixel * 2));
        edge.color = new Color(0.55f, 0.55f, 0.65f);
        CreateText("Letter", face.transform, letter, 36, new Color(0.1f, 0.1f, 0.15f), new Vector2(0, Pixel), new Vector2(90, 90));
    }

    Button CreateButton(string name, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        return PixelButton.Create(name, parent, label, position, font, buttonColor, buttonHoverColor, titleColor, Color.white, onClick);
    }

    // Text with a hard, pixel-offset shadow behind it instead of a soft outline.
    TextMeshProUGUI CreateShadowedText(string name, Transform parent, string text, float size, Color color, Vector2 position, Vector2 boxSize, int shadowPixels)
    {
        Vector2 offset = new Vector2(Pixel, -Pixel) * shadowPixels;
        CreateText(name + "Shadow", parent, text, size, new Color(0.0f, 0.0f, 0.0f, 0.8f), position + offset, boxSize);
        return CreateText(name, parent, text, size, color, position, boxSize);
    }

    TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, Vector2 position, Vector2 boxSize)
    {
        TextMeshProUGUI label = CreateRect<TextMeshProUGUI>(name, parent, position, boxSize);
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    // Creates a UI child with a component, either at a position and size or stretched to fill its parent.
    static T CreateRect<T>(string name, Transform parent, Vector2 position, Vector2 size, bool fill = false) where T : Component
    {
        // Graphics need a CanvasRenderer to be drawn; add it up front so every child has one.
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        child.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)child.transform;
        if (fill)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        return typeof(T) == typeof(RectTransform) ? (T)(Component)rect : child.AddComponent<T>();
    }
}
