using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static async Task<Process> _exec(string program, IEnumerable<string> args, Action<ProcessStartInfo> setProcessStartInfoProperties)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = program,
                CreateNoWindow = true,
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

            process.Start();
            await WaitForExitAsync(process);
            return process;
        }


        public static async Task<int> Exec(string program, IEnumerable<string> args)
        {
            var process = await _exec(program, args, p =>
            {
                p.RedirectStandardOutput = false;
                p.UseShellExecute = false;
            });

            return process.ExitCode;
        }

        public static async Task<Process> ExecWithStdoutRedirect(string program, IEnumerable<string> args)
        {

            var process = await _exec(program, args, p =>
            {
                p.RedirectStandardOutput = true;
                p.UseShellExecute = false;
            });

            return process;
        }
    }
}
