using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class JobWorkflow
    {
        private Guid key = Guid.Empty;
        private Guid jobWorkflowId = Guid.Empty;
        private Guid orderId = Guid.Empty;
        private Guid workflowId = Guid.Empty;
        private int workIndex;
        private string workTitle = string.Empty;
        private string workInstruction = string.Empty;
        private int workStatus;
        private string workNotes = string.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;

        public JobWorkflow() { }

        public static JobWorkflow? Load(Guid jobWorkflowId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobWorkflow_SelRec", new SqlParameter[] { new SqlParameter("@JobWorkflowId", jobWorkflowId) });
            if (reader.Read()) { var r = new JobWorkflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobWorkflow? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobWorkflow(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobWorkflowCollection LoadCollection()
            => LoadCollection("spJobWorkflow_SelAll", new SqlParameter[] { });

        public static JobWorkflowCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobWorkflowCollection LoadCollection(string whereClause)
            => LoadCollection("spJobWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobWorkflowCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobWorkflow_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobWorkflowCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobWorkflowCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobWorkflow(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid jobWorkflowId)
            => SqlHelper.Default.ExecuteNonQuery("spJobWorkflow_DelRec", new SqlParameter[] { new SqlParameter("@JobWorkflowId", jobWorkflowId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) jobWorkflowId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) orderId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) workflowId = reader.GetGuid(2);
                if (!reader.IsDBNull(3)) workIndex = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) workTitle = reader.GetString(4);
                if (!reader.IsDBNull(5)) workInstruction = reader.GetString(5);
                if (!reader.IsDBNull(6)) workStatus = reader.GetInt32(6);
                if (!reader.IsDBNull(7)) workNotes = reader.GetString(7);
                if (!reader.IsDBNull(8)) modifiedOn = reader.GetDateTime(8);
                if (!reader.IsDBNull(9)) modifiedBy = reader.GetGuid(9);
            }
        }

        public void Delete() => Delete(this.JobWorkflowId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != JobWorkflowId) this.Delete(); Update(); }
        }

        public Guid JobWorkflowId { get { return jobWorkflowId; } set { jobWorkflowId = value; } }
        public Guid OrderId { get { return orderId; } set { orderId = value; } }
        public Guid WorkflowId { get { return workflowId; } set { workflowId = value; } }
        public int WorkIndex { get { return workIndex; } set { workIndex = value; } }
        public string WorkTitle { get { return workTitle; } set { workTitle = value; } }
        public string WorkInstruction { get { return workInstruction; } set { workInstruction = value; } }
        public int WorkStatus { get { return workStatus; } set { workStatus = value; } }
        public string WorkNotes { get { return workNotes; } set { workNotes = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spJobWorkflow_InsRec", "@JobWorkflowId", out var rv, GetInsertParameterValues());
            jobWorkflowId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobWorkflow_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@JobWorkflowId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.JobWorkflowId),
            GetSqlParameter("@OrderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameter("@WorkflowId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameter("@WorkIndex", ParameterDirection.Input, SqlDbType.Int, 4, this.WorkIndex),
            GetSqlParameter("@WorkTitle", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.WorkTitle),
            GetSqlParameter("@WorkInstruction", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.WorkInstruction),
            GetSqlParameter("@WorkStatus", ParameterDirection.Input, SqlDbType.Int, 4, this.WorkStatus),
            GetSqlParameter("@WorkNotes", ParameterDirection.Input, SqlDbType.NVarChar, -1, this.WorkNotes),
            GetSqlParameter("@ModifiedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameter("@ModifiedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ModifiedBy)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@JobWorkflowId", SqlDbType.UniqueIdentifier, 16, this.JobWorkflowId),
            GetSqlParameterWithoutDirection("@OrderId", SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameterWithoutDirection("@WorkflowId", SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameterWithoutDirection("@WorkIndex", SqlDbType.Int, 4, this.WorkIndex),
            GetSqlParameterWithoutDirection("@WorkTitle", SqlDbType.NVarChar, 512, this.WorkTitle),
            GetSqlParameterWithoutDirection("@WorkInstruction", SqlDbType.NVarChar, 512, this.WorkInstruction),
            GetSqlParameterWithoutDirection("@WorkStatus", SqlDbType.Int, 4, this.WorkStatus),
            GetSqlParameterWithoutDirection("@WorkNotes", SqlDbType.NVarChar, -1, this.WorkNotes),
            GetSqlParameterWithoutDirection("@ModifiedOn", SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameterWithoutDirection("@ModifiedBy", SqlDbType.UniqueIdentifier, 16, this.ModifiedBy)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("jobWorkflowId: " + jobWorkflowId).Append("\r\n");
            b.Append("orderId: " + orderId).Append("\r\n");
            b.Append("workflowId: " + workflowId).Append("\r\n");
            b.Append("workIndex: " + workIndex).Append("\r\n");
            b.Append("workTitle: " + workTitle).Append("\r\n");
            b.Append("workInstruction: " + workInstruction).Append("\r\n");
            b.Append("workStatus: " + workStatus).Append("\r\n");
            b.Append("workNotes: " + workNotes).Append("\r\n");
            b.Append("modifiedOn: " + modifiedOn).Append("\r\n");
            b.Append("modifiedBy: " + modifiedBy).Append("\r\n");
            return b.ToString();
        }
    }

    public class JobWorkflowCollection : BindingList<JobWorkflow> { }
}
