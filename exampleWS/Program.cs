using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace exampleWS
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                SchedulerService service = new SchedulerService();
                service.StartService();
            }
            else
            {
                using (var service = new SchedulerService())
                {
                    ServiceBase.Run(service);
                }
            }
        }
    }
}
