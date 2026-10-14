using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace CodeAssist.Indexing
{
    public sealed class IndexBuildResult
    {
        public long ElapsedMilliseconds { get; internal set; }
        public bool UsedWorker { get; internal set; }
        public string WorkerFailure { get; internal set; }
    }

    public static class IndexBuildRunner
    {
        public static IndexBuildResult Build(PersistentSymbolIndex index, string mode, string root, string project,
            string workerPath, bool useWorker, CancellationToken cancellation = default(CancellationToken))
        {
            if (mode != "project" && mode != "engine") throw new ArgumentException("Unknown mode", nameof(mode));
            var result = new IndexBuildResult(); var watch = Stopwatch.StartNew();
            if (useWorker)
            {
                var temporary = Path.Combine(Path.GetTempPath(), "CodeAssist-" + Guid.NewGuid().ToString("N") + ".index");
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo(workerPath) { UseShellExecute=false, CreateNoWindow=true,
                            RedirectStandardInput=true, RedirectStandardError=true, WorkingDirectory=Path.GetDirectoryName(workerPath) };
                        process.Start();
                        var error = process.StandardError.ReadToEndAsync();
                        foreach (var value in new[] { mode, root, project ?? "", temporary })
                            process.StandardInput.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
                        process.StandardInput.Close();
                        try
                        {
                            while (!process.WaitForExit(100))
                            { cancellation.ThrowIfCancellationRequested(); if (watch.Elapsed > TimeSpan.FromMinutes(10)) throw new TimeoutException("Index worker timed out"); }
                            cancellation.ThrowIfCancellationRequested();
                            if (process.ExitCode != 0) throw new IOException("Index worker: " + error.GetAwaiter().GetResult());
                            if (!index.Load(temporary)) throw new IOException("Invalid worker index output");
                            result.UsedWorker=true;
                        }
                        finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } }
                    }
                }
                catch (Exception error) when (error is IOException || error is System.ComponentModel.Win32Exception || error is TimeoutException)
                { result.WorkerFailure=error.Message; }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            if (!result.UsedWorker)
            {
                cancellation.ThrowIfCancellationRequested();
                if (mode == "project") index.BuildProject(root, cancellation);
                else index.BuildEngine(root, project, cancellation);
            }
            result.ElapsedMilliseconds=watch.ElapsedMilliseconds;
            return result;
        }
    }
}
