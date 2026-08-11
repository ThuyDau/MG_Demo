using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D Water-Sort style prototype: a grid of bottles, some pre-filled with a
/// shuffled stack of colors (up to MaxLayers each), a few empty. Click a
/// bottle to select it (it rises slightly), click a second bottle to pour
/// into it — standard water-sort rules: the destination must be empty or
/// share the source's top color, and only the source's top contiguous
/// color run moves, up to the destination's remaining room.
/// </summary>
[DisallowMultipleComponent]
public class PourGamePrototype : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Tray_Opject: the bottle sprite (Front + Bottom).")]
    [SerializeField] private GameObject bottlePrefab;
    [Tooltip("Color_Tex: one discrete liquid block, kept at its native size (scale 1) and stacked.")]
    [SerializeField] private GameObject colorTexPrefab;

    [Header("Palette")]
    [SerializeField] private WaterColorPalette palette;

    [Serializable]
    public class GridLevelConfig
    {
        public string levelName = "Level 1";
        public int totalBottles = 16;
        public int columnsPerRow = 8;
        [Tooltip("Flattened grid indices (row * columnsPerRow + col) that start empty.")]
        public int[] emptyBottleSlots = { 10, 13 };
        [Tooltip("Fixed seed so the shuffled layout is the same every Play.")]
        public int seed = 12345;
    }

    [Header("Levels")]
    [SerializeField] private GridLevelConfig[] levels =
    {
        new GridLevelConfig { levelName = "Level 1", totalBottles = 16, columnsPerRow = 8, emptyBottleSlots = new[] { 10, 13 }, seed = 12345 },
        new GridLevelConfig { levelName = "Level 2", totalBottles = 16, columnsPerRow = 8, emptyBottleSlots = new[] { 9, 14 }, seed = 23456 },
        new GridLevelConfig { levelName = "Level 3", totalBottles = 14, columnsPerRow = 7, emptyBottleSlots = new[] { 3, 10 }, seed = 34567 },
    };
    [SerializeField] private int currentLevelIndex = 0;
    [Tooltip("Max Color_Tex blocks stacked in a single bottle (shared by every level).")]
    [SerializeField] private int maxLayers = 4;

    [Header("Bottle Interior (tune to match the sprite art)")]
    [SerializeField] private float fillBottomLocalY = -4f;
    [SerializeField] private float fillTopLocalY = 4.2f;
    [SerializeField] private Vector2 colliderSize = new Vector2(4.04f, 11.16f);

    [Header("Grid Spacing")]
    [SerializeField] private float columnSpacingX = 4.6f;
    [SerializeField] private float rowSpacingY = 12f;
    [SerializeField] private float cameraOrthoSize = 14f;

    [Header("Interaction Timing")]
    [SerializeField] private float selectionRaiseY = 1f;
    [SerializeField] private float selectMoveDuration = 0.2f;
    [SerializeField] private float pourRiseOffset = 6f;
    [SerializeField] private float pourLayerDelay = 0.15f;

    private Camera _cam;
    private LiquidContainer[] _bottles;
    private LiquidContainer _selected;
    private Vector3 _selectedRestPosition;
    private bool _isBusy;
    private GridLevelConfig _activeLevel;
    private int _builtLevelIndex = -1;

    private void Start()
    {
        EnsureCamera();

        if (bottlePrefab == null || colorTexPrefab == null)
        {
            Debug.LogError("PourGamePrototype: assign Bottle Prefab (Tray_Opject) and Color Tex Prefab (Color_Tex) in the Inspector.");
            return;
        }

        if (palette == null)
        {
            Debug.LogWarning("PourGamePrototype: no palette assigned — bottles will use random colors.");
        }

        BuildLevel();
    }

    // Lets grid/sizing/camera fields be tuned live in the Inspector, including during Play.
    // Changing Current Level Index tears down and rebuilds with the new level's layout/seed.
    private void OnValidate()
    {
        if (_cam != null)
        {
            _cam.orthographicSize = cameraOrthoSize;
        }

        if (!Application.isPlaying || _bottles == null || _isBusy)
        {
            return;
        }

        if (currentLevelIndex != _builtLevelIndex)
        {
            RebuildLevel();
            return;
        }

        int rows = RowCount();
        float slotSpacing = SlotSpacing();

        for (int i = 0; i < _bottles.Length; i++)
        {
            if (_bottles[i] == null)
            {
                continue;
            }

            _bottles[i].transform.position = GridPosition(i / _activeLevel.columnsPerRow, i % _activeLevel.columnsPerRow, rows);
            _bottles[i].UpdateSlots(fillBottomLocalY, slotSpacing);

            var col = _bottles[i].GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = colliderSize;
            }
        }
    }

    private GridLevelConfig ActiveLevelOrFallback()
    {
        if (levels == null || levels.Length == 0)
        {
            return new GridLevelConfig();
        }
        return levels[Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1)];
    }

    private int RowCount()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)_activeLevel.totalBottles / _activeLevel.columnsPerRow));
    }

    private float SlotSpacing()
    {
        return maxLayers > 1 ? (fillTopLocalY - fillBottomLocalY) / (maxLayers - 1) : 0f;
    }

    private Vector3 GridPosition(int row, int col, int rows)
    {
        float x = (col - (_activeLevel.columnsPerRow - 1) * 0.5f) * columnSpacingX;
        float topRowY = (rows - 1) * rowSpacingY * 0.5f;
        float y = topRowY - row * rowSpacingY;
        return new Vector3(x, y, 0f);
    }

    private void RebuildLevel()
    {
        if (_bottles != null)
        {
            foreach (LiquidContainer bottle in _bottles)
            {
                if (bottle != null)
                {
                    Destroy(bottle.gameObject);
                }
            }
        }

        _selected = null;
        _isBusy = false;
        BuildLevel();
    }

    private void EnsureCamera()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            _cam = camGO.AddComponent<Camera>();
        }

        _cam.orthographic = true;
        _cam.orthographicSize = cameraOrthoSize;
        _cam.transform.position = new Vector3(0f, 0f, -10f);
        _cam.transform.rotation = Quaternion.identity;
    }

    private LiquidContainer CreateContainer(string name, Vector3 position)
    {
        GameObject bottleGO = Instantiate(bottlePrefab, position, Quaternion.identity);
        bottleGO.name = name;

        var collider = bottleGO.AddComponent<BoxCollider2D>();
        collider.size = colliderSize;

        var container = bottleGO.AddComponent<LiquidContainer>();
        container.Initialize(bottleGO.transform, colorTexPrefab, fillBottomLocalY, SlotSpacing(), maxLayers);
        return container;
    }

    private void BuildLevel()
    {
        _activeLevel = ActiveLevelOrFallback();
        _builtLevelIndex = currentLevelIndex;

        int rows = RowCount();
        var emptySet = new HashSet<int>(_activeLevel.emptyBottleSlots ?? Array.Empty<int>());
        int filledCount = _activeLevel.totalBottles - emptySet.Count;
        List<Color[]> stacks = GenerateShuffledStacks(filledCount, maxLayers, _activeLevel.seed);

        _bottles = new LiquidContainer[_activeLevel.totalBottles];
        int stackCursor = 0;

        for (int i = 0; i < _activeLevel.totalBottles; i++)
        {
            Vector3 pos = GridPosition(i / _activeLevel.columnsPerRow, i % _activeLevel.columnsPerRow, rows);
            var bottle = CreateContainer($"Bottle_{i}", pos);

            if (!emptySet.Contains(i) && stackCursor < stacks.Count)
            {
                bottle.SetStack(stacks[stackCursor]);
                stackCursor++;
            }

            _bottles[i] = bottle;
        }
    }

    // Builds filledCount stacks of maxLayers colors each: one "quad" (maxLayers
    // identical units) per filled bottle, cycling through the palette so counts
    // stay clean, then Fisher-Yates shuffled with a fixed seed for a reproducible
    // scrambled layout.
    private List<Color[]> GenerateShuffledStacks(int filledCount, int layersPerBottle, int seed)
    {
        var flat = new List<Color>(filledCount * layersPerBottle);
        bool hasPalette = palette != null && palette.colors != null && palette.colors.Length > 0;
        var rng = new System.Random(seed);

        for (int q = 0; q < filledCount; q++)
        {
            Color c = hasPalette
                ? palette.colors[q % palette.colors.Length].baseColor
                : UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 0.9f, 0.7f, 1f);
            for (int u = 0; u < layersPerBottle; u++)
            {
                flat.Add(c);
            }
        }

        for (int i = flat.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (flat[i], flat[j]) = (flat[j], flat[i]);
        }

        var stacks = new List<Color[]>(filledCount);
        for (int b = 0; b < filledCount; b++)
        {
            var stack = new Color[layersPerBottle];
            for (int l = 0; l < layersPerBottle; l++)
            {
                stack[l] = flat[b * layersPerBottle + l];
            }
            stacks.Add(stack);
        }

        return stacks;
    }

    private void Update()
    {
        if (_isBusy || _cam == null || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPoint = _cam.ScreenToWorldPoint(mousePos);
        Collider2D hit = Physics2D.OverlapPoint(worldPoint);
        if (hit == null)
        {
            return;
        }

        LiquidContainer clicked = FindBottle(hit.transform);
        if (clicked != null)
        {
            HandleBottleClick(clicked);
        }
    }

    private LiquidContainer FindBottle(Transform t)
    {
        foreach (LiquidContainer bottle in _bottles)
        {
            if (bottle.transform == t)
            {
                return bottle;
            }
        }
        return null;
    }

    private void HandleBottleClick(LiquidContainer clicked)
    {
        if (_selected == null)
        {
            if (clicked.IsEmpty)
            {
                return;
            }
            Select(clicked);
            return;
        }

        if (_selected == clicked)
        {
            Deselect();
            return;
        }

        StartCoroutine(PourRoutine(_selected, clicked));
    }

    private void Select(LiquidContainer bottle)
    {
        _selected = bottle;
        _selectedRestPosition = bottle.transform.position;
        StartCoroutine(RaiseSelected());
    }

    private IEnumerator RaiseSelected()
    {
        _isBusy = true;
        yield return MoveBottle(_selected.transform, _selectedRestPosition + new Vector3(0f, selectionRaiseY, 0f), selectMoveDuration);
        _isBusy = false;
    }

    private void Deselect()
    {
        LiquidContainer bottle = _selected;
        Vector3 restPos = _selectedRestPosition;
        _selected = null;
        StartCoroutine(LowerBottle(bottle, restPos));
    }

    private IEnumerator LowerBottle(LiquidContainer bottle, Vector3 restPos)
    {
        _isBusy = true;
        yield return MoveBottle(bottle.transform, restPos, selectMoveDuration);
        _isBusy = false;
    }

    private IEnumerator PourRoutine(LiquidContainer from, LiquidContainer to)
    {
        _isBusy = true;

        bool canPour = !from.IsEmpty && !to.IsFull &&
                       (to.IsEmpty || LiquidContainer.ColorsApproximatelyEqual(to.TopColor, from.TopColor));

        if (!canPour)
        {
            yield return RejectFeedback(to.transform);
            yield return MoveBottle(from.transform, _selectedRestPosition, selectMoveDuration);
            _selected = null;
            _isBusy = false;
            yield break;
        }

        Vector3 pourPosition = to.transform.position + new Vector3(0f, pourRiseOffset, 0f);
        yield return MoveBottle(from.transform, pourPosition, selectMoveDuration);

        int amount = Mathf.Min(from.TopRunLength(), to.MaxLayers - to.LayerCount);
        for (int i = 0; i < amount; i++)
        {
            Color c = from.PopLayer();
            to.PushLayer(c);
            yield return new WaitForSeconds(pourLayerDelay);
        }

        yield return MoveBottle(from.transform, _selectedRestPosition, selectMoveDuration);
        _selected = null;
        _isBusy = false;
    }

    private IEnumerator RejectFeedback(Transform target)
    {
        Vector3 basePos = target.position;
        const float shakeDuration = 0.3f;
        const float shakeStrength = 0.2f;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float offset = Mathf.Sin(t * 40f) * shakeStrength * (1f - t / shakeDuration);
            target.position = basePos + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        target.position = basePos;
    }

    private IEnumerator MoveBottle(Transform t, Vector3 target, float duration)
    {
        Vector3 start = t.position;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / duration));
            yield return null;
        }

        t.position = target;
    }
}
