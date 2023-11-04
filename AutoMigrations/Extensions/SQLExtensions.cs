using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.Data;
using System.Data.Common;

namespace AutoMigrations.Extensions
{
    internal static class SQLExtensions
    {
        public static string GetRenameSQL(this DbContext context, RenameColumnOperation opt)
        {
            //query table infomation
            string sql =
                $"select column_type from information_schema.columns where table_name='{opt.Table}' and column_name='{opt.Name}'";

            DataTable dataTable = context.ExecuteQuerySQL(sql);

            string column_type =
                dataTable != null && dataTable.Rows.Count > 0
                    ? dataTable.Rows[0]!.ItemArray[0]!.ToString()!
                    : string.Empty;

            sql = $"alter table {opt.Table} change column {opt.Name} {opt.NewName} {column_type} ;";

            return sql.Trim();
        }

        public static DataTable ExecuteQuerySQL(
            this DbContext context,
            string commandText,
            params object[] commandParameters
        )
        {
            DbConnection connection = context.Database.GetDbConnection();

            try
            {
                using DbCommand cmd = connection.CreateCommand();

                cmd.CommandText = commandText;

                if (commandParameters != null && commandParameters.Length > 0)
                {
                    cmd.Parameters.AddRange(commandParameters);
                }

                connection.Open();

                using DbDataReader reader = cmd.ExecuteReader();

                DataTable dt = new();

                dt.Load(reader);

                return dt;
            }
            finally
            {
                connection.Close();
            }
        }

        public static int ExecuteSQL(this DbContext context, ICollection<string> commandTexts)
        {
            int executeResult = -1;

            if (commandTexts is null || commandTexts.Count == 0)
            {
                return executeResult;
            }

            DbConnection connection = context.Database.GetDbConnection();

            try
            {
                connection.Open();

                using DbCommand executeCommand = connection.CreateCommand();

                foreach (string commandText in commandTexts)
                {
                    executeCommand.CommandText = commandText;

                    executeResult += executeCommand.ExecuteNonQuery();
                }
            }
            finally
            {
                connection.Close();
            }

            return executeResult;
        }
    }
}
