using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class Z_OrderTypeWorkflow
    {
        private Guid key = Guid.Empty;
        private Guid orderTypeWorkflowId = Guid.Empty;
        private Guid workflowId = Guid.Empty;
        private int orderType;
        private int workIndex;

        public Z_OrderTypeWorkflow() { }

        public static Z_OrderTypeWorkflow? Load(Guid orderTypeWorkflowId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_OrderTypeWorkflow_SelRec", new SqlParameter[] { new SqlParameter("@OrderTypeWorkflowId", orderTypeWorkflowId) });
            if (reader.Read()) { var r = new Z_OrderTypeWorkflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_OrderTypeWorkflow? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_OrderTypeWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Z_OrderTypeWorkflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_OrderTypeWorkflowCollection LoadCollection()
            => LoadCollection("spZ_OrderTypeWorkflow_SelAll", new SqlParameter[] { });

        public static Z_OrderTypeWorkflowCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_OrderTypeWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_OrderTypeWorkflowCollection LoadCollection(string whereClause)
            => LoadCollection("spZ_OrderTypeWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static Z_OrderTypeWorkflowCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_OrderTypeWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_OrderTypeWorkflowCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new Z_OrderTypeWorkflowCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Z_OrderTypeWorkflow(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid orderTypeWorkflowId)
            => SqlHelper.Default.ExecuteNonQuery("spZ_OrderTypeWorkflow_DelRec", new SqlParameter[] { new SqlParameter("@OrderTypeWorkflowId", orderTypeWorkflowId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) orderTypeWorkflowId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) workflowId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) orderType = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) workIndex = reader.GetInt32(3);
            }
        }

        public void Delete() => Delete(this.OrderTypeWorkflowId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != OrderTypeWorkflowId) this.Delete(); Update(); }
        }

        public Guid OrderTypeWorkflowId { get { return orderTypeWorkflowId; } set { orderTypeWorkflowId = value; } }
        public Guid WorkflowId { get { return workflowId; } set { workflowId = value; } }
        public int OrderType { get { return orderType; } set { orderType = value; } }
        public int WorkIndex { get { return workIndex; } set { workIndex = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spZ_OrderTypeWorkflow_InsRec", "@OrderTypeWorkflowId", out var rv, GetInsertParameterValues());
            orderTypeWorkflowId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spZ_OrderTypeWorkflow_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@OrderTypeWorkflowId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.OrderTypeWorkflowId),
            GetSqlParameter("@WorkflowId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameter("@OrderType", ParameterDirection.Input, SqlDbType.Int, 4, this.OrderType),
            GetSqlParameter("@WorkIndex", ParameterDirection.Input, SqlDbType.Int, 4, this.WorkIndex)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@OrderTypeWorkflowId", SqlDbType.UniqueIdentifier, 16, this.OrderTypeWorkflowId),
            GetSqlParameterWithoutDirection("@WorkflowId", SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameterWithoutDirection("@OrderType", SqlDbType.Int, 4, this.OrderType),
            GetSqlParameterWithoutDirection("@WorkIndex", SqlDbType.Int, 4, this.WorkIndex)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("orderTypeWorkflowId: " + orderTypeWorkflowId).Append("\r\n");
            b.Append("workflowId: " + workflowId).Append("\r\n");
            b.Append("orderType: " + orderType).Append("\r\n");
            b.Append("workIndex: " + workIndex).Append("\r\n");
            return b.ToString();
        }
    }

    public class Z_OrderTypeWorkflowCollection : BindingList<Z_OrderTypeWorkflow> { }
}
