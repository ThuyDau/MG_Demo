using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMatch
{
    // Fills activeSlotPositions with the first activeSlotCount trays from allTrays on
    // start. When an active tray empties, the next tray in the waiting queue slides
    // (Vector3.Lerp) into the slot that just freed up.
    public class TrayQueueManager : MonoBehaviour
    {
        [SerializeField] private List<TrayObject> allTrays = new List<TrayObject>();
        [SerializeField] private int activeSlotCount = 4;
        [SerializeField] private Transform[] activeSlotPositions;
        [SerializeField] private float slideDuration = 0.35f;

        private readonly Queue<TrayObject> waitingQueue = new Queue<TrayObject>();
        private TrayObject[] slotOccupants;

        private void Start()
        {
            slotOccupants = new TrayObject[activeSlotPositions.Length];
            waitingQueue.Clear();

            foreach (TrayObject tray in allTrays)
            {
                if (tray != null)
                    waitingQueue.Enqueue(tray);
            }

            int slotsToFill = Mathf.Min(activeSlotCount, activeSlotPositions.Length);
            for (int i = 0; i < slotsToFill; i++)
                FillSlot(i);
        }

        private void FillSlot(int slotIndex)
        {
            if (waitingQueue.Count == 0)
                return;

            TrayObject tray = waitingQueue.Dequeue();
            Transform slot = activeSlotPositions[slotIndex];

            tray.gameObject.SetActive(true);
            tray.transform.SetPositionAndRotation(slot.position, slot.rotation);

            slotOccupants[slotIndex] = tray;
            tray.OnTrayEmptied += HandleTrayEmptied;
            tray.Activate();
        }

        private void HandleTrayEmptied(TrayObject tray)
        {
            tray.OnTrayEmptied -= HandleTrayEmptied;

            int slotIndex = System.Array.IndexOf(slotOccupants, tray);
            if (slotIndex < 0)
                return;

            slotOccupants[slotIndex] = null;

            if (waitingQueue.Count == 0)
                return;

            TrayObject nextTray = waitingQueue.Dequeue();
            slotOccupants[slotIndex] = nextTray;

            nextTray.gameObject.SetActive(true);
            StartCoroutine(SlideTrayIntoSlot(nextTray, slotIndex));
        }

        private IEnumerator SlideTrayIntoSlot(TrayObject tray, int slotIndex)
        {
            Transform slot = activeSlotPositions[slotIndex];
            Vector3 startPos = tray.transform.position;

            float t = 0f;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / slideDuration);
                tray.transform.position = Vector3.Lerp(startPos, slot.position, lerp);
                yield return null;
            }
            tray.transform.position = slot.position;

            tray.OnTrayEmptied += HandleTrayEmptied;
            tray.Activate();
        }

        private void OnDrawGizmos()
        {
            if (activeSlotPositions == null)
                return;

            Gizmos.color = Color.yellow;
            foreach (Transform slot in activeSlotPositions)
            {
                if (slot != null)
                    Gizmos.DrawWireCube(slot.position, Vector3.one * 0.5f);
            }
        }
    }
}
