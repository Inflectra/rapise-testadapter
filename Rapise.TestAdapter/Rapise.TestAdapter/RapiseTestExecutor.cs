using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;

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
            supportedProperties.Add("TestCategory");
            ITestCaseFilterExpression fe = runContext.GetTestCaseFilter(supportedProperties, PropertyProvider);

            if (runContext.RunSettings != null)
            {
                log.Debug("Run settings:\n" + runContext.RunSettings.SettingsXml);
            }
            else 
            {
                log.Debug("No .runsettings provided");
            }

            log.Debug("RunTests from Test Cases");
            foreach (TestCase tc in tests)
            {
                string[] cats = tc.GetPropertyValue(RapiseTestCategoryProperty) as string[];
                string catss = cats != null ? string.Join(",", cats) : "";

                if( (","+catss+",").Contains(",disabled," ) )
                {
                    log.Debug("Test case disabled: " + tc.FullyQualifiedName + " / " + tc.Id + " / " + catss);
                    TestResult tr = new TestResult(tc);
                    tr.Outcome = TestOutcome.None;
                    frameworkHandle.RecordResult(tr);
                }
                else if (fe == null || fe.MatchTestCase(tc, p => PropertyValueProvider(tc, p)))
                {
                    log.Debug("Run test case: " + tc.FullyQualifiedName + " / " + tc.Id + " / "+cats);
                    Console.WriteLine("Starting: " + tc.DisplayName + " from " + tc.Source);
                    frameworkHandle.RecordStart(tc);
                    DateTime startTime = DateTime.Now;
                    TestResult tr = runner.RunTest(tc, runContext);
                    tc.Traits.Add(new Trait(RapiseTestExecutor.RapiseTestCategoryProperty.Label, catss));
                    tr.SetPropertyValue(RapiseTestCategoryProperty, cats);
                    DateTime endTime = DateTime.Now;
                    tr.Duration = endTime - startTime;
                    frameworkHandle.RecordEnd(tc, tr.Outcome);
                    frameworkHandle.RecordResult(tr);
                }
                else
                {
                    log.Debug("Test case filtered out: " + tc.FullyQualifiedName + " / " + tc.Id + " / " + catss);
                    TestResult tr = new TestResult(tc);
                    tr.Outcome = TestOutcome.Skipped;
                    frameworkHandle.RecordResult(tr);
                }
            }
        }

        public static readonly TestProperty RapiseTestCategoryProperty = TestProperty.Register(
            id: "Rapise.TestCategory",
            label: "TestCategory",
            valueType: typeof(string[]),
            TestPropertyAttributes.Hidden
        #pragma warning disable CS0618 // This is the only way to fix https://github.com/nunit/nunit3-vs-adapter/issues/310, and MSTest also depends on this.
                        | TestPropertyAttributes.Trait,
        #pragma warning restore CS0618
                    owner: typeof(TestCase));

        public static readonly TestProperty RapiseTestOwnerProperty = TestProperty.Register(
            id: "Rapise.TestOwner",
            label: "Owner",
            valueType: typeof(string),
            TestPropertyAttributes.Hidden
        #pragma warning disable CS0618 // This is the only way to fix https://github.com/nunit/nunit3-vs-adapter/issues/310, and MSTest also depends on this.
                        | TestPropertyAttributes.Trait,
        #pragma warning restore CS0618
                    owner: typeof(TestCase));

        public static TestProperty PropertyProvider(string propertyName)
        {
            if(propertyName=="TestCategory")
            {
                return RapiseTestCategoryProperty;
            } 
            return TestCaseProperties.FullyQualifiedName;
        }

        public static object PropertyValueProvider(TestCase currentTest, string propertyName)
        {
            if(propertyName=="TestCategory")
            {
                if( currentTest.Properties.Contains(RapiseTestCategoryProperty) )
                {
                    object res = currentTest.GetPropertyValue(RapiseTestCategoryProperty);
                    return res;
                }
                return null;
            }
            return currentTest.FullyQualifiedName;
        }
    }
}
