using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationLoopback.NET
{
    public delegate void AudioDataHandler(Span<float> data);
    public delegate void AudioEventHandler();

    public class ApplicationLoopbackCapture : IDisposable
    {
        public int Channels { get; private set; }
        public int SampleRate { get; private set; }

        bool _captureActive;
        bool _captureStopRequested;
        bool _disposed;

        TaskCompletionSource<bool> _captureCompleted;

        IntPtr _capture;

        public event AudioDataHandler NewDataAvailable;
        public event AudioEventHandler CaptureStopped;

        public ApplicationLoopbackCapture(int channels, int sampleRate)
        {
            this.Channels = channels;
            this.SampleRate = sampleRate;

            _capture = Native.InitializeCapture((ushort)Channels, (uint)SampleRate, 16, OnData, OnCaptureStopped);

            if (_capture == IntPtr.Zero)
                throw new Exception($"Failed to initialize capture");
        }

        public void StartCapture(Process process, CaptureMode mode) => StartCapture((uint)process.Id, mode);

        public void StartCapture(uint processId, CaptureMode mode)
        {
            CheckDisposed();
            CheckCaptureActive();

            _captureActive = true;
            _captureStopRequested = false;

            var success = Native.StartCaptureAsync(_capture, processId, mode == CaptureMode.IncludeProcessTree);

            if (success < 0)
                throw new Exception($"Failed to start capture. Error code: {success}");

            // The capture won't stop immediatelly so we might have systems that wait on it stopping
            // Create the completion source now when we started, because it's the cleanest time to do it
            _captureCompleted = new TaskCompletionSource<bool>();
        }

        public Task StopCapture()
        {
            CheckDisposed();

            if (!_captureActive)
                throw new InvalidOperationException("Capture is not active");

            if (_captureStopRequested)
                throw new InvalidOperationException("Capture stop was already requested");

            _captureStopRequested = true;

            Native.StopCaptureAsync(_capture);

            return _captureCompleted.Task;
        }

        void OnData(IntPtr instance, IntPtr data, uint length)
        {
            unsafe
            {
                var rawData = new Span<short>(data.ToPointer(), (int)length);

                // Convert the sample data to floats. We don't want the caller to worry about the internal formats
                // And floats are nice way to represent audio data in agnostic way
                // They still have to worry about channel interleaving and such, but that's pretty normal
                Span<float> buffer = stackalloc float[rawData.Length];

                for (int i = 0; i < rawData.Length; i++)
                    buffer[i] = ToFloat(rawData[i]);

                NewDataAvailable?.Invoke(buffer);
            }
        }

        static float ToFloat(short sample)
        {
            // The MinValue is 1 larger than MaxValue so use that
            // TODO!!! Is this accurate enough? Should better conversion be used?
            const float INV = 1f / -short.MinValue;

            return sample * INV;
        }

        void OnCaptureStopped(IntPtr instance)
        {
            _captureActive = false;
            _captureCompleted.TrySetResult(true);
        }

        void CheckDisposed()
        {
            if (_disposed)
                throw new InvalidOperationException("This instance has been disposed");
        }

        void CheckCaptureActive()
        {
            if (_captureActive)
                throw new InvalidOperationException("Capture is already active!");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_captureActive)
            {
                // If the capture is still active (e.g. Dispose was called too soon)
                // Let's wait for it to finish and then call it again
                Task.Run(async () =>
                {
                    // Wait for the capture to complete
                    await _captureCompleted.Task;

                    // Do the actual dispose
                    DisposeInternal();
                });

                return;
            }

            DisposeInternal();
        }

        void DisposeInternal()
        {
            // Free the native resource
            Native.FreeCapture(_capture);

            _capture = IntPtr.Zero;
        }
    }
}
