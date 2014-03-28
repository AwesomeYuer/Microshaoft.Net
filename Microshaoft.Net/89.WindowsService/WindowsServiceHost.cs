namespace Microshaoft
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Security.Principal;
    using System.ServiceProcess;
    using Microshaoft.Win32;
    public class WindowsServiceHost : ServiceBase
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CommandLine);
            WindowsServiceHost service = new WindowsServiceHost();
            int l = 0;
            bool needFreeConsole = false;
            if (args != null)
            {
                l = args.Length;
            }
            if (l > 0)
            {
                if (args[0].ToLower() == "/console")
                {
                    needFreeConsole = true;
                    Console.Title = "Service Run as Console ...";
                    Console.WriteLine("Alloc Console ...");
                    NativeMethods.AllocConsole();
                    service.OnStart(args);
                    Console.ReadLine();
                    return;
                }
            }
            Console.WriteLine("Service");
            ServiceBase.Run(service);
            if (needFreeConsole)
            {
                Console.WriteLine("Free Console ...");
                NativeMethods.FreeConsole();
            }
        }
        //public WindowsServiceHost()
        //{
        //CanPauseAndContinue = true;
        //}
        protected override void OnStart(string[] args)
        {
            Console.WriteLine("[{0}]", string.Join(" ", args));
            Console.WriteLine("Current User Identity: {0}", WindowsIdentity.GetCurrent().Name);
            Console.WriteLine(".Net Framework version: {0}", Environment.Version.ToString());
        }
    }
    [RunInstallerAttribute(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceInstaller _serviceInstaller;
        private ServiceProcessInstaller _processInstaller;
        public ProjectInstaller()
        {
            _processInstaller = new ServiceProcessInstaller();
            _serviceInstaller = new ServiceInstaller();
            // Service will run under system account
            _processInstaller.Account = ServiceAccount.LocalSystem;
            // Service will have Start Type of Manual
            _serviceInstaller.StartType = ServiceStartMode.Manual;
            //_serviceInstaller.ServiceName = WindowsServiceHost.serviceName;
            Installers.Add(_serviceInstaller);
            Installers.Add(_processInstaller);
        }
        public override void Install(IDictionary stateSaver)
        {
            SetServiceName();
            base.Install(stateSaver);
        }
        public override void Uninstall(IDictionary savedState)
        {
            SetServiceName();
            base.Uninstall(savedState);
        }
        private void SetServiceName()
        {
            var parameters = Context.Parameters;
            var parametersKeys = parameters.Keys;
            //foreach (KeyValuePair<string, string> kvp in parameters)
            foreach (string s in parametersKeys)
            {
                var k = s.Trim().ToLower();
                if (k == "servicename")
                {
                    //var serviceName = kvp.Value;
                    var serviceName = parameters[k];
                    _serviceInstaller.ServiceName = serviceName;
                    _serviceInstaller.DisplayName = serviceName;
                    break;
                }
            }
        }
    }
}
namespace Microshaoft.Win32
{
    using System.Runtime.InteropServices;
    public class NativeMethods
    {
        /// <summary>
        /// 启动控制台
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();
        /// <summary>
        /// 释放控制台
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
    }
}