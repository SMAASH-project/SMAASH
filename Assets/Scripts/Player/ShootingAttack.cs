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
        if (!canAttack) return;

        // Determine which attack point to use based on sprite direction
        Transform activePoint = spriteRenderer.flipX ? attackPointOpposite : attackPoint;
        
        // Send RPC to server to spawn bullet
        SpawnBulletRpc(activePoint.position, activePoint.rotation);

        StartCoroutine(AttackCooldown());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void SpawnBulletRpc(Vector3 position, Quaternion rotation)
    {
        if (Runner != null && bulletPrefab.IsValid)
        {
            Debug.Log("Spawning bullet at: " + position);
            Runner.Spawn(bulletPrefab, position, rotation);
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
