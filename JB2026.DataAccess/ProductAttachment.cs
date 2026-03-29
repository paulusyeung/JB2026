using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class ProductAttachment
    {
        private Guid key = Guid.Empty;
        private Guid attachmentId = Guid.Empty;
        private Guid productId = Guid.Empty;
        private int attachmentIndex;
        private string originalFileName = string.Empty;

        public ProductAttachment() { }

        public ProductAttachment(Guid attachmentId, Guid productId, int attachmentIndex, string originalFileName)
        {
            this.attachmentId = attachmentId; this.productId = productId;
            this.attachmentIndex = attachmentIndex; this.originalFileName = originalFileName;
        }

        public static ProductAttachment? Load(Guid attachmentId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spProductAttachment_SelRec", new SqlParameter[] { new SqlParameter("@AttachmentId", attachmentId) });
            if (reader.Read()) { var r = new ProductAttachment(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static ProductAttachment? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spProductAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new ProductAttachment(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static ProductAttachmentCollection LoadCollection()
            => LoadCollection("spProductAttachment_SelAll", new SqlParameter[] { });

        public static ProductAttachmentCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spProductAttachment_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static ProductAttachmentCollection LoadCollection(string whereClause)
            => LoadCollection("spProductAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static ProductAttachmentCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spProductAttachment_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static ProductAttachmentCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new ProductAttachmentCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new ProductAttachment(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid attachmentId)
            => SqlHelper.Default.ExecuteNonQuery("spProductAttachment_DelRec", new SqlParameter[] { new SqlParameter("@AttachmentId", attachmentId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) attachmentId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) productId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) attachmentIndex = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) originalFileName = reader.GetString(3);
            }
        }

        public void Delete() => Delete(this.AttachmentId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != AttachmentId) this.Delete(); Update(); }
        }

        public Guid AttachmentId { get { return attachmentId; } set { attachmentId = value; } }
        public Guid ProductId { get { return productId; } set { productId = value; } }
        public int AttachmentIndex { get { return attachmentIndex; } set { attachmentIndex = value; } }
        public string OriginalFileName { get { return originalFileName; } set { originalFileName = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spProductAttachment_InsRec", "@AttachmentId", out var rv, GetInsertParameterValues());
            attachmentId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spProductAttachment_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@AttachmentId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.AttachmentId),
            GetSqlParameter("@ProductId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameter("@AttachmentIndex", ParameterDirection.Input, SqlDbType.Int, 4, this.AttachmentIndex),
            GetSqlParameter("@OriginalFileName", ParameterDirection.Input, SqlDbType.NVarChar, 255, this.OriginalFileName)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@AttachmentId", SqlDbType.UniqueIdentifier, 16, this.AttachmentId),
            GetSqlParameterWithoutDirection("@ProductId", SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameterWithoutDirection("@AttachmentIndex", SqlDbType.Int, 4, this.AttachmentIndex),
            GetSqlParameterWithoutDirection("@OriginalFileName", SqlDbType.NVarChar, 255, this.OriginalFileName)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("attachmentId: " + attachmentId).Append("\r\n");
            b.Append("productId: " + productId).Append("\r\n");
            b.Append("attachmentIndex: " + attachmentIndex).Append("\r\n");
            b.Append("originalFileName: " + originalFileName).Append("\r\n");
            return b.ToString();
        }
    }

    public class ProductAttachmentCollection : BindingList<ProductAttachment> { }
}
