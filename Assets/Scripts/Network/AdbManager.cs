using System.Diagnostics;
using System.Threading.Tasks;

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
            await ExecuteCommand($"adb reverse tcp:{port} tcp:{port}");
        }

        private static async Task ExecuteCommand(string command)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = true,
            };
            process.Start();

            await WaitForExitAsync(process);
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