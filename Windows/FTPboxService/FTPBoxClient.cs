using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FTPboxService
{
  public class FTPBoxClient
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void RunClient(string[] args, string param)
        {
            if (!IsServerRunning)
            {
                logger.Error("FTPbox must be running to run the client");
                return;
            }

            var pipeClient = new NamedPipeClientStream(".", "FTPbox Server", PipeDirection.InOut, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Impersonation);

            logger.Debug("Connecting client...");
            pipeClient.Connect();

            var ss = new StreamString(pipeClient);
            if (ss.ReadString() == "ftpbox")
            {
                var p = CombineParameters(args, param);
                ss.WriteString(p);
                logger.Debug(ss.ReadString());
            }
            else
            {
                logger.Debug ("Server couldnt be verified.");
            }
            pipeClient.Close();
            Thread.Sleep(4000);

            Process.GetCurrentProcess().Kill();
        }


        private static bool IsServerRunning
        {
            get
            {
                var processes = Process.GetProcesses();
                return processes.Any(p => p.ProcessName == "FTPbox" && p.Id != Process.GetCurrentProcess().Id);
            }
        }

        private static string CombineParameters(IEnumerable<string> args, string param)
        {
            var r = param + "\"";

            foreach (var s in args)
                r += string.Format("{0}\"", s);

            r = r.Substring(0, r.Length - 1);

            return r;
        }
    }
}
