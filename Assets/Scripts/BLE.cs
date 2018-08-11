using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

[StructLayout (LayoutKind.Sequential)]
public class BLEContext : SafeHandleZeroOrMinusOneIsInvalid
{
    GCHandle gch;

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEInitialise(BLEContext self, IntPtr context);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEDeInitialise(IntPtr native);

    // Called by the runtime
    public BLEContext() : base(true) {
    }

    public void Initialise(BLE cs) {
	gch = GCHandle.Alloc(cs);
	BLEInitialise(this, GCHandle.ToIntPtr(gch));
    }

    protected override bool ReleaseHandle() {
	if (!this.IsInvalid) {
	    BLEDeInitialise(handle);
	    handle = IntPtr.Zero;
	}
        return true;
    }
}

[StructLayout (LayoutKind.Sequential)]
public class BLENativePeripheral : SafeHandleZeroOrMinusOneIsInvalid {
    public BLEContext context; // XXX Fix visibility

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEPeripheralGetIdentifier(
	IntPtr peripheral, StringBuilder identifier, int len);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEPeripheralGetName(
	IntPtr peripheral, StringBuilder name, int len);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEPeripheralRelease(IntPtr peripheral);

    [DllImport ("Unity3D_BLE")]
    private static extern BLEConnection BLEConnect(
	BLEContext context, BLENativePeripheral peripheral);

    // Called by the runtime when BLECreatePeripheral has returned
    public BLENativePeripheral() : base(true) {
    }

    public string identifier {
	get {
	    StringBuilder sb = new StringBuilder(256/*XXX*/);
	    BLEPeripheralGetIdentifier(handle, sb, sb.Capacity);
	    return sb.ToString();
	}
    }

    public string name {
	get {
	    StringBuilder sb = new StringBuilder(256/*XXX*/);
	    BLEPeripheralGetName(handle, sb, sb.Capacity);
	    return sb.ToString();
	}
    }

    public BLEConnection Connect() {
	BLEConnection conn = BLEConnect(context, this);
	conn.context = context;
	return conn;
    }

    protected override bool ReleaseHandle() {
	if (!this.IsInvalid) {
	    BLEPeripheralRelease(handle);
	}
	return true;
    }
}

[StructLayout (LayoutKind.Sequential)]
public class BLEConnection : SafeHandleZeroOrMinusOneIsInvalid
{
    public BLEContext context; // XXX Fix visibility

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEDisconnect(BLEContext context, IntPtr connection);

    // Called by the runtime
    public BLEConnection() : base(true) {
	Log("constructor");
    }

    public void Disconnect() {
	Log("disconnect");
	BLEDisconnect(context, handle);
	handle = IntPtr.Zero;
    }

    protected override bool ReleaseHandle() {
	Log("destructor");
	if (!this.IsInvalid) {
	    Disconnect();
	}
	Log("destructor...done.");
        return true;
    }

    protected static void Log(string message) {
	Debug.Log("BLEConnection: " + message);
    }
}

public class BLE : MonoBehaviour
{
    public delegate void discoveredPeripheral(BLENativePeripheral p);
    public static event discoveredPeripheral PeripheralDiscovered;

    delegate void BLEScanDeviceFoundCallback(
	IntPtr context, IntPtr peripheral, IntPtr advertisementdata, long RSSI);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEInitLog();

    [DllImport ("Unity3D_BLE")]
    private static extern BLEContext BLECreateContext();

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEScanStart(
	BLEContext context, string serviceUUIDstring, BLEScanDeviceFoundCallback callback);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLEScanStop(BLEContext context);

    [DllImport ("Unity3D_BLE")]
    private static extern BLENativePeripheral BLECreatePeripheral(IntPtr peripheral);

    public bool scanning = false;
    protected bool _scanning = false;

    /**
     * XXX
     *
     * Restarts scanning with the new UUID, if changed while scanning.
     */
    public string serviceUUID = ""; // Empty: scan for all
    protected string _serviceUUID;

    /**
     * Context that holds pointers both to the native BLE and the C# BLE objects.
     */
    BLEContext context;

    void Start() {
	Log("Initialise");
	BLEInitLog();
	context = BLECreateContext();
	context.Initialise(this);
	Log("Initialise...done.");
    }

    void Update() {
	if (scanning != _scanning || serviceUUID != _serviceUUID) {
	    SetScanning(scanning);
	}
    }

    void OnDisable() {
	Log("DeInitialise");
	context.Dispose();
    }

    [MonoPInvokeCallback (typeof(BLEScanDeviceFoundCallback))]
    static void ScanDeviceFound(IntPtr ctx, IntPtr peripheral, IntPtr add, long RSSI) {
	GCHandle gch = GCHandle.FromIntPtr(ctx);
	BLE ble = (BLE)gch.Target;
	// Log("Device found: " + ble + ", " + peripheral + ", " + add + ", " + RSSI);

	if (PeripheralDiscovered != null) {
	    BLENativePeripheral peri = BLECreatePeripheral(peripheral);
	    peri.context = ble.context;
	    PeripheralDiscovered(peri);
	}
    }

    protected void SetScanning(bool scanning) {
	_serviceUUID = serviceUUID;
	// If currently scanning, stop.
	if (_scanning) {
	    Log("Stopping scan");
	    BLEScanStop(context);
	}
	// Then, if asked to scan, start again.
	if (scanning) {
	    Log("Starting scan for " + _serviceUUID);
	    BLEScanStart(context, _serviceUUID, ScanDeviceFound);
	}
	_scanning = scanning;
    }

    protected static void Log(string message) {
	Debug.Log("BLE: " + message);
    }
}
