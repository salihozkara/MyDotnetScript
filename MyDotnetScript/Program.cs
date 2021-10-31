using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;


namespace MyDotnetScript
{
    internal static class Program
    {
        private const string DefaultUsings = "using System;\nusing System.Collections.Generic;\nusing System.IO;";
        private static void Main(string[] args)
        {
            CSharpScriptEngine.Execute(DefaultUsings);
            if (args == null || args.Length < 1)
            {
                CodeRun();
            }
            else
            {
                var code = File.ReadAllText(args[0]);
                if (args.Length > 1 && (args.Any(x => x == "edit") || args.Any(x => x == "-edit") ||
                                        args.Any(x => x == "-e")))
                {
                    CodeRun(code);
                }
                else
                    CSharpScriptEngine.Execute(code);
            }
        }

        private static void CodeRun(string code = "")
        {
            CSharpScriptEngine.Execute(code);
            while (true)
            {
                Console.Write(">");
                var c = Console.ReadLine();
                if (c != null)
                {
                    
                    if (c.EndsWith("{"))
                    {
                        var c2 = "";
                        while (!c2.EndsWith("}"))
                        {
                            c2 += Console.ReadLine();
                        }

                        c += c2;
                    }else if (c.StartsWith(Commands.AddNuget))
                    {
                        c = c.Remove(0, Commands.AddNuget.Length);
                        var parameters = c.Split(" ");
                        Nuget.AddNuget(parameters[0]);
                        c = "";
                    }
                    else if (c.StartsWith(Commands.AddAssembly))
                    {
                        try
                        {
                            var path = c.Replace(Commands.AddAssembly + " ", "");
                            var assembly = Assembly.LoadFrom(path);
                            CSharpScriptEngine.AddAssembly(assembly);
                            if (assembly.FullName != null) c = "using " + assembly.FullName.Split(",")[0];
                        }
                        catch (Exception e)
                        {
                            c = "";
                            Console.WriteLine("Error: " + e.Message);
                        }
                    }
                    else
                        switch (c)
                        {
                            case Commands.Exit:
                                Environment.Exit(1);
                                break;
                            case Commands.Restart:
                                CSharpScriptEngine.ScriptState = null;
                                c = DefaultUsings;
                                break;
                            case Commands.PathAdd:
                                c = "";
                                const string name = "PATH";
                                const EnvironmentVariableTarget scope = EnvironmentVariableTarget.User; // or User
                                var oldValue = Environment.GetEnvironmentVariable(name, scope);
                                var path = Directory.GetCurrentDirectory();
                                if (oldValue != null && !oldValue.Contains(path))
                                {
                                    var newValue = oldValue + @";" + path + ";";
                                    Environment.SetEnvironmentVariable(name, newValue, scope);
                                }

                                break;
                            case Commands.GetCode:
                                c = "";
                                if (CSharpScriptEngine.ScriptState != null)
                                {
                                    var getCode = "";
                                    GetCode(CSharpScriptEngine.ScriptState.Script);
                                    getCode += CSharpScriptEngine.ScriptState.Script.Code;

                                    Console.WriteLine(getCode);

                                    Script GetCode(Script scriptState)
                                    {
                                        Script result = null;
                                        if (scriptState != null)
                                        {
                                            if (scriptState.Previous != null)
                                            {
                                                result = GetCode(scriptState.Previous);
                                            }
                                            else

                                                return scriptState;
                                        }

                                        if (result == null) return null;
                                        if (!string.IsNullOrEmpty(result.Code))
                                        {
                                            getCode += result.Code + "\n";
                                        }

                                        return scriptState;
                                    }
                                }

                                break;
                            case Commands.GetCommands:
                                c = "";
                                var type = typeof(Commands);
                                var fields = type.GetFields();
                                foreach (var fieldInfo in fields)
                                {
                                    Console.WriteLine(fieldInfo.GetValue(null));
                                }
                                break;
                        }
                }

                CSharpScriptEngine.Execute(c);
            }
        }
    }
}