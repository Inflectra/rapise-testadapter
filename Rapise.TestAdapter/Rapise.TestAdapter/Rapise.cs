using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml;
using System.IO.Compression;

namespace Rapise.TestAdapter
{
    public interface IRapiseRunner
    {
        TestResult RunTest(TestCase tc, IRunContext runContext);
    }

    public class Rapise : IRapiseRunner
    {
        private static log4net.ILog log = RapiseTestAdapter.InitLogging();

        private static string rapiseEnginePath;
        private static string GetRapiseEnginePath()
        {
            if (!string.IsNullOrEmpty(rapiseEnginePath))
            {
                return rapiseEnginePath;
            }

            Type helperType = Type.GetTypeFromProgID("SeSHelper");
            if (null != helperType)
            {
                object helperObj = Activator.CreateInstance(helperType);
                if (null != helperObj)
                {
                    object res = helperObj.GetType().InvokeMember("GetEnginePath", BindingFlags.InvokeMethod, null, helperObj, new object[0]);
                    if (null != res)
                    {
                        rapiseEnginePath = "" + res;
                        return rapiseEnginePath;
                    }
                }
            }
            else
            {
                log.Debug("Rapise is not installed, unable to create instance of SeSHelper");
            }
            return null;
        }

        private static string rapisePath;
        private static string GetRapisePath()
        {
            if (!string.IsNullOrEmpty(rapisePath))
            {
                return rapisePath;
            }

            string epath = GetRapiseEnginePath();
            if (null != epath)
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(epath);
                rapisePath = di.Parent.FullName;
                return rapisePath;
            }
            return null;
        }

        public TestResult RunTest(TestCase tc, IRunContext runContext)
        {
            TestResult tr = new TestResult(tc);

            log.Debug("Run settings:\n" + runContext.RunSettings.SettingsXml);

            XmlDocument rs = new XmlDocument();
            rs.LoadXml(runContext.RunSettings.SettingsXml);
            XmlNodeList nodes = rs.SelectNodes("//TestRunParameters/Parameter");

            string parameters = "";
            // "-eval:g_testSetParams.g_browserLibrary='Chrome HTML'"
            foreach (XmlNode node in nodes)
            {
                string key = node.Attributes["name"].Value;
                string value = "" + node.Attributes["value"].Value;
                
                if (key.StartsWith("g_"))
                {
                    parameters += " \"-eval:g_testSetParams." + key + "=\'" + value + "\'\"";
                }
            }

            string path = tc.Source;

            Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            path = path.Replace("%SMARTESTUDIO%", GetRapisePath());
            path = path.Replace("%ENGINE%", System.IO.Path.Combine(GetRapiseEnginePath(), "\\.."));
            string executorLine = "\""+System.IO.Path.Combine(GetRapiseEnginePath(), "SeSExecutor.js")+ "\"" + " \"" + path + "\"" + parameters;
            Process myProc = Process.Start("cscript.exe", executorLine);
            myProc.WaitForExit();
            log.Debug("Exit code: " + myProc.ExitCode);

            string testFolderPath = System.IO.Path.GetDirectoryName(path);

            var attachmentSet = new AttachmentSet(new Uri(RapiseTestAdapter.ExecutorUri), "Attachments");

            
            string outPath = System.IO.Path.Combine(testFolderPath, "summary.log");
            if (System.IO.File.Exists(outPath))
            {
                Uri fileUri = new Uri(outPath, UriKind.Absolute);
                attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, "Output Log"));
            }

            string errPath = System.IO.Path.Combine(testFolderPath, "err.log");
            if (System.IO.File.Exists(errPath))
            {
                Uri fileUri = new Uri(errPath, UriKind.Absolute);
                attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, "Error Log"));
            }

            string tapPath = System.IO.Path.Combine(testFolderPath, "last.tap");
            if (System.IO.File.Exists(tapPath))
            {
                Uri fileUri = new Uri(tapPath, UriKind.Absolute);
                attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, "Report in TAP format"));
            }

            string trpPath = System.IO.Path.Combine(testFolderPath, "last.trp");
            if (System.IO.File.Exists(trpPath))
            {
                Uri fileUri = new Uri(trpPath, UriKind.Absolute);
                attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, "Report in Rapise format"));

                string trpString = "<report>" + File.ReadAllText(trpPath) + "</report>";
                XmlDocument trpXml = new XmlDocument();
                trpXml.LoadXml(trpString);
                XmlNode sfn = trpXml.SelectSingleNode("//log[@name='Screen flow']/data");
                if (sfn != null)
                {
                    string htmlPath = sfn.Attributes["url"].Value;
                    if (!string.IsNullOrEmpty(htmlPath))
                    {
                        string screenFlowPath = Path.GetDirectoryName(htmlPath);
                        string zipFileName = tc.FullyQualifiedName + "_" + Path.GetFileName(screenFlowPath) + "_screen_flow.zip";
                        string zipFilePath = Path.Combine(runContext.TestRunDirectory, zipFileName);
                        ZipFile.CreateFromDirectory(screenFlowPath, zipFilePath);
                        Uri zipFileUri = new Uri(zipFilePath, UriKind.Absolute);
                        attachmentSet.Attachments.Add(new UriDataAttachment(zipFileUri, zipFileName));
                    }
                }
            }

            tr.Attachments.Add(attachmentSet);
            tr.Outcome = myProc.ExitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
            return tr;
        }
    }
}