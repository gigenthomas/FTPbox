using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FTPboxService
{
    public partial class FTPboxService : ServiceBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger _eventLog = LogManager.GetLogger(Constants.EVENT_LOGGER);
        public FTPboxService()
        {
            try {

                InitializeComponent();
              
            }
            catch ( Exception e )
            {
                logger.Error(e, "Error initalizing service");
            }
        }

        protected override void OnStart(string[] args)
        {
            try {
                FTPBoxServer server = new FTPBoxServer();
                server.RunServer();
                server.RunClient();
                logger.Debug($"Started Service FTPBoxService ");
                _eventLog.Info($"Started Service FTPBoxService ");
            }
            catch (Exception e)
            {
                logger.Error(e, "Error Starting FTPBoxService service");
            }
        }

        protected override void OnStop()
        {
            _eventLog.Info($"Stopped Service FTPBoxService ");
        }
    }
}
