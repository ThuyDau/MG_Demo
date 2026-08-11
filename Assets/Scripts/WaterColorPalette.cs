using System;
using UnityEngine;

/// <summary>
/// Holds the set of colors WaterFlowController can cycle through, plus a
/// handful of levels that each pick which of those colors are in play.
/// </summary>
[CreateAssetMenu(fileName = "WaterColorPalette", menuName = "Water/Water Color Palette")]
public class WaterColorPalette : ScriptableObject
{
    [Serializable]
    public class WaterColorEntry
    {
        public string colorName = "Color";
        public Color baseColor = Color.white;
    }

    [Serializable]
    public class LevelDefinition
    {
        public string levelName = "Level 1";
        public int[] colorIndices = new int[0];
    }

    public WaterColorEntry[] colors = new WaterColorEntry[]
    {
        new WaterColorEntry { colorName = "Red",    baseColor = new Color(0.80f, 0.10f, 0.10f) },
        new WaterColorEntry { colorName = "Green",  baseColor = new Color(0.10f, 0.75f, 0.20f) },
        new WaterColorEntry { colorName = "Blue",   baseColor = new Color(0.10f, 0.35f, 0.90f) },
        new WaterColorEntry { colorName = "Purple", baseColor = new Color(0.55f, 0.15f, 0.85f) },
        new WaterColorEntry { colorName = "Pink",   baseColor = new Color(0.95f, 0.45f, 0.65f) },
        new WaterColorEntry { colorName = "Orange", baseColor = new Color(0.95f, 0.55f, 0.10f) },
        new WaterColorEntry { colorName = "Yellow", baseColor = new Color(0.95f, 0.85f, 0.15f) },
        new WaterColorEntry { colorName = "Cyan",   baseColor = new Color(0.15f, 0.75f, 0.75f) },
        new WaterColorEntry { colorName = "Maroon", baseColor = new Color(0.50f, 0.10f, 0.15f) },
        new WaterColorEntry { colorName = "SkyBlue",baseColor = new Color(0.45f, 0.70f, 0.95f) },
    };

    public LevelDefinition[] levels = new LevelDefinition[]
    {
        new LevelDefinition { levelName = "Level 1", colorIndices = new[] { 0, 1 } },
        new LevelDefinition { levelName = "Level 2", colorIndices = new[] { 0, 1, 2 } },
        new LevelDefinition { levelName = "Level 3", colorIndices = new[] { 0, 1, 2, 3 } },
        new LevelDefinition { levelName = "Level 4", colorIndices = new[] { 0, 1, 2, 3 } },
        new LevelDefinition { levelName = "Level 5", colorIndices = new[] { 0, 1, 2, 3 } },
    };

    public Color[] GetLevelColors(int levelIndex)
    {
        if (levels == null || levels.Length == 0 || colors == null || colors.Length == 0)
        {
            return Array.Empty<Color>();
        }

        LevelDefinition level = levels[Mathf.Clamp(levelIndex, 0, levels.Length - 1)];
        var result = new Color[level.colorIndices.Length];
        for (int i = 0; i < level.colorIndices.Length; i++)
        {
            int idx = Mathf.Clamp(level.colorIndices[i], 0, colors.Length - 1);
            result[i] = colors[idx].baseColor;
        }
        return result;
    }
}
