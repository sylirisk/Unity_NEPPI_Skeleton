using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

[StructLayout (LayoutKind.Sequential)]
class BLEContext : SafeHandleZeroOrMinusOneIsInvalid
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

public class BLE : MonoBehaviour
{
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
    private static extern IntPtr BLEConnect(IntPtr peripheral);

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
	    setScanning(scanning);
	}
    }
    
    void OnDisable() {
	Log("DeInitialise");
	context.Dispose();
    }

    [MonoPInvokeCallback (typeof(BLEScanDeviceFoundCallback))]
    static void scanDeviceFound(IntPtr context, IntPtr peripheral, IntPtr add, long RSSI) {
	Log("Scan device found: " + peripheral + ", " + add + ", " + RSSI);
	Log("Scan device context: " + context);
	GCHandle gch = GCHandle.FromIntPtr(context);
	BLE ble = (BLE)gch.Target;
	Log("Scan device BLE: " + ble);
    }

    protected void setScanning(bool scanning) {
	_serviceUUID = serviceUUID;
	// If currently scanning, stop.
	if (_scanning) {
	    Log("Stopping scan");
	    BLEScanStop(context);
	}
	// Then, if asked to scan, start again.
	if (scanning) {
	    Log("Starting scan for " + _serviceUUID);
	    BLEScanStart(context, _serviceUUID, scanDeviceFound);
	}
	_scanning = scanning; 
    }
	    

    protected static void Log(string message) {
	Debug.Log("BLE: " + message);
    }

}
