using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputHandler : MonoBehaviour
{
    private Vector2 moveInput;
    private bool jumpPressed;

    //OnMove is called by the Input System when movement input is detected
    public void OnMove(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) jumpPressed = true;
    }

    //This method packages the current input state into a NetworkInputData struct
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData data = new NetworkInputData();
        data.moveInput = moveInput;
        data.jumpPressed = jumpPressed;
        
        // Reset jump after it's been read
        jumpPressed = false; 
        return data;
    }
}