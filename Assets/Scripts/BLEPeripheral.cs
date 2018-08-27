using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

// XXX Refactor into a Neppi-specific file
public struct NeppiValue {
    public short a_x;
    public short a_y;
    public short a_z;
    public short g_x;
    public short g_y;
    public short g_z;
    public short m_x;
    public short m_y;
    public short m_z;
    short uuid; // XXX
}

public class BLEPeripheral : MonoBehaviour
{
    public delegate void BLECharacteristicUpdatedCallback(string uuid, IntPtr valuep);

    public delegate void updateCharacteristic(string uuid, NeppiValue value);
    public static event updateCharacteristic CharacteristicUpdated;

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralSetService(
	BLENativePeripheral native, string service);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralAddCharacteristic(
	BLENativePeripheral native, string characteristic,
	BLECharacteristicUpdatedCallback callback);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralRemoveCharacteristic(
	BLENativePeripheral native, string characteristic);

    public string device = "00:7E:6B:5F:95:30"; // nRF52 DK

    public string service = "b131abdc-7195-142b-e012-0808817f198d"; // Neppi
    protected string _service;

    // XXX Should have an array of characteristics
    public string characteristic = "b131bbd0-7195-142b-e012-0808817f198d"; // Neppi button
    protected string _characteristic;

    public bool connect = false;
    protected bool _connect;

    public int a_y = -1;
    public int a_z = -1;
    public int a_x = -1;

    public int g_y = -1;
    public int g_z = -1;
    public int g_x = -1;

    public int m_y = -1;
    public int m_z = -1;
    public int m_x = -1;

    protected BLENativePeripheral native;
    protected BLEConnection conn;
    
    void OnEnable() {
	BLE.PeripheralDiscovered += PeripheralDiscovered;
	CharacteristicUpdated += CharacteristicNotify;
    }

    void OnDisable() {
	CharacteristicUpdated -= CharacteristicNotify;
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
		BLENativePeripheralAddCharacteristic(
		    native, characteristic, characteristicUpdatedCallback);
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
	Log("Discovered " + p.identifier);
	if (device == p.identifier) {
	    if (native == null) {
		Log("Adapted " + device);
		native = p;
	    }
	}
    }

    void CharacteristicNotify(string uuid, NeppiValue v) {
	a_x = v.a_x;
	a_y = v.a_y;
	a_z = v.a_z;

	g_x = v.g_x;
	g_y = v.g_y;
	g_z = v.g_z;

	m_x = v.m_x;
	m_y = v.m_y;
	m_z = v.m_z;
    }

    [MonoPInvokeCallback (typeof(BLECharacteristicUpdatedCallback))]
    static void characteristicUpdatedCallback(string uuid, IntPtr valuep) {

	NeppiValue value =
	    (NeppiValue)Marshal.PtrToStructure(valuep, typeof(NeppiValue));

	if (CharacteristicUpdated != null) {
	    CharacteristicUpdated(uuid, value);
	}
    }

    protected static void Log(string message) {
	Debug.Log("BLEPeripheral: " + message);
    }
}
