using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class StockInOut
    {
        private Guid key = Guid.Empty;
        private Guid inOutId = Guid.Empty;
        private Guid productId = Guid.Empty;
        private DateTime inOutDate = DateTime.Parse("1900-1-1");
        private string reference = string.Empty;
        private int qty;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;

        public StockInOut() { }

        public static StockInOut? Load(Guid inOutId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spStockInOut_SelRec", new SqlParameter[] { new SqlParameter("@InOutId", inOutId) });
            if (reader.Read()) { var r = new StockInOut(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static StockInOut? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spStockInOut_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new StockInOut(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static StockInOutCollection LoadCollection()
            => LoadCollection("spStockInOut_SelAll", new SqlParameter[] { });

        public static StockInOutCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spStockInOut_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static StockInOutCollection LoadCollection(string whereClause)
            => LoadCollection("spStockInOut_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static StockInOutCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spStockInOut_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static StockInOutCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new StockInOutCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new StockInOut(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid inOutId)
            => SqlHelper.Default.ExecuteNonQuery("spStockInOut_DelRec", new SqlParameter[] { new SqlParameter("@InOutId", inOutId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) inOutId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) productId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) inOutDate = reader.GetDateTime(2);
                if (!reader.IsDBNull(3)) reference = reader.GetString(3);
                if (!reader.IsDBNull(4)) qty = reader.GetInt32(4);
                if (!reader.IsDBNull(5)) createdOn = reader.GetDateTime(5);
                if (!reader.IsDBNull(6)) createdBy = reader.GetGuid(6);
                if (!reader.IsDBNull(7)) modifiedOn = reader.GetDateTime(7);
                if (!reader.IsDBNull(8)) modifiedBy = reader.GetGuid(8);
            }
        }

        public void Delete() => Delete(this.InOutId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != InOutId) this.Delete(); Update(); }
        }

        public Guid InOutId { get { return inOutId; } set { inOutId = value; } }
        public Guid ProductId { get { return productId; } set { productId = value; } }
        public DateTime InOutDate { get { return inOutDate; } set { inOutDate = value; } }
        public string Reference { get { return reference; } set { reference = value; } }
        public int Qty { get { return qty; } set { qty = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spStockInOut_InsRec", "@InOutId", out var rv, GetInsertParameterValues());
            inOutId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spStockInOut_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@InOutId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.InOutId),
            GetSqlParameter("@ProductId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameter("@InOutDate", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.InOutDate),
            GetSqlParameter("@Reference", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.Reference),
            GetSqlParameter("@Qty", ParameterDirection.Input, SqlDbType.Int, 4, this.Qty),
            GetSqlParameter("@CreatedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameter("@CreatedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
            GetSqlParameter("@ModifiedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameter("@ModifiedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ModifiedBy)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@InOutId", SqlDbType.UniqueIdentifier, 16, this.InOutId),
            GetSqlParameterWithoutDirection("@ProductId", SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameterWithoutDirection("@InOutDate", SqlDbType.SmallDateTime, 4, this.InOutDate),
            GetSqlParameterWithoutDirection("@Reference", SqlDbType.NVarChar, 32, this.Reference),
            GetSqlParameterWithoutDirection("@Qty", SqlDbType.Int, 4, this.Qty),
            GetSqlParameterWithoutDirection("@CreatedOn", SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameterWithoutDirection("@CreatedBy", SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
            GetSqlParameterWithoutDirection("@ModifiedOn", SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameterWithoutDirection("@ModifiedBy", SqlDbType.UniqueIdentifier, 16, this.ModifiedBy)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("inOutId: " + inOutId).Append("\r\n");
            b.Append("productId: " + productId).Append("\r\n");
            b.Append("inOutDate: " + inOutDate).Append("\r\n");
            b.Append("reference: " + reference).Append("\r\n");
            b.Append("qty: " + qty).Append("\r\n");
            b.Append("createdOn: " + createdOn).Append("\r\n");
            b.Append("createdBy: " + createdBy).Append("\r\n");
            b.Append("modifiedOn: " + modifiedOn).Append("\r\n");
            b.Append("modifiedBy: " + modifiedBy).Append("\r\n");
            return b.ToString();
        }
    }

    public class StockInOutCollection : BindingList<StockInOut> { }
}
