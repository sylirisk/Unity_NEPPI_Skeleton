using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

public class BLEPeripheral : MonoBehaviour
{
    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralSetService(
	BLENativePeripheral native, string service);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralAddCharacteristic(
	BLENativePeripheral native, string characteristic);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralRemoveCharacteristic(
	BLENativePeripheral native, string characteristic);

    public string device = "9504E2CA-6C5D-4951-9D88-62FD75E608B9"; // XXX Pekka's iPhone on Pekka's Mac

    public string service = "180F"; // Battery
    protected string _service;

    // XXX Should have an array of characteristics
    public string characteristic = "2a19"; // Battery level
    protected string _characteristic;

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

    protected void UpdateConnect() {
	if (connect != _connect) {
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

    protected void UpdateService() {
	if (service != _service && native != null) {
	    BLENativePeripheralSetService(native, service);
	    _service = service;
	}
    }

    protected void UpdateCharacteristics() {
	if (_characteristic != characteristic && native != null) {
	    if (_characteristic != null && _characteristic != "") {
		BLENativePeripheralRemoveCharacteristic(native, _characteristic);
		_characteristic = null;
	    }
	    if (characteristic != null && characteristic != "") {
		BLENativePeripheralAddCharacteristic(native, characteristic);
	    }
	    _characteristic = characteristic;
	}
    }

    void Update() {
	UpdateConnect();
	UpdateService();
	UpdateCharacteristics();
    }

    void PeripheralDiscovered(BLENativePeripheral p) {
	// Log("Discovered " + device);
	if (device == p.identifier) {
	    if (native == null) {
		Log("Adapted " + device);
		native = p;
	    }
	}
    }

    protected static void Log(string message) {
	Debug.Log("BLEPeripheral: " + message);
    }
}
