using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using log4net;
using System.ServiceProcess;
using System.Threading;
using System.Globalization;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using exampleWS;

namespace exampleWS
{
    public class SFTPManager
    {
        public static List<Element> CheckSFTP(DateTime presentTime, List<Element> SFTP_FilesList)
        {
            List<Element> sftp_filesToProcess = new List<Element>();
            foreach (Element file in SFTP_FilesList)
            {
                file.availableFiles.Clear();
                if (file.way == "download")
                {
                    List<string> availableFiles = new List<string>();
                    bool hasTimeStamp = (file.localCommand == "ADD_TIMESTAMP");
                    string wantedFileName = file.filename;
                    if (hasTimeStamp)
                    {
                        string ext = Path.GetExtension(file.filename);
                        if (ext != "")
                        {
                            wantedFileName = file.filename.Substring(0, file.filename.Length - ext.Length) + "*";
                        }
                    }
                    DirectoryInfo di = Directory.CreateDirectory(file.localDirectoryPath);
                    List<string> localFilesList = Directory.GetFiles(file.localDirectoryPath, wantedFileName).ToList();

                    using (SftpClient client = new SftpClient(file.home, 22, file.login, file.pw))
                    {
                        try
                        {
                            client.Connect();
                            string workingDirectory = file.remoteDirectory == "" ? "." : file.remoteDirectory;
                            bool doChangeDir = (workingDirectory != ".");
                            if (doChangeDir)
                            {
                                client.ChangeDirectory(workingDirectory);
                                workingDirectory = ".";
                            }
                            List<SftpFile> listDirectory = client.ListDirectory(workingDirectory).ToList();
                            foreach (SftpFile aFile in listDirectory)
                            {
                                SftpFileAttributes fileAttributes = client.GetAttributes(aFile.Name);
                                string remoteFile = aFile.Name;
                                string remoteExt = Path.GetExtension(remoteFile);
                                if (remoteExt != "") remoteFile = remoteFile.Substring(0, remoteFile.Length - remoteExt.Length);
                                string modelFile = file.filename;
                                string modelExt = Path.GetExtension(modelFile);
                                if (modelExt != "") modelFile = modelFile.Substring(0, modelFile.Length - modelExt.Length);
                                if (remoteFile.Contains(modelFile))
                                {
                                    string pattern = fileAttributes.LastWriteTime.ToString("yyyyMMddHHmmss") + modelExt;
                                    bool alreadyDownLoaded = false;
                                    if (localFilesList.Any())
                                    {
                                        foreach (string localFile in localFilesList)
                                        {
                                            if (localFile.Contains(pattern))
                                            {
                                                alreadyDownLoaded = true;
                                            }
                                        }
                                    }
                                    if (!alreadyDownLoaded)
                                    {
                                        availableFiles.Add(aFile.Name);
                                    }
                                }
                            }
                            if (availableFiles.Any())
                            {
                                file.availableFiles.AddRange(availableFiles);
                                sftp_filesToProcess.Add(file);
                            }
                        }
                        catch (Exception er)
                        {
                            LogManager.GetLogger("SERVICE").Info("SSH.NET exception" + er.ToString());
                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }
                else if (file.way == "upload")
                {
                    string wantedFileName = file.filename;
                    bool hasFileToProcess = false;
                    bool hasTimeStamp = (file.localCommand == "REMOVE_DATESTAMP" || file.localCommand == "REMOVE_TIMESTAMP");
                    if (hasTimeStamp)
                    {
                        string ext = Path.GetExtension(file.filename);
                        if (ext != "")
                        {
                            wantedFileName = file.filename.Substring(0, file.filename.Length - ext.Length) + "*";
                        }
                    }
                    DirectoryInfo di = Directory.CreateDirectory(file.localDirectoryPath);

                    List<string> localFilesList = new List<string>();
                    try
                    {
                        localFilesList = Directory.GetFiles(file.localDirectoryPath, wantedFileName).ToList();
                        if (localFilesList != null) 
                        {
                            if (localFilesList.Any()) 
                            {
                                hasFileToProcess = true;
                            }
                        }
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        LogManager.GetLogger("ERROR").InfoFormat(ex.Message);
                        LogManager.GetLogger("ERROR").InfoFormat("Cannot process file {0} !", file.filename);
                    }
                    if (hasFileToProcess)
                    { 
                        using (SftpClient client = new SftpClient(file.home, 22, file.login, file.pw))
                        {

                            bool doProcess = true;
                            try
                            {
                                client.Connect();
                                string workingDirectory = file.remoteDirectory == "" ? "." : file.remoteDirectory;
                                bool doChangeDir = (workingDirectory != ".");
                                if (doChangeDir)
                                {
                                    client.ChangeDirectory(workingDirectory);
                                    workingDirectory = ".";
                                }
                                List<SftpFile> listDirectory = client.ListDirectory(workingDirectory).ToList();
                                foreach (SftpFile aFile in listDirectory)
                                {
                                    if (aFile.Name == file.filename && file.remoteCommand == "NO_OVERWRITE")
                                    {
                                        doProcess = false; 
                                    }
                                }
                                if (doProcess)
                                {
                                    if (file.remoteCommand == "NO_OVERWRITE")
                                    {
                                        file.availableFiles.Add(localFilesList[0]); 
                                    }
                                    else
                                    {
                                        foreach (string localFile in localFilesList)
                                        {
                                            file.availableFiles.Add(localFile);
                                        }
                                    }
                                    sftp_filesToProcess.Add(file);
                                }
                            }
                            catch (Exception er)
                            {
                                LogManager.GetLogger("SERVICE").Info("SSH.NET exception" + er.ToString());
                            }
                            finally
                            {
                                client.Disconnect();
                            }
                        }
                    }

                }
            }
            return sftp_filesToProcess;
        }

        public static void ProcessSFTP(DateTime presentTime, List<Element> sftp_filesToProcess)
        {
            string fileDateEnd = presentTime.ToString("yyyyMMdd");
            List<Archiver> toArchive = new List<Archiver>();
            foreach (Element file in sftp_filesToProcess)
            {
                if (file.way == "download") 
                {
                    DirectoryInfo di = Directory.CreateDirectory(file.localDirectoryPath);

                    using (SftpClient client = new SftpClient(file.home, 22, file.login, file.pw))
                    {
                        try
                        {
                            client.Connect();
                            string workingDirectory = file.remoteDirectory == "" ? "." : file.remoteDirectory;
                            bool doChangeDir = (workingDirectory != ".");
                            if (doChangeDir)
                            {
                                client.ChangeDirectory(workingDirectory);
                                workingDirectory = ".";
                            }
                            foreach (string filename in file.availableFiles)
                            {
                                string destFile = filename;
                                if (file.localCommand == "ADD_TIMESTAMP")
                                {
                                    string ext = Path.GetExtension(filename);
                                    if (ext != "")
                                    {
                                        destFile = filename.Substring(0, file.filename.Length - ext.Length);
                                    }
                                    if (ext == "")
                                    {
                                        string modelExt = Path.GetExtension(file.filename);
                                        if (modelExt != "") ext = modelExt;
                                    }
                                    SftpFileAttributes fileAttributes = client.GetAttributes(filename);
                                    destFile += "_" + fileAttributes.LastWriteTime.ToString("yyyyMMddHHmmss") + ext;

                                }
                                destFile = Path.Combine(file.localDirectoryPath, destFile);
                                using (Stream file2get = File.OpenWrite(destFile))
                                {
                                    client.DownloadFile(Path.GetFileName(filename), file2get);
                                }
                                LogManager.GetLogger("SERVICE").InfoFormat("File {0} downloaded", Path.GetFileName(destFile));
                            }
                        }
                        catch (Exception er)
                        {
                            LogManager.GetLogger("SERVICE").Info("SSH.NET exception" + er.ToString());

                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }
                else if (file.way == "upload") // nolimit
                {
                    DirectoryInfo di = Directory.CreateDirectory(file.localDoneDirectoryPath);

                    using (SftpClient client = new SftpClient(file.home, 22, file.login, file.pw))
                    {
                        try
                        {
                            client.Connect();
                            string workingDirectory = file.remoteDirectory == "" ? "." : file.remoteDirectory;
                            bool doChangeDir = (workingDirectory != ".");
                            if (doChangeDir)
                            {
                                client.ChangeDirectory(workingDirectory);
                                workingDirectory = ".";
                            }
                            foreach (string filepath in file.availableFiles)
                            {
                                using (Stream fileStream = new FileStream(filepath, FileMode.Open))
                                {
                                    client.BufferSize = 512 * 1024; // bypass Payload error large files
                                    client.UploadFile(fileStream, file.filename);

                                    LogManager.GetLogger("SERVICE").InfoFormat("File {0} uploaded", file.filename);

                                    if (file.localDoneDirectoryPath != null && file.localDoneDirectoryPath != "") // à archiver à la fin
                                    {
                                        string destFile = Path.Combine(file.localDoneDirectoryPath, Path.GetFileName(filepath));
                                        toArchive.Add(new Archiver
                                        {
                                            SourceFile = filepath,
                                            DestFile = destFile
                                        });

                                    }
                                }
                            }
                        }
                        catch (Exception er)
                        {
                            LogManager.GetLogger("SERVICE").Info("SSH.NET exception" + er.ToString());

                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }
            }
            if (toArchive.Any())
            {
                foreach (Archiver arch in toArchive)
                {
                    int cpt = 1;
                    string ext = "";
                    string filenameWoExt = arch.DestFile;
                    if (File.Exists(arch.DestFile))
                    {
                        ext = Path.GetExtension(arch.DestFile);
                        if (ext != "")
                        {
                            filenameWoExt = arch.DestFile.Substring(0, arch.DestFile.Length - ext.Length);
                        }
                    }
                    while (File.Exists(arch.DestFile))
                    {
                        cpt++;
                        arch.DestFile = string.Format("{0}({1}){2}", filenameWoExt, cpt, ext);
                    }
                    File.Move(arch.SourceFile, arch.DestFile);
                    LogManager.GetLogger("SERVICE").InfoFormat("Uploaded File {0} archived ", Path.GetFileName(arch.SourceFile));
                }
            }
        }
        public class Archiver
        {
            public string SourceFile { get; set; }
            public string DestFile { get; set; }
        }
    }
}
