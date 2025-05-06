using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Application = UnityEngine.Application;

namespace Network
{
    public static class AdbManager
    {
        /// <summary>
        /// Forwards a port to allow adb passthrough
        /// </summary>
        /// <param name="port">The port to allow</param>
        public static async Task ForwardPort(int port)
        {
            await ExecuteCommand($"reverse tcp:{port} tcp:{port}");
            UnityEngine.Debug.Log($"port {port} forwarded");
            
            // try
            // {
            //     await ExecuteCommand($"reverse tcp:{port} tcp:{port}");
            //     UnityEngine.Debug.Log("ports forwarded");
            // }
            // catch
            // {
            //     UnityEngine.Debug.LogError($"Failed to forward tcp:{port} tcp:{port}");
            // }
        }

        private static async Task ExecuteCommand(string command)
        {
            // get adb from streaming assets
            string path = Path.Combine(Application.streamingAssetsPath, "adb.exe");
            
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(path)
            {
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            process.Start();

            await WaitForExitAsync(process);
            UnityEngine.Debug.Log($"adb stdout: {await process.StandardOutput.ReadToEndAsync()}");
            UnityEngine.Debug.Log($"adb stderr: {await process.StandardError.ReadToEndAsync()}");
        }

        private static Task WaitForExitAsync(Process process)
        {
            // exit early if process has already exited
            if (process.HasExited) return Task.CompletedTask;

            // setup completion
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => tcs.TrySetResult(true);

            // exit if process has already exited or return wait task
            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }
    }
}