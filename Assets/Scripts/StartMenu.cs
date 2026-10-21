using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// The Moon Hunter start menu, in a pixel-art style: a starry night with a spinning pixel moon,
// the title, the goal, a Play button that loads the game and a Settings button with the difficulty and controls.
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
    private GameObject settingsPanel;
    private Button playButton;
    private Button backButton;

    // Difficulty slider: snaps between one notch per level, and its fill takes the level's color.
    private Image difficultyFill;
    private readonly List<TextMeshProUGUI> difficultyStepLabels = new List<TextMeshProUGUI>();
    private TextMeshProUGUI difficultyLabel;
    static readonly Color[] DifficultyColors =
    {
        new Color(0.3f, 0.85f, 0.35f),
        new Color(1.0f, 0.8f, 0.2f),
        new Color(0.95f, 0.25f, 0.2f),
    };

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
        CreateButton("SettingsButton", main, "SETTINGS", new Vector2(0, -330), ShowSettings);

        // Settings screen, hidden until the Settings button is pressed.
        settingsPanel = CreateRect<RectTransform>("SettingsPanel", canvas, Vector2.zero, Vector2.zero, true).gameObject;
        BuildSettings(settingsPanel.transform);
        settingsPanel.SetActive(false);

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

    void BuildSettings(Transform panel)
    {
        Image dim = CreateRect<Image>("Dim", panel, Vector2.zero, Vector2.zero, true);
        dim.color = new Color(0.0f, 0.0f, 0.0f, 0.7f);

        CreateShadowedText("Heading", panel, "SETTINGS", 64, titleColor, new Vector2(0, 450), new Vector2(1600, 100), 2);

        // Difficulty: a stepped slider; drag it, click the track, or use left/right when it's selected.
        CreateShadowedText("DifficultyHeading", panel, "DIFFICULTY", 32, titleColor, new Vector2(0, 360), new Vector2(1600, 60), 1);
        CreateDifficultySlider(panel, new Vector2(0, 280));
        difficultyLabel = CreateText("DifficultyLabel", panel, "", 22, Color.white, new Vector2(0, 160), new Vector2(1600, 50));
        SetDifficulty(GameSettings.Difficulty);

        // Controls: W A S D keys laid out like on a keyboard, and what to do.
        CreateShadowedText("ControlsHeading", panel, "CONTROLS", 32, titleColor, new Vector2(0, 100), new Vector2(1600, 60), 1);
        CreateKey(panel, "W", new Vector2(0, 15), 78);
        CreateKey(panel, "A", new Vector2(-90, -70), 78);
        CreateKey(panel, "S", new Vector2(0, -70), 78);
        CreateKey(panel, "D", new Vector2(90, -70), 78);

        string help =
            "<color=#FFEE99>MOVE</color>  W A S D / Arrow Keys / Left Stick\n" +
            "<color=#FFEE99>GOAL</color>  Roll into all 12 moons\n" +
            "<color=#FF7777>DANGER</color>  Don't get caught by the enemy!";
        TextMeshProUGUI helpText = CreateText("Help", panel, help, 22, Color.white, new Vector2(0, -230), new Vector2(1700, 160));
        helpText.lineSpacing = 30;

        backButton = CreateButton("BackButton", panel, "BACK", new Vector2(0, -420), ShowMain);
    }

    // Colors the slider for the chosen level, highlights its step label, and saves the choice.
    void SetDifficulty(int level)
    {
        GameSettings.Difficulty = level;
        difficultyFill.color = DifficultyColors[level];
        for (int i = 0; i < difficultyStepLabels.Count; i++)
        {
            difficultyStepLabels[i].color = i == level ? DifficultyColors[level] : new Color(0.55f, 0.55f, 0.65f);
        }
        difficultyLabel.text = GameSettings.DifficultyNames[level] + " - " + GameSettings.DifficultyDescriptions[level];
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

    void ShowSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        Select(backButton);
    }

    void ShowMain()
    {
        settingsPanel.SetActive(false);
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
    void CreateKey(Transform parent, string letter, Vector2 position, float size)
    {
        Image outline = CreateRect<Image>("Key" + letter, parent, position, new Vector2(size, size));
        outline.color = outlineColor;
        float inner = size - Pixel * 2;
        Image face = CreateRect<Image>("Face", outline.transform, Vector2.zero, new Vector2(inner, inner));
        face.color = new Color(0.9f, 0.9f, 0.95f);
        Image edge = CreateRect<Image>("Edge", face.transform, new Vector2(0, -inner / 2 + Pixel), new Vector2(inner, Pixel * 2));
        edge.color = new Color(0.55f, 0.55f, 0.65f);
        CreateText("Letter", face.transform, letter, size * 0.36f, new Color(0.1f, 0.1f, 0.15f), new Vector2(0, Pixel / 2), new Vector2(inner, inner));
    }

    // A pixel-art slider that snaps to one notch per difficulty level: an outlined track that fills
    // in the level's color, a chunky handle, tick marks under each notch and a label under each tick.
    void CreateDifficultySlider(Transform parent, Vector2 position)
    {
        Vector2 trackSize = new Vector2(600, 30);
        Vector2 handleSize = new Vector2(42, 66);
        int steps = GameSettings.DifficultyNames.Length;

        RectTransform root = CreateRect<RectTransform>("DifficultySlider", parent, position, trackSize);

        // Track: dark outline with a dim inside, and the colored fill on top of it.
        CreateRect<Image>("TrackOutline", root, Vector2.zero, Vector2.zero, true).color = outlineColor;
        Image inside = CreateRect<Image>("TrackInside", root, Vector2.zero, trackSize - Vector2.one * Pixel * 2);
        inside.color = new Color(0.15f, 0.15f, 0.22f);
        RectTransform fillArea = CreateRect<RectTransform>("FillArea", root, Vector2.zero, Vector2.zero, true);
        fillArea.offsetMin = new Vector2(Pixel, Pixel);
        fillArea.offsetMax = new Vector2(-Pixel, -Pixel);
        difficultyFill = CreateRect<Image>("Fill", fillArea, Vector2.zero, Vector2.zero, true);

        // The handle moves between the ends of this area, so its center lines up with the ticks below.
        RectTransform handleArea = CreateRect<RectTransform>("HandleArea", root, Vector2.zero, Vector2.zero, true);
        handleArea.offsetMin = new Vector2(handleSize.x / 2, 0);
        handleArea.offsetMax = new Vector2(-handleSize.x / 2, 0);
        RectTransform handle = CreateRect<RectTransform>("Handle", handleArea, Vector2.zero, new Vector2(handleSize.x, handleSize.y - trackSize.y));
        CreateRect<Image>("HandleShadow", handle, new Vector2(Pixel, -Pixel), handleSize).color = new Color(0, 0, 0, 0.6f);
        CreateRect<Image>("HandleOutline", handle, Vector2.zero, handleSize).color = outlineColor;
        Image handleFace = CreateRect<Image>("HandleFace", handle, Vector2.zero, handleSize - Vector2.one * Pixel * 2);

        // A tick under each notch, with the level's name under it.
        float travel = trackSize.x - handleSize.x;
        for (int i = 0; i < steps; i++)
        {
            float x = -travel / 2 + travel * i / (steps - 1);
            CreateRect<Image>("Tick", root, new Vector2(x, -trackSize.y / 2 - Pixel * 3), new Vector2(Pixel, Pixel * 2)).color = new Color(0.55f, 0.55f, 0.65f);
            difficultyStepLabels.Add(CreateText("StepLabel", root, GameSettings.DifficultyNames[i], 18, Color.white, new Vector2(x, -trackSize.y / 2 - 52), new Vector2(200, 40)));
        }

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = difficultyFill.rectTransform;
        slider.handleRect = handle;
        slider.targetGraphic = handleFace;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = steps - 1;
        slider.wholeNumbers = true;
        slider.value = GameSettings.Difficulty;

        // The handle dims slightly at rest and lights up when hovered, dragged or selected with the keyboard.
        ColorBlock colors = slider.colors;
        colors.normalColor = new Color(0.8f, 0.8f, 0.85f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = titleColor;
        slider.colors = colors;

        // Added after setting the starting value, so opening the menu doesn't click.
        slider.onValueChanged.AddListener(value =>
        {
            SetDifficulty(Mathf.RoundToInt(value));
            ButtonSounds.PlayClick();
        });
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
