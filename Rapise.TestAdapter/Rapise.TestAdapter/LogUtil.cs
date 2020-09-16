using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rapise.TestAdapter
{
    public static class LogUtil
    {
        public static bool ConsoleLogging { get; set; }

        public static log4net.ILog InitLogging(string logfilename)
        {
            return InitLogging(logfilename, true);
        }

        public static log4net.ILog InitLogging(string logfilename, bool verbose)
        {
            ConsoleLogging = verbose;
            logfilename = System.Environment.ExpandEnvironmentVariables(logfilename);
            if (verbose)
            {
                Console.WriteLine("Initializing logfile: {0}", logfilename);
            }
            log4net.Appender.ForwardingAppender appender = new log4net.Appender.ForwardingAppender();
            log4net.Appender.RollingFileAppender rfa = new log4net.Appender.RollingFileAppender
            {
                AppendToFile = true,
                LockingModel = new log4net.Appender.RollingFileAppender.MinimalLock(),
                Layout = new log4net.Layout.PatternLayout("%date\t%message%newline"),
                File = logfilename,
                MaxSizeRollBackups = 10,
                MaxFileSize = 10 * 1024 * 1024,
                RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Size,
                DatePattern = "yyyyMMdd"
            };
            rfa.ActivateOptions();
            appender.AddAppender(rfa);
            if (verbose)
            {
                appender.AddAppender(
                    new log4net.Appender.ConsoleAppender
                    {
                        Layout = new log4net.Layout.PatternLayout("%message%newline")
                    }
                    );
            }

            log4net.Config.BasicConfigurator.Configure(appender);
            return log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
    }
}
