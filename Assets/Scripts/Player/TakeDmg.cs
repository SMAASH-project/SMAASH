using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TakeDmg : MonoBehaviour, IPunObservable
{
    public Animator animator;

    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;
    public HealthBarChecker healthBarChecker;

    // Start is called before the first frame update
    void Start()
    {
        //Megkeresi a checkert es a script functionjet lefutatja
        healthBarChecker = GameObject.Find("HealthBarChecker").GetComponent<HealthBarChecker>();
        healthBarChecker.HealthBarCheck();
        
        //Megnezi hanyadikkent kell healthbart csatolni
        if(healthBarChecker.healthBarNum == 1)
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
		    healthBar.SetMaxHealth(maxHealth);
        }else if(healthBarChecker.healthBarNum == 2)
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
		    healthBar.SetMaxHealth(maxHealth);
        }

    }
    //Megadott damaget levon
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        //Ha nincs eletero, akkor meghak
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    //Halal animacio
    void Die()
    {
        animator.SetBool("isDead", true);
        this.enabled = false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(this.enabled);
            stream.SendNext(currentHealth);
        }else if(stream.IsReading)
        {
            this.enabled = (bool)stream.ReceiveNext();
            currentHealth = (int)stream.ReceiveNext();
        }
    }
}
