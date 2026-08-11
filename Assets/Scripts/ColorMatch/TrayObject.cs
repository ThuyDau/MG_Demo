using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColorMatch
{
    // A single tray: fixed color, spawns 1 bee per click while active (requires a
    // Collider on this GameObject for the click raycast to hit). Only responds to
    // clicks while activated by TrayQueueManager; trays waiting in queue never spawn
    // on their own.
    public class TrayObject : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private BlockColor trayColor;
        [SerializeField] private int beeCount = 10;
        [SerializeField] private BeeController beePrefab;
        [SerializeField] private float spawnInterval = 1f;
        [SerializeField] private Transform beeSpawnPoint;

        [Header("UI")]
        [SerializeField] private TMP_Text counterText;

        [Header("Visuals")]
        [SerializeField] private Renderer trayRenderer;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        public event Action<TrayObject> OnTrayEmptied;

        public BlockColor TrayColor => trayColor;
        public bool IsActiveSlot { get; private set; }

        // spawnInterval now acts as a click cooldown, so mashing the tray can't
        // spawn bees faster than this.
        private float nextSpawnAllowedTime;

        private void Awake()
        {
            UpdateCounterText();
            ApplyTrayColor();
        }

        private void OnValidate()
        {
            ApplyTrayColor();
        }

        private void ApplyTrayColor()
        {
            if (trayRenderer == null)
                return;

            Color c = BlockColorUtility.ToColor(trayColor);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            trayRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorID, c);
            block.SetColor(ColorID, c);
            trayRenderer.SetPropertyBlock(block);
        }

        // Called by TrayQueueManager when this tray takes an active slot.
        public void Activate()
        {
            if (IsActiveSlot)
                return;
            IsActiveSlot = true;
            nextSpawnAllowedTime = 0f;

            if (beeCount <= 0)
                TrayEmptied();
        }

        public void Deactivate()
        {
            IsActiveSlot = false;
        }

        private void Update()
        {
            if (!IsActiveSlot || beeCount <= 0)
                return;
            if (Time.time < nextSpawnAllowedTime)
                return;
            if (!WasClickedThisFrame())
                return;

            SpawnBee();
            beeCount--;
            UpdateCounterText();
            nextSpawnAllowedTime = Time.time + spawnInterval;

            if (beeCount <= 0)
                TrayEmptied();
        }

        // True if the left mouse button (or primary touch) was pressed this frame
        // and the resulting ray hits this tray's own collider.
        private bool WasClickedThisFrame()
        {
            Vector2? screenPos = null;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                screenPos = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();

            if (screenPos == null)
                return false;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            Ray ray = cam.ScreenPointToRay(screenPos.Value);
            return Physics.Raycast(ray, out RaycastHit hit) && hit.collider != null && hit.collider.gameObject == gameObject;
        }

        private void SpawnBee()
        {
            if (beePrefab == null)
                return;

            Transform spawnFrom = beeSpawnPoint != null ? beeSpawnPoint : transform;
            BeeController bee = Instantiate(beePrefab, spawnFrom.position, spawnFrom.rotation);
            bee.Init(trayColor, spawnFrom.position);
        }

        private void UpdateCounterText()
        {
            if (counterText != null)
                counterText.text = beeCount.ToString();
        }

        private void TrayEmptied()
        {
            IsActiveSlot = false;
            gameObject.SetActive(false);
            OnTrayEmptied?.Invoke(this);
        }
    }
}
