using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Builds the game's pixel-art button: hard drop shadow, dark outline, colored face with a light strip
// along the top, and a label with a pixel-offset shadow. Every button in the game uses this one style.
public static class PixelButton
{
    // Size of one "pixel" of the pixel-art style, in canvas units (1920x1080 reference).
    public const float Pixel = 6.0f;

    public static readonly Vector2 Size = new Vector2(462, 102);
    public static readonly Color OutlineColor = new Color(0.02f, 0.02f, 0.06f);

    // A button with an explicit hover and pressed color.
    public static Button Create(string name, Transform parent, string label, Vector2 position, TMP_FontAsset font,
                                Color faceColor, Color hoverColor, Color pressedColor, Color textColor,
                                UnityEngine.Events.UnityAction onClick)
    {
        RectTransform root = CreateRect<RectTransform>(name, parent, position, Size);

        Image shadow = CreateRect<Image>("Shadow", root, new Vector2(Pixel * 2, -Pixel * 2), Size);
        shadow.color = new Color(0, 0, 0, 0.6f);
        shadow.raycastTarget = false;
        Image outline = CreateRect<Image>("Outline", root, Vector2.zero, Size);
        outline.color = OutlineColor;
        Image face = CreateRect<Image>("Face", root, Vector2.zero, Size - Vector2.one * Pixel * 2);
        face.color = Color.white;
        Image shine = CreateRect<Image>("Shine", face.transform, new Vector2(0, (Size.y - Pixel * 2) / 2 - Pixel / 2), new Vector2(Size.x - Pixel * 2, Pixel));
        shine.color = new Color(1, 1, 1, 0.3f);
        shine.raycastTarget = false;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.normalColor = faceColor;
        colors.highlightedColor = hoverColor;
        colors.selectedColor = hoverColor;
        colors.pressedColor = pressedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);
        ButtonSounds.Add(button);

        Vector2 offset = new Vector2(Pixel, -Pixel);
        CreateLabel("LabelShadow", root, label, font, new Color(0.0f, 0.0f, 0.0f, 0.8f), offset);
        CreateLabel("Label", root, label, font, textColor, Vector2.zero);
        return button;
    }

    // A button whose hover and pressed colors are worked out from its face color.
    public static Button Create(string name, Transform parent, string label, Vector2 position, TMP_FontAsset font,
                                Color faceColor, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        return Create(name, parent, label, position, font, faceColor, Shade(faceColor, 1), Shade(faceColor, 2), textColor, onClick);
    }

    // Steps a color towards white (or towards black, if it's already light) so the change is always visible.
    static Color Shade(Color color, int steps)
    {
        bool light = color.grayscale > 0.6f;
        return Color.Lerp(color, light ? Color.black : Color.white, 0.18f * steps);
    }

    static void CreateLabel(string name, Transform parent, string text, TMP_FontAsset font, Color color, Vector2 position)
    {
        TextMeshProUGUI label = CreateRect<TextMeshProUGUI>(name, parent, position, Size);
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = 40;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    static T CreateRect<T>(string name, Transform parent, Vector2 position, Vector2 size) where T : Component
    {
        // Graphics need a CanvasRenderer to be drawn; add it up front so every child has one.
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        child.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)child.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return typeof(T) == typeof(RectTransform) ? (T)(Component)rect : child.AddComponent<T>();
    }
}
