using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PainterController : MonoBehaviour {

    public delegate void AddSunLight();
    public static event AddSunLight AddSunEvent;

    public float thrust = 1;
    private Rigidbody rb;
    private TrailRenderer tr;
    Material mat;
    Color baseColor = Color.yellow;

    public bool button_down = false;

    public int a_x = -1;
    public int a_y = -1;
    public int a_z = -1;

    private int shakeStackIndex = 0; 
    private int[] shakeStackX = new int[5]; // stores 5 last values of a_x, used to recognize shaking
    private int[] shakeStackY = new int[5];
    private int[] shakeStackZ = new int[5];

    public int g_x = -1;
    public int g_y = -1;
    public int g_z = -1;

    public int m_x = -1;
    public int m_y = -1;
    public int m_z = -1;

    public float x_force = 0;
    public float y_force = 0;
    public float lift_force = 0;

    private bool deviceShaken = false;

    // This is executed when the script becomes active in Unity
    void OnEnable()
    {
        BLEPeripheral.CharacteristicUpdated += IOTDeviceListener; // add our event listener
    }

    // This is executed when the script becomes inactive in Unity
    private void OnDisable()
    {
        BLEPeripheral.CharacteristicUpdated += IOTDeviceListener; // remove our event listener
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Renderer renderer = GetComponent<Renderer>();
        mat = renderer.material;
        tr = GetComponent<TrailRenderer>();
    }

    private void Update()
    {
        float emission = Mathf.PingPong(Time.time, 1.0f);

        Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);
        mat.SetColor("_EmissionColor", finalColor);

        if (deviceShaken) {
            deviceShaken = false;
            if (AddSunEvent != null)
                AddSunEvent();
        }

    }

    private void FixedUpdate()
    {
        rb.AddForce(x_force * thrust, lift_force * thrust, y_force * thrust);
    }

    void UpdatePainterForces() 
    {
        if (button_down) {
            if (lift_force < 100) lift_force += 3.0f;

            float temp_x = g_x / 50;

            if (temp_x > 100) x_force = 100;
            else if (temp_x < -100) x_force = -100;
            else x_force += temp_x;

            if (x_force > 100) x_force = 100;
            else if (x_force < -100) x_force = -100;

            float temp_y = g_y / 50;

            if (temp_y > 100) y_force = 100;
            else if (temp_y < -100) y_force = -100;
            else y_force += temp_y;

            if (y_force > 100) y_force = 100;
            else if (y_force < -100) y_force = -100;

            mat.EnableKeyword("_EMISSION");

        }
        else {
            lift_force = 0;
            x_force = 0;
            y_force = 0;

            mat.DisableKeyword("_EMISSION");
        }
    }

    void UpdateColor(int hue) // Expecting 0-360 
    {
        float trueHue = (float)hue / 360;
        print("Updating color, hue " + hue + " trueHue: "+trueHue);
        baseColor = Color.HSVToRGB(trueHue, 1.0f, 1.0f);
        tr.material.SetColor("_EmissionColor",baseColor);
    }

    bool ShakeCheck() // if accelerator trend over shaking margin, cast event to sun
    {
        bool shaken = false;
        float x_i = 0;
        float y_i = 0;
        float z_i = 0;

        for (int n = 0; n < 5; n++) 
        {
            x_i += shakeStackX[n];
            y_i += shakeStackY[n];
            z_i += shakeStackZ[n];
        }
        x_i = x_i / shakeStackX.Length; // get average of last 5
        y_i = y_i / shakeStackY.Length;
        z_i = z_i / shakeStackZ.Length;

        float x_diff = x_i - a_x;
        float y_diff = y_i - a_y;
        float z_diff = z_i - a_z;

        if (x_diff < -1000 || x_diff > 1000) // the value 1000 is empirically tested, don't change!
        {
            shaken = true;
        }
        else if (y_diff < -1000 || y_diff > 1000)
        {
            shaken = true;
        }
        else if (z_diff < -1000 || z_diff > 1000)
        {
            shaken = true;
        }

        // store the most recent values to shakeStack
        shakeStackIndex++;
        if (shakeStackIndex >= 5) shakeStackIndex = 0;
        shakeStackX[shakeStackIndex] = a_x;
        shakeStackY[shakeStackIndex] = a_y;
        shakeStackZ[shakeStackIndex] = a_z;

        return shaken;
    }

    void IOTDeviceListener(string uuid, NeppiValue v)
    {
        a_x = v.a_x;
        a_y = v.a_y;
        a_z = v.a_z;
        deviceShaken = ShakeCheck();

        g_x = v.g_x;
        g_y = v.g_y;
        g_z = v.g_z;
        UpdatePainterForces();

        m_x = v.m_x;
        m_y = v.m_y;
        m_z = v.m_z;

        // Simulates device button press
        if (Input.GetKey(KeyCode.Space)) button_down = true;    
        else button_down = false;

        // reset colour with R-key
        if (Input.GetKey(KeyCode.R)) 
        {
            int i = Random.Range(0, 360); // fakes the 'hue' from device
            UpdateColor(i);
        }
    }
}
