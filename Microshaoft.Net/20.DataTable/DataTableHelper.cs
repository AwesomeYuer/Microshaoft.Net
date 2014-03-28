namespace ConsoleApplication
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Microshaoft;
    public class Class111
    {
        //[STAThread]
        [Serializable]
        public class Entry
        {
            [XmlElement("F1")]
            public string F1 { get; set; }
            [XmlElement("F2")]
            public int F2 { get; set; }
            [XmlAttribute("F3")]
            public DateTime F3 { get; set; }
            public DateTime? FF3 { get; set; }
            [XmlArrayItem("Entry2", typeof(Entry2))]
            [XmlArray("Entry2S")]
            public Entry2[] Entry2S { get; set; }
        };
        public class Entry2
        {
            [XmlElement("F1")]
            public string F1 { get; set; }
            [XmlElement("F2")]
            public int F2 { get; set; }
            [XmlAttribute("F3")]
            public DateTime F3 { get; set; }
            public DateTime? FF3 { get; set; }
        };
        static void Main(string[] args)
        {
            var list = new List<Entry>()
							{
								new Entry() 
									{
										F1 = "a"
										, F2= 1
										, F3 = DateTime.Now
										, FF3 = null
										, Entry2S = new []
														{
															new Entry2 ()
															{
																F1 = "sadasd"
																, F2 = 10
																, F3 = DateTime.Now
															}
															, new Entry2 ()
															{
																F1 = "sadasd"
																, F2 = 10
																, F3 = DateTime.Now
															}
															, new Entry2 ()
															{
																F1 = "sadasd"
																, F2 = 10
																, F3 = DateTime.Now
															}
														}
									}
								,new Entry() 
									{
										F1= "b"
										, F2= 2
										, F3 = DateTime.Now
										, FF3 = null
										, Entry2S = new []
													{
														new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
														, new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
														, new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
													}
									}
								,new Entry() 
									{
										F1= "c"
										, F2= 3
										, F3 = DateTime.Now
										, FF3 = null
										, Entry2S = new []
													{
														new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
														, new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
														, new Entry2 ()
														{
															F1 = "sadasd"
															, F2 = 10
															, F3 = DateTime.Now
														}
													}
									}
							};
            var dataTable = list.ToDataTable<Entry>();
            DataTableHelper.DataTableRowsForEach
                                (
                                    dataTable
                                    , (x, y) =>
                                    {
                                        Console.WriteLine("{1}{0}{2}", " : ", y, x.ColumnName);
                                        return false;
                                    }
                                    , (x) =>
                                    {
                                        Console.WriteLine("{1}", " : ", x.Count);
                                        return false;
                                    }
                                    , (x, y, z, w) =>
                                    {
                                        Console.WriteLine("{1}{0}{2}{0}{3}{0}{4}", " : ", x.ColumnName, y, z, w);
                                        return false;
                                    }
                                    , (x, y, z) =>
                                    {
                                        Console.WriteLine("{1}{0}{2}", " : ", x.Count, z);
                                        return false;
                                    }
                                );
            dataTable = DataTableHelper.GenerateEmptyDataTable<Entry>();
            Console.ReadLine();
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    public static class DataTableHelper
    {
        private static List<Type> _typesWhiteList = new List<Type>()
														{
															typeof(int)
															//, typeof(int?)
															, typeof(long)
															//, typeof(long?)
															, typeof(string)
															, typeof(DateTime)
															//, typeof(DateTime?)
														};
        public static DataTable GenerateEmptyDataTable<T>()
        {
            var type = typeof(T);
            return GenerateEmptyDataTable(type);
        }
        public static DataTable GenerateEmptyDataTable(Type type)
        {
            var properties = type.GetProperties().Where
                                                    (
                                                        (x) =>
                                                        {
                                                            return
                                                                _typesWhiteList.Any
                                                                                 (
                                                                                    (xx) =>
                                                                                    {
                                                                                        return x.PropertyType == xx;
                                                                                    }
                                                                                 );
                                                        }
                                                    )
                                                    .ToList();
            DataTable dataTable = null;
            DataColumnCollection dataColumnsCollection = null;
            properties.ForEach
                        (
                            (x) =>
                            {
                                if (dataTable == null)
                                {
                                    dataTable = new DataTable();
                                }
                                if (dataColumnsCollection == null)
                                {
                                    dataColumnsCollection = dataTable.Columns;
                                }
                                dataColumnsCollection.Add
                                                    (
                                                        x.Name
                                                        , x.PropertyType
                                                    );
                            }
                        );
            return dataTable;
        }
        public static void DataTableRowsForEach
                                        (
                                            DataTable dataTable
                                            , Func<DataColumn, int, bool> processHeaderDataColumnFunc = null
                                            , Func<DataColumnCollection, bool> processHeaderDataColumnsFunc = null
                                            , Func<DataColumn, int, object, int, bool> processRowDataColumnFunc = null
                                            , Func<DataColumnCollection, DataRow, int, bool> processRowFunc = null
                                        )
        {
            DataColumnCollection dataColumnCollection = null;
            int i = 0;
            bool r = false;
            if (processHeaderDataColumnFunc != null)
            {
                dataColumnCollection = dataTable.Columns;
                foreach (DataColumn dc in dataColumnCollection)
                {
                    i++;
                    r = processHeaderDataColumnFunc(dc, i);
                    if (r)
                    {
                        break;
                    }
                }
            }
            if (processHeaderDataColumnsFunc != null)
            {
                if (dataColumnCollection == null)
                {
                    dataColumnCollection = dataTable.Columns;
                }
                r = processHeaderDataColumnsFunc(dataColumnCollection);
                if (r)
                {
                    return;
                }
            }
            DataRowCollection drc = null;
            if
                (
                    processRowDataColumnFunc != null
                    || processRowFunc != null
                )
            {
                drc = dataTable.Rows;
                if
                    (
                        (
                            processRowDataColumnFunc != null
                            || processRowFunc != null
                        )
                        && dataColumnCollection == null
                    )
                {
                    dataColumnCollection = dataTable.Columns;
                }
                i = 0;
                var j = 0;
                foreach (DataRow dataRow in drc)
                {
                    i++;
                    foreach (DataColumn dc in dataColumnCollection)
                    {
                        if (processRowDataColumnFunc != null)
                        {
                            j++;
                            r = processRowDataColumnFunc
                                            (
                                                dc
                                                , j
                                                , dataRow[dc]
                                                , i
                                            );
                            if (r)
                            {
                                j = 0;
                                break;
                            }
                        }
                    }
                    if (processRowFunc != null)
                    {
                        processRowFunc(dataColumnCollection, dataRow, i);
                    }
                }
            }
        }
    }
}


