using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

// XXX Refactor into a Neppi-specific file
[StructLayout(LayoutKind.Explicit)]
public struct NeppiValue {
    // MPU characteristic
    [FieldOffset( 0)] public short a_x;
    [FieldOffset( 2)] public short a_y;
    [FieldOffset( 4)] public short a_z;
    [FieldOffset( 6)] public short g_x;
    [FieldOffset( 8)] public short g_y;
    [FieldOffset(10)] public short g_z;
    [FieldOffset(12)] public short m_x;
    [FieldOffset(14)] public short m_y;
    [FieldOffset(16)] public short m_z;
    [FieldOffset(18)] short uuid; // XXX
    // Color characteristics
    [FieldOffset(0)]  public short color_hue;
    [FieldOffset(0)]  public byte  color_value;
    // State characteristic
    [FieldOffset(0)]  public byte  state;
    [FieldOffset(1)]  public short button_adc;
}

public class BLEPeripheral : MonoBehaviour
{
    public delegate void BLECharacteristicUpdatedCallback(
	IntPtr native, string uuid, IntPtr valuep);

    public delegate void updateCharacteristic(string uuid, NeppiValue value);
    public event updateCharacteristic CharacteristicUpdated;

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralSetService(
	BLENativePeripheral native, string service, IntPtr nativep);

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

    // Characteristics
    // XXX Should have an array of characteristics
    public string charactMPU        = "b131abbb-7195-142b-e012-0808817f198d"; // Neppi MPU
    public string charactColorHue   = "b131bbd0-7195-142b-e012-0808817f198d";
    public string charactColorValue = "b131bbcf-7195-142b-e012-0808817f198d";
    public string charactState      = "b131cccf-7195-142b-e012-0808817f198d"; // Neppi state
    protected string _charactMPU;
    protected string _charactColorHue;
    protected string _charactColorValue;
    protected string _charactState;

    public bool connect = false;
    protected bool _connect;

    // MPU values
    public int a_y = -1;
    public int a_z = -1;
    public int a_x = -1;

    public int g_y = -1;
    public int g_z = -1;
    public int g_x = -1;

    public int m_y = -1;
    public int m_z = -1;
    public int m_x = -1;

    // Color values
    public Color color = Color.red;

    protected BLENativePeripheral native;
    protected BLEConnection conn;

    // State
    public enum State { INIT, OFF, SLEEP, ACTIVE, PAINTING, BATTERY_LOW, CHARGING };
    public State state = State.INIT;

    // Debugging: Button ADC
    public short button_adc = -1;
    
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

    GCHandle gch;

    protected void UpdateService() {
	if (service != _service && native != null) {
	    gch = GCHandle.Alloc(native);
	    BLENativePeripheralSetService(native, service, GCHandle.ToIntPtr(gch));
	    _service = service;
	}
    }

    protected string UpdateCharacteristic(string newUUID, string oldUUID) {
	if (oldUUID != newUUID) {
	    if (oldUUID != null && oldUUID != "") {
		BLENativePeripheralRemoveCharacteristic(native, oldUUID);
		return null;
	    }
	    if (newUUID != null && newUUID != "") {
		BLENativePeripheralAddCharacteristic(
		    native, newUUID, characteristicUpdatedCallback);
	    }
	}
	return newUUID;
    }

    protected void UpdateCharacteristics() {
	if (native != null) {
	    _charactMPU        = UpdateCharacteristic(charactMPU, _charactMPU);
	    _charactColorHue   = UpdateCharacteristic(charactColorHue, _charactColorHue);
	    _charactColorValue = UpdateCharacteristic(charactColorValue, _charactColorValue);
	    _charactState      = UpdateCharacteristic(charactState, _charactState);
	}
    }

    void Update() {
	UpdateConnect();
	UpdateService();
	UpdateCharacteristics();
    }

    void PeripheralDiscovered(BLENativePeripheral p) {
	Log("Discovered " + p.identifier + " name=" + p.name);
	if (device == p.identifier) {
	    if (native == null) {
		Log("Adapted " + device);
		native = p;
		p.client = this;
	    }
	}
    }

    void CharacteristicNotify(string uuid, NeppiValue v) {
	if (String.Equals(uuid, charactMPU, StringComparison.OrdinalIgnoreCase)) {
	    a_x = v.a_x;
	    a_y = v.a_y;
	    a_z = v.a_z;

	    g_x = v.g_x;
	    g_y = v.g_y;
	    g_z = v.g_z;

	    m_x = v.m_x;
	    m_y = v.m_y;
	    m_z = v.m_z;
	    return;
	}

	if (String.Equals(uuid, charactColorHue, StringComparison.OrdinalIgnoreCase)) {
	    float H, S, V;
	    Color.RGBToHSV(color, out H, out S, out V);
	    color = Color.HSVToRGB((float)(v.color_hue/360.0), 1.0f, V);
	    return;
	}

	if (String.Equals(uuid, charactColorValue, StringComparison.OrdinalIgnoreCase)) {
	    double value = ((double)v.color_value)/100.0;
	    Debug.Assert(value >= 0.0 && value <= 1.0);
	    // Brighten the intensity for Unity, as the unit is quite bright
	    value = Math.Pow(value, 0.25);

	    float H, S, V;
	    Color.RGBToHSV(color, out H, out S, out V);
	    color = Color.HSVToRGB(H, S, (float)value);
	    return;
	}

	if (String.Equals(uuid, charactState, StringComparison.OrdinalIgnoreCase)) {
	    state = (State)v.state;
	    button_adc = v.button_adc;
	}
    }

    [MonoPInvokeCallback (typeof(BLECharacteristicUpdatedCallback))]
    static void characteristicUpdatedCallback(IntPtr nativep, string uuid, IntPtr valuep) {
	NeppiValue value =
	    (NeppiValue)Marshal.PtrToStructure(valuep, typeof(NeppiValue));

	GCHandle gch = GCHandle.FromIntPtr(nativep);
	BLENativePeripheral native = (BLENativePeripheral)gch.Target;
	BLEPeripheral peri = native.client;

	if (peri != null && peri.CharacteristicUpdated != null) {
	    peri.CharacteristicUpdated(uuid, value);
	}

    }

    protected static void Log(string message) {
	Debug.Log("BLEPeripheral: " + message);
    }
}
