using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OutOfBoundsKillZone : MonoBehaviour
{
    private void Reset()
    {
        var trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerHealth>(out var health))
        {
            health.RequestOutOfBoundsDeath();
            Debug.Log("Player entered OutOfBoundsKillZone: " + other.name);
            return;
        }

        // Some characters can have child colliders, so we also check the parent.
        var parentHealth = other.GetComponentInParent<PlayerHealth>();
        if (parentHealth != null)
            parentHealth.RequestOutOfBoundsDeath();
    }
}
