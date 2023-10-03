using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Rapise.TestAdapter.Test
{
    [TestClass]
    public class RapiseTestAdapterTests
    {
        private readonly Mock<IRunContext> runContext;
        private readonly Mock<IFrameworkHandle> frameworkhandle;
        private readonly Mock<ITestCaseFilterExpression> filterExpression;
        private readonly Mock<IRapiseRunner> rapiseRunner;

        public RapiseTestAdapterTests()
        {
            this.runContext = new Mock<IRunContext>();
            this.frameworkhandle = new Mock<IFrameworkHandle>();
            this.filterExpression = new Mock<ITestCaseFilterExpression>();
            this.rapiseRunner = new Mock<IRapiseRunner>();
        }

        [TestMethod]
        public void TestRunWithNameFilter()
        {
            this.filterExpression.Setup(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>())).Returns((TestCase tc, object callback) => tc.FullyQualifiedName == "Test1" ? true : false);
            this.runContext.Setup(m => m.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(), (Func<string, TestProperty>)It.IsAny<object>())).Returns(filterExpression.Object);
            this.rapiseRunner.Setup(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>())).Returns((TestCase tc, IRunContext ctx) => new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc));
            RapiseTestExecutor rte = new RapiseTestExecutor(this.rapiseRunner.Object);
            string[] sources = new string[] { "Test1\\Test1.sstest", "Test2\\Test2.sstest" };
            rte.RunTests(sources, runContext.Object, frameworkhandle.Object);

            this.filterExpression.Verify(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>()), Times.Exactly(2));
            this.rapiseRunner.Verify(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>()), Times.Exactly(1));
        }

        [TestMethod]
        public void TestRunWithCategory()
        {
            this.filterExpression.Setup(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>())).Returns((TestCase tc, object callback) =>
            {
                string[] tags = (string[])tc.GetPropertyValue(RapiseTestExecutor.RapiseTestCategoryProperty);
                return ("" + tags[0]) == "framework" ? true : false;
            });
            this.runContext.Setup(m => m.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(), (Func<string, TestProperty>)It.IsAny<object>())).Returns(filterExpression.Object);
            this.rapiseRunner.Setup(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>())).Returns((TestCase tc, IRunContext ctx) => new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc));
            RapiseTestExecutor rte = new RapiseTestExecutor(this.rapiseRunner.Object);
            string[] sources = new string[] { @"..\..\Tests\Test1.sstest", @"..\..\Tests\Test2.sstest", @"..\..\Tests\Test3.sstest" };
            rte.RunTests(sources, runContext.Object, frameworkhandle.Object);

            this.filterExpression.Verify(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>()), Times.Exactly(3));
            this.rapiseRunner.Verify(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>()), Times.Exactly(1));
        }

        [TestMethod]
        public void TestRunNoFilter()
        {
            this.runContext.Setup(m => m.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(), (Func<string, TestProperty>)It.IsAny<object>())).Returns(() => null);
            this.rapiseRunner.Setup(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>())).Returns((TestCase tc, IRunContext ctx) => new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc));
            RapiseTestExecutor rte = new RapiseTestExecutor(this.rapiseRunner.Object);
            string[] sources = new string[] { "Test1\\Test1.sstest", "Test2\\Test2.sstest" };
            rte.RunTests(sources, runContext.Object, frameworkhandle.Object);

            this.rapiseRunner.Verify(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>()), Times.Exactly(2));
        }

        private static string RandomString(int length)
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [TestMethod]
        public void TestRandomGenerator()
        { 
            List<Task> ts = new List<Task>();
            Dictionary<string, int> d = new Dictionary<string, int>();
            for (int i = 0; i < 100; i++)
            {
                Task t = Task.Run(() => {
                    string r = RandomString(5);
                    if (d.ContainsKey(r))
                    {
                        Assert.IsTrue(false, "Dup: " + i + "/" + r + "/" + d.Count);
                    }
                    lock (d)
                    {
                        d[r] = i;
                    }
                });
                ts.Add(t);
            }
            Task.WaitAll(ts.ToArray());
        
        }
    }
}
