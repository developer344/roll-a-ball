using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Shared base for the full-screen win and lose screens: an overlay canvas on top of everything,
// helpers to build UI on it at runtime, Replay and Home buttons, and timing helpers on unscaled time.
public abstract class EndScreen : MonoBehaviour
{
    // The end screen currently showing, so only one ever runs (no lose screen on top of a win).
    private static EndScreen current;

    // Creates the screen's GameObject with an overlay canvas and returns its component,
    // or null if an end screen is already showing.
    protected static T Create<T>(string name) where T : EndScreen
    {
        if (current != null) return null;

        T screen = new GameObject(name).AddComponent<T>();
        current = screen;

        Canvas canvas = screen.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = screen.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        screen.gameObject.AddComponent<GraphicRaycaster>();
        return screen;
    }

    // Plays a sound effect. Audio keeps playing while the game is frozen behind the screen.
    protected void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        AudioSource source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.PlayOneShot(clip, volume);
    }

    // The start menu scene the Home button goes back to.
    const string HomeScenePath = "Assets/Scenes/StartMenu.unity";

    protected void Replay()
    {
        LoadScene(SceneManager.GetActiveScene().path);
    }

    protected void GoHome()
    {
        LoadScene(HomeScenePath);
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

    // A big centered title, using the HUD text's font when there is one.
    protected TextMeshProUGUI CreateTitle(string text, TextMeshProUGUI hudLabel, float fontSize, Color color)
    {
        TextMeshProUGUI title = CreateFullScreen<TextMeshProUGUI>("Title");
        if (hudLabel != null) title.font = hudLabel.font;
        title.text = text;
        title.fontSize = fontSize;
        title.alignment = TextAlignmentOptions.Center;
        title.color = color;
        title.raycastTarget = false;
        return title;
    }

    // Hidden Replay and Home buttons below the title, in the same pixel style as the menu buttons;
    // fade them in with ShowReplay().
    protected CanvasGroup CreateEndButtons(TMP_FontAsset font, Color buttonColor, Color textColor)
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
