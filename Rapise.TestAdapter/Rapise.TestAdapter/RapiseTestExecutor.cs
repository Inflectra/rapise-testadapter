using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;

namespace Rapise.TestAdapter
{
    [ExtensionUri(RapiseTestAdapter.ExecutorUri)]
    public class RapiseTestExecutor : ITestExecutor
    {
        private static log4net.ILog log = RapiseTestAdapter.InitLogging();

        private IRapiseRunner runner;

        public RapiseTestExecutor()
        {
            runner = new Rapise();
        }

        public RapiseTestExecutor(IRapiseRunner r)
        {
            this.runner = r;
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            log.Debug("RunTests from Sources");
            List<TestCase> tests = RapiseTestDiscoverer.GetTests(sources, null, null, null);
            RunTests(tests, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // more on filtering
            // https://github.com/nunit/nunit3-vs-adapter/blob/master/src/NUnitTestAdapter/VsTestFilter.cs
            List<string> supportedProperties = new List<string>();
            supportedProperties.Add("FullyQualifiedName");
            ITestCaseFilterExpression fe = runContext.GetTestCaseFilter(supportedProperties, PropertyProvider);

            log.Debug("Run settings:\n" + runContext.RunSettings.SettingsXml);

            log.Debug("RunTests from Test Cases");
            foreach (TestCase tc in tests)
            {
                if (fe == null || fe.MatchTestCase(tc, p => PropertyValueProvider(tc, p)))
                {
                    log.Debug("Run test case: " + tc.FullyQualifiedName + " / " + tc.Id);
                    frameworkHandle.RecordStart(tc);
                    DateTime startTime = DateTime.Now;
                    TestResult tr = runner.RunTest(tc, runContext);
                    DateTime endTime = DateTime.Now;
                    tr.Duration = endTime - startTime;
                    frameworkHandle.RecordEnd(tc, tr.Outcome);
                    frameworkHandle.RecordResult(tr);
                }
                else
                {
                    log.Debug("Test case filtered out: " + tc.FullyQualifiedName + " / " + tc.Id);
                }
            }
        }
        public static TestProperty PropertyProvider(string propertyName)
        {
            return TestCaseProperties.FullyQualifiedName;
        }

        public static object PropertyValueProvider(TestCase currentTest, string propertyName)
        {
            return currentTest.FullyQualifiedName;
        }
    }
}
