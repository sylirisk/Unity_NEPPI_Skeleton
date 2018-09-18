using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO.Ports;
using UnityEngine.Assertions;

using System;
using System.IO;
using System.Diagnostics;


//WARNING: Edit>Project Settings>Player->Other Settings->Configuration->Api Compability Level 2.0

public class SunContoller : MonoBehaviour
{

    //SerialPort ArduPort = new SerialPort("/dev/tty.usbmodem1421", 9600);      // UNCOMMENT!

    public OSC osc;

    public Light sunVisualisation;
    public float sunriseSpeed = 1.0f; // controls in seconds how fast sun changes, greater = slower
    public float sunBrightness = 1.2f;
    private int sunClock = 0;
    public int temp = 1;
    public float timeSinceLastSunriseRequest;

    private void Start()
    {
        //ArduPort.Open(); // UNCOMMENT!

        sunVisualisation = GetComponent<Light>();
        sunVisualisation.intensity = 0.0f;
    }

    private void Update()
    {
        timeSinceLastSunriseRequest += Time.deltaTime;
        if (timeSinceLastSunriseRequest < sunriseSpeed) {
            sunVisualisation.intensity += 0.1f * Time.deltaTime;
            if (sunBrightness<256.0f) sunBrightness += 0.2f;

        } else if (timeSinceLastSunriseRequest > sunriseSpeed) {
            if (sunVisualisation.intensity > 0 ) {
                sunVisualisation.intensity -= 0.1f * Time.deltaTime;
                if (sunBrightness > 1.2f) sunBrightness -= 0.2f;
            }
        }

        if (sunClock < 10) sunClock++;
        else {
            temp = (int)sunBrightness;
            if (temp <= 2) temp = 2;
            //ArduPort.WriteLine( temp.ToString() );    // UNCOMMENT!

            /*  // UNCOMMENT!
            OscMessage oscSunIntensity = new OscMessage();
            oscSunIntensity.address = "/sunIntensity";
            oscSunIntensity.values.Add(orginalSunIntesity);
            osc.Send(oscSunIntensity);
            */

            sunClock = 0;
        }



        /*
        //The bigger the number between 1 to 256, the brighter the led
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ArduPort.WriteLine("25");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ArduPort.WriteLine("50");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ArduPort.WriteLine("75");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ArduPort.WriteLine("100");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ArduPort.WriteLine("125");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ArduPort.WriteLine("150");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ArduPort.WriteLine("175");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ArduPort.WriteLine("200");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ArduPort.WriteLine("256");

        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ArduPort.WriteLine("1"); //this turns the led off
        }
        */
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
