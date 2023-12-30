using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TakeDmg : MonoBehaviour
{

    public Animator animator;
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;
    public HealthBarChecker healthBarChecker;
    // Start is called before the first frame update
    void Start()
    {

        healthBarChecker = GameObject.Find("HealthBarChecker").GetComponent<HealthBarChecker>();
        healthBarChecker.HealthBarCheck();
        if(healthBarChecker.healthBarNum == 1)
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
            Debug.Log(healthBar);
		    healthBar.SetMaxHealth(maxHealth);
        }else if(healthBarChecker.healthBarNum == 2)
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
            Debug.Log(healthBar);
		    healthBar.SetMaxHealth(maxHealth);
        }

    }
    public void TakeDamage(int damage)
    {
        Debug.Log(currentHealth);
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy Died");
        //animator.SetBool("isDead", true);
    }
}
