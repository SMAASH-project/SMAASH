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

    private InputActionAsset inputAsset;
    private InputActionMap playerMap;
    private bool canAttack = true;

    private void Awake()
    {
        inputAsset = GetComponent<PlayerInput>()?.actions;
        playerMap = inputAsset?.FindActionMap("Player");
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
        if (!canAttack) return;

        // Determine which attack point to use based on sprite direction
        Transform activePoint = spriteRenderer.flipX ? attackPointOpposite : attackPoint;
        
        // Send RPC to server to spawn bullet
        SpawnBulletRpc(activePoint.position, activePoint.rotation, spriteRenderer.flipX);

        StartCoroutine(AttackCooldown());
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
