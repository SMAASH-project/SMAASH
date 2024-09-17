using UnityEngine;
using Photon.Pun;

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
        this.enabled = false;
        animator.SetBool("isDead", true);
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(currentHealth);
        }else if(stream.IsReading)
        {
            enemyHealth = (int)stream.ReceiveNext();
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
