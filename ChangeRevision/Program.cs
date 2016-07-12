using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChangeRevision
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var process = new Process();
                process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                process.StartInfo.FileName = "\"c:\\Program Files (x86)\\Git\\cmd\\git.exe\"";
                process.StartInfo.Arguments = @"rev-list master --count";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                var output = new StringBuilder();
                var timeout = 10000;

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                            output.AppendLine(e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();

                    if (process.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout))
                    {
                        var text = File.ReadAllText(@"..\..\..\" + args[1] + @"\Properties\AssemblyInfo.cs");

                        var match = new Regex("AssemblyVersion\\(\"(.*?)\"\\)").Match(text);
                        var ver = new Version(match.Groups[1].Value);
                        var build = args[0] == "Release" ? ver.Build + 1 : ver.Build;
                        var newVer = new Version(ver.Major, ver.Minor, build, Convert.ToInt16(output.ToString().Trim()));

                        text = Regex.Replace(text, @"AssemblyVersion\((.*?)\)", "AssemblyVersion(\"" + newVer + "\")");
                        text = Regex.Replace(text, @"AssemblyFileVersionAttribute\((.*?)\)",
                            "AssemblyFileVersionAttribute(\"" + newVer + "\")");
                        text = Regex.Replace(text, @"AssemblyFileVersion\((.*?)\)",
                            "AssemblyFileVersion(\"" + newVer + "\")");

                        File.WriteAllText(@"..\..\..\" + args[1] + @"\Properties\AssemblyInfo.cs", text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}