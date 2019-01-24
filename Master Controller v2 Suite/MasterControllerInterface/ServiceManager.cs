using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Security.Permissions;

namespace MasterControllerInterface
{
    public static class ServiceManager
    {
        private static readonly string[] service_names = new string[]{ "MCIv2DeviceServer"};

        private static string[] GetInstalledServices()
        {
            var all_services = ServiceController.GetServices().Select(x => x.ServiceName);
            return all_services.Where(x => service_names.Contains(x)).ToArray();
        }

        public static bool isDeviceServerServiceInstalled()
        {
            var services = GetInstalledServices();
            return services.Contains(service_names[0]);
        }

        public static bool isDeviceServerServiceRunning()
        {
            var service = ServiceController.GetServices().Where(x => x.ServiceName == service_names[0]).ToArray();

            if (service.Count() == 0)
                return false;
            else
                return service[0].Status.Equals(ServiceControllerStatus.Running);
        }

        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static Task StartDeviceServerService()
        {
            return Task.Run(() =>
            { 
                var service = ServiceController.GetServices().Where(x => x.ServiceName == service_names[0]).ToArray();

                if (service.Count() == 0)
                    return;
                else
                    service[0].Start();
            });
        }

        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static Task StopDeviceServerService()
        {
            return Task.Run(() =>
            {
                var service = ServiceController.GetServices().Where(x => x.ServiceName == service_names[0]).ToArray();

                if (service.Count() == 0)
                    return;
                else
                    service[0].Stop();
            });
        }

        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static Task UninstallDeviceServerService()
        {
            return Task.Run(() => { ManagedInstallerClass.InstallHelper(new string[] { "/u", "DeviceServer.exe" }); });
        }

        //[PrincipalPermission(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
        public static Task InstallDeviceServerService()
        {
            return Task.Run(() => { ManagedInstallerClass.InstallHelper(new string[] { "DeviceServer.exe" }); });
        }
    }
}
