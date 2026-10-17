using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Blood dripping down from points on the screen, drawn as a UI graphic.
// Add drips with AddDrip(), then advance them with SetTime() every frame.
// Each drip is a thin stream that runs down and slows, with a round drop at its tip.
public class BloodDrips : MaskableGraphic
{
    private class Drip
    {
        public Vector2 start;
        public float width;
        public float length;
        public float delay;
        public float duration;
    }

    private readonly List<Drip> drips = new List<Drip>();
    private float time;

    // `start` is where the drip leaves the letter, in this graphic's local space.
    public void AddDrip(Vector2 start, float width, float length, float delay, float duration)
    {
        drips.Add(new Drip { start = start, width = width, length = length, delay = delay, duration = duration });
        SetVerticesDirty();
    }

    public void SetTime(float seconds)
    {
        time = seconds;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        foreach (Drip drip in drips)
        {
            float progress = Mathf.Clamp01((time - drip.delay) / drip.duration);
            if (progress <= 0.0f) continue;

            // Runs fast at first, then slows to a stop, like thick liquid.
            float eased = 1.0f - (1.0f - progress) * (1.0f - progress);
            float length = drip.length * eased;

            // The drop at the tip swells as it gathers, from a bead to its full size.
            float dropRadius = drip.width * Mathf.Lerp(0.5f, 0.85f, eased);
            Vector2 tip = drip.start + Vector2.down * length;

            // Stream: slightly narrower at the top than at the bottom.
            AddQuad(vh,
                drip.start + new Vector2(-drip.width * 0.35f, 0),
                drip.start + new Vector2(drip.width * 0.35f, 0),
                tip + new Vector2(drip.width * 0.5f, 0),
                tip + new Vector2(-drip.width * 0.5f, 0));
            AddCircle(vh, tip, dropRadius);
        }
    }

    void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int first = vh.currentVertCount;
        AddVertex(vh, a);
        AddVertex(vh, b);
        AddVertex(vh, c);
        AddVertex(vh, d);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first, first + 2, first + 3);
    }

    void AddCircle(VertexHelper vh, Vector2 center, float radius)
    {
        const int sides = 14;
        int first = vh.currentVertCount;
        AddVertex(vh, center);
        for (int i = 0; i <= sides; i++)
        {
            float angle = i * Mathf.PI * 2.0f / sides;
            AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        for (int i = 1; i <= sides; i++)
        {
            vh.AddTriangle(first, first + i, first + i + 1);
        }
    }

    void AddVertex(VertexHelper vh, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vh.AddVert(vertex);
    }
}
