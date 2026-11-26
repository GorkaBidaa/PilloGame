using UnityEngine;
using Hulan.PilloSDK.DeviceManager;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PilloWindowsBridge : MonoBehaviour
{
#if UNITY_EDITOR_WIN
    [Header("Windows Simulator Settings")]
    [Tooltip("Pressure value sent to the SDK while holding the Space key.")]
    public int simulatedPressValue = 100;

    private const string VirtualDeviceId = "Windows-Virtual-Pillo";

    private void Start()
    {
        // Manually trigger the SDK initialization events on Windows Editor
        // so the Pillo Simulator window leaves the "not initialized" state
        PilloDeviceManager.onCentralDidInitialize?.Invoke();
        PilloDeviceManager.onCentralDidStartScanning?.Invoke();
        PilloDeviceManager.onPeripheralDidConnect?.Invoke(VirtualDeviceId);

        Debug.Log("[PilloWindowsBridge] Virtual Pillo Central initialized for Windows Editor.");
    }

    private void Update()
    {
        bool pressed = false;
        bool released = false;

        // Supports both Unity 6 New Input System and Legacy Input Manager
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            pressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            released = Keyboard.current.spaceKey.wasReleasedThisFrame;
        }
#else
        pressed = Input.GetKeyDown(KeyCode.Space);
        released = Input.GetKeyUp(KeyCode.Space);
#endif

        // Fire the exact same static delegate the real Pillo hardware uses
        if (pressed)
        {
            PilloDeviceManager.onPeripheralPressureDidChange?.Invoke(VirtualDeviceId, simulatedPressValue);
        }
        else if (released)
        {
            PilloDeviceManager.onPeripheralPressureDidChange?.Invoke(VirtualDeviceId, 0);
        }
    }
#endif
}
