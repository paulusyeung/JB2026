using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class JobAttachment
    {
        private Guid key = Guid.Empty;
        private Guid attachmentId = Guid.Empty;
        private Guid orderId = Guid.Empty;
        private int attachmentType;
        private int attachmentIndex;
        private string originalFileName = string.Empty;

        public JobAttachment() { }

        public JobAttachment(Guid attachmentId, Guid orderId, int attachmentType, int attachmentIndex, string originalFileName)
        {
            this.attachmentId = attachmentId; this.orderId = orderId; this.attachmentType = attachmentType;
            this.attachmentIndex = attachmentIndex; this.originalFileName = originalFileName;
        }

        public static JobAttachment? Load(Guid attachmentId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobAttachment_SelRec", new SqlParameter[] { new SqlParameter("@AttachmentId", attachmentId) });
            if (reader.Read()) { var r = new JobAttachment(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobAttachment? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobAttachment(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobAttachmentCollection LoadCollection()
            => LoadCollection("spJobAttachment_SelAll", new SqlParameter[] { });

        public static JobAttachmentCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobAttachment_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobAttachmentCollection LoadCollection(string whereClause)
            => LoadCollection("spJobAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobAttachmentCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobAttachmentCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobAttachmentCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobAttachment(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid attachmentId)
            => SqlHelper.Default.ExecuteNonQuery("spJobAttachment_DelRec", new SqlParameter[] { new SqlParameter("@AttachmentId", attachmentId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) attachmentId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) orderId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) attachmentType = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) attachmentIndex = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) originalFileName = reader.GetString(4);
            }
        }

        public void Delete() => Delete(this.AttachmentId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != AttachmentId) this.Delete(); Update(); }
        }

        public Guid AttachmentId { get { return attachmentId; } set { attachmentId = value; } }
        public Guid OrderId { get { return orderId; } set { orderId = value; } }
        public int AttachmentType { get { return attachmentType; } set { attachmentType = value; } }
        public int AttachmentIndex { get { return attachmentIndex; } set { attachmentIndex = value; } }
        public string OriginalFileName { get { return originalFileName; } set { originalFileName = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spJobAttachment_InsRec", "@AttachmentId", out var rv, GetInsertParameterValues());
            attachmentId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobAttachment_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@AttachmentId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.AttachmentId),
            GetSqlParameter("@OrderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameter("@AttachmentType", ParameterDirection.Input, SqlDbType.Int, 4, this.AttachmentType),
            GetSqlParameter("@AttachmentIndex", ParameterDirection.Input, SqlDbType.Int, 4, this.AttachmentIndex),
            GetSqlParameter("@OriginalFileName", ParameterDirection.Input, SqlDbType.NVarChar, 255, this.OriginalFileName)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@AttachmentId", SqlDbType.UniqueIdentifier, 16, this.AttachmentId),
            GetSqlParameterWithoutDirection("@OrderId", SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameterWithoutDirection("@AttachmentType", SqlDbType.Int, 4, this.AttachmentType),
            GetSqlParameterWithoutDirection("@AttachmentIndex", SqlDbType.Int, 4, this.AttachmentIndex),
            GetSqlParameterWithoutDirection("@OriginalFileName", SqlDbType.NVarChar, 255, this.OriginalFileName)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("attachmentId: " + attachmentId).Append("\r\n");
            b.Append("orderId: " + orderId).Append("\r\n");
            b.Append("attachmentType: " + attachmentType).Append("\r\n");
            b.Append("attachmentIndex: " + attachmentIndex).Append("\r\n");
            b.Append("originalFileName: " + originalFileName).Append("\r\n");
            return b.ToString();
        }
    }

    public class JobAttachmentCollection : BindingList<JobAttachment> { }
}
