using UnityEngine;

// A small flat triangle that floats above a target and points down at it.
// It follows the target's position but not its rotation (so it doesn't spin with a rolling ball),
// always turns to face the camera, and removes itself when the target is destroyed.
// Use MarkerArrow.Create(target, color).
public class MarkerArrow : MonoBehaviour
{
    // Gap between the top of the target and the tip of the triangle.
    public float gap = 0.3f;

    private Transform target;

    // How far the top of the target is above its position, measured once at the start.
    // (Measuring every frame would make the marker bob: a rolling ball's bounding box grows and
    // shrinks as it rotates.)
    private float targetHeight;

    public static MarkerArrow Create(Transform target, Color color)
    {
        GameObject arrowObject = new GameObject(target.name + " Arrow");
        MarkerArrow arrow = arrowObject.AddComponent<MarkerArrow>();
        arrow.target = target;
        foreach (Renderer part in target.GetComponentsInChildren<Renderer>())
        {
            arrow.targetHeight = Mathf.Max(arrow.targetHeight, part.bounds.max.y - target.position.y);
        }

        // A dark outline arrow slightly behind, then the colored arrow in front of it.
        AddArrowMesh(arrowObject.transform, "Outline", new Color(0.02f, 0.02f, 0.06f), 1.35f, 0.02f, -0.02f);
        AddArrowMesh(arrowObject.transform, "Fill", color, 1.0f, 0.0f, 0.0f);

        arrow.LateUpdate();
        return arrow;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Float a fixed distance above the target, so it works for any size of object.
        transform.position = target.position + Vector3.up * (targetHeight + gap);

        // Face the camera fully, tilting towards it when it looks down from above, so the outline
        // (which sits just behind the triangle) shows evenly all round. The tip still points down.
        Camera view = Camera.main;
        if (view != null)
        {
            Vector3 away = transform.position - view.transform.position;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }

    // A small flat triangle pointing down, with its tip at the origin.
    // `drop` moves it down, e.g. so the outline shows a little more below the tip.
    static void AddArrowMesh(Transform parent, string name, Color color, float scale, float depth, float drop)
    {
        Vector3[] outline =
        {
            new Vector3(0.0f, 0.0f),     // tip
            new Vector3(-0.15f, 0.22f),  // top left
            new Vector3(0.15f, 0.22f),   // top right
        };

        // Scale around the triangle's middle, so the outline sticks out evenly on every side.
        Vector3 middle = new Vector3(0.0f, 0.12f);
        Vector3[] vertices = new Vector3[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            vertices[i] = middle + (outline[i] - middle) * scale + new Vector3(0, drop, depth);
        }

        // One triangle, added in both windings so it's visible from either side.
        int[] front = { 0, 1, 2 };
        int[] triangles = new int[front.Length * 2];
        for (int i = 0; i < front.Length; i += 3)
        {
            triangles[i] = front[i];
            triangles[i + 1] = front[i + 1];
            triangles[i + 2] = front[i + 2];
            triangles[front.Length + i] = front[i];
            triangles[front.Length + i + 1] = front[i + 2];
            triangles[front.Length + i + 2] = front[i + 1];
        }

        Mesh mesh = new Mesh { name = "Arrow" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material = UnlitMaterial(color);
    }

    // A flat color that ignores scene lighting, so the arrow reads clearly anywhere.
    static Material UnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        return material;
    }
}
