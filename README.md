# ApplicationLoopback.NET
This library allows capturing audio from a particular process on Windows or the reverse - capturing all system audio except for a specific process in C# applications.

It wraps the Windows API for Application Loopback Capture based on the example code from this repository: https://github.com/microsoft/Windows-classic-samples

## Usage
The API is designed to be pretty simple to use and C#/.NET idiomatic, hiding the details of the native code.

```CSharp

const int CHANNELS = 2;
const int SAMPLE_RATE = 48000;

const int PROCESS_ID = 0; // Replace with the ID of the process tree you want to capture (or exclude)

// Create new capture. You need one for each process you want to capture (in case you want to have multiple in parallel)
var capture = new ApplicationLoopbackCapture(CHANNELS, SAMPLE_RATE);
capture.NewDataAvailable += Capture_NewDataAvailable;
capture.CaptureStopped += Capture_CaptureStopped;

// Start the capture. You can also pass it instance of Process class as well.
// If you want to exclude the process audio and capture everything else give it ExcludeProcessTree instead
capture.StartCapture(PROCESS_ID, CaptureMode.IncludeProcessTree);

Console.WriteLine("Capturing... Press Enter to stop");
Console.ReadLine();

// The capture isn't guaranteed to stop immediatelly. The method is awaitable for that reason in case you need to wait
await capture.StopCapture();

// You are free to call Dispose without waiting on StopCapture, it will schedule disposal of native resources when capture stops automatically
capture.Dispose();

void Capture_NewDataAvailable(ReadoSpan<float> data)
{
    // Do something with the audio data
    // The audio samples will be interleaved
}

void Capture_CaptureStopped()
{
    // This is optional if you need to do something on when it stops
}
```
