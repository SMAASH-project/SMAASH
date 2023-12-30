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

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();

        //Megkeresi a checkert es a script functionjet lefutatja
        healthBarChecker = GameObject.Find("HealthBarChecker").GetComponent<HealthBarChecker>();
        healthBarChecker.HealthBarCheck();
        Debug.Log(healthBarChecker.healthBarNum);
        
        //Megnezi hanyadikkent kell healthbart csatolni
        if(healthBarChecker.healthBarNum == 1)
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
		    healthBar.SetMaxHealth(maxHealth);
        }else
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

    public void TakeDamageCaller(int damage)
    {
        view.RPC("TakeDamage", RpcTarget.All, damage);
    }

    //Megadott damaget levon
    [PunRPC]
    public void TakeDamage(int damage)
    {
        Debug.Log("asd");
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

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            //stream.SendNext(this.enabled);
            stream.SendNext(currentHealth);
            Debug.Log("sajat jatekos elete: " + currentHealth);
            //stream.SendNext(healthBar);
        }else if(stream.IsReading)
        {
            //this.enabled = (bool)stream.ReceiveNext();
            currentHealth = (int)stream.ReceiveNext();
            Debug.Log("masik jatekos elete: " + currentHealth);
            //this.healthBar = (HealthBar)stream.ReceiveNext();
        }
    }
}
