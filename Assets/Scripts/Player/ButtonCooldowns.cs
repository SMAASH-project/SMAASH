using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonCooldowns : MonoBehaviour
{

    public Animator attack_anim;
    public Button attack_btn;

    public bool isDead = false;


    // Start is called before the first frame update
    void Start()
    {
        attack_btn.interactable = true;
    }

    void Awake()
    {
        attack_btn.onClick.RemoveListener(Attack_Button_pressed);
        attack_btn.onClick.AddListener(Attack_Button_pressed);
    }

    void OnDestroy()
    {
        if (attack_btn != null)
            attack_btn.onClick.RemoveListener(Attack_Button_pressed);
    }

    void Attack_Button_pressed(){
        if(isDead == false){
            StartCoroutine(AttackCooldownStart());
        }
    }

    IEnumerator AttackCooldownStart(){
       attack_anim.SetTrigger("AttackCooldown");
       attack_btn.interactable = false;
       yield return new WaitForSecondsRealtime(1);
       
       attack_btn.interactable = true;
    }

}
