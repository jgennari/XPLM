﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FlyByWireless.XPLM;

public enum CameraControlDuration
{
    Uncontrolled,
    UntilViewChanges,
    Forever
}

public readonly struct CameraPosition
{
    public readonly float X, Y, Z, Pitch, Heading, Roll, Zoom;

    public CameraPosition(float x, float y, float z, float pitch, float heading, float roll, float zoom) =>
        (X, Y, Z, Pitch, Heading, Roll, Zoom) = (x, y, z, pitch, heading, roll, zoom);
}

public delegate bool CameraControl(out CameraPosition? cameraPosition, bool isLosingControl);

public static class Camera
{
    static GCHandle? _handle;

    public static unsafe void Control(CameraControlDuration howLong, CameraControl control)
    {
        [DllImport(Defs.Lib)]
        static extern void XPLMControlCamera(CameraControlDuration howLong, delegate* unmanaged<ref CameraPosition, int, nint, int> control, nint state);

        [UnmanagedCallersOnly]
        static int C(ref CameraPosition cameraPosition, int isLosingControl, nint state)
        {
            var c = ((CameraControl)GCHandle.FromIntPtr(state).Target!)(out var position, isLosingControl != 0);
            if (position.HasValue && !Unsafe.IsNullRef(ref cameraPosition))
                cameraPosition = position!.Value;
            return c ? 1 : 0;
        }

        if (_handle.HasValue)
            _handle.Value.Free();
        _handle = GCHandle.Alloc(control);
        XPLMControlCamera(howLong, &C, GCHandle.ToIntPtr(_handle.Value));
    }

    public static void DontControl()
    {
        [DllImport(Defs.Lib)]
        static extern void XPLMDontControlCamera();

        XPLMDontControlCamera();
        if (_handle.HasValue)
        {
            _handle.Value.Free();
            _handle = null;
        }
    }

    public static CameraControlDuration IsBeingControlled
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern int XPLMIsCameraBeingControlled(out CameraControlDuration duration);

            return XPLMIsCameraBeingControlled(out var duration) != 0 ? duration : default;
        }
    }

    public static CameraPosition Position
    {
        get
        {
            [DllImport(Defs.Lib)]
            static extern void XPLMReadCameraPosition(out CameraPosition cameraPosition);

            XPLMReadCameraPosition(out var position);
            return position;
        }
    }
}