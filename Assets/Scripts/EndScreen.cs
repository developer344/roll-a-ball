using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Shared base for the full-screen win and lose screens: an overlay canvas on top of everything,
// helpers to build UI on it at runtime, a Replay button, and timing helpers on unscaled time.
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

    protected void Replay()
    {
        Time.timeScale = 1.0f;
        string scenePath = SceneManager.GetActiveScene().path;
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

    // Fades the Replay button in and makes it clickable.
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

    // A hidden Replay button below the title; fade it in with ShowReplay().
    protected CanvasGroup CreateReplayButton(TMP_FontAsset font, Color buttonColor, Color textColor)
    {
        GameObject buttonObject = new GameObject("ReplayButton", typeof(RectTransform), typeof(CanvasRenderer));
        buttonObject.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchoredPosition = new Vector2(0, -220);
        rect.sizeDelta = new Vector2(360, 100);

        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(Replay);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = "Replay";
        label.fontSize = 48;
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;

        CanvasGroup group = buttonObject.AddComponent<CanvasGroup>();
        group.alpha = 0.0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }
}
