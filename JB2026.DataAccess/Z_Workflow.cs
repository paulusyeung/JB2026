using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class Z_Workflow
    {
        private Guid key = Guid.Empty;
        private Guid workflowId = Guid.Empty;
        private string workflowName = string.Empty;
        private string workTitle = string.Empty;
        private string workInstruction = string.Empty;

        public Z_Workflow() { }

        public static Z_Workflow? Load(Guid workflowId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Workflow_SelRec", new SqlParameter[] { new SqlParameter("@WorkflowId", workflowId) });
            if (reader.Read()) { var r = new Z_Workflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_Workflow? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Workflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Z_Workflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_WorkflowCollection LoadCollection()
            => LoadCollection("spZ_Workflow_SelAll", new SqlParameter[] { });

        public static Z_WorkflowCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Workflow_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_WorkflowCollection LoadCollection(string whereClause)
            => LoadCollection("spZ_Workflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static Z_WorkflowCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Workflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_WorkflowCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new Z_WorkflowCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Z_Workflow(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid workflowId)
            => SqlHelper.Default.ExecuteNonQuery("spZ_Workflow_DelRec", new SqlParameter[] { new SqlParameter("@WorkflowId", workflowId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) workflowId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) workflowName = reader.GetString(1);
                if (!reader.IsDBNull(2)) workTitle = reader.GetString(2);
                if (!reader.IsDBNull(3)) workInstruction = reader.GetString(3);
            }
        }

        public void Delete() => Delete(this.WorkflowId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != WorkflowId) this.Delete(); Update(); }
        }

        public Guid WorkflowId { get { return workflowId; } set { workflowId = value; } }
        public string WorkflowName { get { return workflowName; } set { workflowName = value; } }
        public string WorkTitle { get { return workTitle; } set { workTitle = value; } }
        public string WorkInstruction { get { return workInstruction; } set { workInstruction = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spZ_Workflow_InsRec", "@WorkflowId", out var rv, GetInsertParameterValues());
            workflowId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spZ_Workflow_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@WorkflowId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameter("@WorkflowName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.WorkflowName),
            GetSqlParameter("@WorkTitle", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.WorkTitle),
            GetSqlParameter("@WorkInstruction", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.WorkInstruction)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@WorkflowId", SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameterWithoutDirection("@WorkflowName", SqlDbType.NVarChar, 64, this.WorkflowName),
            GetSqlParameterWithoutDirection("@WorkTitle", SqlDbType.NVarChar, 512, this.WorkTitle),
            GetSqlParameterWithoutDirection("@WorkInstruction", SqlDbType.NVarChar, 512, this.WorkInstruction)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("workflowId: " + workflowId).Append("\r\n");
            b.Append("workflowName: " + workflowName).Append("\r\n");
            b.Append("workTitle: " + workTitle).Append("\r\n");
            b.Append("workInstruction: " + workInstruction).Append("\r\n");
            return b.ToString();
        }
    }

    public class Z_WorkflowCollection : BindingList<Z_Workflow> { }
}
