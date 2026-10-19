using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The lose screen: the screen goes black, a blood-red "You Lose!" fades in, blood drips
// run down from the bottom of its letters, and after a moment Replay and Home buttons show up.
// Everything is built at runtime by LoseScreen.Play(), so nothing needs to be set up in the scene.
public class LoseScreen : EndScreen
{
    // How long the text takes to fade in, and how long the drips keep running.
    public float textFade = 0.8f;
    public float dripTime = 6.0f;

    // Delay after the text appears before the Replay and Home buttons show up.
    public float replayDelay = 2.5f;

    // The text and the drips share this color so the blood looks like it comes out of the letters.
    public Color bloodColor = new Color(0.6f, 0.0f, 0.02f);

    private TextMeshProUGUI loseLabel;
    private CanvasGroup loseGroup;
    private BloodDrips drips;
    private CanvasGroup replayGroup;

    // When each drip starts, so a few of them can make a sound.
    private readonly List<float> dripDelays = new List<float>();

    public static void Play(GameObject winTextObject)
    {
        LoseScreen screen = Create<LoseScreen>("LoseScreen");
        if (screen == null) return;

        // The lose screen shows its own text, so hide the HUD one.
        TextMeshProUGUI hudLabel = winTextObject.GetComponent<TextMeshProUGUI>();
        winTextObject.SetActive(false);

        screen.Build(hudLabel);
        screen.StartCoroutine(screen.Animate());
    }

    void Build(TextMeshProUGUI hudLabel)
    {
        Image background = CreateFullScreen<Image>("Background");
        background.color = Color.black;

        loseLabel = CreateTitle("You Lose!", hudLabel, 140, bloodColor);
        loseGroup = loseLabel.gameObject.AddComponent<CanvasGroup>();
        loseGroup.alpha = 0.0f;

        // Drawn over the text, in the same color, so the drips merge into the letters.
        drips = CreateFullScreen<BloodDrips>("BloodDrips");
        drips.color = bloodColor;
        drips.raycastTarget = false;

        replayGroup = CreateEndButtons(loseLabel.font, new Color(0.35f, 0.0f, 0.0f), Color.white);
    }

    IEnumerator Animate()
    {
        // Freeze the game behind the lose screen; the animation runs on unscaled time.
        Time.timeScale = 0.0f;

        // Wait a frame so the new canvas has its screen size before the letters are measured.
        yield return null;
        AddDripsUnderLetters();

        PlaySound(SoundFx.Doom());
        yield return Over(textFade, t => loseGroup.alpha = t);

        // Drips run while we wait for the buttons, and keep going after they appear.
        StartCoroutine(Over(dripTime, t => drips.SetTime(t * dripTime)));
        StartCoroutine(PlayDripSounds());
        yield return new WaitForSecondsRealtime(replayDelay);
        yield return ShowReplay(replayGroup);
    }

    // Puts one to three drips under each letter, starting slightly inside it so they look attached.
    void AddDripsUnderLetters()
    {
        loseLabel.ForceMeshUpdate();
        TMP_TextInfo info = loseLabel.textInfo;
        float letterHeight = 0.0f;

        for (int c = 0; c < info.characterCount; c++)
        {
            TMP_CharacterInfo character = info.characterInfo[c];
            if (!character.isVisible) continue;

            // Use the letter's ink, not its glyph box, so drips start on the letter itself.
            float left = character.bottomLeft.x;
            float right = character.topRight.x;
            float bottom = character.baseLine;
            letterHeight = Mathf.Max(letterHeight, character.topRight.y - bottom);

            int count = Random.Range(1, 4);
            for (int d = 0; d < count; d++)
            {
                float x = Mathf.Lerp(left, right, Random.Range(0.15f, 0.85f));
                Vector2 start = new Vector2(x, bottom + 8.0f);
                float width = Random.Range(6.0f, 16.0f);
                // Mostly short drips with the occasional long run.
                float length = Random.value < 0.3f ? Random.Range(250.0f, 450.0f) : Random.Range(40.0f, 200.0f);
                float delay = Random.Range(0.0f, 1.5f);
                float duration = Random.Range(2.5f, 5.0f);
                drips.AddDrip(start, width, length, delay, Mathf.Min(duration, dripTime - delay));
                dripDelays.Add(delay);
            }
        }
    }

    // A "plip" for some of the drips as they start; every drip would be too busy.
    IEnumerator PlayDripSounds()
    {
        dripDelays.Sort();
        float elapsed = 0.0f;
        for (int i = 0; i < dripDelays.Count; i += 3)
        {
            yield return new WaitForSecondsRealtime(dripDelays[i] - elapsed);
            elapsed = dripDelays[i];
            PlaySound(SoundFx.Drip(Random.Range(0.8f, 1.25f)), 0.5f);
        }
    }
}
