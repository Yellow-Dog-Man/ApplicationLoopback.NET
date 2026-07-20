using ApplicationLoopback.NET;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

var file = File.OpenWrite("Test.raw");

var capture = new ApplicationLoopbackCapture(2, 48000);
capture.NewDataAvailable += Capture_NewDataAvailable;
capture.CaptureStopped += Capture_CaptureStopped;

var processes = Process.GetProcesses();
var process = processes.FirstOrDefault(p => p.ProcessName.Contains("firefox"));

Console.WriteLine($"Found process: {process.ProcessName} {process.Id}");

capture.StartCapture(process, CaptureMode.IncludeProcessTree);

Console.WriteLine("Capturing... Press Enter to stop");

Console.ReadLine();

await capture.StopCapture();
capture.Dispose();

await file.DisposeAsync();

void Capture_NewDataAvailable(Span<float> data)
{
    file.Write(MemoryMarshal.Cast<float, byte>(data));
}

void Capture_CaptureStopped()
{
    Console.WriteLine("Capture stopped");
}