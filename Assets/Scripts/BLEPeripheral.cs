using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

public class BLEPeripheral : MonoBehaviour
{
    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralSetService(
	BLENativePeripheral native, string service);

    // XXX Pekka's iPhone on Pekka's Mac
    public string device = "9504E2CA-6C5D-4951-9D88-62FD75E608B9";
    public string service = "180F"; // Battery
    // XXX Should have an array of characteristics
    public string characteristic = "2a19"; // Battery level

    public bool connect = false;
    protected bool _connect;

    protected BLENativePeripheral native;
    protected BLEConnection conn;
    
    void OnEnable() {
	BLE.PeripheralDiscovered += PeripheralDiscovered;
    }

    void OnDisable() {
	BLE.PeripheralDiscovered -= PeripheralDiscovered;
    }

    void Update() {
	if (connect != _connect) {
	    // XXX Refactor into a method of its own
	    if (connect) {
		if (native == null) {
		    Log("Peripheral not available, cannot connect");
		    connect = false;
		} else {
		    Debug.Assert(conn == null);
		    Log("Connecting...");
		    conn = native.Connect();
		}
	    } else {
		if (conn != null) {
		    conn.Disconnect();
		    conn = null;
		}
	    }
	    _connect = connect;
	}
    }

    void PeripheralDiscovered(BLENativePeripheral p) {
	// Log("Discovered " + device);
	if (device == p.identifier) {
	    if (native == null) {
		Log("Adapted " + device);
		native = p;
		BLENativePeripheralSetService(native, service);
	    }
	}
    }

    protected static void Log(string message) {
	Debug.Log("BLEPeripheral: " + message);
    }
}
