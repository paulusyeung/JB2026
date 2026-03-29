using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class Z_WorkflowForms
    {
        private Guid key = Guid.Empty;
        private Guid workflowFormId = Guid.Empty;
        private Guid workflowId = Guid.Empty;
        private Guid formId = Guid.Empty;
        private int seqNumber;

        public Z_WorkflowForms() { }

        public static Z_WorkflowForms? Load(Guid workflowFormId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_WorkflowForms_SelRec", new SqlParameter[] { new SqlParameter("@WorkflowFormId", workflowFormId) });
            if (reader.Read()) { var r = new Z_WorkflowForms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_WorkflowForms? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_WorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Z_WorkflowForms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_WorkflowFormsCollection LoadCollection()
            => LoadCollection("spZ_WorkflowForms_SelAll", new SqlParameter[] { });

        public static Z_WorkflowFormsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_WorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_WorkflowFormsCollection LoadCollection(string whereClause)
            => LoadCollection("spZ_WorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static Z_WorkflowFormsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_WorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_WorkflowFormsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new Z_WorkflowFormsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Z_WorkflowForms(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid workflowFormId)
            => SqlHelper.Default.ExecuteNonQuery("spZ_WorkflowForms_DelRec", new SqlParameter[] { new SqlParameter("@WorkflowFormId", workflowFormId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) workflowFormId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) workflowId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) formId = reader.GetGuid(2);
                if (!reader.IsDBNull(3)) seqNumber = reader.GetInt32(3);
            }
        }

        public void Delete() => Delete(this.WorkflowFormId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != WorkflowFormId) this.Delete(); Update(); }
        }

        public Guid WorkflowFormId { get { return workflowFormId; } set { workflowFormId = value; } }
        public Guid WorkflowId { get { return workflowId; } set { workflowId = value; } }
        public Guid FormId { get { return formId; } set { formId = value; } }
        public int SeqNumber { get { return seqNumber; } set { seqNumber = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spZ_WorkflowForms_InsRec", "@WorkflowFormId", out var rv, GetInsertParameterValues());
            workflowFormId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spZ_WorkflowForms_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@WorkflowFormId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.WorkflowFormId),
            GetSqlParameter("@WorkflowId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameter("@FormId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameter("@SeqNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.SeqNumber)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@WorkflowFormId", SqlDbType.UniqueIdentifier, 16, this.WorkflowFormId),
            GetSqlParameterWithoutDirection("@WorkflowId", SqlDbType.UniqueIdentifier, 16, this.WorkflowId),
            GetSqlParameterWithoutDirection("@FormId", SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameterWithoutDirection("@SeqNumber", SqlDbType.Int, 4, this.SeqNumber)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("workflowFormId: " + workflowFormId).Append("\r\n");
            b.Append("workflowId: " + workflowId).Append("\r\n");
            b.Append("formId: " + formId).Append("\r\n");
            b.Append("seqNumber: " + seqNumber).Append("\r\n");
            return b.ToString();
        }
    }

    public class Z_WorkflowFormsCollection : BindingList<Z_WorkflowForms> { }
}
