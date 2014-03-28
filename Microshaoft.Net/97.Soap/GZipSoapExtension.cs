namespace Microshaoft
{
    using System;
    using System.IO;
    using System.Web.Services.Protocols;
    public class GZipSoapExtension : SoapExtension
    {
        private Stream _originalStream;
        private Stream _workStream;
        public override Stream ChainStream(Stream stream)
        {
            _originalStream = stream;
            _workStream = new MemoryStream();
            return _workStream;
        }
        public override object GetInitializer
                                    (
                                        LogicalMethodInfo methodInfo
                                        , SoapExtensionAttribute attribute
                                    )
        {
            return null;
        }
        public override object GetInitializer(Type WebServiceType)
        {
            return null;
        }
        public override void Initialize(object initializer)
        {
            //to do ...
        }
        public override void ProcessMessage(SoapMessage message)
        {
            switch (message.Stage)
            {
                case SoapMessageStage.BeforeSerialize:
                    break;
                case SoapMessageStage.AfterSerialize:
                    CompressStream();
                    break;
                case SoapMessageStage.BeforeDeserialize:
                    DecompressStream();
                    break;
                case SoapMessageStage.AfterDeserialize:
                    break;
                default:
                    throw new Exception("invalid stage");
            }
        }
        public void CompressStream()
        {
            //压缩 响应
            Stream stream = CompressHelper.GZipCompress(_workStream);
            byte[] buffer = StreamDataHelper.ReadDataToBytes(stream);
            _originalStream.Write(buffer, 0, buffer.Length);
        }
        public void DecompressStream()
        {
            //解压 请求
            byte[] bytes = StreamDataHelper.ReadDataToBytes(_originalStream);
            bytes = CompressHelper.GZipDecompress(bytes);
            _workStream.Write(bytes, 0, bytes.Length);
            _workStream.Position = 0;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class GZipSoapExtensionAttribute : SoapExtensionAttribute
    {
        private int _priority;
        public override int Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                _priority = value;
            }
        }
        public override Type ExtensionType
        {
            get
            {
                return typeof(GZipSoapExtension);
            }
        }
    }
}

