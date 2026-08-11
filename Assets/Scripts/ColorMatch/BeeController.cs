using System.Collections;
using UnityEngine;

namespace ColorMatch
{
    // State machine: FlyToBlock -> RemoveBlock -> FlyBack -> Done.
    // Movement uses a simple quadratic-bezier arc via Vector3.Lerp coroutines (no
    // DOTween/LeanTween in this project yet).
    public class BeeController : MonoBehaviour
    {
        public enum State { FlyToBlock, RemoveBlock, FlyBack, Done }

        [Header("Movement")]
        [SerializeField] private float flySpeed = 4f;
        [SerializeField] private float arcHeight = 1f;

        [Header("Target Search")]
        [Tooltip("How often to re-check for a free matching block if none was available yet.")]
        [SerializeField] private float retryInterval = 0.2f;

        [Header("Visuals")]
        [Tooltip("If no MeshRenderer is found on this object, a placeholder cube is created automatically (same fallback pattern as BuildStructure) and tinted to the target color, so it visually reads as \"this piece flies to match that block\".")]
        [SerializeField] private float placeholderSize = 0.3f;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        public State CurrentState { get; private set; } = State.FlyToBlock;

        private BlockColor targetColor;
        private Vector3 homePosition;
        private BuildBlock targetBlock;
        private Renderer placeholderRenderer;

        private void Awake()
        {
            placeholderRenderer = GetComponentInChildren<Renderer>();
            if (placeholderRenderer == null)
                placeholderRenderer = CreatePlaceholderVisual();
        }

        private Renderer CreatePlaceholderVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "PlaceholderVisual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * placeholderSize;
            return visual.GetComponent<Renderer>();
        }

        private void ApplyPlaceholderColor()
        {
            if (placeholderRenderer == null)
                return;

            Color c = BlockColorUtility.ToColor(targetColor);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            placeholderRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorID, c);
            block.SetColor(ColorID, c);
            placeholderRenderer.SetPropertyBlock(block);
        }

        // Called by TrayObject right after Instantiate.
        public void Init(BlockColor color, Vector3 home)
        {
            targetColor = color;
            homePosition = home;
            ApplyPlaceholderColor();
            StartCoroutine(RunStateMachine());
        }

        private IEnumerator RunStateMachine()
        {
            yield return StartCoroutine(FlyToBlockRoutine());
            yield return StartCoroutine(RemoveBlockRoutine());
            yield return StartCoroutine(FlyBackRoutine());
            CurrentState = State.Done;
            Destroy(gameObject);
        }

        private IEnumerator FlyToBlockRoutine()
        {
            CurrentState = State.FlyToBlock;

            while (targetBlock == null)
            {
                if (BuildStructure.Instance != null &&
                    BuildStructure.Instance.TryReserveBlock(targetColor, transform.position, out BuildBlock block))
                {
                    targetBlock = block;
                }
                else
                {
                    yield return new WaitForSeconds(retryInterval);
                }
            }

            yield return FlyArc(targetBlock.transform.position);
        }

        private IEnumerator RemoveBlockRoutine()
        {
            CurrentState = State.RemoveBlock;
            if (targetBlock != null)
                targetBlock.RemoveBlock();
            yield return null;
        }

        private IEnumerator FlyBackRoutine()
        {
            CurrentState = State.FlyBack;
            yield return FlyArc(homePosition);
        }

        // Quadratic-bezier arc; duration is derived from distance/flySpeed so near
        // targets take proportionally less time.
        private IEnumerator FlyArc(Vector3 destination)
        {
            Vector3 start = transform.position;
            float distance = Vector3.Distance(start, destination);
            float duration = flySpeed > 0f ? distance / flySpeed : 0f;

            if (duration <= 0f)
            {
                transform.position = destination;
                yield break;
            }

            Vector3 mid = (start + destination) * 0.5f + Vector3.up * arcHeight;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / duration);
                Vector3 a = Vector3.Lerp(start, mid, lerp);
                Vector3 b = Vector3.Lerp(mid, destination, lerp);
                transform.position = Vector3.Lerp(a, b, lerp);
                yield return null;
            }

            transform.position = destination;
        }
    }
}
