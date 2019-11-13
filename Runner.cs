using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace svisha
{
    class Runner
    {

        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for cancellation.</param>
        /// <param name="cancellationToken">A cancellation token. If invoked, the task will return 
        /// immediately as canceled.</param>
        /// <returns>A Task representing waiting for the process to end.</returns>
        private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if(cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(tcs.SetCanceled);
            }

            return tcs.Task;
        }

        private static string FindProgram(string program)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                program = program + ".exe";
            }

            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var local = Path.Combine(dir, program);
            if (File.Exists(local))
            {
#if DEBUG
                Console.Error.WriteLine($"Using {local}");
#endif
                return Path.Combine(dir, program);
            }

            return program;
        }

        private static async Task<Process> _exec(string program, IEnumerable<string> args, Action<ProcessStartInfo> setProcessStartInfoProperties)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = FindProgram(program),
                UseShellExecute = false,
            };
            
            foreach (var a in args)
            {
                startInfo.ArgumentList.Add(a);
            }

            setProcessStartInfoProperties(startInfo);

            var process = new Process()
            {
                StartInfo = startInfo
            };

#if DEBUG
            Console.Error.WriteLine($"{program} {string.Join(" ", startInfo.ArgumentList)}");
#endif

            process.Start();
            await WaitForExitAsync(process);
            return process;
        }


        public static async Task<int> Exec(string program, IEnumerable<string> args)
        {
            var process = await _exec(program, args, p =>
            {
            });

            return process.ExitCode;
        }

        public static async Task<Process> ExecWithStdoutRedirect(string program, IEnumerable<string> args)
        {

            var process = await _exec(program, args, p =>
            {
                p.RedirectStandardOutput = true;
            });

            return process;
        }
    }
}
