namespace Test
{
    using System;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Reflection;
    using Microshaoft;
    public class ContextInfo
    {
        public string F1;
    }
    class Program1111
    {
        static void Main1(string[] args)
        {
            string codeSnippet1 = @"
Console.WriteLine(x);
Console.WriteLine(y.F1);
//TResult r;
return true;
";
            string codeSnippet2 = @"
Console.WriteLine(x);
Console.WriteLine(y.F1);
Console.WriteLine(z);
//TResult r;
//return true;
";
            string codeSnippet3 = @"
//Console.WriteLine(x);
//Console.WriteLine(y.F1);
Console.WriteLine(""Action()"");
//TResult r;
//return true;
";
            SourceCodeDynamicCompilerInvokers invokers = new SourceCodeDynamicCompilerInvokers();
            invokers
                .Add
                    (
                        new string[] { "System.dll", @"Noname3.exe" }
                        , new string[] { "System", "System", "Test" }
                        , "Func<string, ContextInfo, bool>"
                        , "Call"
                        , new string[] { "x", "y" }
                        , codeSnippet1
                    );
            invokers
                .Add
                    (
                        new string[] { "System.dll", "System.dll", @"Noname3.exe" }
                        , new string[] { "System", "System.Text", "Test" }
                        , "Action<int, ContextInfo, bool>"
                        , "Call2"
                        , new string[] { "x", "y", "z" }
                        , codeSnippet2
                    );
            invokers
                .Add
                    (
                        new string[] { "System.dll", "System.dll", @"Noname3.exe" }
                        , new string[] { "System", "System.Text", "Test" }
                        , "Action"
                        , "Call3"
                        , null //new string[] { "x", "y", "z" }
                        , codeSnippet3
                    );
            invokers.Build();
            //Func<string, ContextInfo, bool> func = (Func<string, ContextInfo, bool>) cr["Call"];
            dynamic func = invokers["Call"];
            var r = func("xxxx", new ContextInfo() { F1 = "FF1" });
            Console.WriteLine(r);
            //Action<int, ContextInfo, bool> action = (Action<int, ContextInfo, bool>) cr["Call2"];
            dynamic action = invokers["Call2"];
            action(999, new ContextInfo() { F1 = "FFF1" }, false);
            //Action action = (Action) cr["Call3"];
            dynamic action2 = invokers["Call3"];
            action2();
            Console.ReadLine();
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    public class SourceCodeDynamicCompilerInvokers
    {
        private string _codeTemplate = @"
namespace Microshaoft.Temp
{{
    {0}
    public static partial class InvokersManager
    {{
        public static
                        {1}                        //方法签名定义,如: Func<string, string, bool>
                        {2}                        //方法名称
                            ()
        {{
            var invoker = new {1}                //方法签名定义,如: Func<string, string, bool>
                                (
                                    (
                                        {3}        //参数列表
                                    ) =>
                                    {{
                                        {4}        //代码
                                    }}
                                );
            return invoker;
        }}
    }}
}}
";
        private class CodeTemplatePlaceHolder
        {
            public string[] ReferencedAssemblies;
            public string[] UsingsTargets;
            public string MethodDefinitionStatment;
            public string MethodName;
            public string[] MethodArgs;
            public string EmbedInlineCodeSnippet;
            public Delegate MethodInvoker;
        }
        public Delegate this[string key]
        {
            get
            {
                return
                    _sourceCodes[key].MethodInvoker;
            }
        }
        private ConcurrentDictionary<string, CodeTemplatePlaceHolder> _sourceCodes
                        = new ConcurrentDictionary<string, CodeTemplatePlaceHolder>();
        private void Compile
                    (
                        string[] referencedAssemblies
                        , string sourceCode
                    )
        {
            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
            var compilerParameters = new CompilerParameters();
            Array
                .ForEach
                    (
                        referencedAssemblies
                        , (x) =>
                        {
                            compilerParameters
                                .ReferencedAssemblies
                                .Add(x);
                        }
                    );
            compilerParameters.GenerateExecutable = false;
            compilerParameters.GenerateInMemory = true;
            //Console.WriteLine(code);
            var compilerResults = codeDomProvider
                                        .CompileAssemblyFromSource
                                            (
                                                compilerParameters
                                                , sourceCode
                                            );
            var assembly = compilerResults.CompiledAssembly;
            var codes = _sourceCodes.AsEnumerable();
            var stringBuilder = new StringBuilder();
            foreach (var kvp in codes)
            {
                MethodInfo mi = assembly
                                    .GetType("Microshaoft.Temp.InvokersManager")
                                    .GetMethod(kvp.Key);
                Delegate invoker = (Delegate)mi.Invoke(null, null);
                kvp.Value.MethodInvoker = invoker;
            }
        }
        public void Add
                        (
                            string[] referencedAssemblies
                            , string[] usingsTargets
                            , string methodSignatureDefinition
                            , string methodName
                            , string[] methodArgs
                            , string embedInlineCodeSnippet
                        )
        {
            _sourceCodes
                .TryAdd
                    (
                            methodName
                            , new CodeTemplatePlaceHolder()
                            {
                                ReferencedAssemblies = referencedAssemblies
                                 ,
                                UsingsTargets = usingsTargets
                                 ,
                                MethodDefinitionStatment = methodSignatureDefinition
                                 ,
                                MethodName = methodName
                                 ,
                                MethodArgs = methodArgs
                                 ,
                                EmbedInlineCodeSnippet = embedInlineCodeSnippet
                            }
                    );
        }
        public void Build()
        {
            string[] referencedAssemblies
                        = _sourceCodes
                            .SelectMany
                                (
                                    (x) =>
                                    {
                                        return
                                            x
                                                .Value
                                                .ReferencedAssemblies;
                                    }
                                ).Distinct()
                                 .ToArray();
            var sourceCodes = _sourceCodes.AsEnumerable();
            var stringBuilder = new StringBuilder();
            foreach (var kvp in sourceCodes)
            {
                var usingStatement = "using {0};";
                var usingsTargets = kvp
                                        .Value
                                        .UsingsTargets
                                        .Distinct();
                var usingsStatementsItems
                            = usingsTargets
                                .Select
                                    (
                                        (xx) =>
                                        {
                                            var rr = string.Format(usingStatement, xx);
                                            return rr;
                                        }
                                    );
                var usingsStatements = string.Join("\r\n", usingsStatementsItems);
                var methodArgs = kvp
                                    .Value
                                    .MethodArgs;
                var methodArgsStatement = string.Empty;
                if
                    (
                        methodArgs != null
                        &&
                        methodArgs.Length > 0
                    )
                {
                    methodArgsStatement = string.Join(", ", methodArgs);
                }
                var sourceCode = string
                                    .Format
                                        (
                                            _codeTemplate
                                            , usingsStatements
                                            , kvp.Value.MethodDefinitionStatment
                                            , kvp.Value.MethodName
                                            , methodArgsStatement
                                            , kvp.Value.EmbedInlineCodeSnippet
                                        );
                stringBuilder
                    .AppendLine(sourceCode);
            }
            Compile(referencedAssemblies, stringBuilder.ToString());
        }
    }
}