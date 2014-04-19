namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlTypes;
    using System.Linq;
    public static class DataTableHelper
    {
        private static List<Type> _typesWhiteList =
                            new Func<List<Type>>
                                    (
                                        () =>
                                        {
                                            var r = new List<Type>()
												{
													typeof(int)
													//, typeof(int?)
													, typeof(long)
													//, typeof(long?)
													, typeof(string)
													, typeof(DateTime)
													//, typeof(DateTime?)
												};
                                            var sqlTypes = AssemblyHelper.GetAssembliesTypes
                                                    (
                                                        (x, y) =>
                                                        {
                                                            return
                                                                (
                                                                    x.Namespace == "System.Data.SqlTypes"
                                                                    && x.IsValueType
                                                                    && typeof(INullable).IsAssignableFrom(x)
                                                                );
                                                        }
                                                    );
                                            r.AddRange(sqlTypes);
                                            return r;
                                        }
                                    )();
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
            var dataColumns
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
                        .Select
                        (
                            (x) =>
                            {
                                var columnID = 0;
                                var columnName = x.Name;
                                var columnType = x.PropertyType;
                                var attribute = x.GetCustomAttributes(typeof(DataTableColumnDefinitionAttribute), false).FirstOrDefault(); //as DataTableColumnIDAttribute;
                                if (attribute != null)
                                {
                                    var dataTableColumnDefinitionAttribute = attribute as DataTableColumnDefinitionAttribute;
                                    if (dataTableColumnDefinitionAttribute != null)
                                    {
                                        if (dataTableColumnDefinitionAttribute.ColumnID != null)
                                        {
                                            columnID = dataTableColumnDefinitionAttribute.ColumnID.Value;
                                        }
                                        if
                                            (
                                                !string.IsNullOrEmpty(dataTableColumnDefinitionAttribute.ColumnName)
                                            )
                                        {
                                            columnName = dataTableColumnDefinitionAttribute.ColumnName;
                                        }
                                        if (dataTableColumnDefinitionAttribute.ColumnSqlType != null)
                                        {
                                            columnType = dataTableColumnDefinitionAttribute.ColumnSqlType;
                                        }
                                    }

                                }
                                if (TypeHelper.IsNullableType(columnType))
                                {
                                    columnType = TypeHelper.GetNullableTypeUnderlyingType(columnType);
                                }
                                var r = new
                                {
                                    ColumnID = columnID
                                    ,
                                    ColumnName = columnName
                                    ,
                                    ColumnType = columnType
                                };
                                return r;
                            }
                        )
                        .OrderBy
                        (
                            (x) =>
                            {
                                return x.ColumnID;
                            }
                        )
                        .ToList();

            DataTable dataTable = null;
            DataColumnCollection dataColumnsCollection = null;
            dataColumns.ForEach
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
                                                        x.ColumnName
                                                        , x.ColumnType
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