/**
 * Author: Omar Al Yousuf
 * Date: 03/21/2022
 * Description: build executables to run hard coded atomic tests
 * **/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace AtomicTestTool
{
    internal class Builder
    {
        public static void Main()
        {
            try
            {
                // get Atomic from YAML files
                string[] atomicNum = { "T1016,0", "T1016,1" }; // specify Atomic num and sub num
                var my_executor = "";
                var my_commands = "";

                foreach (string i in atomicNum)
                {
                    string atomicTestNum = i.Substring(0, i.IndexOf(','));
                    int atomicTestSubNum = int.Parse(i.Substring(i.LastIndexOf(',') + 1));
                    
                    // get the commands from YAML file
                    PowerShell ps = PowerShell.Create();
                    ps.AddCommand("Get-AtomicTechnique")
                      .AddParameter("Path", $@"C:\AtomicRedTeam\atomics\{atomicTestNum}\{atomicTestNum}.yaml");

                    dynamic technique = ps.Invoke()[0];
                    var atomic_tests = technique.atomic_tests;
                    var my_test = atomic_tests[atomicTestSubNum];
                    my_executor = my_test.executor.name;
                    my_commands = my_test.executor.command;
                    
                }

                String exec_command = "";

                if (my_executor == "command_prompt")
                {
                    exec_command = my_commands.Replace("\n", " & "); // replace newlines with ampersand
                }
                else if (my_executor == "powershell")
                {
                    exec_command = Regex.Replace(my_commands, "(?<!;)\n", "; "); // add semicolon to end of line unless already has one
                }

                // directory of AtomicTestTool
                var dir = ((Directory.GetParent(Directory.GetCurrentDirectory())).Parent).FullName;
                var file = File.ReadAllText(dir + "\\Program.cs");

                // handle commands with double quotes in them, then replace into program.cs
                var escaped_command = exec_command.Replace("\"", "\\\"");
                
                var file2 = file.Replace("replace-here", escaped_command);
                file2 = file2.Replace("the-executor", my_executor);
                var exportFilePath = (dir + "\\Program2.cs");

                // export the file into exportFilePath
                File.WriteAllText(exportFilePath, file2);

                // cmd / csc.exe/ compile program.cs into art.exe
                string compileCSC = $@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc /reference:C:\Windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\system.management.automation.dll /out:{dir}\art.exe {exportFilePath}";

                using (Process process = new Process())
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/C " + compileCSC;
                    process.Start();
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
