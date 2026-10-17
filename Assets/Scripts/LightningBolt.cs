using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Realistic lightning drawn as a UI graphic: a main bolt (plus optional side bolts),
// finely jagged with forking branches, a white-hot core and a soft glow.
// Set `progress` (0 to 1) to make the strike race down from the top, and call Regenerate()
// for new random bolts. The Graphic's color tints everything, and its alpha fades it out.
public class LightningBolt : MaskableGraphic
{
    // How far down the strike has reached, from 0 (nothing) to 1 (full screen).
    public float progress = 1.0f;

    // Glow colors of the main bolt and of the side bolts. The core is always white.
    public Color mainColor = new Color(0.55f, 0.65f, 1.0f);
    public Color sideColor = new Color(0.55f, 0.55f, 1.0f);

    // How many extra bolts strike alongside the main one.
    public int sideBolts = 0;

    // Widths of the white core, the bright inner glow and the faint outer glow, for a full-thickness bolt.
    public float coreWidth = 3.0f;
    public float innerGlowWidth = 12.0f;
    public float outerGlowWidth = 40.0f;

    // One jagged line of the lightning, with how thick it is and what color it glows.
    private class Path
    {
        public List<Vector2> points;
        public float thickness;
        public Color color;
    }

    private readonly List<Path> paths = new List<Path>();

    // Builds new random bolts inside the current rect.
    public void Regenerate()
    {
        Rect rect = rectTransform.rect;
        paths.Clear();

        // Main bolt: from near the top-center to the bottom of the screen.
        Vector2 mainStart = new Vector2(Random.Range(-0.1f, 0.1f) * rect.width, rect.yMax);
        Vector2 mainEnd = new Vector2(Random.Range(-0.15f, 0.15f) * rect.width, rect.yMin);
        AddBolt(mainStart, mainEnd, 1.0f, mainColor, 6, 2);

        // Side bolts: from random points along the top, ending partway down at either side.
        for (int i = 0; i < sideBolts; i++)
        {
            Vector2 start = new Vector2(Random.Range(-0.5f, 0.5f) * rect.width, rect.yMax);
            Vector2 end = new Vector2(Random.Range(-0.5f, 0.5f) * rect.width,
                                      Mathf.Lerp(rect.yMax, rect.yMin, Random.Range(0.5f, 1.0f)));
            AddBolt(start, end, 0.6f, sideColor, 5, 1);
        }

        SetVerticesDirty();
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        SetVerticesDirty();
    }

    // Adds a jagged path from `start` to `end`, then forks off it `forkDepth` levels deep.
    void AddBolt(Vector2 start, Vector2 end, float thickness, Color color, int detail, int forkDepth)
    {
        List<Vector2> points = Jagged(start, end, detail);
        paths.Add(new Path { points = points, thickness = thickness, color = color });
        if (forkDepth <= 0) return;

        int forks = Random.Range(3, 7);
        for (int f = 0; f < forks; f++)
        {
            // Fork from somewhere along the first 80% of this path.
            int index = Random.Range(1, Mathf.Max(2, (int)(points.Count * 0.8f)));
            Vector2 from = points[index];
            Vector2 direction = (points[Mathf.Min(index + 1, points.Count - 1)] - from).normalized;
            if (direction == Vector2.zero) direction = Vector2.down;

            // Branch off at an angle, for a fraction of the parent's length.
            float angle = Random.Range(20.0f, 55.0f) * (Random.value < 0.5f ? -1.0f : 1.0f);
            float length = Vector2.Distance(start, end) * Random.Range(0.15f, 0.4f);
            Vector2 to = from + (Vector2)(Quaternion.Euler(0, 0, angle) * direction) * length;

            AddBolt(from, to, thickness * 0.45f, color, detail - 2, forkDepth - 1);
        }
    }

    // Midpoint displacement: repeatedly splits every segment and nudges the new midpoint
    // sideways by a shrinking random amount, giving the fine zig-zag of real lightning.
    static List<Vector2> Jagged(Vector2 start, Vector2 end, int detail)
    {
        List<Vector2> points = new List<Vector2> { start, end };
        float offset = Vector2.Distance(start, end) * 0.15f;
        for (int level = 0; level < Mathf.Max(detail, 2); level++)
        {
            List<Vector2> next = new List<Vector2>(points.Count * 2);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 side = Vector2.Perpendicular((b - a).normalized);
                next.Add(a);
                next.Add((a + b) / 2.0f + side * Random.Range(-offset, offset));
            }
            next.Add(points[points.Count - 1]);
            points = next;
            offset *= 0.55f;
        }
        return points;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        // Anything below this height hasn't been reached by the strike yet.
        float cutoff = Mathf.Lerp(rect.yMax, rect.yMin, progress);

        // Faint wide glow first, then the bright inner glow, then the white core on top.
        foreach (Path path in paths) AddSoftLine(vh, path, outerGlowWidth, Tint(path.color, 0.18f), cutoff);
        foreach (Path path in paths) AddSoftLine(vh, path, innerGlowWidth, Tint(path.color, 0.8f), cutoff);
        foreach (Path path in paths) AddSoftLine(vh, path, coreWidth, Tint(Color.white, 1.0f), cutoff);
    }

    Color Tint(Color baseColor, float alpha)
    {
        Color tinted = baseColor * color;
        tinted.a = alpha * color.a;
        return tinted;
    }

    // Draws a line that is solid in the middle and fades to transparent at both edges.
    static void AddSoftLine(VertexHelper vh, Path path, float width, Color color, float cutoff)
    {
        List<Vector2> points = path.points;
        Color edge = color;
        edge.a = 0.0f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            if (a.y < cutoff) return;
            if (b.y < cutoff) b = Vector2.Lerp(a, b, (a.y - cutoff) / Mathf.Max(a.y - b.y, 0.0001f));

            // Thinner and dimmer towards the end of the path, like a real bolt tapering off.
            float taper = Mathf.Lerp(1.0f, 0.15f, (float)i / points.Count);
            Vector2 side = Vector2.Perpendicular((b - a).normalized) * (width * path.thickness * taper / 2.0f);
            Color middle = color;
            middle.a *= Mathf.Lerp(1.0f, 0.3f, (float)i / points.Count);

            int first = vh.currentVertCount;
            AddVertex(vh, a - side, edge);
            AddVertex(vh, a, middle);
            AddVertex(vh, a + side, edge);
            AddVertex(vh, b - side, edge);
            AddVertex(vh, b, middle);
            AddVertex(vh, b + side, edge);

            vh.AddTriangle(first, first + 1, first + 4);
            vh.AddTriangle(first, first + 4, first + 3);
            vh.AddTriangle(first + 1, first + 2, first + 5);
            vh.AddTriangle(first + 1, first + 5, first + 4);
        }
    }

    static void AddVertex(VertexHelper vh, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vh.AddVert(vertex);
    }
}
