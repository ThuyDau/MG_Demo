using System;
using UnityEngine;

/// <summary>
/// Holds the set of colors WaterFlowController can cycle through
/// (Red, Green, Blue, Purple, ...). Assign one asset to a WaterFlowController.
/// </summary>
[CreateAssetMenu(fileName = "WaterColorPalette", menuName = "Water/Water Color Palette")]
public class WaterColorPalette : ScriptableObject
{
    [Serializable]
    public class WaterColorEntry
    {
        public string colorName = "Color";
        public Color baseColor = Color.white;

        // Not wired into the shader yet — reserved for a later pass
        // (e.g. gradient shading / rim highlight). See file header notes.
        public Color darkColor = Color.gray;
        public Color highlightColor = Color.white;
    }

    public WaterColorEntry[] colors = new WaterColorEntry[]
    {
        new WaterColorEntry { colorName = "Red",    baseColor = new Color(0.80f, 0.10f, 0.10f) },
        new WaterColorEntry { colorName = "Green",  baseColor = new Color(0.10f, 0.75f, 0.20f) },
        new WaterColorEntry { colorName = "Blue",   baseColor = new Color(0.10f, 0.35f, 0.90f) },
        new WaterColorEntry { colorName = "Purple", baseColor = new Color(0.55f, 0.15f, 0.85f) },
    };
}
