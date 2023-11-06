using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
                string currentDirectory = Directory.GetCurrentDirectory();
                string testCaseName = Path.GetFileName(Path.GetDirectoryName(source));
                string aliasName = testCaseName;
                log.Debug("Current directory: " + currentDirectory);
                log.Debug("Test Source: " + source);
                log.Debug("Test Case Name: " + testCaseName);
                
                if(source.EndsWith(".sstest"))
                {
                    testCaseName = Path.GetDirectoryName(source);
                    log.Debug("Path TC Name: " + testCaseName);
                }
                currentDirectory = currentDirectory.Replace("\\", "/");
                testCaseName = testCaseName.Replace("\\", "/");
                if (testCaseName.StartsWith(currentDirectory+"/"))
                {
                    testCaseName = testCaseName.Substring(currentDirectory.Length+1);
                }
                testCaseName = testCaseName.Trim();
                testCaseName = testCaseName.Replace("/", ".");
                log.Debug("TC Name after convertion: " + testCaseName);

                string ownerValue = "";
                string tagss = "";
                List<string> tagValues = new List<string>();

                try
                {
                    XmlDocument txml = new XmlDocument();
                    txml.Load(source);
                    XmlNode sfn = txml.SelectSingleNode("/Test/Tags");
                    
                    if (sfn != null)
                    {
                        tagss = "" + sfn.InnerText;
                        tagss = ("" + tagss).Replace(';', ',');
                    }

                    foreach (string t in tagss.Split(','))
                    {
                        tagValues.Add(t.Trim());
                    }

                    sfn = txml.SelectSingleNode("/Test/AliasName");
                    if (sfn != null)
                    {
                        aliasName = sfn.InnerText;
                        testCaseName += "." + aliasName;
                    }

                    sfn = txml.SelectSingleNode("//TestParam[@name='Owner']");
                    
                    if (sfn != null )
                    {
                        ownerValue = sfn.Attributes["defaultValue"].Value;
                    }

                }
                catch (Exception ex)
                {
                    log.Debug("Error reading tags for " + source + ": ", ex);
                }

                log.Debug("Creating TC: Name: " + aliasName+ "\nDisplayName: " + testCaseName);

                TestCase tc = new TestCase(testCaseName, new Uri(RapiseTestAdapter.ExecutorUri), source)
                {
                    DisplayName = aliasName,
                    CodeFilePath = source,
                };

                log.Debug("Created Test Case: " + tc);

                try
                {
                    tc.Traits.Add(new Trait(RapiseTestExecutor.RapiseTestCategoryProperty.Label, tagss));
                    tc.SetPropertyValue(RapiseTestExecutor.RapiseTestCategoryProperty, tagValues.ToArray());
                    tc.Traits.Add(new Trait(RapiseTestExecutor.RapiseTestOwnerProperty.Label, ownerValue));
                    tc.SetPropertyValue(RapiseTestExecutor.RapiseTestOwnerProperty, ownerValue);

                }
                catch (Exception ex)
                {
                    log.Debug("Error assigning attributes "+source+": ", ex);
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
