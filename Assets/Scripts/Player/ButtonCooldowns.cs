using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonCooldowns : MonoBehaviour
{

    public Animator attack_anim;
    public Button attack_btn;

    public bool isDead = false;

    public bool isCountingDown = false;
    private bool isOnCooldown = false;


    // Start is called before the first frame update
    void Start()
    {
        RefreshButtonState();
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
        if (isDead || isCountingDown || isOnCooldown)
            return;

        StartCoroutine(AttackCooldownStart());
    }

    IEnumerator AttackCooldownStart(){
       if (isDead || isCountingDown || isOnCooldown)
           yield break;

       isOnCooldown = true;
       RefreshButtonState();

       if (attack_anim != null)
           attack_anim.SetTrigger("AttackCooldown");

       yield return new WaitForSecondsRealtime(1);

       isOnCooldown = false;
       RefreshButtonState();
    }

    public void SetCountdownState(bool countingDown)
    {
        isCountingDown = countingDown;
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (attack_btn == null)
            return;

        attack_btn.interactable = !isDead && !isCountingDown && !isOnCooldown;
    }

}
