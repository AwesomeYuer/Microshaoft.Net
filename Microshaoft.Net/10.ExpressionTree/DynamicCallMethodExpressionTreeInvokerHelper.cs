#if NET45
namespace ConsoleApplication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using Microshaoft;
    public class Class1
    {
        static void Main(string[] args)
        {
            Console.WriteLine("生成 eml 文件");
            string html = "<html><body><a href=\"http://www.live.com\"><img src=\"cid:attachment1\"></a>";
            html += "<script src=\"cid:attachment2\"></script>中国字";
            html += "<a href=\"http://www.google.com\"><br><img src=\"cid:attachment1\"></a><script>alert('mail body xss')<script></body></html>";
            AlternateView view = AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html);
            //LinkedResource picture = new LinkedResource(@"pic.JPG", MediaTypeNames.Image.Jpeg);
            //picture.ContentId = "attachment1";
            //view.LinkedResources.Add(picture);
            //LinkedResource script = new LinkedResource(@"a.js", MediaTypeNames.Text.Plain);
            //script.ContentId = "attachment2";
            //view.LinkedResources.Add(script);
            MailMessage mail = new MailMessage();
            mail.AlternateViews.Add(view);
            mail.From = new MailAddress("test@microshaoft.com", "<script>alert('mail from xss')</script>");
            mail.To.Add(new MailAddress("microshaoft@gmail.com", "<script>alert('mail to xss')</script>"));
            mail.To.Add(new MailAddress("microshaoft@qq.com", "<script>alert('mail to xss')</script>"));
            mail.Subject = "<script>alert('mail subject xss')</script>" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] buffer = mail.GetBytes();
            File.WriteAllBytes(@"d:\temp.eml", buffer);
            Console.WriteLine("====================================================================");
            Console.ReadLine();
            Console.WriteLine("计算表达式");
            string formula = "(({0}-{1})/{2}+{3})*{4}";
            string result = JScriptEvaluator.ComputeFormula<double>
                                                    (
                                                        formula
                                                        , 1f
                                                        , 2.1
                                                        , 3.1
                                                        , 4.0
                                                        , 5.0
                                                    );
            Console.WriteLine(result);
            double x;
            x = DataTableColumnExpression.ComputeFormula<double, double>
                                                    (
                                                        formula
                                                        , 1f
                                                        , 2.1
                                                        , 3.1
                                                        , 4.0
                                                        , 5.0
                                                    );
            Console.WriteLine(x);
            //=================================================================================================
            formula = "IIF(1=2, F1, F2) + ((--F1) * F2) + F3";
            var tuples = new Tuple<string, double>[]
											{
												Tuple.Create<string,double>("F1", 1.0)
												, Tuple.Create<string,double>("F2", 2.0)
												, Tuple.Create<string,double>("F3", 3.0)
												, Tuple.Create<string,double>("F4", 4.0)
												, Tuple.Create<string,double>("F3", 2.0)
											};
            x = DataTableColumnExpression.ComputeFormula<double, double>(formula, tuples);
            Console.WriteLine(x);
            Console.ReadLine();
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    public class DataTableColumnExpression
    {
        private static MethodInfo _mi = typeof(string).GetMethods().First
                                                                    (
                                                                        m => m.Name.Equals("Format")
                                                                        && m.GetParameters().Length == 2
                                                                        && m.IsStatic
                                                                        && m.GetParameters()[1].Name == "args"
                                                                    );
        private class ObjectEqualityComparer<T> : IEqualityComparer<T>
        {
            private Func<T, T, bool> _onEqualsProcessFunc;
            private Func<T, int> _onGetHashCodeProcessFunc;
            public ObjectEqualityComparer
                        (
                            Func<T, T, bool> onEqualsProcessFunc
                            , Func<T, int> onGetHashCodeProcessFunc
                        )
            {
                _onEqualsProcessFunc = onEqualsProcessFunc;
                _onGetHashCodeProcessFunc = onGetHashCodeProcessFunc;
            }
            public bool Equals(T x, T y)
            {
                if (Object.ReferenceEquals(x, y))
                {
                    return true;
                }
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                {
                    return false;
                }
                return _onEqualsProcessFunc(x, y);
            }
            public int GetHashCode(T x)
            {
                if (Object.ReferenceEquals(x, null))
                {
                    return 0;
                }
                return _onGetHashCodeProcessFunc(x);
            }
        }
        public static TResult ComputeFormula<TResult, TParameter>
                                                    (
                                                        string formula
                                                        , params TParameter[] parameters
                                                    )
        {
            var dt = new DataTable();
            var list = parameters.ToList();
            var parametersNames = new List<string>(); ;
            int i = 0;
            Array.ForEach
                    (
                        parameters
                        , (x) =>
                        {
                            string f = string.Format("F{0}", i++);
                            parametersNames.Add(f);
                            var dc = new DataColumn(f, typeof(TParameter));
                            dt.Columns.Add(dc);
                        }
                    );
            string expression = string.Format(formula, parametersNames.ToArray());
            dt.Columns.Add(new DataColumn("Microshaoft", typeof(TResult), expression));
            var dr = dt.NewRow();
            i = 0;
            Array.ForEach
            (
                parameters
                , (x) =>
                {
                    dr[i++] = x;
                }
            );
            dt.Rows.Add(dr);
            return (TResult)dr["Microshaoft"];
        }
        public static TResult ComputeFormula<TResult, TParameter>
                                                    (
                                                        string formula
                                                        , params Tuple<string, TParameter>[] parameters
                                                    )
        {
            var dt = new DataTable();
            var comparer = new ObjectEqualityComparer<Tuple<string, TParameter>>
                                            (
                                                (x, y) =>
                                                {
                                                    return x.Item1 == y.Item1;
                                                }
                                                , (x) =>
                                                {
                                                    return x.Item1.GetHashCode();
                                                }
                                            );
            var list = parameters.Distinct
                                    (
                                        comparer
                                    )
                                    .ToList();
            list.ForEach
                    (
                        (x) =>
                        {
                            var dc = new DataColumn
                                            (
                                                x.Item1
                                                , x.Item2.GetType()
                                            );
                            dt.Columns.Add(dc);
                        }
                    );
            dt.Columns.Add(new DataColumn("Microshaoft", typeof(TResult), formula));
            var dr = dt.NewRow();
            list.ForEach
                    (
                        (x) =>
                        {
                            dr[x.Item1] = x.Item2;
                        }
                    );
            dt.Rows.Add(dr);
            return (TResult)dr["Microshaoft"];
        }
    }
    public class JScriptEvaluator
    {
        private static MethodInfo _mi = typeof(string).GetMethods().First
                                                                        (
                                                                            m => m.Name.Equals("Format")
                                                                            && m.GetParameters().Length == 2
                                                                            && m.IsStatic
                                                                            && m.GetParameters()[1].Name == "args"
                                                                        );
        private static Func<string, object[], object> _func = null;
        public static string ComputeFormula<TParameter>(string formula, params TParameter[] parameters)
        {
            object[] objects = new object[parameters.Length];
            Array.Copy(parameters, objects, objects.Length);
            string expression1 = string.Format(formula, objects);
            //return (string)JScriptEvaluator.Eval(expression1);
            //=====================================================================
            object[] ps = new object[parameters.Length];
            Array.Copy(parameters, 0, ps, 0, ps.Length);
            if (_func == null)
            {
                _func = DynamicCallMethodExpressionTreeInvokerHelper.CreateMethodCallInvokerFunc<string, string>
                                                (
                                                    typeof(string)
                                                    , () =>
                                                    {
                                                        var methodsInfos = typeof(string).GetMethods();
                                                        var methodInfo = methodsInfos.First
                                                                                        (
                                                                                            (x) =>
                                                                                            {
                                                                                                var parametersInfos = x.GetParameters();
                                                                                                //Debug.Assert(x.Name.ToLower() == "Format".ToLower());
                                                                                                return
                                                                                                    x.Name.ToLower() == "Format".ToLower()
                                                                                                    && x.IsStatic
                                                                                                    && parametersInfos[0].ParameterType == typeof(string)
                                                                                                    && parametersInfos[1].ParameterType == typeof(object[])
                                                                                                    && Attribute.IsDefined
                                                                                                                    (
                                                                                                                        parametersInfos[1]
                                                                                                                        , typeof(ParamArrayAttribute)
                                                                                                                    );
                                                                                            }
                                                                                        );
                                                        return methodInfo;
                                                    }
                                                );
            }
            string expression = (string)_mi.Invoke
                                                (
                                                    null
                                                    , new object[]
															{
																formula
																, ps
															}
                                                );
            expression = (string)_func
                                    (
                                        formula
                                        , new object[]
													{
														formula
														, ps
													}
                                    );
            return (string)JScriptEvaluator.Eval(expression);
        }
        public static object Eval(string statement)
        {
            return _evaluatorType.InvokeMember
                                        (
                                            "Eval"
                                            , BindingFlags.InvokeMethod
                                            , null
                                            , _evaluator
                                            , new object[]
													{
														statement
													}
                                        );
        }
        static JScriptEvaluator()
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("JScript");
            CompilerParameters parameters;
            parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            CompilerResults results;
            results = provider.CompileAssemblyFromSource(parameters, _JScript);
            Assembly assembly = results.CompiledAssembly;
            _evaluatorType = assembly.GetType("Microshaoft.JScriptEvaluator");
            var constructorInfo = _evaluatorType.GetConstructors().First();
            var func = DynamicCallMethodExpressionTreeInvokerHelper.CreateNewInstanceConstructorInvokerFunc
                                                            (
                                                                _evaluatorType
                                                                , constructorInfo
                                                            );
            _evaluator = func(null);
            //_evaluator = Activator.CreateInstance(_evaluatorType);
        }
        private static object _evaluator = null;
        private static Type _evaluatorType = null;
        /// <summary>
        /// JScript代码
        /// </summary>
        private static readonly string _JScript =
            @"
				package Microshaoft
				{
					class JScriptEvaluator
					{
						public function Eval(statement : String) : String
						{
							return eval(statement);
						}
					}
				}
			";
    }
}
namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    public static class DynamicCallMethodExpressionTreeInvokerHelper
    {
        public static Func<object[], object> CreateNewInstanceConstructorInvokerFunc
                                                        (
                                                            Type type
                                                            , Func<ConstructorInfo> getConstructorInfoFunc
                                                        )
        {
            var constructorInfo = getConstructorInfoFunc();
            return CreateNewInstanceConstructorInvokerFunc<object>
                                                        (
                                                            type
                                                            , constructorInfo
                                                        );
        }
        public static Func<object[], T> CreateNewInstanceConstructorInvokerFunc<T>
                                                        (
                                                            Type type
                                                            , Func<ConstructorInfo> getConstructorInfoFunc
                                                        )
        {
            var constructorInfo = getConstructorInfoFunc();
            return CreateNewInstanceConstructorInvokerFunc<T>
                                                        (
                                                            type
                                                            , constructorInfo
                                                        );
        }
        public static Func<object[], object> CreateNewInstanceConstructorInvokerFunc
                                                        (
                                                            Type type
                                                            , ConstructorInfo constructorInfo
                                                        )
        {
            return CreateNewInstanceConstructorInvokerFunc<object>(type, constructorInfo);
        }
        public static Func<object[], T> CreateNewInstanceConstructorInvokerFunc<T>
                                                        (
                                                            Type type
                                                            , ConstructorInfo constructorInfo
                                                        )
        {
            var parametersInfos = constructorInfo.GetParameters();
            var constructorParametersExpressions = new List<ParameterExpression>();
            int i = 0;
            Array.ForEach
                    (
                        parametersInfos
                        , (x) =>
                        {
                            var parameterExpression = Expression.Parameter
                                                                    (
                                                                        x.ParameterType
                                                                        , "p" + i.ToString()
                                                                    );
                            constructorParametersExpressions.Add(parameterExpression);
                            i++;
                        }
                    );
            var newExpression = Expression.New(constructorInfo, constructorParametersExpressions);
            var inner = Expression.Lambda(newExpression, constructorParametersExpressions);
            var args = Expression.Parameter(typeof(object[]), "args");
            var body = Expression.Invoke
                                    (
                                        inner
                                        , constructorParametersExpressions.Select
                                                                    (
                                                                        (p, ii) =>
                                                                        {
                                                                            return Expression.Convert
                                                                                            (
                                                                                                Expression.ArrayIndex
                                                                                                                (
                                                                                                                    args
                                                                                                                    , Expression.Constant(ii)
                                                                                                                )
                                                                                                , p.Type
                                                                                            );
                                                                        }
                                                                    ).ToArray()
                                    );
            var outer = Expression.Lambda<Func<object[], T>>(body, args);
            var func = outer.Compile();
            return func;
        }
        public static Action<T, object[]> CreateMethodCallInvokerAction<T>
                                                            (
                                                                Type type
                                                                , Func<MethodInfo> getMethodInfoFunc
                                                            )
        {
            var methodInfo = getMethodInfoFunc();
            return CreateMethodCallInvokerAction<T>
                                                (
                                                    type
                                                    , methodInfo
                                                );
        }
        public static Action<T, object[]> CreateMethodCallInvokerAction<T>
                                                            (
                                                                Type type
                                                                , MethodInfo methodInfo
                                                            )
        {
            ParameterExpression instanceParameterExpression;
            MethodCallExpression methodCallExpression;
            ParameterExpression argumentsParameterExpression = GetMethodArgumentsParameterExpression
                                    (
                                        type
                                        , methodInfo
                                        , out instanceParameterExpression
                                        , out methodCallExpression
                                    );
            var lambda = Expression.Lambda<Action<T, object[]>>(methodCallExpression, instanceParameterExpression, argumentsParameterExpression);
            var action = lambda.Compile();
            return action;
        }
        public static Func<T, object[], TResult> CreateMethodCallInvokerFunc<T, TResult>
                                                            (
                                                                Type type
                                                                , Func<MethodInfo> getMethodInfoFunc
                                                            )
        {
            var methodInfo = getMethodInfoFunc();
            return
                CreateMethodCallInvokerFunc<T, TResult>
                                                    (
                                                        type
                                                        , methodInfo
                                                    );
        }
        public static Func<T, object[], TResult> CreateMethodCallInvokerFunc<T, TResult>
                                                            (
                                                                Type type
                                                                , MethodInfo methodInfo
                                                            )
        {
            ParameterExpression instanceParameterExpression;
            MethodCallExpression methodCallExpression;
            ParameterExpression argumentsParameterExpression = GetMethodArgumentsParameterExpression
                                    (
                                        type
                                        , methodInfo
                                        , out instanceParameterExpression
                                        , out methodCallExpression
                                    );
            var lambda = Expression.Lambda<Func<T, object[], TResult>>(methodCallExpression, instanceParameterExpression, argumentsParameterExpression);
            var func = lambda.Compile();
            return func;
        }
        private static ParameterExpression GetMethodArgumentsParameterExpression
                                            (
                                                Type type
                                                , MethodInfo methodInfo
                                                , out ParameterExpression instanceParameterExpression
                                                , out MethodCallExpression methodCallExpression
                                            )
        {
            var argumentsParameterExpression = Expression.Parameter(typeof(object[]), "args");
            instanceParameterExpression = Expression.Parameter(type, "p");
            UnaryExpression instanceConvertUnaryExpression = null;
            if (!methodInfo.IsStatic)
            {
                instanceConvertUnaryExpression = Expression.Convert(instanceParameterExpression, type);
            }
            var parametersParameterExpressionList = new List<Expression>();
            int i = 0;
            var parametersInfos = methodInfo.GetParameters();
            Array.ForEach
                    (
                        parametersInfos
                        , (x) =>
                        {
                            BinaryExpression valueObject = Expression.ArrayIndex
                                                                        (
                                                                            argumentsParameterExpression
                                                                            , Expression.Constant(i)
                                                                        );
                            UnaryExpression valueCast = Expression.Convert
                                                                        (
                                                                            valueObject
                                                                            , x.ParameterType
                                                                        );
                            parametersParameterExpressionList.Add(valueCast);
                            i++;
                        }
                    );
            methodCallExpression = Expression.Call(instanceConvertUnaryExpression, methodInfo, parametersParameterExpressionList);
            return argumentsParameterExpression;
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.IO;
    using System.Net.Mail;
    using System.Reflection;
    public static partial class ExtensionMethodsManager
    {
        public static byte[] GetBytes(this MailMessage mailMessage)
        {
            Assembly assembly = typeof(SmtpClient).Assembly;
            Type type = assembly.GetType("System.Net.Mail.MailWriter");
            var parametersTypes = new[]
									{ 
										typeof(Stream)
									};
            object x = null;
            using (Stream stream = new MemoryStream())
            {
                var constructorInfo = type.GetConstructor
                                                    (
                                                        BindingFlags.Instance | BindingFlags.NonPublic
                                                        , null
                                                        , parametersTypes
                                                        , null
                                                    );
                var func = DynamicCallMethodExpressionTreeInvokerHelper.CreateNewInstanceConstructorInvokerFunc<object>
                                                        (
                                                            type
                                                            , constructorInfo
                                                        );
                x = func(new[] { stream });
                var action = DynamicCallMethodExpressionTreeInvokerHelper.CreateMethodCallInvokerAction<MailMessage>
                                                        (
                                                            typeof(MailMessage)
                                                            , () =>
                                                            {
                                                                var methodInfo = typeof(MailMessage).GetMethod
                                                                                        (
                                                                                            "Send"
                                                                                            , BindingFlags.NonPublic | BindingFlags.Instance
                                                                                            , null
                                                                                            , new[] { type, typeof(bool) }
                                                                                            , null
                                                                                        );
                                                                return methodInfo;
                                                            }
                                                        );
                action
                    (
                        mailMessage
                        , new[]
							{
								x
								, true
							}
                    );
                byte[] buffer = StreamDataHelper.ReadDataToBytes(stream);
                return buffer;
            }
        }
    }
}
#endif
