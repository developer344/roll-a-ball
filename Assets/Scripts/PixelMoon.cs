using UnityEngine;
using UnityEngine.UI;

// A pixel-art moon: each frame it paints a tiny texture (64x64 by default) with the moon map
// wrapped around a shaded sphere, banded lighting and a stepped halo, then shows it on this
// RawImage without smoothing so every pixel stays a crisp block. It turns in small steps.
[RequireComponent(typeof(RawImage))]
public class PixelMoon : MonoBehaviour
{
    // Moon map in longitude/latitude layout. Without one, the moon is plain grey.
    public Texture sourceTexture;

    // Texture size in pixels, and the moon's radius inside it.
    public int size = 64;
    public float moonRadius = 22.0f;

    // Degrees turned per second, and how often the picture updates (a choppy, retro spin).
    public float spinSpeed = 8.0f;
    public float frameTime = 0.15f;

    public Color haloColor = new Color(0.85f, 0.88f, 1.0f);

    // Direction the light comes from (x right, y up, z towards the viewer).
    public Vector3 lightDirection = new Vector3(-0.5f, 0.35f, 0.8f);

    // Brightness bands, darkest to brightest, for a pixel-art shading look.
    static readonly float[] ShadeLevels = { 0.3f, 0.55f, 0.8f, 1.05f };

    private Texture2D map;
    private Texture2D pixels;
    private Color32[] buffer;
    private float spin;
    private float timer;

    void Start()
    {
        map = MakeReadableCopy(sourceTexture, 256, 128);
        pixels = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        buffer = new Color32[size * size];
        GetComponent<RawImage>().texture = pixels;
        Draw();
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < frameTime) return;
        timer = 0.0f;
        spin += spinSpeed * frameTime;
        Draw();
    }

    void OnDestroy()
    {
        if (map != null) Destroy(map);
        if (pixels != null) Destroy(pixels);
    }

    void Draw()
    {
        Vector3 light = lightDirection.normalized;
        float center = (size - 1) / 2.0f;
        float spinOffset = spin / 360.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / moonRadius;
                float dy = (y - center) / moonRadius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                // How many texture pixels outside the moon's edge this pixel is.
                float outside = (distance - 1.0f) * moonRadius;
                Color color;

                if (distance <= 1.0f)
                {
                    // Point on the sphere, and which part of the moon map is visible there.
                    float z = Mathf.Sqrt(1.0f - distance * distance);
                    float latitude = Mathf.Asin(dy);
                    float longitude = Mathf.Atan2(dx, z);
                    float u = Mathf.Repeat(0.5f + longitude / (Mathf.PI * 2.0f) + spinOffset, 1.0f);
                    float v = 0.5f + latitude / Mathf.PI;
                    Color surface = map != null ? map.GetPixelBilinear(u, v) : new Color(0.75f, 0.75f, 0.75f);

                    float lit = Mathf.Clamp01(Vector3.Dot(new Vector3(dx, dy, z), light));
                    float shade = ShadeLevels[Mathf.Min((int)(lit * ShadeLevels.Length), ShadeLevels.Length - 1)];
                    color = surface * shade;
                    color.a = 1.0f;
                }
                else if (outside <= 3.0f)
                {
                    // Inner halo ring.
                    color = haloColor;
                    color.a = 0.35f;
                }
                else if (outside <= 6.0f && (x + y) % 2 == 0)
                {
                    // Outer halo, dithered in a checkerboard like old-school pixel art.
                    color = haloColor;
                    color.a = 0.2f;
                }
                else
                {
                    color = Color.clear;
                }

                buffer[y * size + x] = color;
            }
        }

        pixels.SetPixels32(buffer);
        pixels.Apply(false);
    }

    // Imported textures usually can't be read from scripts, so copy it through the GPU at a small size.
    static Texture2D MakeReadableCopy(Texture source, int width, int height)
    {
        if (source == null) return null;

        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, temporary);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temporary;

        Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        copy.Apply(false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return copy;
    }
}
