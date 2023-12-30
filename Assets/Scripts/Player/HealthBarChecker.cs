using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarChecker : MonoBehaviour
{
    public int counter;
    public int healthBarNum;
    
    // Start is called before the first frame update
    void Start()
    {
        counter = 1;
    }

    public void HealthBarCheck()
    {
        if(counter == 1)
        {
            healthBarNum = 1;
            counter = 2;
        }else if(counter == 2)
        {
            healthBarNum = 2;
        }
    }
}
