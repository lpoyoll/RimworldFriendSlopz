using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// v0.1.19 crash breadcrumbs. This cannot catch a native Unity/driver process abort,
    /// but it preserves managed exceptions/errors and unobserved task failures in the
    /// Rimjob diagnostics folder so a client crash does not erase the useful context.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RimjobCrashCapture
    {
        private static readonly object WriteLock = new object();
        private static bool Initialised;

        static RimjobCrashCapture()
        {
            try
            {
                if (Initialised) return;
                Initialised = true;

                Application.logMessageReceivedThreaded += OnUnityLog;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                WriteBreadcrumb("Crash capture initialised.");
            }
            catch (Exception exception)
            {
                try { Log.Warning("[Rimjob] Crash capture could not initialise: " + exception.Message); }
                catch { }
            }
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            WriteBreadcrumb($"UNITY {type}: {condition}{Environment.NewLine}{stackTrace}");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            WriteBreadcrumb($"UNHANDLED IsTerminating={args.IsTerminating}: {args.ExceptionObject}");
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            WriteBreadcrumb("UNOBSERVED TASK: " + args.Exception);
        }

        public static void Mark(string stage)
        {
            WriteBreadcrumb("HANDOVER: " + stage);
        }

        private static void WriteBreadcrumb(string text)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}";
                lock (WriteLock)
                {
                    string path = RimjobClientDiagnostics.LogPath;
                    if (!string.IsNullOrWhiteSpace(path)) File.AppendAllText(path, line);

                    string folder = RimjobClientDiagnostics.LogsFolder;
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        Directory.CreateDirectory(folder);
                        File.WriteAllText(Path.Combine(folder, "LastCrashContext.txt"), line);
                    }
                }
            }
            catch
            {
                // Diagnostic capture must never destabilise the game itself.
            }
        }
    }
}
