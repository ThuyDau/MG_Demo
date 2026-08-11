using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A bottle: holds a stack of up to MaxLayers colors (bottom to top), each
/// rendered as a discrete Color_Tex block at native scale (1,1,1). Supports
/// standard water-sort rules via TopColor/TopRunLength.
/// </summary>
[DisallowMultipleComponent]
public class LiquidContainer : MonoBehaviour
{
    public int MaxLayers { get; private set; }
    public int LayerCount => _colors.Count;
    public bool IsEmpty => _colors.Count == 0;
    public bool IsFull => _colors.Count >= MaxLayers;
    public Color TopColor => _colors.Count > 0 ? _colors[_colors.Count - 1] : Color.clear;

    private readonly List<Color> _colors = new List<Color>();
    private readonly List<Transform> _layerRoots = new List<Transform>();
    private readonly List<SpriteRenderer> _layerRenderers = new List<SpriteRenderer>();

    // Pre-creates MaxLayers Color_Tex instances (kept at whatever scale the prefab itself defines),
    // one per stack slot, all hidden.
    public void Initialize(Transform parent, GameObject colorTexPrefab, float slotBottomY, float slotSpacing, int maxLayers)
    {
        MaxLayers = maxLayers;
        _colors.Clear();
        _layerRoots.Clear();
        _layerRenderers.Clear();

        for (int i = 0; i < maxLayers; i++)
        {
            GameObject layerGO = Instantiate(colorTexPrefab, parent);
            layerGO.name = colorTexPrefab.name;
            layerGO.transform.localPosition = new Vector3(0f, slotBottomY + i * slotSpacing, 0f);
            layerGO.SetActive(false);

            SpriteRenderer renderer = layerGO.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                // Higher slots (further up the stack) draw on top of lower ones,
                // starting from whatever base sortingOrder the prefab itself defines.
                renderer.sortingOrder += i;
            }

            _layerRoots.Add(layerGO.transform);
            _layerRenderers.Add(renderer);
        }
    }

    public void SetStack(IReadOnlyList<Color> colorsBottomToTop)
    {
        _colors.Clear();
        _colors.AddRange(colorsBottomToTop);
        RefreshVisuals();
    }

    public void PushLayer(Color color)
    {
        _colors.Add(color);
        RefreshVisuals();
    }

    public Color PopLayer()
    {
        Color color = _colors[_colors.Count - 1];
        _colors.RemoveAt(_colors.Count - 1);
        RefreshVisuals();
        return color;
    }

    // How many layers of the top color are contiguous — the amount a pour can move.
    public int TopRunLength()
    {
        if (_colors.Count == 0)
        {
            return 0;
        }

        Color top = _colors[_colors.Count - 1];
        int run = 0;
        for (int i = _colors.Count - 1; i >= 0; i--)
        {
            if (!ColorsApproximatelyEqual(_colors[i], top))
            {
                break;
            }
            run++;
        }
        return run;
    }

    public static bool ColorsApproximatelyEqual(Color a, Color b)
    {
        return Vector4.Distance(a, b) < 0.01f;
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < _layerRoots.Count; i++)
        {
            bool active = i < _colors.Count;
            _layerRoots[i].gameObject.SetActive(active);
            if (active && _layerRenderers[i] != null)
            {
                _layerRenderers[i].color = _colors[i];
            }
        }
    }

    // Repositions the existing slots live (e.g. from PourGamePrototype.OnValidate while tuning).
    public void UpdateSlots(float slotBottomY, float slotSpacing)
    {
        for (int i = 0; i < _layerRoots.Count; i++)
        {
            _layerRoots[i].localPosition = new Vector3(0f, slotBottomY + i * slotSpacing, 0f);
        }
    }
}
