using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TakeDmg : MonoBehaviour, IPunObservable
{
    PhotonView view;
    
    public Animator animator;

    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;
    public HealthBarChecker healthBarChecker;

    int local_dmg;

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();

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

    public void Update()
    {
        //healthBar.SetHealth(currentHealth);
    }

    public void TakeDamageCall(int damage)
    {
        local_dmg = damage;
        Debug.Log("1");
        view.RPC("TakeDamage", RpcTarget.All);
    }

    //Megadott damaget levon
    [PunRPC]
    public void TakeDamage()
    {
        Debug.Log("asd");
        currentHealth -= local_dmg;
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

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            //stream.SendNext(this.enabled);
            stream.SendNext(currentHealth);
            //stream.SendNext(healthBar);
        }else if(stream.IsReading)
        {
            //this.enabled = (bool)stream.ReceiveNext();
            currentHealth = (int)stream.ReceiveNext();
            //this.healthBar = (HealthBar)stream.ReceiveNext();
        }
    }
}
