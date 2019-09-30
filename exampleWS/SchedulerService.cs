using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using System.Globalization;

namespace exampleWS
{
    public partial class SchedulerService : ServiceBase
    {
        private SchedulerServiceManager SFTPService;
        private Thread SFTPServiceThread;

        public SchedulerService()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            this.StartService();
        }

        protected override void OnStop()
        {
            this.StopService();
        }

        public void StartService()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            log4net.Util.SystemInfo.NullText = string.Empty;
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            CultureInfo culture = new CultureInfo("fr-FR");
            LogManager.GetLogger("SERVICE").Info("Service started");

            this.SFTPService = new SchedulerServiceManager();
            this.SFTPServiceThread = new Thread(() => { SFTPService.StartService(); });
            this.SFTPServiceThread.Start();
        }

        public void StopService()
        {
            if (SFTPService != null && this.SFTPServiceThread != null)
            {
                this.SFTPService.StopService();
                this.SFTPServiceThread.Join();
            }

        }
    }
}
