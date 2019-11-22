using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MasterServer
{
    public class UnityManager
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool SendMessageCallback(IntPtr hWnd, uint Msg, UIntPtr wParam,
            IntPtr lParam, SendMessageDelegate lpCallBack, UIntPtr dwData);

        internal delegate int WindowEnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        internal static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc func, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_ACTIVATE = 0x0006;
        private readonly IntPtr WA_ACTIVE = new IntPtr(1);
        private readonly IntPtr WA_INACTIVE = new IntPtr(0);

        delegate void SendMessageDelegate(IntPtr hWnd, uint uMsg, UIntPtr dwData, IntPtr lResult);

        UnityInstance[] unityInstances;
        bool[] cleanServers;
        int serverCount;
        string unityPath;
        ushort portStart;

        public UnityManager()
        {

        }

        private void ActivateUnityWindow(UnityInstance ui)
        {
            SendMessage(ui.handle, WM_ACTIVATE, WA_ACTIVE, IntPtr.Zero);
        }

        private void DeactivateUnityWindow(UnityInstance ui)
        {
            SendMessage(ui.handle, WM_ACTIVATE, WA_INACTIVE, IntPtr.Zero);
        }

        public int EnumerateStoredWindows()
        {
            foreach (UnityInstance ui in unityInstances)
            {
                ActivateUnityWindow(ui);
            }
            return 0;
        }

        int GetCleanServer(out UnityInstance instance)
        {
            for(int i = 0; i < cleanServers.Length; i++)
            {
                if(cleanServers[i])
                {
                    cleanServers[i] = false;
                    instance = unityInstances[i];
                    return 0;
                }
            }
            instance = new UnityInstance();
            return -1;//tells them no instance available
        }

        public int StartSpawning(int serverCount, string unityPath, ushort port)
        {
            unityInstances = new UnityInstance[serverCount];
            cleanServers = new bool[serverCount];
            portStart = port;
            this.unityPath = unityPath;
            this.serverCount = serverCount;
            for (int i = 0; i < serverCount; i++)
            {
                int spacer =((i+1) * 10);
                ushort portToUse = (ushort)((int)port + spacer);
                unityInstances[i] = RunUnityBuild(unityPath, portToUse);
                cleanServers[i] = true;
                Console.WriteLine("[UnitySpawner] starting instance " + i + " on port " + portToUse);
            }
            return 0;
        }

        public int UpdateServers()
        {
            for (int i = 0; i < serverCount; i++)
            {
                if(unityInstances[i].process.HasExited)
                {
                    int spacer = ((i + 1) * 10);
                    ushort portToUse = (ushort)((int)portStart + spacer);
                    unityInstances[i] = RunUnityBuild(unityPath, portToUse);
                    cleanServers[i] = true;
                    Console.WriteLine("[UnitySpawner] resetting instance " + i);
                }
            }

            return 0;
        }

        public UnityInstance RunUnityBuild(String path, int port)
        {
            IntPtr unityHWND = IntPtr.Zero;
            Process processToUse;
            processToUse = new Process();
            try
            {
                processToUse.StartInfo.FileName = path;// + "\\Torque Burnout.exe";
                processToUse.StartInfo.Arguments = "-portUseage " + port.ToString();
                processToUse.StartInfo.UseShellExecute = true;
                processToUse.StartInfo.CreateNoWindow = true;

                processToUse.Start();

                processToUse.WaitForInputIdle();
                // Doesn't work for some reason ?!
                unityHWND = processToUse.MainWindowHandle;
                //EnumChildWindows(desiredPanel.Handle, WindowEnum, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UnityManager] error starting new unity instance," + ex.InnerException);
            }

            UnityInstance tracker;
            tracker.process = processToUse;
            tracker.handle = unityHWND;

            return tracker;
        }

        public void KillUnityInstances()
        {
            Console.WriteLine("[UnitySpawner] killing unity instances");
            foreach (UnityInstance ui in unityInstances)
            {
                while (!ui.process.HasExited)
                {
                    ui.process.Kill();
                }
            }
        }

        public struct UnityInstance
        {
            public Process process;
            public IntPtr handle;
        }
    }
}
