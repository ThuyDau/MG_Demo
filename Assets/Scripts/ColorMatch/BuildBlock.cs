using System;
using System.Collections;
using UnityEngine;

namespace ColorMatch
{
    // A single block on the Rocket. BuildStructure spawns and colors these; bees
    // reserve one via BuildStructure before flying to it, so two bees never target
    // the same block.
    public class BuildBlock : MonoBehaviour
    {
        [SerializeField] private BlockColor blockColor;
        [SerializeField] private Renderer blockRenderer;
        [SerializeField] private float removeDuration = 0.2f;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        public BlockColor BlockColor => blockColor;
        public bool IsReserved { get; private set; }
        public bool IsRemoved { get; private set; }

        // Fired the instant this block is logically removed (before the shrink
        // animation finishes), so BuildStructure can update the win condition/count
        // immediately instead of waiting on the visual effect.
        public event Action<BuildBlock> OnRemoved;

        private void Awake()
        {
            if (blockRenderer == null)
                blockRenderer = GetComponent<Renderer>();
            ApplyColor();
        }

        // Called by BuildStructure right after instantiating this block.
        public void Setup(BlockColor color)
        {
            blockColor = color;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (blockRenderer == null)
                return;

            Color c = BlockColorUtility.ToColor(blockColor);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            blockRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorID, c);
            block.SetColor(ColorID, c);
            blockRenderer.SetPropertyBlock(block);
        }

        public bool Reserve()
        {
            if (IsRemoved || IsReserved)
                return false;
            IsReserved = true;
            return true;
        }

        public void CancelReservation()
        {
            IsReserved = false;
        }

        public void RemoveBlock()
        {
            if (IsRemoved)
                return;
            IsRemoved = true;
            OnRemoved?.Invoke(this);
            StartCoroutine(RemoveEffect());
        }

        private IEnumerator RemoveEffect()
        {
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < removeDuration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / removeDuration);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
