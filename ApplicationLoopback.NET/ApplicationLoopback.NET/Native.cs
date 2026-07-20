using System;
using System.Runtime.InteropServices;

namespace ApplicationLoopback.NET
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void AudioCallback(IntPtr instance, IntPtr data, uint length);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void AudioEvent(IntPtr instance);

    public static class Native
    {
        [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr InitializeCapture(ushort channels, uint sampleRate, ushort bitsPerSample, 
            AudioCallback callback, AudioEvent audioCaptureStopped);

        [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int StartCaptureAsync(IntPtr capture, uint processId, bool includeProcessTree);

        [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int StopCaptureAsync(IntPtr capture);

        [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void FreeCapture(IntPtr capture);
    }
}
