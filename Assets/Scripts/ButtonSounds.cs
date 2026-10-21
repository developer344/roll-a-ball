using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plays a hover sound when a button is pointed at (or reached with the keyboard/gamepad)
// and a click sound when it is pressed. Add it to a button with ButtonSounds.Add(button).
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    // Shared by every button, and kept across scene loads so a click that loads a scene isn't cut off.
    private static AudioSource source;
    private static AudioClip hoverClip;
    private static AudioClip clickClip;

    private Button button;

    public static void Add(Button button)
    {
        button.gameObject.AddComponent<ButtonSounds>();
    }

    // Plays the button click sound from code, e.g. when a slider changes.
    public static void PlayClick()
    {
        EnsureSource();
        Play(clickClip);
    }

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => Play(clickClip));
        EnsureSource();
    }

    static void EnsureSource()
    {
        if (source != null) return;
        GameObject player = new GameObject("ButtonSounds");
        DontDestroyOnLoad(player);
        source = player.AddComponent<AudioSource>();
        // Clips live in Assets/Sounds/Resources.
        hoverClip = Resources.Load<AudioClip>("button_hover");
        clickClip = Resources.Load<AudioClip>("button_click");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.IsInteractable()) Play(hoverClip);
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Only when moved to with the keyboard/gamepad, not when the menu selects a button from code.
        if (eventData is AxisEventData) Play(hoverClip);
    }

    static void Play(AudioClip clip)
    {
        if (clip != null) source.PlayOneShot(clip);
    }
}
