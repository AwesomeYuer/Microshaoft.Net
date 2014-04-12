namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

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

        public static DataTable GenerateEmptyDataTable
                (
                    Type type

                )
        {
            var properties
                = type
                    .GetProperties()
                        .Where
                            (
                                (x) =>
                                {
                                    var r =
                                        _typesWhiteList.Any
                                                            (
                                                                (xx) =>
                                                                {
                                                                    var propertyType = x.PropertyType;
                                                                    if (TypeHelper.IsNullableType(propertyType))
                                                                    {
                                                                        propertyType = TypeHelper.GetNullableTypeUnderlyingType(propertyType);
                                                                    }
                                                                    var rr = (propertyType == xx);
                                                                    return rr;
                                                                }
                                                            );
                                    return r;
                                }
                            )
                            .OrderBy
                            (
                                (x) =>
                                {
                                    var r = 0;
                                    var attribute = x.GetCustomAttributes(typeof(DataTableColumnDefinitionAttribute), false).FirstOrDefault(); //as DataTableColumnIDAttribute;
                                    if (attribute != null)
                                    {
                                        var dataTableColumnDefinitionAttribute = attribute as DataTableColumnDefinitionAttribute;
                                        if (dataTableColumnDefinitionAttribute != null)
                                        {
                                            r = dataTableColumnDefinitionAttribute.ColumnID;
                                        }
                                    }
                                    return r;
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
                                Type propertyType = x.PropertyType;
                                if (propertyType.IsGenericType)
                                {
                                    propertyType = Nullable.GetUnderlyingType(propertyType);
                                }
                                var columnName = x.Name;
                                var attribute = x.GetCustomAttributes(typeof(DataTableColumnDefinitionAttribute), false).FirstOrDefault(); //as DataTableColumnIDAttribute;

                                if (attribute != null)
                                {
                                    var dataTableColumnDefinitionAttribute = attribute as DataTableColumnDefinitionAttribute;
                                    if (dataTableColumnDefinitionAttribute != null)
                                    {
                                        if (!string.IsNullOrEmpty(dataTableColumnDefinitionAttribute.ColumnName))
                                        {
                                            columnName = dataTableColumnDefinitionAttribute.ColumnName;
                                        }

                                    }
                                }
                                dataColumnsCollection.Add
                                                    (
                                                        columnName
                                                        , propertyType
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
namespace Microshaoft
{
    using System;
    using System.Net;
    using Newtonsoft.Json;
    using System.Collections;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public class DataTableColumnDefinitionAttribute : Attribute
    {

        public DataTableColumnDefinitionAttribute(int columnID)
        {
            ColumnID = columnID;
        }
        public int ColumnID
        {
            get;
            private set;
        }
        public string ColumnName
        {
            get;
            private set;
        }
        public DataTableColumnDefinitionAttribute(int columnID, string columnName)
        {
            ColumnID = columnID;
            ColumnName = columnName;
        }

    }
}

