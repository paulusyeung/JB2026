using System.Data;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class SqlHelper
    {
        private static SqlHelper? _default;

        public static SqlHelper Default
        {
            get
            {
                _default ??= new SqlHelper();
                return _default;
            }
        }

        private string ConnectionString => Common.Config.ConnectionString;

        public SqlDataReader ExecuteReader(string storedProcName, SqlParameter[] parameters)
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            var cmd = new SqlCommand(storedProcName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = Common.Config.CommandTimedOut;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public void ExecuteNonQuery(string storedProcName, SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(storedProcName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = Common.Config.CommandTimedOut;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }

        public void ExecuteNonQuery(string storedProcName, string outputParameterName, out object returnedValue, SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(storedProcName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = Common.Config.CommandTimedOut;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
            returnedValue = cmd.Parameters[outputParameterName].Value;
        }
    }
}
