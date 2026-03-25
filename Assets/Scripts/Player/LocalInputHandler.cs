using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LocalInputHandler : MonoBehaviour
{
    private FloatingJoystick joystick;
    
    void Start()
    {
        joystick = FindObjectOfType<FloatingJoystick>();
    }
    
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData data = new NetworkInputData();
        
        // BILLENTYŰZET INPUT (PC)
        Vector2 keyboardInput = Vector2.zero;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                keyboardInput.x = -1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                keyboardInput.x = 1;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                keyboardInput.y = 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                keyboardInput.y = -1;
        }
        
        // JOYSTICK INPUT (Mobil UI)
        Vector2 joystickInput = Vector2.zero;
        if (joystick != null)
        {
            joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        
        // KOMBINÁLT INPUT (bármelyik működik)
        data.moveInput = keyboardInput.magnitude > 0.1f ? keyboardInput : joystickInput;
        
        // UGRÁS - csak billentyűzet (UI Jump gombot a PlayerMovement kezeli)
        bool keyboardJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        data.jumpPressed = keyboardJump;
        
        return data;
    }
}