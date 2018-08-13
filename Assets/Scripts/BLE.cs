using UnityEngine;
using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

[StructLayout (LayoutKind.Sequential)]
public class BLEManager : SafeHandleZeroOrMinusOneIsInvalid
{
    GCHandle gch;
    Thread linuxHelperThread;

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeInitialise(BLEManager self, IntPtr manager);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeDeInitialise(IntPtr native);

#if UNITY_STANDALONE_LINUX || UNITY_TEST_THREADING
    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeLinuxHelper(BLEManager self);

    protected void LinuxHelperLoop() {
	Debug.Log("BLEManager: Linux thread starting");
	try {
	    while (true) {
		BLENativeLinuxHelper(this);
	    }
	} catch (ThreadInterruptedException) {
	    Debug.Log("BLEManager: Linux thread interrupted");
	}
	Debug.Log("BLEManager: Linux thread exiting");
    }

    protected void LinuxHelperTerminate() {
	if (linuxHelperThread != null) {
	    // Interrupt the Linux thread at next block
	    linuxHelperThread.Interrupt();
	    // Wait until the Linux thread exists
	    linuxHelperThread.Join();
	    linuxHelperThread = null;
	}
    }
#endif

    // Called by the runtime
    public BLEManager() : base(true) {
    }

    public void Initialise(BLE cs) {
	gch = GCHandle.Alloc(cs);
	BLENativeInitialise(this, GCHandle.ToIntPtr(gch));
#if UNITY_STANDALONE_LINUX || UNITY_TEST_THREADING
	linuxHelperThread = new Thread(LinuxHelperLoop);
	linuxHelperThread.Start();
#endif
    }


    protected override bool ReleaseHandle() {
	if (!this.IsInvalid) {
#if UNITY_STANDALONE_LINUX || UNITY_TEST_THREADING
	    LinuxHelperTerminate();
#endif
	    BLENativeDeInitialise(handle);
	    handle = IntPtr.Zero;
	}
        return true;
    }
}

[StructLayout (LayoutKind.Sequential)]
public class BLENativePeripheral : SafeHandleZeroOrMinusOneIsInvalid {
    public BLEManager manager; // XXX Fix visibility

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralGetIdentifier(
	IntPtr peripheral, StringBuilder identifier, int len);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralGetName(
	IntPtr peripheral, StringBuilder name, int len);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativePeripheralRelease(IntPtr peripheral);

    [DllImport ("Unity3D_BLE")]
    private static extern BLEConnection BLENativeConnect(
	BLEManager manager, BLENativePeripheral peripheral);

    // Called by the runtime when BLENativeCreatePeripheral has returned
    public BLENativePeripheral() : base(true) {
	Debug.Log("BLENativePeripheral: constructor " + this + ": " +
		  RuntimeHelpers.GetHashCode(this));
    }

    public string identifier {
	get {
	    StringBuilder sb = new StringBuilder(256/*XXX*/);
	    BLENativePeripheralGetIdentifier(handle, sb, sb.Capacity);
	    return sb.ToString();
	}
    }

    public string name {
	get {
	    StringBuilder sb = new StringBuilder(256/*XXX*/);
	    BLENativePeripheralGetName(handle, sb, sb.Capacity);
	    return sb.ToString();
	}
    }

    public BLEConnection Connect() {
	BLEConnection conn = BLENativeConnect(manager, this);
	conn.manager = manager;
	return conn;
    }

    protected override bool ReleaseHandle() {
	Debug.Log("BLENativePeripheral: release " + this + ": " +
		  RuntimeHelpers.GetHashCode(this));
	if (!this.IsInvalid) {
	    BLENativePeripheralRelease(handle);
	    handle = IntPtr.Zero;
	}
	return true;
    }
}

[StructLayout (LayoutKind.Sequential)]
public class BLEConnection : SafeHandleZeroOrMinusOneIsInvalid
{
    public BLEManager manager; // XXX Fix visibility

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeDisconnect(BLEManager manager, IntPtr connection);

    // Called by the runtime
    public BLEConnection() : base(true) {
	Log("constructor");
    }

    public void Disconnect() {
	Log("disconnect");
	BLENativeDisconnect(manager, handle);
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
	IntPtr manager, IntPtr peripheral, IntPtr advertisementdata, long RSSI);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeInitLog();

    [DllImport ("Unity3D_BLE")]
    private static extern BLEManager BLENativeCreateManager();

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeScanStart(
	BLEManager manager, string serviceUUIDstring, BLEScanDeviceFoundCallback callback);

    [DllImport ("Unity3D_BLE")]
    private static extern void BLENativeScanStop(BLEManager manager);

    [DllImport ("Unity3D_BLE")]
    private static extern BLENativePeripheral BLENativeCreatePeripheral(IntPtr peripheral);

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
     * Manager that holds pointers both to the native BLE and the C# BLE objects.
     */
    BLEManager manager;

    void Start() {
	Log("Initialise");
	BLENativeInitLog();
	manager = BLENativeCreateManager();
	manager.Initialise(this);
	Log("Initialise...done.");
    }

    void Update() {
	if (scanning != _scanning || serviceUUID != _serviceUUID) {
	    SetScanning(scanning);
	}
    }

    void OnDisable() {
	Log("DeInitialise");
	manager.Dispose();
    }

    [MonoPInvokeCallback (typeof(BLEScanDeviceFoundCallback))]
    static void ScanDeviceFound(IntPtr ctx, IntPtr peripheral, IntPtr add, long RSSI) {
	GCHandle gch = GCHandle.FromIntPtr(ctx);
	BLE ble = (BLE)gch.Target;
	Log("Device found: " + ble + ", " + peripheral + ", " + add + ", " + RSSI);

	if (PeripheralDiscovered != null) {
	    BLENativePeripheral peri = BLENativeCreatePeripheral(peripheral);
	    peri.manager = ble.manager;
	    PeripheralDiscovered(peri);
	}
    }

    protected void SetScanning(bool scanning) {
	_serviceUUID = serviceUUID;
	// If currently scanning, stop.
	if (_scanning) {
	    Log("Stopping scan");
	    BLENativeScanStop(manager);
	}
	// Then, if asked to scan, start again.
	if (scanning) {
	    Log("Starting scan for " + _serviceUUID);
	    BLENativeScanStart(manager, _serviceUUID, ScanDeviceFound);
	}
	_scanning = scanning;
    }

    protected static void Log(string message) {
	Debug.Log("BLE: " + message);
    }
}
