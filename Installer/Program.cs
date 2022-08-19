using System;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Installer
{
    internal class Program
    {
        private static bool IsElevated()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RunCommandWriteOutput(string program, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = program;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                // Synchronously read the standard output of the spawned process.
                StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();

                // Write the redirected output to this application's window.
                Console.WriteLine(output);

                process.WaitForExit();
            }
        }

        private static bool CreateDirectWithFullControl(string path)
        {
            FileSystemAccessRule directoryRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
            DirectorySecurity directorySecurity = new DirectorySecurity();
            directorySecurity.AddAccessRule(directoryRule);


            if (Directory.Exists(path))
            {
                return false;
            }

            Directory.CreateDirectory(path, directorySecurity);
            return true;
        }

        static void Main(string[] args)
        {
            // Verify elevation
            if (!IsElevated())
            {
                Console.Error.WriteLine("[X] The installer must be launched in an elevated context");
                return;
            }

            // Create folder structure and grant Everyone full control
            if (CreateDirectWithFullControl(@"C:\Bad Windows Service"))
            {
                Console.WriteLine("[+] Created folder {0}", @"C:\Bad Windows Service");
            }
            else
            {
                Console.WriteLine("[*] Folder {0} already exists", @"C:\Bad Windows Service");
            }
            if (CreateDirectWithFullControl(@"C:\Bad Windows Service\Service Executable"))
            {
                Console.WriteLine("[+] Created folder {0}", @"C:\Bad Windows Service\Service Executable");
            }
            else
            {
                Console.WriteLine("[*] Folder {0} already exists", @"C:\Bad Windows Service\Service Executable");
            }

            // Copy executable to destination
            if (File.Exists("BadWindowsService.exe"))
            {
                Console.WriteLine("[+] Located BadWindowsService.exe in current working directory");
                File.Copy("BadWindowsService.exe", @"C:\Bad Windows Service\Service Executable\BadWindowsService.exe", true);
                Console.WriteLine(@"[+] Copied BadWindowsService.exe to C:\Bad Windows Service\Service Executable\BadWindowsService.exe");

                // Grant Everyone full control
                FileSystemAccessRule fileRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow);
                FileSecurity fileSecurity = new FileSecurity();
                fileSecurity.AddAccessRule(fileRule);
                File.SetAccessControl(@"C:\Bad Windows Service\Service Executable\BadWindowsService.exe", fileSecurity);
                Console.WriteLine("[+] Granted Everyone full control");
            }
            else
            {
                Console.Error.WriteLine("[X] Service executable not found in current working directory");
            }

            //Run installer
            string InstallUtilPath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory() + "InstallUtil.exe";
            if (!File.Exists(InstallUtilPath))
            {
                Console.Error.WriteLine("[X] Could not locate InstallUtil.exe");
                return;
            }
            try
            {
                RunCommandWriteOutput(InstallUtilPath, "\"C:\\Bad Windows Service\\Service Executable\\BadWindowsService.exe\"");
                Console.WriteLine("[+] Service installed");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[X] Service installation failed: {0}", ex.Message);
                return;
            }

            // Modify service binpath to be an unquoted path
            try
            {
                RunCommandWriteOutput("sc.exe", "config BadWindowsService binpath= \"C:\\Bad Windows Service\\Service Executable\\BadWindowsService.exe\"");
                Console.WriteLine("[+] Service binpath is now unquoted");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[!] Service binpath modification failed: {0}", ex.Message);
                return;
            }

            // Modify service permissions - grant Everyone full control
            try
            {
                RunCommandWriteOutput("sc.exe", "sdset BadWindowsService \"D:PAI(A;;FA;;;WD)\"");
                Console.WriteLine("[+] Granted Everyone full control on the service");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[!] Service permissions modification failed: {0}", ex.Message);
                return;
            }

            // Grant Everyone full control over the service's Registry key
            try
            {
                RegistrySecurity rs = new RegistrySecurity();
                rs.AddAccessRule(new RegistryAccessRule("Everyone",
                RegistryRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BadWindowsService", true);
                rk.SetAccessControl(rs);
                Console.WriteLine("[+] Granted Everyone full control on the service's Registry key");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[!] Service registry permissions modification failed: {0}", ex.Message);
                return;
            }

            // Start the service
            ServiceController service = new ServiceController("BadWindowsService");
            service.Start();
            Thread.Sleep(3000);
            if (service.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine("[+] Service started!"); 
            }
            else
            {
                Console.Error.WriteLine("[X] Service failed to start"); 
            }


        }
    }
}
