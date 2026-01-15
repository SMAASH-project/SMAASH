using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    public float speed = 0.001f;
    public Rigidbody2D rb;
    private int damage = 20;

    public override void FixedUpdateNetwork()
    {

        if (rb != null)
        {
            rb.velocity = transform.right * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
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
