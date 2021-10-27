using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;


namespace MyDotnetScript
{
    class Program
    {
        static void Main(string[] args)
        {
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
            const string defaultUsings = "using System;\nusing System.Collections.Generic;\nusing System.IO;";
            CSharpScriptEngine.Execute(defaultUsings);
            CSharpScriptEngine.Execute(code);
            while (true)
            {
                Console.Write(">");
                var c = Console.ReadLine();
                if (c == "exit")
                    break;
                else if (c.EndsWith("{"))
                {
                    var c2 = "";
                    while (!c2.EndsWith("}"))
                    {
                        c2 += Console.ReadLine();
                    }

                    c += c2;
                }
                else if (c.StartsWith("import dll"))
                {
                    try
                    {
                        var path = c.Replace("import dll ", "");
                        var assembly = Assembly.LoadFrom(path);
                        CSharpScriptEngine.AddAsembly(assembly);
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
                        case "restart":
                            CSharpScriptEngine.ScriptState = null;
                            c = defaultUsings;
                            break;
                        case "getCode":
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
                                        getCode += result.Code+"\n";
                                    }
                                    return scriptState;
                                }
                                // Console.WriteLine(a);
                            }

                            break;
                    }

                CSharpScriptEngine.Execute(c);
            }
        }
    }

    public static class CSharpScriptEngine
    {
        private static ScriptState<object> _scriptState = null;

        public static ScriptState<object> ScriptState
        {
            get => _scriptState;
            set => _scriptState = value;
        }

        public static object Execute(string code)
        {
            try
            {
                _scriptState = _scriptState == null
                    ? CSharpScript.RunAsync(code, ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly()))
                        .Result
                    : _scriptState.ContinueWithAsync(code,
                        ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly())).Result;
                if (_scriptState.Variables.Any(x => x.Name == code))
                {
                    Console.WriteLine(JsonConvert.SerializeObject(_scriptState.ReturnValue));
                }

                if (_scriptState.ReturnValue != null)
                    return _scriptState.ReturnValue;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("error CS1002: ; "))
                    Execute(code + ";");
                else if (e.Message.Contains("error CS0103") || e.Message.Contains("error CS0029"))
                    Execute("var " + code);
                else
                    Console.WriteLine("Error:" + e.Message);
            }

            return null;
        }

        public static void AddAsembly(Assembly assembly)
        {
            try
            {
                _scriptState = _scriptState == null
                    ? CSharpScript.RunAsync("", ScriptOptions.Default.WithReferences(assembly))
                        .Result
                    : _scriptState.ContinueWithAsync("",
                        ScriptOptions.Default.WithReferences(assembly)).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
            }
        }
    }
}