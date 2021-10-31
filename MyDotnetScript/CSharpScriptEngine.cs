using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;

namespace MyDotnetScript
{
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
                {
                    Console.WriteLine(JsonConvert.SerializeObject(_scriptState.ReturnValue));
                    return _scriptState.ReturnValue;
                }
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

        public static void AddAssembly(Assembly assembly)
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