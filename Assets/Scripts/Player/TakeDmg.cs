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

        
        if(PhotonNetwork.CurrentRoom.PlayerCount == 1){
            currentHealth = maxHealth;
            myHealthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
		    myHealthBar.SetMaxHealth(maxHealth);

            enemyHealth = maxHealth;
            enemyHealthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
		    enemyHealthBar.SetMaxHealth(maxHealth);
        }else if(PhotonNetwork.CurrentRoom.PlayerCount == 2){
            currentHealth = maxHealth;
            myHealthBar = GameObject.Find("HealthBar2").GetComponent<HealthBar>();
		    myHealthBar.SetMaxHealth(maxHealth);

            enemyHealth = maxHealth;
            enemyHealthBar = GameObject.Find("HealthBar1").GetComponent<HealthBar>();
		    enemyHealthBar.SetMaxHealth(maxHealth);
        }
        
    }

    public void TakeDamageCaller(int damage)
    {
        view.RPC("TakeDamage", RpcTarget.All, damage);
    }

    //Megadott damaget levon
    [PunRPC]
    public void TakeDamage(int damage)
    {
        if(view.IsMine){
            currentHealth -= damage;
            myHealthBar.SetHealth(currentHealth);

            enemyHealthBar.SetHealth(enemyHealth);
        }else{
            enemyHealth -= damage;
            enemyHealthBar.SetHealth(enemyHealth);

            myHealthBar.SetHealth(currentHealth);
        }
        //Ha nincs eletero, akkor meghak
        if(currentHealth <= 0)
        {
             Die();
        }

        Debug.Log("my health: " + currentHealth);
        Debug.Log("enemy health: " + enemyHealth);
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
            //stream.SendNext(currentHealth);
            stream.SendNext(enemyHealth);
        }else if(stream.IsReading)
        {
            currentHealth = (int)stream.ReceiveNext();
            //enemyHealth = (int)stream.ReceiveNext();
        }
    }
    
    
}
