using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Shared base for the win and lose screens. Each screen is its own scene (WinScreen, LoseScreen)
// holding a camera and one object with the screen's component; the game loads it with
// EndScreen.ShowWin() or EndScreen.ShowLose(). On start, the component turns its object into a
// full-screen canvas and builds the UI on it at runtime. Also provides Replay and Home buttons
// and timing helpers on unscaled time.
public abstract class EndScreen : MonoBehaviour
{
    const string WinScenePath = "Assets/Scenes/WinScreen.unity";
    const string LoseScenePath = "Assets/Scenes/LoseScreen.unity";

    // Font for the screen's text, and the scenes the Replay and Home buttons load.
    public TMP_FontAsset font;
    public string gameScenePath = "Assets/Scenes/MiniGame.unity";
    public string homeScenePath = "Assets/Scenes/StartMenu.unity";

    public static void ShowWin()
    {
        LoadScene(WinScenePath);
    }

    public static void ShowLose()
    {
        LoadScene(LoseScenePath);
    }

    protected virtual void Awake()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
    }

    // Plays a sound effect.
    protected void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.PlayOneShot(clip, volume);
    }

    protected void Replay()
    {
        LoadScene(gameScenePath);
    }

    protected void GoHome()
    {
        LoadScene(homeScenePath);
    }

    static void LoadScene(string scenePath)
    {
        Time.timeScale = 1.0f;
#if UNITY_EDITOR
        // Works in the editor even if the scene isn't in the Build Settings.
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scenePath);
#endif
    }

    // The buttons need an EventSystem to be clicked; this scene has none of its own.
    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        // A module created from code has no input actions yet; give it the standard UI ones.
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    // Runs `step` every frame with t going from 0 to 1 over `duration` real seconds.
    protected static IEnumerator Over(float duration, System.Action<float> step)
    {
        for (float elapsed = 0.0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            step(elapsed / duration);
            yield return null;
        }
        step(1.0f);
    }

    // Fades the Replay and Home buttons in and makes them clickable.
    protected static IEnumerator ShowReplay(CanvasGroup replayGroup)
    {
        yield return Over(0.4f, t => replayGroup.alpha = t);
        replayGroup.interactable = true;
        replayGroup.blocksRaycasts = true;
    }

    protected T CreateFullScreen<T>(string name) where T : Component
    {
        // Graphics need a CanvasRenderer to be drawn; add it up front so every child has one.
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        child.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return child.AddComponent<T>();
    }

    // How far above the middle of the screen the title sits, leaving room for the buttons below.
    const float TitleRaise = 150.0f;

    // A big centered title in the screen's font, raised above the middle of the screen.
    protected TextMeshProUGUI CreateTitle(string text, float fontSize, Color color)
    {
        TextMeshProUGUI title = CreateFullScreen<TextMeshProUGUI>("Title");
        if (font != null) title.font = font;
        title.text = text;
        title.fontSize = fontSize;
        title.alignment = TextAlignmentOptions.Center;
        // A bottom margin shifts the centered text up while the title still fills the screen,
        // so effects placed from its letter positions (like the blood drips) stay lined up.
        title.margin = new Vector4(0, 0, 0, TitleRaise * 2);
        title.color = color;
        title.raycastTarget = false;
        return title;
    }

    // Hidden Replay and Home buttons below the title, in the same pixel style as the menu buttons;
    // fade them in with ShowReplay().
    protected CanvasGroup CreateEndButtons(Color buttonColor, Color textColor)
    {
        GameObject buttons = new GameObject("EndButtons", typeof(RectTransform));
        buttons.transform.SetParent(transform, false);
        ((RectTransform)buttons.transform).anchoredPosition = new Vector2(0, -220);

        PixelButton.Create("ReplayButton", buttons.transform, "REPLAY", Vector2.zero, font, buttonColor, textColor, Replay);
        PixelButton.Create("HomeButton", buttons.transform, "HOME", new Vector2(0, -140), font, buttonColor, textColor, GoHome);

        CanvasGroup group = buttons.AddComponent<CanvasGroup>();
        group.alpha = 0.0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }
}