//==================================================================================================
// WebService.asmx
//<%@ WebService Language="c#" Class="Microshaoft.Service1Class" Debug="true"%>
namespace Microshaoft
{
    using System.Diagnostics;
    using System.Web;
    using System.Web.Services;
    using System.Collections.Generic;
    using System.Data;
    [WebService(Namespace = "http://www.microshaoft.com/")]
    public class Service1Class : WebService
    {
        [WebMethodAttribute]
        [Microshaoft.GZipSoapExtension()]
        public string HelloWorld(string x)
        {
            return string.Format("hello {0}", x);// + a;
        }
        [WebMethod]
        [Microshaoft.GZipSoapExtension()]
        public DataSet HelloWorld1(DataSet x)
        {
            x.Tables[0].Rows[0][1] += "\tserver";
            return x;//string.Format("hello {0}", x);// + a;
        }
    }
    public class aaResponse
    {
        public string Name;
        public int age;
        public byte[] data;
    }
    public class aaRequest
    {
        public string Name;
        public int age;

    }
}
//==================================================================================================
//Proxy Client
namespace ConsoleApplication
{
    using System;
    using System.Data;
    using Proxy;
    /// <summary>
    /// Class1 的摘要说明。
    /// </summary>
    public class Class1111
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        //[STAThread]
        static void Main(string[] args)
        {
            //
            // TODO: 在此处添加代码以启动应用程序
            //
            Service1Class x = new Service1Class();
            string s = x.HelloWorld("于溪玥");
            Console.WriteLine(s);
            DataSet d = x.HelloWorld1(CreateDataSet());
            string ss = d.Tables[0].Rows[0][1].ToString();
            Console.WriteLine(ss);
            Console.WriteLine(Environment.Version.ToString());
        }
        private static DataSet CreateDataSet()
        {
            DataSet dataSet = new DataSet();
            // Create two DataTable objects using a function.
            DataTable table1 = MakeTable("idTable1", "thing1");
            DataTable table2 = MakeTable("idTable2", "thing2");
            dataSet.Tables.Add(table1);
            dataSet.Tables.Add(table2);
            return dataSet;
            //Console.WriteLine(dataSet.Tables.Count)
        }
        private static DataTable MakeTable
                            (
                                string c1Name
                                , string c2Name
                            )
        {
            DataTable table = new DataTable();
            //' Add two DataColumns
            DataColumn column = new DataColumn(c1Name, typeof(int));
            table.Columns.Add(column);
            column = new DataColumn(c2Name, typeof(string));
            table.Columns.Add(column);
            table.Rows.Add(1, "aa");
            table.Rows.Add(2, "bb");
            return table;
        }
    }
}
namespace Proxy
{
    //------------------------------------------------------------------------------
    // <auto-generated>
    //	 此代码由工具生成。
    //	 运行时版本:2.0.50727.3053
    //
    //	 对此文件的更改可能会导致不正确的行为，并且如果
    //	 重新生成代码，这些更改将会丢失。
    // </auto-generated>
    //------------------------------------------------------------------------------
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Web.Services;
    using System.Web.Services.Protocols;
    using System.Xml.Serialization;
    // 
    // 此源代码由 wsdl 自动生成, Version=2.0.50727.42。
    // 

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name = "Service1ClassSoap", Namespace = "http://www.microshaoft.com/")]
    public partial class Service1Class : System.Web.Services.Protocols.SoapHttpClientProtocol
    {

        private System.Threading.SendOrPostCallback HelloWorldOperationCompleted;

        private System.Threading.SendOrPostCallback HelloWorld1OperationCompleted;

        /// <remarks/>
        public Service1Class()
        {
            this.Url = "http://test.v20.asp.net/SoapExtension/Noname1.asmx";
        }

        /// <remarks/>
        public event HelloWorldCompletedEventHandler HelloWorldCompleted;

        /// <remarks/>
        public event HelloWorld1CompletedEventHandler HelloWorld1Completed;

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.microshaoft.com/HelloWorld", RequestNamespace = "http://www.microshaoft.com/", ResponseNamespace = "http://www.microshaoft.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [Microshaoft.GZipSoapExtension()]
        public string HelloWorld(string x)
        {
            object[] results = this.Invoke("HelloWorld", new object[] {
						x});
            return ((string)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginHelloWorld(string x, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("HelloWorld", new object[] {
						x}, callback, asyncState);
        }

        /// <remarks/>
        public string EndHelloWorld(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((string)(results[0]));
        }

        /// <remarks/>
        public void HelloWorldAsync(string x)
        {
            this.HelloWorldAsync(x, null);
        }

        /// <remarks/>
        public void HelloWorldAsync(string x, object userState)
        {
            if ((this.HelloWorldOperationCompleted == null))
            {
                this.HelloWorldOperationCompleted = new System.Threading.SendOrPostCallback(this.OnHelloWorldOperationCompleted);
            }
            this.InvokeAsync("HelloWorld", new object[] {
						x}, this.HelloWorldOperationCompleted, userState);
        }

        private void OnHelloWorldOperationCompleted(object arg)
        {
            if ((this.HelloWorldCompleted != null))
            {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.HelloWorldCompleted(this, new HelloWorldCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }

        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://www.microshaoft.com/HelloWorld1", RequestNamespace = "http://www.microshaoft.com/", ResponseNamespace = "http://www.microshaoft.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        [Microshaoft.GZipSoapExtension()]
        public System.Data.DataSet HelloWorld1(System.Data.DataSet x)
        {
            object[] results = this.Invoke("HelloWorld1", new object[] {
						x});
            return ((System.Data.DataSet)(results[0]));
        }

        /// <remarks/>
        public System.IAsyncResult BeginHelloWorld1(System.Data.DataSet x, System.AsyncCallback callback, object asyncState)
        {
            return this.BeginInvoke("HelloWorld1", new object[] {
						x}, callback, asyncState);
        }

        /// <remarks/>
        public System.Data.DataSet EndHelloWorld1(System.IAsyncResult asyncResult)
        {
            object[] results = this.EndInvoke(asyncResult);
            return ((System.Data.DataSet)(results[0]));
        }

        /// <remarks/>
        public void HelloWorld1Async(System.Data.DataSet x)
        {
            this.HelloWorld1Async(x, null);
        }

        /// <remarks/>
        public void HelloWorld1Async(System.Data.DataSet x, object userState)
        {
            if ((this.HelloWorld1OperationCompleted == null))
            {
                this.HelloWorld1OperationCompleted = new System.Threading.SendOrPostCallback(this.OnHelloWorld1OperationCompleted);
            }
            this.InvokeAsync("HelloWorld1", new object[] {
						x}, this.HelloWorld1OperationCompleted, userState);
        }

        private void OnHelloWorld1OperationCompleted(object arg)
        {
            if ((this.HelloWorld1Completed != null))
            {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.HelloWorld1Completed(this, new HelloWorld1CompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }

        /// <remarks/>
        public new void CancelAsync(object userState)
        {
            base.CancelAsync(userState);
        }
    }
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void HelloWorldCompletedEventHandler(object sender, HelloWorldCompletedEventArgs e);
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class HelloWorldCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {

        private object[] results;

        internal HelloWorldCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
            base(exception, cancelled, userState)
        {
            this.results = results;
        }

        /// <remarks/>
        public string Result
        {
            get
            {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void HelloWorld1CompletedEventHandler(object sender, HelloWorld1CompletedEventArgs e);
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class HelloWorld1CompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {

        private object[] results;

        internal HelloWorld1CompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
            base(exception, cancelled, userState)
        {
            this.results = results;
        }

        /// <remarks/>
        public System.Data.DataSet Result
        {
            get
            {
                this.RaiseExceptionIfNecessary();
                return ((System.Data.DataSet)(this.results[0]));
            }
        }
    }
}

/*
<webServices>
	<protocols>
		<add name="protocol name"/>
		<remove name="protocol name"/>
		<clear/>
	</protocols>
	<serviceDescriptionFormatExtensionTypes> 
		<add type="type"/>
		<remove type="type"/>
		<clear/>
	</serviceDescriptionFormatExtensionTypes>
	<soapExtensionTypes>
		<add type="type" priority="number" group="0|1"/>
		<remove type="type"/>
		<clear/>
	</soapExtensionTypes>
	<soapExtensionReflectorTypes>
		<add type="type" priority="number" group="0|1"/>
		<remove type="type"/>
		<clear/>
	</soapExtensionReflectorTypes>
	<soapExtensionImporterTypes>
		<add type="type" priority="number" group="0|1"/>
		<remove type="type"/>
		<clear/>
	</soapExtensionImporterTypes>
	<wsdlHelpGenerator href="help generator file"/>
	<diagnostics suppressReturningExceptions="true|false" />
</webServices>
*/