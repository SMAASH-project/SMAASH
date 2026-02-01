using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    public float speed = 0.001f;
    public Rigidbody2D rb;
    private int damage = 20;
    private Vector2 direction = Vector2.right;

    public override void FixedUpdateNetwork()
    {

        if (rb != null)
        {
            rb.velocity = direction * speed;
            
        }
    }

    // Call this when spawning the bullet to set its direction
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        
        // Rotate bullet to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);
        // Send damage request to the health script
        if (collision.TryGetComponent<PlayerHealth>(out var health))
        {
            health.TakeDamageCaller(damage);
            
            // Despawn the bullet across the network
            if (Runner != null)
            {
                Runner.Despawn(Object);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
