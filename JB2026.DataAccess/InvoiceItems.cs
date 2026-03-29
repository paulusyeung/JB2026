using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class InvoiceItems
    {
        private Guid key = Guid.Empty;
        private Guid itemId = Guid.Empty;
        private Guid headerId = Guid.Empty;
        private Guid smlRtfHeaderId = Guid.Empty;
        private int lineNumber;
        private string notes = string.Empty;

        public InvoiceItems() { }

        public InvoiceItems(Guid itemId, Guid headerId, Guid smlRtfHeaderId, int lineNumber, string notes)
        {
            this.itemId = itemId; this.headerId = headerId; this.smlRtfHeaderId = smlRtfHeaderId;
            this.lineNumber = lineNumber; this.notes = notes;
        }

        public static InvoiceItems? Load(Guid itemId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceItems_SelRec", new SqlParameter[] { new SqlParameter("@ItemId", itemId) });
            if (reader.Read()) { var r = new InvoiceItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceItems? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new InvoiceItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceItemsCollection LoadCollection()
            => LoadCollection("spInvoiceItems_SelAll", new SqlParameter[] { });

        public static InvoiceItemsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceItems_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceItemsCollection LoadCollection(string whereClause)
            => LoadCollection("spInvoiceItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static InvoiceItemsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceItemsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new InvoiceItemsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new InvoiceItems(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid itemId)
            => SqlHelper.Default.ExecuteNonQuery("spInvoiceItems_DelRec", new SqlParameter[] { new SqlParameter("@ItemId", itemId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) itemId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) headerId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) smlRtfHeaderId = reader.GetGuid(2);
                if (!reader.IsDBNull(3)) lineNumber = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) notes = reader.GetString(4);
            }
        }

        public void Delete() => Delete(this.ItemId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != ItemId) this.Delete(); Update(); }
        }

        public Guid ItemId { get { return itemId; } set { itemId = value; } }
        public Guid HeaderId { get { return headerId; } set { headerId = value; } }
        public Guid SmlRtfHeaderId { get { return smlRtfHeaderId; } set { smlRtfHeaderId = value; } }
        public int LineNumber { get { return lineNumber; } set { lineNumber = value; } }
        public string Notes { get { return notes; } set { notes = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spInvoiceItems_InsRec", "@ItemId", out var rv, GetInsertParameterValues());
            itemId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spInvoiceItems_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@ItemId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameter("@HeaderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameter("@SmlRtfHeaderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.SmlRtfHeaderId),
            GetSqlParameter("@LineNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.LineNumber),
            GetSqlParameter("@Notes", ParameterDirection.Input, SqlDbType.NVarChar, 128, this.Notes)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@ItemId", SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameterWithoutDirection("@HeaderId", SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameterWithoutDirection("@SmlRtfHeaderId", SqlDbType.UniqueIdentifier, 16, this.SmlRtfHeaderId),
            GetSqlParameterWithoutDirection("@LineNumber", SqlDbType.Int, 4, this.LineNumber),
            GetSqlParameterWithoutDirection("@Notes", SqlDbType.NVarChar, 128, this.Notes)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("itemId: " + itemId).Append("\r\n");
            b.Append("headerId: " + headerId).Append("\r\n");
            b.Append("smlRtfHeaderId: " + smlRtfHeaderId).Append("\r\n");
            b.Append("lineNumber: " + lineNumber).Append("\r\n");
            b.Append("notes: " + notes).Append("\r\n");
            return b.ToString();
        }
    }

    public class InvoiceItemsCollection : BindingList<InvoiceItems> { }
}
