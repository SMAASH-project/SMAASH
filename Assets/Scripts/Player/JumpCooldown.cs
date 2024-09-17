using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

 public class JumpCooldown : MonoBehaviour
 {
     public Animator jump_anim;
     public Button jump_btn;

     // Start is called before the first frame update
     void Start(){
         jump_btn.interactable = true;
     }

     void Awake()
     {
         jump_btn.onClick.AddListener(Jump_Button_pressed);
     }

     void Jump_Button_pressed(){
         StartCoroutine(JumpCooldownStart());
     }


     IEnumerator JumpCooldownStart(){
       jump_anim.SetTrigger("isJumping");
       jump_btn.interactable = false;
       yield return new WaitForSecondsRealtime(.5f);
       jump_btn.interactable = true;
     }
 }
