using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The win screen: the screen goes black, a lightning bolt strikes, "You Win!" appears
// behind it, and after a moment a Replay button shows up.
// Everything is built at runtime by WinCelebration.Play(), so nothing needs to be set up in the scene.
public class WinCelebration : EndScreen
{
    // Darkness before the strike, and how long the bolt takes to reach the bottom.
    public float darkPause = 0.1f;
    public float strikeTime = 0.12f;

    // How long the white flash and the bolt take to fade out.
    public float flashFade = 0.3f;
    public float boltFade = 0.3f;

    // Delay after the strike before the Replay button appears.
    public float replayDelay = 0.3f;

    // Color of the bolt icons on either side of the text.
    public Color iconColor = new Color(1.0f, 0.85f, 0.2f);

    private TextMeshProUGUI winLabel;
    private CanvasGroup winGroup;
    private BoltIcon[] icons;
    private LightningBolt bolt;
    private Image flash;
    private CanvasGroup replayGroup;

    public static void Play(GameObject winTextObject)
    {
        WinCelebration screen = Create<WinCelebration>("WinCelebration");
        if (screen == null) return;

        // The win screen shows its own text, so hide the HUD one.
        TextMeshProUGUI hudLabel = winTextObject.GetComponent<TextMeshProUGUI>();
        winTextObject.SetActive(false);

        screen.Build(hudLabel);
        screen.StartCoroutine(screen.Animate());
    }

    // Black background, text, bolt icons, bolt, flash and Replay button.
    void Build(TextMeshProUGUI hudLabel)
    {
        // Black background covering the game and the HUD.
        Image background = CreateFullScreen<Image>("Background");
        background.color = Color.black;

        // "You Win!" sits behind the bolt and is hidden until it strikes.
        winLabel = CreateTitle("You Win!", hudLabel, 120, Color.white);
        winGroup = winLabel.gameObject.AddComponent<CanvasGroup>();
        winGroup.alpha = 0.0f;

        // A bolt icon on each side of the text, hidden until the strike.
        // They're moved into place beside the text in AnimateIcons(), once the text has its real size.
        icons = new BoltIcon[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject iconObject = new GameObject("BoltIcon", typeof(RectTransform), typeof(CanvasRenderer));
            iconObject.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)iconObject.transform;
            rect.sizeDelta = new Vector2(140, 200);
            // Mirror the left one so the pair is symmetrical.
            rect.localScale = new Vector3(i == 0 ? -1 : 1, 1, 1);
            icons[i] = iconObject.AddComponent<BoltIcon>();
            icons[i].color = iconColor;
            icons[i].raycastTarget = false;
            icons[i].enabled = false;
        }

        bolt = CreateFullScreen<LightningBolt>("LightningBolt");
        bolt.color = Color.white;
        bolt.raycastTarget = false;

        flash = CreateFullScreen<Image>("Flash");
        flash.color = new Color(1, 1, 1, 0);
        flash.raycastTarget = false;

        replayGroup = CreateReplayButton(winLabel.font, Color.white, Color.black);
    }

    IEnumerator Animate()
    {
        // Freeze the game behind the win screen; the animation runs on unscaled time.
        Time.timeScale = 0.0f;

        // Wait a frame so the new canvas has its screen size before the bolt is shaped.
        yield return null;
        bolt.Regenerate();
        bolt.SetProgress(0.0f);
        yield return new WaitForSecondsRealtime(darkPause);

        // The bolt races down from the top of the screen.
        yield return Over(strikeTime, t => bolt.SetProgress(t));

        // Impact: white flash, and the text is suddenly there behind the bolt.
        flash.color = new Color(1, 1, 1, 0.9f);
        winGroup.alpha = 1.0f;
        PlaySound(SoundFx.Thunder());
        StartCoroutine(PlayVictoryJingle());
        StartCoroutine(AnimateIcons());

        // The bolt flickers, jumping to a new shape once, while the flash fades.
        StartCoroutine(Over(flashFade, t => flash.color = new Color(1, 1, 1, 0.9f * (1.0f - t))));
        for (int i = 0; i < 4; i++)
        {
            bolt.enabled = i % 2 == 1;
            if (i == 2) bolt.Regenerate();
            yield return new WaitForSecondsRealtime(0.06f);
        }
        bolt.enabled = true;

        // The bolt fades away, leaving just the text.
        yield return Over(boltFade, t => bolt.color = new Color(1, 1, 1, 1.0f - t));
        bolt.enabled = false;

        yield return new WaitForSecondsRealtime(replayDelay);
        yield return ShowReplay(replayGroup);
    }

    // The jingle comes in just after the thunder crack.
    IEnumerator PlayVictoryJingle()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        PlaySound(SoundFx.Victory(), 0.7f);
    }

    // The icons slam in from large to normal size, then flicker like live electricity.
    IEnumerator AnimateIcons()
    {
        // Place the icons just outside the text, but always on screen.
        winLabel.ForceMeshUpdate();
        float iconX = Mathf.Clamp(winLabel.textBounds.extents.x + 110.0f, 200.0f, 850.0f);

        Vector3[] scales = new Vector3[icons.Length];
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].rectTransform.anchoredPosition = new Vector2(i == 0 ? -iconX : iconX, 0);
            scales[i] = icons[i].rectTransform.localScale;
            icons[i].enabled = true;
        }

        yield return Over(0.25f, t =>
        {
            float grow = Mathf.Lerp(1.8f, 1.0f, t * t);
            for (int i = 0; i < icons.Length; i++) icons[i].rectTransform.localScale = scales[i] * grow;
        });

        // Every so often, one icon blinks dim for a moment.
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(0.15f, 0.8f));
            BoltIcon icon = icons[Random.Range(0, icons.Length)];
            icon.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f);
            yield return new WaitForSecondsRealtime(0.05f);
            icon.color = iconColor;
        }
    }
}
