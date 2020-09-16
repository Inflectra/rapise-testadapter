using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rapise.TestAdapter
{
    [FileExtension(".sstest")]
    [DefaultExecutorUri(RapiseTestAdapter.ExecutorUri)]
    public class RapiseTestDiscoverer : ITestDiscoverer
    {
        private static log4net.ILog log = RapiseTestAdapter.InitLogging();

        public static List<TestCase> GetTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            List<TestCase> tests = new List<TestCase>();
            foreach (string source in sources)
            {
                log.Debug("Test Source: " + source);
                string testCaseName = Path.GetFileNameWithoutExtension(source);
                TestCase tc = new TestCase(testCaseName, new Uri(RapiseTestAdapter.ExecutorUri), source);
                if (discoverySink != null)
                {
                    discoverySink.SendTestCase(tc);
                }
                tests.Add(tc);
            }
            return tests;
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            log.Debug("DiscoverTests");
            GetTests(sources, discoveryContext, logger, discoverySink);
        }
    }
}
