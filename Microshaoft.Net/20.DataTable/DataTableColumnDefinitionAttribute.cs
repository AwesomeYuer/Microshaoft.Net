namespace Microshaoft
{
    using System;
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public class DataTableColumnDefinitionAttribute : Attribute
    {
        public DataTableColumnDefinitionAttribute(int columnID)
        {
            ColumnID = columnID;
        }
        public DataTableColumnDefinitionAttribute(int columnID, string columnName)
            : this(columnID)
        {
            ColumnName = columnName;
        }
        public DataTableColumnDefinitionAttribute(int columnID, string columnName, Type columnSqlType)
            : this(columnID, columnName)
        {
            ColumnSqlType = columnSqlType;
        }
        public int? ColumnID
        {
            get;
            private set;
        }
        public string ColumnName
        {
            get;
            private set;
        }
        public Type ColumnSqlType
        {
            get;
            private set;
        }
    }
}