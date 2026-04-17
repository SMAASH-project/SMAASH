using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class ShootingAttack : NetworkBehaviour
{
    public Animator animator;
    public Transform attackPoint;
    public Transform attackPointOpposite;
    public SpriteRenderer spriteRenderer;
    public int damage = 20;
    public NetworkPrefabRef bulletPrefab;
    public bool isCountingDown = false;

    private InputActionAsset inputAsset;
    private InputActionMap playerMap;
    private bool canAttack = true;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        inputAsset = GetComponent<PlayerInput>()?.actions;
        playerMap = inputAsset?.FindActionMap("Player");
        playerMovement = GetComponent<PlayerMovement>();
    }

    public override void Spawned()
    {
        // Only the player sitting at the keyboard should trigger the Attack input
        if (Object.HasInputAuthority && playerMap != null)
        {
            playerMap.FindAction("Attack").started += OnAttackInput;
            playerMap.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerMap != null)
        {
            playerMap.FindAction("Attack").started -= OnAttackInput;
            playerMap.Disable();
        }
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        Debug.Log("Attack input received. Sending RPC to perform attack.");
        if (!canAttack || isCountingDown) return;

        // Determine which attack point to use based on sprite direction
        bool facingLeft = playerMovement != null ? playerMovement.IsFacingLeft : spriteRenderer != null && spriteRenderer.flipX;
        Transform activePoint = facingLeft ? attackPointOpposite : attackPoint;
        Vector3 spawnPosition = GetSpawnPosition(activePoint);
        
        // Send RPC to server to spawn bullet
        SpawnBulletRpc(spawnPosition, activePoint.rotation, facingLeft);

        StartCoroutine(AttackCooldown());
    }

    private Vector3 GetSpawnPosition(Transform activePoint)
    {
        if (activePoint == null)
            return transform.position;

        Vector3 position = activePoint.position;

        if (attackPoint != null)
            position.y = attackPoint.position.y;

        return position;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void SpawnBulletRpc(Vector3 position, Quaternion rotation, bool facingLeft)
    {
        Debug.Log("SpawnBulletRpc called at position: " + position);
        if (Runner != null && bulletPrefab.IsValid)
        {
            Debug.Log("Spawning bullet at: " + position);
            NetworkObject bulletNetObj = Runner.Spawn(bulletPrefab, position, rotation);
            Bullet bullet = bulletNetObj.GetComponent<Bullet>();

            if (bullet != null)
            {
                bullet.SetOwner(Object.InputAuthority);
                Vector2 fireDirection = facingLeft ? Vector2.left : Vector2.right;
                bullet.SetDirection(fireDirection);
            }
        }
        else
        {
            Debug.LogError("Runner is null or bulletPrefab is not valid!");
        }
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(1f);
        canAttack = true;
    }

    
}
