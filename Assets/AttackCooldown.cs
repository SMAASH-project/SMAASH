using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttackCooldown : MonoBehaviour
{

    public Animator attack_anim;
    public Button attack_btn;

    bool canAttack = true;
    // Start is called before the first frame update
    void Start(){
        attack_anim.SetBool("AttackCooldown", true);
    }
    
    void Awake()
    {
        attack_btn.onClick.AddListener(Button_pressed);
    }



    void Button_pressed(){
        attack_anim.SetBool("AttackCooldown", true);
        StartCoroutine(AttackCooldownStart());
        attack_anim.SetBool("AttackCooldown", false);
    }
    IEnumerator AttackCooldownStart(){
      Debug.Log("1 sec");
      attack_anim.SetBool("AttackCooldown", true);
      yield return new WaitForSeconds(1);
      attack_anim.SetBool("AttackCooldown", false);
    }
}
