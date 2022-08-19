using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;


namespace BadWindowsService
{
    public partial class BadWindowsService : ServiceBase
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        public static Thread thread1;
        public static Thread thread2;
        public static string newPath;

        public BadWindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string value = Environment.GetEnvironmentVariable("PATH");
            newPath = @"C:\Temp\;" + value;

            thread1 = new Thread(Restart);
            thread1.Start();

            thread2 = new Thread(DoBadThings);
            thread2.Start();
        }

        private static void Restart()
        {
            Thread.Sleep(60000);
            ServiceController service = new ServiceController("BadWindowsService");
            service.Stop();
            Thread.Sleep(5000);
            service.Start();
            thread2.Abort();
            thread1.Abort();
        }

        protected override void OnStop()
        {
        }

        private static void DoBadThings()
        {
            while (true)
            {
                // Add C:\Temp to the top of the PATH environment variable
                var value = Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", newPath);

                // Set the current directory
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); // @"C:\Bad Windows Service\Service Executable\");

                // Load DLL
                try
                {
                    IntPtr handle = LoadLibrary("BadDll.dll");
                }
                catch { };

                // Run executable
                try
                {
                    System.Diagnostics.Process.Start("cmd.exe", "/c exit");
                }
                catch { };

                Thread.Sleep(10000);
            }
        }
    }
}
