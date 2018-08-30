using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunContoller : MonoBehaviour {

    public Light sunVisualisation;
    public float sunriseSpeed = 1.0f; // controls in seconds how fast sun changes, greater = slower
    public float timeSinceLastSunriseRequest;

    private void Start()
    {
        sunVisualisation = GetComponent<Light>();
        sunVisualisation.intensity = 0.0f;
    }

    private void Update()
    {
        timeSinceLastSunriseRequest += Time.deltaTime;
        if (timeSinceLastSunriseRequest < sunriseSpeed) {
            sunVisualisation.intensity += 0.1f * Time.deltaTime;
        } else if (timeSinceLastSunriseRequest > sunriseSpeed * 2) {
            if (sunVisualisation.intensity > 0 ) {
                sunVisualisation.intensity -= 0.1f * Time.deltaTime;
            }
        } 
    }

    // This is executed when the script becomes active in Unity
    void OnEnable()
    {
        PainterController.AddSunEvent += IncreaseSun; // add our event listener
    }

    // This is executed when the script becomes inactive in Unity
    private void OnDisable()
    {
        PainterController.AddSunEvent -= IncreaseSun; // remove our event listener
    }

    void IncreaseSun() 
    {
        timeSinceLastSunriseRequest = 0.0f;
        print("SUN RISES!");
    }

}
