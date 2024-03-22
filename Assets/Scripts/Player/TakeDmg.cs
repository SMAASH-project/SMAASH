using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TakeDmg : MonoBehaviourPunCallbacks, IPunObservable
{
    PhotonView view;
    
    public Animator animator;

    public int maxHealth = 50;
    public int currentHealth;
    public int enemyHealth;
    public HealthBar myHealthBar;
    public HealthBar enemyHealthBar;
    public int playerCount;
    //public HealthBarChecker healthBarChecker;

    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();

        currentHealth = maxHealth;
        myHealthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
		myHealthBar.SetMaxHealth(maxHealth);

        enemyHealth = maxHealth;
        enemyHealthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
		enemyHealthBar.SetMaxHealth(maxHealth);

        //Megkeresi a checkert es a script functionjet lefutatja
        
        //healthBarChecker = GameObject.Find("HealthBarChecker").GetComponent<HealthBarChecker>();
        //healthBarChecker.HealthBarCheck();
        //Debug.Log(healthBarChecker.healthBarNum);
        
        //Megnezi hanyadikkent kell healthbart csatolni
        /*
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
        

        healthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
        if(healthBar.isTaken == false)
        {
            currentHealth = maxHealth;
		    healthBar.SetMaxHealth(maxHealth);
        }else
        {
            currentHealth = maxHealth;
            healthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
		    healthBar.SetMaxHealth(maxHealth);
        }

        */

        

    }

    public void TakeDamageCaller(int damage)
    {
        view.RPC("TakeDamage", RpcTarget.All, damage);
    }

    //Megadott damaget levon
    [PunRPC]
    public void TakeDamage(int damage)
    {
        Debug.Log("utes!");
        currentHealth -= damage;
        myHealthBar.SetHealth(currentHealth);

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
            //Debug.Log("sajat jatekos elete: " + currentHealth);
            //stream.SendNext(healthBar);
        }else if(stream.IsReading)
        {
            //this.enabled = (bool)stream.ReceiveNext();
            //currentHealth = (int)stream.ReceiveNext();
            enemyHealth = (int)stream.ReceiveNext();
            //Debug.Log("masik jatekos elete: " + currentHealth);
            //this.healthBar = (HealthBar)stream.ReceiveNext();
        }
    }

    public override void OnJoinedRoom()
    {
        view.RPC("UpdatePlayerCount", RpcTarget.All, true);
    }

    public override void OnLeftRoom()
    {
        view.RPC("UpdatePlayerCount", RpcTarget.All, false);
    }

    [PunRPC]
    void UpdatePlayerCount(bool AddToCount){
    if (AddToCount){
        playerCount += 1;
    }
    else{
        playerCount -= 1;
    }
    }
}
