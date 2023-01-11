using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
                string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                log.Debug("Current directory: " + currentDirectory);
                log.Debug("Test Source: " + source);
                string testCaseName = Path.GetFileNameWithoutExtension(source);
                TestCase tc = new TestCase(testCaseName, new Uri(RapiseTestAdapter.ExecutorUri), source);
                try
                {
                    XmlDocument txml = new XmlDocument();
                    txml.Load(source);
                    XmlNode sfn = txml.SelectSingleNode("/Test/Tags");
                    string tagss = "";
                    if (sfn != null)
                    {
                        tagss = "" + sfn.InnerText;
                        tagss = ("" + tagss).Replace(';', ',');
                    }
                    List<string> tagValues = new List<string>();
                    foreach (string t in tagss.Split(','))
                    {
                        tagValues.Add(t.Trim());
                    }
                    tc.Traits.Add(new Trait(RapiseTestExecutor.RapiseTestCategoryProperty.Label, tagss));
                    tc.SetPropertyValue(RapiseTestExecutor.RapiseTestCategoryProperty, tagValues.ToArray());

                }
                catch (Exception ex)
                {
                    log.Debug("Error reading tags for " + source + ": ", ex);
                }

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
