namespace ConsoleApplication
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Microshaoft;
    public class Class11
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
																, FF3 = null
															}
															, new Entry2 ()
															{
																F1 = "sadasd"
																, F2 = 10
																, F3 = DateTime.Now
																, FF3 = DateTime.Now
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
										, FF3 = DateTime.Now
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
            var keyWords = new[] { "", "" };
            dataTable = list.AsEnumerable().ToDataTable();
            var l = dataTable.ToList<Entry>();
            l.ForEach
                (
                    (x) =>
                    {
                        x.Entry2S = new[]
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
										};
                    }
                );
            string xml = SerializerHelper.XmlSerializerObjectToXml<List<Entry>>(l, Encoding.UTF8);
            Console.WriteLine(xml);
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
    using System.Reflection;
    using System.ComponentModel;
    public static partial class ExtensionMethodsManager
    {
        public static DataRow[] FullTextSearch(this DataTable dataTable, string[] keyWords)
        {
            return
                dataTable.AsEnumerable().Where<DataRow>
                                        (
                                            (x) =>
                                            {
                                                return
                                                        keyWords.All
                                                                (
                                                                    (xx) =>
                                                                    {
                                                                        return
                                                                                 x.ItemArray
                                                                                        .Select
                                                                                            (
                                                                                                (xxx) =>
                                                                                                {
                                                                                                    return xxx.ToString();
                                                                                                }
                                                                                            )
                                                                                        .Any<string>
                                                                                            (
                                                                                                (xxx) =>
                                                                                                {
                                                                                                    return xxx.Contains(xx);
                                                                                                }
                                                                                            );
                                                                    }
                                                                );
                                            }
                                        ).ToArray();
        }
        private static List<Type> _typesWhiteList = new List<Type>()
														{
															typeof(int)
															, typeof(int?)
															, typeof(long)
															, typeof(long?)
															, typeof(string)
															, typeof(DateTime)
															, typeof(DateTime?)
														};
        private class PropertyAccessor
        {
            public Func<object, object> Getter;
            public Action<object, object> Setter;
            public PropertyInfo Property;
        }
        private static Dictionary
                            <
                                Type
                                , Dictionary
                                        <
                                            string
                                            , PropertyAccessor
                                        >
                            > _typesPropertiesAccessors = new Dictionary<Type, Dictionary<string, PropertyAccessor>>();
        private static Dictionary<string, PropertyAccessor> GetTypePropertiesAccessors(Type type)
        {
            var properties = type.GetProperties();
            Dictionary<string, PropertyAccessor> dictionary = null;
            Array.ForEach
                    (
                        properties
                        , (x) =>
                        {
                            if (
                                    _typesWhiteList.Exists
                                                    (
                                                        (xx) =>
                                                        {
                                                            return xx == x.PropertyType;
                                                        }
                                                    )
                                )
                            {
                                var accessor = new PropertyAccessor()
                                {
                                    Getter = DynamicPropertyAccessor.CreateGetPropertyValueFunc(type, x.Name)
                                    ,
                                    Setter = DynamicPropertyAccessor.CreateSetPropertyValueAction(type, x.Name)
                                    ,
                                    Property = x
                                };
                                if (dictionary == null)
                                {
                                    dictionary = new Dictionary<string, PropertyAccessor>();
                                }
                                dictionary.Add(x.Name, accessor);
                            }
                        }
                    );
            return dictionary;
        }
        public static DataTable ToDataTable<TEntry>(this IEnumerable<TEntry> ie)
        {
            var type = typeof(TEntry);
            var accessors = GetTypePropertiesAccessors(type);
            var accessorsList = accessors.ToList();
            DataTable dataTable = GenerateEmptyDataTable(accessorsList);
            DataColumnCollection dcc = dataTable.Columns;
            if (dataTable != null)
            {
                using (IEnumerator<TEntry> enumerator = ie.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var row = dataTable.NewRow();
                        foreach (DataColumn c in dcc)
                        {
                            PropertyAccessor accessor = null;
                            if (accessors.TryGetValue(c.ColumnName, out accessor))
                            {
                                object v = accessor.Getter(enumerator.Current);
                                if (v == null)
                                {
                                    v = DBNull.Value;
                                }
                                row[c] = v;
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
            return dataTable;
        }
        private static DataTable GenerateEmptyDataTable(List<KeyValuePair<string, PropertyAccessor>> accessorsList)
        {
            DataTable dataTable = null;
            accessorsList
                    .ForEach
                        (
                            (x) =>
                            {
                                if (dataTable == null)
                                {
                                    dataTable = new DataTable();
                                }
                                var propertyType = x.Value.Property.PropertyType;
                                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                                {
                                    NullableConverter nullableConvert = new NullableConverter(propertyType);
                                    propertyType = nullableConvert.UnderlyingType;
                                }
                                var propertyName = x.Value.Property.Name;
                                var column = new DataColumn
                                                    (
                                                        propertyName
                                                        , propertyType
                                                    );
                                dataTable.Columns.Add(column);
                            }
                        );
            return dataTable;
        }
        public static DataTable ToDataTable<TEntry>(this List<TEntry> list)
        // where TEntry : new()
        {
            var type = typeof(TEntry);
            var accessors = GetTypePropertiesAccessors(type);
            var accessorsList = accessors.ToList();
            DataTable dataTable = GenerateEmptyDataTable(accessorsList);
            DataColumnCollection dcc = dataTable.Columns;
            if (dataTable != null)
            {
                list.ForEach
                        (
                            (x) =>
                            {
                                var row = dataTable.NewRow();
                                foreach (DataColumn c in dcc)
                                {
                                    PropertyAccessor accessor = null;
                                    if (accessors.TryGetValue(c.ColumnName, out accessor))
                                    {
                                        object v = accessor.Getter(x);
                                        if (v == null)
                                        {
                                            v = DBNull.Value;
                                        }
                                        row[c] = v;
                                    }
                                }
                                dataTable.Rows.Add(row);
                            }
                        );
            }
            return dataTable;
        }
        public static List<TEntry> ToList<TEntry>(this DataTable dataTable)
                                            where TEntry : new()
        {
            var type = typeof(TEntry);
            var columns = dataTable.Columns;
            var actions = new Dictionary<string, Action<object, object>>();
            foreach (DataColumn c in columns)
            {
                var columnName = c.ColumnName;
                var action = DynamicPropertyAccessor.CreateSetPropertyValueAction
                                                (
                                                    typeof(TEntry)
                                                    , columnName
                                                );
                actions[columnName] = action;
            }
            List<TEntry> list = null;
            var rows = dataTable.Rows;
            foreach (DataRow r in rows)
            {
                var entry = new TEntry();
                if (list == null)
                {
                    list = new List<TEntry>();
                }
                foreach (DataColumn c in columns)
                {
                    var columnName = c.ColumnName;
                    var v = r[columnName];
                    if (!DBNull.Value.Equals(v))
                    {
                        var action = actions[columnName];
                        action(entry, v);
                    }
                }
                list.Add(entry);
            }
            return list;
        }
    }
}




