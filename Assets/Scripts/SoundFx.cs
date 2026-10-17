using UnityEngine;

// Sound effects for the end screens, synthesized in code so no audio files are needed.
public static class SoundFx
{
    const int SampleRate = 44100;

    // A sharp thunder crack followed by a long, uneven rumble.
    public static AudioClip Thunder()
    {
        float[] data = new float[Samples(2.5f)];
        float rumble = 0.0f;
        float swell = 0.0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float noise = Random.Range(-1.0f, 1.0f);

            // The crack: bright noise that dies away almost instantly.
            float crack = noise * Mathf.Exp(-t * 25.0f);

            // The rumble: heavily smoothed noise (deep), swelling and fading unevenly.
            rumble += (noise - rumble) * 0.02f;
            swell += (Random.Range(0.3f, 1.0f) - swell) * 0.0005f;
            float envelope = Mathf.Min(t / 0.05f, 1.0f) * Mathf.Exp(-t * 1.5f);
            data[i] = crack * 0.8f + rumble * 6.0f * envelope * swell;
        }
        return Clip("Thunder", data);
    }

    // A quick retro arpeggio: C, E, G, then a held high C.
    public static AudioClip Victory()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        float[] lengths = { 0.1f, 0.1f, 0.1f, 0.45f };
        float[] data = new float[Samples(0.75f)];
        int position = 0;
        for (int n = 0; n < notes.Length; n++)
        {
            int count = Samples(lengths[n]);
            for (int i = 0; i < count && position < data.Length; i++, position++)
            {
                float t = (float)i / SampleRate;
                // Square wave for a chiptune sound, with a short fade so notes don't click.
                float wave = Mathf.Sign(Mathf.Sin(2.0f * Mathf.PI * notes[n] * t));
                float envelope = Mathf.Min(t / 0.005f, 1.0f) * Mathf.Exp(-t * (n == notes.Length - 1 ? 4.0f : 8.0f));
                data[position] = wave * envelope * 0.25f;
            }
        }
        return Clip("Victory", data);
    }

    // A deep boom that sinks in pitch, with a dissonant drone hanging underneath.
    public static AudioClip Doom()
    {
        float[] data = new float[Samples(3.5f)];
        float phaseLow = 0.0f;
        float phaseHigh = 0.0f;
        float rumble = 0.0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;

            // Boom: a sine sliding from 110 Hz down to 35 Hz.
            float frequency = Mathf.Lerp(110.0f, 35.0f, Mathf.Min(t / 1.5f, 1.0f));
            phaseLow += 2.0f * Mathf.PI * frequency / SampleRate;
            float boom = Mathf.Sin(phaseLow) * Mathf.Exp(-t * 1.2f);

            // Drone: a tritone above, wobbling slightly, fading in and out.
            phaseHigh += 2.0f * Mathf.PI * (155.6f + Mathf.Sin(t * 5.0f) * 2.0f) / SampleRate;
            float drone = Mathf.Sin(phaseHigh) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 3.5f)) * 0.35f;

            rumble += (Random.Range(-1.0f, 1.0f) - rumble) * 0.01f;
            data[i] = (boom * 0.8f + drone + rumble * 2.0f * Mathf.Exp(-t * 2.0f)) * 0.6f;
        }
        return Clip("Doom", data);
    }

    // A single liquid "plip": a short tone that bends upwards. `pitch` around 1 varies it.
    public static AudioClip Drip(float pitch)
    {
        float[] data = new float[Samples(0.15f)];
        float phase = 0.0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float frequency = Mathf.Lerp(400.0f, 1300.0f, Mathf.Min(t / 0.05f, 1.0f)) * pitch;
            phase += 2.0f * Mathf.PI * frequency / SampleRate;
            data[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 30.0f) * 0.5f;
        }
        return Clip("Drip", data);
    }

    static int Samples(float seconds)
    {
        return (int)(seconds * SampleRate);
    }

    static AudioClip Clip(string name, float[] data)
    {
        // Keep the loudest point just under full volume so nothing distorts.
        float peak = 0.0001f;
        foreach (float sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
        float gain = Mathf.Min(1.0f, 0.95f / peak);
        for (int i = 0; i < data.Length; i++) data[i] *= gain;

        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
