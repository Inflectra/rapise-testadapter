using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rapise.TestAdapter
{
    public class RapiseTestAdapter
    {
        public const string ExecutorUri = "executor://rapise/v1";

        private static log4net.ILog log = null;

        public static log4net.ILog InitLogging()
        {
            if (log == null)
            {
                string rootPath = Environment.ExpandEnvironmentVariables("%BUILD_ARTIFACTSTAGINGDIRECTORY%");
                if (rootPath.StartsWith("%"))
                {
                    rootPath = @"c:\ProgramData\Inflectra\Rapise\Logs";
                }
                log = LogUtil.InitLogging(Path.Combine(rootPath, "rapise_testadapter.log"));
            }
            return log;
        }
    }
}
