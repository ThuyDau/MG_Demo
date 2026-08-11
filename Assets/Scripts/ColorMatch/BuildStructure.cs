using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMatch
{
    // Manages the Rocket: procedurally generates a pyramid of BuildBlocks with random
    // colors on Awake, and lets bees reserve the nearest remaining block of a given
    // color. Fires OnStructureCompleted once every block has been removed.
    public class BuildStructure : MonoBehaviour
    {
        public static BuildStructure Instance { get; private set; }

        [Header("Pyramid Generation")]
        [Tooltip("Leave empty to auto-create plain Cube primitives at runtime.")]
        [SerializeField] private GameObject blockPrefab;
        [Tooltip("Assign a URP/Lit material here if blockPrefab is left empty, otherwise " +
                 "runtime-created cubes render pink in URP (missing default material).")]
        [SerializeField] private Material blockMaterial;
        [SerializeField] private int layerCount = 5;
        [SerializeField] private float blockSize = 1f;
        [SerializeField] private float spacing = 0.05f;

        public event Action OnStructureCompleted;

        private readonly List<BuildBlock> allBlocks = new List<BuildBlock>();
        private int remainingCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple {nameof(BuildStructure)} instances found; destroying the duplicate on '{name}'.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GeneratePyramid();
            LogColorBreakdown();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void GeneratePyramid()
        {
            allBlocks.Clear();
            float cell = blockSize + spacing;

            // `layerCount` layers stacked upward; each layer is a square of blocks
            // that shrinks by one block per side as it goes up, forming a pyramid.
            for (int layer = 0; layer < layerCount; layer++)
            {
                int sideCount = layerCount - layer;
                float y = layer * cell;
                float offset = (sideCount - 1) * cell * 0.5f;

                for (int x = 0; x < sideCount; x++)
                {
                    for (int z = 0; z < sideCount; z++)
                    {
                        Vector3 localPos = new Vector3(x * cell - offset, y, z * cell - offset);
                        SpawnBlock(localPos);
                    }
                }
            }

            remainingCount = allBlocks.Count;
        }

        private void SpawnBlock(Vector3 localPosition)
        {
            GameObject go;
            if (blockPrefab != null)
            {
                go = Instantiate(blockPrefab, transform);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(transform, false);
                if (blockMaterial != null)
                {
                    Renderer primitiveRenderer = go.GetComponent<Renderer>();
                    if (primitiveRenderer != null)
                        primitiveRenderer.sharedMaterial = blockMaterial;
                }
            }

            go.name = "Block";
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * blockSize;

            BuildBlock block = go.GetComponent<BuildBlock>();
            if (block == null)
                block = go.AddComponent<BuildBlock>();

            BlockColor randomColor = (BlockColor)UnityEngine.Random.Range(0, 4);
            block.Setup(randomColor);
            block.OnRemoved += HandleBlockRemoved;

            allBlocks.Add(block);
        }

        // Finds and reserves the nearest un-removed, un-reserved block of the given
        // color to fromPosition. Returns false (reserved stays null) if none are free.
        public bool TryReserveBlock(BlockColor color, Vector3 fromPosition, out BuildBlock reserved)
        {
            reserved = null;
            float bestSqrDist = float.MaxValue;

            foreach (BuildBlock block in allBlocks)
            {
                if (block == null || block.IsRemoved || block.IsReserved || block.BlockColor != color)
                    continue;

                float sqrDist = (block.transform.position - fromPosition).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    reserved = block;
                }
            }

            if (reserved != null)
                reserved.Reserve();

            return reserved != null;
        }

        public int GetRemainingCount(BlockColor color)
        {
            int count = 0;
            foreach (BuildBlock block in allBlocks)
            {
                if (block != null && !block.IsRemoved && block.BlockColor == color)
                    count++;
            }
            return count;
        }

        private void LogColorBreakdown()
        {
            foreach (BlockColor color in (BlockColor[])Enum.GetValues(typeof(BlockColor)))
                Debug.Log($"[BuildStructure] {color}: {GetRemainingCount(color)} block(s)");
        }

        private void HandleBlockRemoved(BuildBlock block)
        {
            remainingCount--;
            if (remainingCount <= 0)
            {
                Debug.Log("[BuildStructure] All blocks removed — level complete!");
                OnStructureCompleted?.Invoke();
            }
        }
    }
}
