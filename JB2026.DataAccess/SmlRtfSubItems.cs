using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class SmlRtfSubItems
    {
        private Guid key = Guid.Empty;
        private Guid subItemId = Guid.Empty;
        private Guid itemId = Guid.Empty;
        private int subLineNumber;
        private string start_End = string.Empty;
        private string referenceNumber = string.Empty;
        private string labelSize = string.Empty;
        private string qty = string.Empty;

        public SmlRtfSubItems() { }

        public static SmlRtfSubItems? Load(Guid subItemId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfSubItems_SelRec", new SqlParameter[] { new SqlParameter("@SubItemId", subItemId) });
            if (reader.Read()) { var r = new SmlRtfSubItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfSubItems? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new SmlRtfSubItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfSubItemsCollection LoadCollection()
            => LoadCollection("spSmlRtfSubItems_SelAll", new SqlParameter[] { });

        public static SmlRtfSubItemsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfSubItems_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfSubItemsCollection LoadCollection(string whereClause)
            => LoadCollection("spSmlRtfSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SmlRtfSubItemsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfSubItemsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SmlRtfSubItemsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new SmlRtfSubItems(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid subItemId)
            => SqlHelper.Default.ExecuteNonQuery("spSmlRtfSubItems_DelRec", new SqlParameter[] { new SqlParameter("@SubItemId", subItemId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) subItemId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) itemId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) subLineNumber = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) start_End = reader.GetString(3);
                if (!reader.IsDBNull(4)) referenceNumber = reader.GetString(4);
                if (!reader.IsDBNull(5)) labelSize = reader.GetString(5);
                if (!reader.IsDBNull(6)) qty = reader.GetString(6);
            }
        }

        public void Delete() => Delete(this.SubItemId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != SubItemId) this.Delete(); Update(); }
        }

        public Guid SubItemId { get { return subItemId; } set { subItemId = value; } }
        public Guid ItemId { get { return itemId; } set { itemId = value; } }
        public int SubLineNumber { get { return subLineNumber; } set { subLineNumber = value; } }
        public string Start_End { get { return start_End; } set { start_End = value; } }
        public string ReferenceNumber { get { return referenceNumber; } set { referenceNumber = value; } }
        public string LabelSize { get { return labelSize; } set { labelSize = value; } }
        public string Qty { get { return qty; } set { qty = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSmlRtfSubItems_InsRec", "@SubItemId", out var rv, GetInsertParameterValues());
            subItemId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSmlRtfSubItems_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@SubItemId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.SubItemId),
            GetSqlParameter("@ItemId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameter("@SubLineNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.SubLineNumber),
            GetSqlParameter("@Start_End", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.Start_End),
            GetSqlParameter("@ReferenceNumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.ReferenceNumber),
            GetSqlParameter("@LabelSize", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.LabelSize),
            GetSqlParameter("@Qty", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.Qty)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@SubItemId", SqlDbType.UniqueIdentifier, 16, this.SubItemId),
            GetSqlParameterWithoutDirection("@ItemId", SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameterWithoutDirection("@SubLineNumber", SqlDbType.Int, 4, this.SubLineNumber),
            GetSqlParameterWithoutDirection("@Start_End", SqlDbType.NVarChar, 256, this.Start_End),
            GetSqlParameterWithoutDirection("@ReferenceNumber", SqlDbType.NVarChar, 32, this.ReferenceNumber),
            GetSqlParameterWithoutDirection("@LabelSize", SqlDbType.NVarChar, 32, this.LabelSize),
            GetSqlParameterWithoutDirection("@Qty", SqlDbType.NVarChar, 10, this.Qty)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("subItemId: " + subItemId).Append("\r\n");
            b.Append("itemId: " + itemId).Append("\r\n");
            b.Append("subLineNumber: " + subLineNumber).Append("\r\n");
            b.Append("start_End: " + start_End).Append("\r\n");
            b.Append("referenceNumber: " + referenceNumber).Append("\r\n");
            b.Append("labelSize: " + labelSize).Append("\r\n");
            b.Append("qty: " + qty).Append("\r\n");
            return b.ToString();
        }
    }

    public class SmlRtfSubItemsCollection : BindingList<SmlRtfSubItems> { }
}
