using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using System.Globalization;
using log4net;

namespace exampleWS
{
    public class SchedulerServiceManager
    {
        public static List<Element> SFTP_FilesList = null;

        public Timer timerManager;
        public ManualResetEvent AbortEvent;

        const int defaultSftpLoopTimerInMinutes = 15;

        public static int sftpLoopTimerInMinutes;

        private static int dummyInt = 0;
        private static string dummyString = "";

        public static TimeSpan CheckInterval;

        private ManualResetEvent abortEvent = new ManualResetEvent(false);

        public SchedulerServiceManager()
        {

            dummyString = ConfigurationManager.AppSettings["loopTimerInMinutes"];
            sftpLoopTimerInMinutes = int.TryParse(dummyString, out dummyInt) ? dummyInt : defaultSftpLoopTimerInMinutes;

            CheckInterval = TimeSpan.FromMinutes(sftpLoopTimerInMinutes);


            ConfigSettings configSettingMng = new ConfigSettings();
            SFTP_FilesList = configSettingMng.ServerElements.Where(x => x.protocol == "SFTP").ToList();
            if (SFTP_FilesList.Any())
            {
                foreach (Element sftpFile in SFTP_FilesList)
                {
                    sftpFile.availableFiles = new List<string>();
                }
            }

        }

        public void StartService()
        {
            bool firstCheck = true;
            while (firstCheck || this.abortEvent.WaitOne(SchedulerServiceManager.CheckInterval) == false) 
            {
                firstCheck = false;
                this.Execute();
            }
        }

        public void StopService()
        {
            this.abortEvent.Set();
            LogManager.GetLogger("SERVICE").Info("SFTP service stopped");
        }

        public void StopConsole()
        {
            LogManager.GetLogger("SERVICE").Info("SFTP service stopped");
            Environment.Exit(0);
        }

        public void Execute()
        {
            try
            {
                DateTime presentTime;
                string theTime = "";

                SFTPManager manager = new SFTPManager();
                if (!SFTP_FilesList.Any())
                {
                    LogManager.GetLogger("SERVICE").InfoFormat(" => SFTP : No sftp file to process in config file !");
                }
                else
                {
                    LogManager.GetLogger("SERVICE").Info(" => SFTP : Checking available files ");
                    presentTime = DateTime.Now;
                    theTime = presentTime.ToString("MM/dd/yyyy HH:mm:ss");
                    if (SFTP_FilesList.Any())
                    {
                        LogManager.GetLogger("SERVICE").InfoFormat("SFTP :Checking sftp files to process");
                        List<Element> sftp_filesToProcess = SFTPManager.CheckSFTP(presentTime, SFTP_FilesList);

                        int nrFiles = 0;

                        if (sftp_filesToProcess != null && sftp_filesToProcess.Any())
                        {
                            foreach (Element file in sftp_filesToProcess)
                            {
                                nrFiles += file.availableFiles.Count;
                            }
                        }
                        if (nrFiles > 0)
                        {
                            presentTime = DateTime.Now;
                            LogManager.GetLogger("SERVICE").InfoFormat("SFTP : {0} sftp file(s) to process", nrFiles);
                            SFTPManager.ProcessSFTP(presentTime, sftp_filesToProcess);
                        }
                        else
                        {
                            LogManager.GetLogger("SERVICE").InfoFormat("No sftp file to process");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogManager.GetLogger("SERVICE").InfoFormat("SFTP : Fatal error : {0}", e.ToString());

            }
        }

    }
}
