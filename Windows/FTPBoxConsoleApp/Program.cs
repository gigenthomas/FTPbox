using FTPboxService;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPBoxConsoleApp
{
   public class Program
    {
        static void Main(string[] args)
        {

              FTPBoxServer server = new FTPBoxServer();
              server.RunServer();
              server.RunClient();
        }
    }
}
