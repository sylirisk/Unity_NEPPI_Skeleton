using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Illuminator : MonoBehaviour {

    Light lightComp;

    private void Start()
    {
        lightComp = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update () 
    {

        if (Input.GetKey(KeyCode.Space)) 
        {
            lightComp.intensity = 1;
        } else {
            lightComp.intensity = 0;
        }
	}
}
