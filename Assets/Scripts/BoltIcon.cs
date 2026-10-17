using UnityEngine;
using UnityEngine.UI;

// A filled lightning-bolt icon drawn as a UI graphic, with a soft glow behind it.
// It fills its RectTransform; set the color to tint it.
public class BoltIcon : MaskableGraphic
{
    // How much larger the glow is than the bolt, and how see-through it is.
    public float glowScale = 1.2f;
    public float glowAlpha = 0.3f;

    // The bolt outline split into convex pieces, in 0-1 coordinates (0,0 = bottom-left).
    static readonly Vector2[][] Pieces =
    {
        // Slanted top bar.
        new[] { new Vector2(0.45f, 1.0f), new Vector2(0.85f, 1.0f), new Vector2(0.6f, 0.6f), new Vector2(0.268f, 0.6f) },
        // Wide middle step.
        new[] { new Vector2(0.268f, 0.6f), new Vector2(0.85f, 0.6f), new Vector2(0.7125f, 0.45f), new Vector2(0.2f, 0.45f) },
        // Bottom point.
        new[] { new Vector2(0.45f, 0.45f), new Vector2(0.7125f, 0.45f), new Vector2(0.3f, 0.0f) },
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;

        Color glow = color;
        glow.a *= glowAlpha;
        AddBolt(vh, rect, glowScale, glow);
        AddBolt(vh, rect, 1.0f, color);
    }

    static void AddBolt(VertexHelper vh, Rect rect, float scale, Color32 color)
    {
        foreach (Vector2[] piece in Pieces)
        {
            int first = vh.currentVertCount;
            foreach (Vector2 point in piece)
            {
                // Scale around the center so the glow surrounds the bolt evenly.
                Vector2 scaled = (point - new Vector2(0.5f, 0.5f)) * scale + new Vector2(0.5f, 0.5f);
                UIVertex vertex = UIVertex.simpleVert;
                vertex.position = new Vector2(rect.x + scaled.x * rect.width, rect.y + scaled.y * rect.height);
                vertex.color = color;
                vh.AddVert(vertex);
            }
            for (int i = 1; i < piece.Length - 1; i++)
            {
                vh.AddTriangle(first, first + i, first + i + 1);
            }
        }
    }
}
