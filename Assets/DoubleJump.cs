using UnityEngine;
using UnityEngine.UI;

public class DoubleJump : MonoBehaviour
{
    public Animator jump_anim;
    public Button jump_btn;
    
    void Awake()
    {
        jump_btn.onClick.AddListener(DoubleJumping);
    }

    void DoubleJumping(){
        jump_anim.SetTrigger("DoubleJumping");
    }
}
