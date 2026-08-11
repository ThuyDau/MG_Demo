using UnityEngine;

namespace ColorMatch
{
    // Shared movement-tuning asset for bees. Currently just rotation speed (how fast
    // a bee turns to face its flight direction) — BeeController doesn't rotate at all
    // if this isn't assigned. Multiple bee prefabs can reference the same asset so
    // tuning stays in one place.
    [CreateAssetMenu(fileName = "BeeMovementSettings", menuName = "Bee Honey/Bee Movement Settings")]
    public class BeeMovementSettings : ScriptableObject
    {
        [Tooltip("Degrees per second the bee turns to face its current flight direction.")]
        [SerializeField] private float rotationSpeed = 360f;

        public float RotationSpeed => rotationSpeed;
    }
}
