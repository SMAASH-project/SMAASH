using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Singleton pattern allows any script to find the UI easily
    public static UIManager Instance;

    [Header("Fixed UI References")]
    public HealthBar healthBar1; 
    public HealthBar healthBar2;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}