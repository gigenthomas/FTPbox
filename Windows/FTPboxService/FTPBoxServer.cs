using FTPboxLib;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FTPboxService
{

    public class FTPBoxServer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static AccountController Account;
        private DateTime dtLastContextAction = DateTime.Now;


        public FTPBoxServer()
        {
            logger.Debug(" In constructor of FTPBoxServer");
            Settings.Load();
            Account = new AccountController();
            Account = Settings.DefaultProfile;

          
            Account.LoadLocalFolders();
            Account.FolderWatcher.Setup();
        } 

        public void RunServer()
        {
            var tServer = new Thread(RunServerThread);
            tServer.SetApartmentState(ApartmentState.STA);
            tServer.Start();
            logger.Debug("Starting Server");
        }

        public void RunClient()
        {
            logger.Debug(" Starting Client ...");
            new Thread(() =>
            {
                // ...check local folder for changes
                var cpath = Account.GetCommonPath(Account.Paths.Local, true);
               Account.SyncQueue.Add(new SyncQueueItem(Account)
                {
                    Item = new ClientItem
                    {
                        FullPath = Account.Paths.Local,
                        Name = Common._name(cpath),
                        Type = ClientItemType.Folder,
                        Size = 0x0,
                        LastWriteTime = DateTime.MinValue
                    },
                    ActionType = ChangeAction.changed,
                    SyncTo = SyncTo.Remote
                });

              //  Account.SyncQueue.StartTimer(Account);

            }).Start();
        }
 
        private void RunServerThread()
        {
            var i = 1;
            logger.Debug("Started the named-pipe server, waiting for clients (if any)");

            var server = new Thread(ServerThread);
            server.SetApartmentState(ApartmentState.STA);
            server.Start();

            Thread.Sleep(250);

            while (i > 0)
                if (server != null)
                    if (server.Join(250))
                    {
                        logger.Debug("named-pipe server thread finished");
                        server = null;
                        i--;
                    }
            logger.Debug("named-pipe server thread exiting...");

            RunServer();
        }

        public void ServerThread()
        {
            var pipeServer = new NamedPipeServerStream("FTPbox Server", PipeDirection.InOut, 5);
            var threadID = Thread.CurrentThread.ManagedThreadId;

            pipeServer.WaitForConnection();

            logger.Debug("Client connected, id: {0}", threadID);

            try
            {
                var ss = new StreamString(pipeServer);

                ss.WriteString("ftpbox");
                var args = ss.ReadString();

                var fReader = new ReadMessageSent(ss, "All done!");
/*
                logger.Debug(String.Format("named-pipe server thread exiting...
                Reading file: \n {0} \non thread [{1}] as user {2}.", args, threadID,
                    pipeServer.GetImpersonationUserName()));
                    */

                CheckClientArgs(ReadCombinedParameters(args).ToArray());

                pipeServer.RunAsClient(fReader.Start);
            }
            catch (IOException e)
            {
                Common.LogError(e);
            }
            pipeServer.Close();
        }


        private static List<string> ReadCombinedParameters(string args)
        {
            var r = new List<string>(args.Split('"'));
            while (r.Contains(""))
                r.Remove("");

            return r;
        }


        private void CheckClientArgs(IEnumerable<string> args)
        {
            var list = new List<string>(args);
            var param = list[0];
            list.RemoveAt(0);

            switch (param)
            {
                case "copy":
                    CopyArgLinks(list.ToArray());
                    break;
                case "sync":
                    SyncArgItems(list.ToArray());
                    break;
                case "open":
                    OpenArgItemsInBrowser(list.ToArray());
                    break;
                case "move":
                    MoveArgItems(list.ToArray());
                    break;
            }
        }
        private void CopyArgLinks(string[] args)
        {
            string c = null;
            var i = 0;
            foreach (var s in args)
            {
                if (!s.StartsWith(Account.Paths.Local))
                {
                    logger.Error("You cannot use this for files that are not inside the FTPbox folder.");
                        
                    continue;
                }

                i++;
                //if (File.Exists(s))
                c += Account.GetHttpLink(s);
                if (i < args.Count())
                    c += Environment.NewLine;
            }

            if (c == null) return;

            try
            {
                /*
                if ((DateTime.Now - dtLastContextAction).TotalSeconds < 2)
                    Clipboard.SetText(Clipboard.GetText() + Environment.NewLine + c);
                else
                    Clipboard.SetText(c);
                    */
                //SetTray(null, new FTPboxLib.TrayTextNotificationArgs { MessageType = FTPboxLib.MessageType.LinkCopied });
            }
            catch (Exception e)
            {
                Common.LogError(e);
            }
            dtLastContextAction = DateTime.Now;
        }

        /// <summary>
        ///     Called when 'Synchronize this file/folder' is clicked from the context menus
        /// </summary>
        /// <param name="args"></param>
        private static void SyncArgItems(IEnumerable<string> args)
        {
            foreach (var s in args)
            {
                Log.Write(l.Info, "Syncing local item: {0}", s);
                if (!s.StartsWith(Account.Paths.Local))
                {
                    logger.Error("You cannot use this for files that are not inside the FTPbox folder.");
                      
                    continue;
                }
                var cpath = Account.GetCommonPath(s, true);
                var exists = Account.Client.Exists(cpath);

                if (Common.PathIsFile(s) && File.Exists(s))
                {
                    Account.SyncQueue.Add(new SyncQueueItem(Account)
                    {
                        Item = new ClientItem
                        {
                            FullPath = s,
                            Name = Common._name(cpath),
                            Type = ClientItemType.File,
                            Size = exists ? Account.Client.SizeOf(cpath) : new FileInfo(s).Length,
                            LastWriteTime = exists ? Account.Client.GetLwtOf(cpath) : File.GetLastWriteTime(s)
                        },
                        ActionType = ChangeAction.changed,
                        SyncTo = exists ? SyncTo.Local : SyncTo.Remote
                    });
                }
                else if (!Common.PathIsFile(s) && Directory.Exists(s))
                {
                    var di = new DirectoryInfo(s);
                    Account.SyncQueue.Add(new SyncQueueItem(Account)
                    {
                        Item = new ClientItem
                        {
                            FullPath = di.FullName,
                            Name = di.Name,
                            Type = ClientItemType.Folder,
                            Size = 0x0,
                            LastWriteTime = DateTime.MinValue
                        },
                        ActionType = ChangeAction.changed,
                        SyncTo = exists ? SyncTo.Local : SyncTo.Remote,
                        SkipNotification = true
                    });
                }
            }
        }

        /// <summary>
        ///     Called when 'Open in browser' is clicked from the context menus
        /// </summary>
        /// <param name="args"></param>
        private void OpenArgItemsInBrowser(IEnumerable<string> args)
        {
            foreach (var s in args)
            {
                if (!s.StartsWith(Account.Paths.Local))
                {
                    logger.Error("You cannot use this for files that are not inside the FTPbox folder.");
                   
                    continue;
                }

                var link = Account.GetHttpLink(s);
                try
                {
                    Process.Start(link);
                }
                catch (Exception e)
                {
                    Common.LogError(e);
                }
            }

            dtLastContextAction = DateTime.Now;
        }

        /// <summary>
        ///     Called when 'Move to FTPbox folder' is clicked from the context menus
        /// </summary>
        /// <param name="args"></param>
        private static void MoveArgItems(IEnumerable<string> args)
        {
            foreach (var s in args)
            {
                if (!s.StartsWith(Account.Paths.Local))
                {
                    if (File.Exists(s))
                    {
                        var fi = new FileInfo(s);
                        File.Copy(s, Path.Combine(Account.Paths.Local, fi.Name));
                    }
                    else if (Directory.Exists(s))
                    {
                        foreach (var dir in Directory.GetDirectories(s, "*", SearchOption.AllDirectories))
                        {
                            var name = dir.Substring(s.Length);
                            Directory.CreateDirectory(Path.Combine(Account.Paths.Local, name));
                        }
                        foreach (var file in Directory.GetFiles(s, "*", SearchOption.AllDirectories))
                        {
                            var name = file.Substring(s.Length);
                            File.Copy(file, Path.Combine(Account.Paths.Local, name));
                        }
                    }
                }
            }
        }
    }

}
