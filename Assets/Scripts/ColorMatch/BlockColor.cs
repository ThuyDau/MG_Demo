using UnityEngine;

namespace ColorMatch
{
    // The 4 colors used for matching bees/trays to Rocket blocks. Enum comparison is
    // exact (unlike comparing UnityEngine.Color values), so matching logic can never
    // drift due to float precision.
    public enum BlockColor
    {
        Red,
        White,
        Navy,
        Black
    }

    public static class BlockColorUtility
    {
        public static Color ToColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return new Color(0.85f, 0.15f, 0.15f);
                case BlockColor.White: return new Color(0.92f, 0.92f, 0.92f);
                case BlockColor.Navy: return new Color(0.06f, 0.10f, 0.35f);
                case BlockColor.Black: return new Color(0.08f, 0.08f, 0.08f);
                default: return Color.magenta;
            }
        }
    }
}
