using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml;
using System.IO.Compression;
using System.Linq;

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

        public static string GetRapiseEnginePath()
        {
            string studioEnginePath = System.Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Inflectra\Rapise\Engine");

            if (Directory.Exists(studioEnginePath))
            {
                return studioEnginePath;
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
                        return "" + res;
                    }
                }
            }
            else
            {
                log.Debug("Rapise is not installed");
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

        private void RegisterAttachment(TestCase tc, string fileName, string friendlyFileNameWithoutExtension, AttachmentSet attachmentSet)
        {
            string filePath = System.IO.Path.Combine(this.testFolderPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                string attachmentFilePath = Path.Combine(this.testCaseResultDirectory, friendlyFileNameWithoutExtension + "_" + Path.GetFileName(Path.GetDirectoryName(tc.Source)) + "_" + this.timestamp + Path.GetExtension(fileName));
                File.Copy(filePath, attachmentFilePath);

                Uri fileUri = new Uri(attachmentFilePath, UriKind.Absolute);
                attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, friendlyFileNameWithoutExtension));
            }
        }

        private string testFolderPath;
        private string testCaseResultDirectory;
        private string timestamp;

        private static string RandomString(int length)
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public TestResult RunTest(TestCase tc, IRunContext runContext)
        {
            TestResult tr = new TestResult(tc);

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

            XmlNodeList maxCpuCount = rs.SelectNodes("//RunConfiguration/MaxCpuCount");
            bool parallel = maxCpuCount.Count > 0;
            if (parallel)
            {
                log.Debug("Parallel execution is turned ON");
                parameters += " \"-eval:g_testSetParams.g_showExecutionMonitor=''\"";
            }

            string path = tc.Source;

            Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
            path = path.Replace("%SMARTESTUDIO%", GetRapisePath());
            path = path.Replace("%ENGINE%", System.IO.Path.Combine(GetRapiseEnginePath(), "\\.."));
            string executorLine = "\""+System.IO.Path.Combine(GetRapiseEnginePath(), "SeSExecutor.js")+ "\"" + " \"" + path + "\"" + parameters;
            this.timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.Arguments = executorLine;
            startInfo.FileName = "cscript.exe";
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(path);
            string suffix = "";
            if (parallel)
            {
                suffix = "_" + RandomString(5).ToUpper();
                startInfo.EnvironmentVariables["THREAD"] = suffix;
            }

            Process myProc = Process.Start(startInfo);
            FileSystemWatcher fsw = new FileSystemWatcher(startInfo.WorkingDirectory, "last" + suffix + ".tap");
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.IncludeSubdirectories = false;
            fsw.Changed += OnTapFileChanged;
            lastOffset = 0;
            fsw.EnableRaisingEvents = true;
            myProc.WaitForExit();
            fsw.EnableRaisingEvents = false;
            log.Debug("Exit code: " + myProc.ExitCode);

            this.testFolderPath = System.IO.Path.GetDirectoryName(path);
            this.testCaseResultDirectory = Path.Combine(runContext.TestRunDirectory, Path.GetFileName(this.testFolderPath) + "_" + this.timestamp);
            log.Debug("testFolderPath: " + testFolderPath + " testCaseResultDirectory:" + testCaseResultDirectory);
            Directory.CreateDirectory(this.testCaseResultDirectory);

            var attachmentSet = new AttachmentSet(new Uri(RapiseTestAdapter.ExecutorUri), "Attachments");
            
            RegisterAttachment(tc, "summary" + suffix + ".log", "output", attachmentSet);
            RegisterAttachment(tc, "err" + suffix + ".log", "error", attachmentSet);
            RegisterAttachment(tc, "last" + suffix + ".tap", "tap_report", attachmentSet);
            RegisterAttachment(tc, "last" + suffix + ".trp", "trp_report", attachmentSet);

            string trpPath = System.IO.Path.Combine(testFolderPath, "last" + suffix + ".trp");
            if (System.IO.File.Exists(trpPath))
            {
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
                        string zipFileName = "screen_flow_" + tc.FullyQualifiedName + "_" + this.timestamp + ".zip";
                        string zipFilePath = Path.Combine(this.testCaseResultDirectory, zipFileName);
                        ZipFile.CreateFromDirectory(screenFlowPath, zipFilePath);
                        Uri zipFileUri = new Uri(zipFilePath, UriKind.Absolute);
                        attachmentSet.Attachments.Add(new UriDataAttachment(zipFileUri, zipFileName));
                    }
                }
                // <log type="Assert" name="Fail1" status="Fail"  at="2023-07-27 15:14:26.837"><data type="link" url="C:\Outils\Rapise\FWNoSpira\TestCases\t2\Main.rvl.xlsx(RVL,6,1)" text="C:\Outils\Rapise\FWNoSpira\TestCases\t2\Main.rvl.xlsx(RVL,6,1)"/></ log >

                XmlNode firstFailure = trpXml.SelectSingleNode("//log[@type='Assert' and @status='Fail']");
                if(firstFailure!=null)
                {
                    tr.ErrorMessage = firstFailure.Attributes["name"].Value;
                    XmlAttribute comment = firstFailure.Attributes["comment"];
                    if (comment != null)
                    {
                        tr.ErrorMessage += "\t" + comment.Value;
                    }
                    string stack = "";
                    foreach(XmlNode dataNode in firstFailure.SelectNodes(".//data"))
                    {
                        XmlAttribute txt = dataNode.Attributes["text"];
                        if(txt!=null)
                        {
                            stack += txt.Value + "\n";
                        }
                    }
                    tr.ErrorStackTrace = stack;
                }

            }

            string ownerValue = ""+tc.GetPropertyValue(RapiseTestExecutor.RapiseTestOwnerProperty);
            tr.Traits.Add(new Trait("Owner", ownerValue));
            tr.Attachments.Add(attachmentSet);
            tr.Outcome = myProc.ExitCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;

            return tr;
        }
        private int lastOffset = 0;
        private void OnTapFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                string data = File.ReadAllText(e.FullPath);
                if (data != null)
                {
                    string newData = data.Substring(lastOffset) + "\n";
                    lastOffset=data.Length;
                    Console.Write(newData);
                }
            }catch(Exception ex)
            {
                log.Debug("Error watching tap file: " + e.FullPath, ex);
            }
        }
    }
}