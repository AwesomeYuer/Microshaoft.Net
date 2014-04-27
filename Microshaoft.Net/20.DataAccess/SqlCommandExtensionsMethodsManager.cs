namespace Microshaoft
{
    using System;
    using System.Data;
    using System.Data.SqlClient;

    public static class SqlCommandExtensionsMethodsManager
    {
        public static void AttachPager
                (
                    this SqlCommand command
                    , int pageFetchRows
                    , Func<int, DataTable, bool> onPageProcessFunc
                )
        {
            SqlConnection connection = command.Connection;
            SqlParameter parameterOffsetRows = command.Parameters.Add("@OffsetRows", SqlDbType.Int);
            SqlParameter parameterFetchRows = command.Parameters.Add("@FetchRows", SqlDbType.Int);
            SqlParameter parameterTotalRows = command.Parameters.Add("@TotalRows", SqlDbType.Int);
            parameterTotalRows.Direction = ParameterDirection.InputOutput;
            SqlParameter parameterIsLast = command.Parameters.Add("@IsLast", SqlDbType.Bit);
            parameterIsLast.Direction = ParameterDirection.Output;
            int p_OffsetRows = 0;
            bool p_IsLast = false;
            int p_TotalRows = -1;
            int page = 0;
            parameterFetchRows.Value = pageFetchRows;
            DataTable dataTable = null;
            while (!p_IsLast)
            {
                parameterOffsetRows.Value = p_OffsetRows;
                parameterFetchRows.Value = pageFetchRows;
                using (var sqlDataAdapter = new SqlDataAdapter(command))
                {
                    using (DataSet dataSet = new DataSet())
                    {
                        sqlDataAdapter.Fill(dataSet);
                        dataTable = dataSet.Tables[0];
                        if (parameterTotalRows.Value != DBNull.Value)
                        {
                            p_TotalRows = ((int)(parameterTotalRows.Value));
                        }
                        if (parameterIsLast.Value != DBNull.Value)
                        {
                            p_IsLast = ((bool)(parameterIsLast.Value));
                        }
                        connection.Close();
                    }
                }
                var r = false;
                if (dataTable != null)
                {
                    if (dataTable.Rows.Count > 0)
                    {
                        r = onPageProcessFunc(++page, dataTable);
                    }
                }
                if (r)
                {
                    break;
                }
                p_OffsetRows += pageFetchRows;
            }
        }
    }
}
