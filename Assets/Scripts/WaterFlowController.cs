using UnityEngine;

/// <summary>
/// Drives WaterFlow.shader's color-transition properties on a single, fixed
/// mesh/material via MaterialPropertyBlock. Never creates a mesh or material
/// at runtime — only pushes property values to the existing Renderer.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class WaterFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaterColorPalette palette;
    [SerializeField] private Renderer targetRenderer;

    [Header("Transition")]
    [Tooltip("Transition progress change per second (0..1 range).")]
    [SerializeField] private float transitionSpeed = 1f;
    [SerializeField] private int startColorIndex = 0;

    [Header("Dissolve")]
    [Tooltip("Dissolve progress change per second (0..1 range). 0 = fully visible, 1 = fully dissolved.")]
    [SerializeField] private float dissolveSpeed = 1f;

    private static readonly int CurrentColorID = Shader.PropertyToID("_CurrentColor");
    private static readonly int TargetColorID = Shader.PropertyToID("_TargetColor");
    private static readonly int TransitionID = Shader.PropertyToID("_Transition");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private MaterialPropertyBlock _mpb;
    private Color _currentColor;
    private Color _targetColor;
    private float _transition = 1f;
    private bool _isTransitioning;
    private int _currentColorIndex;

    private float _dissolveAmount;
    private float _dissolveTarget;
    private bool _isDissolving;

    public int CurrentColorIndex => _currentColorIndex;
    public bool IsTransitioning => _isTransitioning;
    public bool IsDissolved => _dissolveAmount >= 1f;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        _mpb = new MaterialPropertyBlock();

        if (palette != null && palette.colors != null && palette.colors.Length > 0)
        {
            startColorIndex = Mathf.Clamp(startColorIndex, 0, palette.colors.Length - 1);
            _currentColorIndex = startColorIndex;
            _currentColor = palette.colors[startColorIndex].baseColor;
            _targetColor = _currentColor;
        }

        _transition = 1f;
        _isTransitioning = false;
        ApplyToMaterial();
    }

    /// <summary>
    /// Starts (or redirects) a color transition toward the palette entry at colorIndex.
    /// If a transition is already running, it continues smoothly from the current
    /// blended color instead of resetting — no mesh/material is created.
    /// </summary>
    public void SetWaterColor(int colorIndex)
    {
        if (palette == null || palette.colors == null || palette.colors.Length == 0)
        {
            Debug.LogWarning("WaterFlowController: no palette assigned.", this);
            return;
        }

        colorIndex = Mathf.Clamp(colorIndex, 0, palette.colors.Length - 1);
        Color newTarget = palette.colors[colorIndex].baseColor;

        if (_isTransitioning)
        {
            // Fold current progress into a concrete color, then continue from there.
            _currentColor = Color.Lerp(_currentColor, _targetColor, _transition);
        }

        _targetColor = newTarget;
        _transition = 0f;
        _isTransitioning = true;
        _currentColorIndex = colorIndex;

        ApplyToMaterial();
    }

    /// <summary>
    /// Dissolves the water away (e.g. call this when it has no color/is empty).
    /// Animates smoothly from whatever the current dissolve amount is.
    /// </summary>
    public void Dissolve()
    {
        _dissolveTarget = 1f;
        _isDissolving = true;
    }

    /// <summary>
    /// Reverses the dissolve, bringing the water back to fully visible.
    /// </summary>
    public void Appear()
    {
        _dissolveTarget = 0f;
        _isDissolving = true;
    }

    private void Update()
    {
        bool dirty = false;

        if (_isTransitioning)
        {
            _transition += Time.deltaTime * transitionSpeed;

            if (_transition >= 1f)
            {
                _transition = 1f;
                _currentColor = _targetColor;
                _isTransitioning = false;
            }

            dirty = true;
        }

        if (_isDissolving)
        {
            _dissolveAmount = Mathf.MoveTowards(_dissolveAmount, _dissolveTarget, Time.deltaTime * dissolveSpeed);

            if (Mathf.Approximately(_dissolveAmount, _dissolveTarget))
            {
                _dissolveAmount = _dissolveTarget;
                _isDissolving = false;
            }

            dirty = true;
        }

        if (dirty)
        {
            ApplyToMaterial();
        }
    }

    private void ApplyToMaterial()
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(CurrentColorID, _currentColor);
        _mpb.SetColor(TargetColorID, _targetColor);
        _mpb.SetFloat(TransitionID, _transition);
        _mpb.SetFloat(DissolveAmountID, _dissolveAmount);
        targetRenderer.SetPropertyBlock(_mpb);
    }
}
