using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttackCooldown : MonoBehaviour
{

    public Animator attack_anim;
    public Button attack_btn;
    public MeleeAttack meleeAttack;

    // Start is called before the first frame update
    void Start(){
        attack_anim.SetBool("AttackCooldown", false);
        attack_btn.interactable = true;
        meleeAttack = new MeleeAttack();
    }
    
    void Awake()
    {
        attack_btn.onClick.AddListener(Button_pressed);
        
    }



    void Button_pressed(){
        attack_anim.SetBool("AttackCooldown", true);
        StartCoroutine(AttackCooldownStart());
    
    }
    IEnumerator AttackCooldownStart(){
      attack_anim.SetBool("AttackCooldown", true);
      //meleeAttack.canAttack = false;
      attack_btn.interactable = false;
      yield return new WaitForSecondsRealtime(1);
      Debug.Log("1 sec");
      attack_btn.interactable = true;
    }
}
