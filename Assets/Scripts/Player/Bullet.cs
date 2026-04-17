using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    private const string GroundLayerName = "Ground";

    public float speed = 0.001f;
    public Rigidbody2D rb;
    [SerializeField] private float lifeTimeSeconds = 5f;
    private int damage = 20;
    private Vector2 direction = Vector2.right;
    private float spawnTime;
    private PlayerRef owner;

    public override void Spawned()
    {
        spawnTime = Time.time;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object != null && Object.HasStateAuthority && Time.time - spawnTime >= lifeTimeSeconds)
        {
            DespawnBullet();
            return;
        }

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

    public void SetOwner(PlayerRef ownerRef)
    {
        owner = ownerRef;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);

        if (Object == null || !Object.HasStateAuthority)
            return;

        if (IsGroundLayer(collision.gameObject.layer))
        {
            DespawnBullet();
            return;
        }

        // Send damage request to the health script
        var health = collision.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            if (health.Object != null && health.Object.InputAuthority == owner)
                return;

            health.TakeDamageCaller(damage);

            DespawnBullet();
        }
    }

    private void DespawnBullet()
    {
        if (Object == null)
            return;

        if (!Object.HasStateAuthority)
            return;

        if (Runner != null)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static bool IsGroundLayer(int layer)
    {
        return LayerMask.LayerToName(layer) == GroundLayerName;
    }
}
