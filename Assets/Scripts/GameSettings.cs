using UnityEngine;

// Settings chosen in the start menu, saved between sessions with PlayerPrefs.
public static class GameSettings
{
    const string DifficultyKey = "Difficulty";

    public static readonly string[] DifficultyNames = { "EASY", "NORMAL", "HARD" };

    public static readonly string[] DifficultyDescriptions =
    {
        "The enemy is slow",
        "The enemy chases at normal speed",
        "The enemy is fast!",
    };

    // How much faster or slower than normal the enemy moves at each difficulty.
    static readonly float[] EnemySpeedMultipliers = { 0.7f, 1.0f, 1.5f };

    // 0 = Easy, 1 = Normal (the default), 2 = Hard.
    public static int Difficulty
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, DifficultyNames.Length - 1);
        set
        {
            PlayerPrefs.SetInt(DifficultyKey, Mathf.Clamp(value, 0, DifficultyNames.Length - 1));
            PlayerPrefs.Save();
        }
    }

    public static float EnemySpeedMultiplier => EnemySpeedMultipliers[Difficulty];
}
