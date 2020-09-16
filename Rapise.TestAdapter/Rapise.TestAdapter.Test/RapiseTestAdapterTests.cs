using System;
using System.Collections;
using System.Collections.Generic;
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
        public void TestRunWithFilter()
        {
            this.filterExpression.Setup(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>())).Returns((TestCase tc, object callback) => tc.FullyQualifiedName == "Test1" ? true : false);
            this.runContext.Setup(m => m.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(), (Func<string, TestProperty>)It.IsAny<object>())).Returns(filterExpression.Object);
            this.rapiseRunner.Setup(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>())).Returns((TestCase tc, IRunContext ctx) => new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc));
            RapiseTestExecutor rte = new RapiseTestExecutor(this.rapiseRunner.Object);
            string[] sources = new string[] { "Test1.sstest", "Test2.sstest" };
            rte.RunTests(sources, runContext.Object, frameworkhandle.Object);

            this.filterExpression.Verify(m => m.MatchTestCase(It.IsAny<TestCase>(), (Func<string, object>)It.IsAny<object>()), Times.Exactly(2));
            this.rapiseRunner.Verify(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>()), Times.Exactly(1));
        }

        [TestMethod]
        public void TestRunNoFilter()
        {
            this.runContext.Setup(m => m.GetTestCaseFilter(It.IsAny<IEnumerable<string>>(), (Func<string, TestProperty>)It.IsAny<object>())).Returns(() => null);
            this.rapiseRunner.Setup(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>())).Returns((TestCase tc, IRunContext ctx) => new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(tc));
            RapiseTestExecutor rte = new RapiseTestExecutor(this.rapiseRunner.Object);
            string[] sources = new string[] { "Test1.sstest", "Test2.sstest" };
            rte.RunTests(sources, runContext.Object, frameworkhandle.Object);

            this.rapiseRunner.Verify(m => m.RunTest(It.IsAny<TestCase>(), It.IsAny<IRunContext>()), Times.Exactly(2));
        }
    }
}
