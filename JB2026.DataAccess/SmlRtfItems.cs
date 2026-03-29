using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class SmlRtfItems
    {
        private Guid key = Guid.Empty;
        private Guid itemId = Guid.Empty;
        private Guid headerId = Guid.Empty;
        private int lineNumber;
        private string productCode = string.Empty;
        private string productDescription = string.Empty;
        private string price = string.Empty;
        private string discount = string.Empty;
        private string qty = string.Empty;
        private string amount = string.Empty;
        private string postProcess = string.Empty;

        public SmlRtfItems() { }

        public static SmlRtfItems? Load(Guid itemId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfItems_SelRec", new SqlParameter[] { new SqlParameter("@ItemId", itemId) });
            if (reader.Read()) { var r = new SmlRtfItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfItems? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new SmlRtfItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfItemsCollection LoadCollection()
            => LoadCollection("spSmlRtfItems_SelAll", new SqlParameter[] { });

        public static SmlRtfItemsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfItems_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfItemsCollection LoadCollection(string whereClause)
            => LoadCollection("spSmlRtfItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SmlRtfItemsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfItemsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SmlRtfItemsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new SmlRtfItems(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid itemId)
            => SqlHelper.Default.ExecuteNonQuery("spSmlRtfItems_DelRec", new SqlParameter[] { new SqlParameter("@ItemId", itemId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) itemId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) headerId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) lineNumber = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) productCode = reader.GetString(3);
                if (!reader.IsDBNull(4)) productDescription = reader.GetString(4);
                if (!reader.IsDBNull(5)) price = reader.GetString(5);
                if (!reader.IsDBNull(6)) discount = reader.GetString(6);
                if (!reader.IsDBNull(7)) qty = reader.GetString(7);
                if (!reader.IsDBNull(8)) amount = reader.GetString(8);
                if (!reader.IsDBNull(9)) postProcess = reader.GetString(9);
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
        public int LineNumber { get { return lineNumber; } set { lineNumber = value; } }
        public string ProductCode { get { return productCode; } set { productCode = value; } }
        public string ProductDescription { get { return productDescription; } set { productDescription = value; } }
        public string Price { get { return price; } set { price = value; } }
        public string Discount { get { return discount; } set { discount = value; } }
        public string Qty { get { return qty; } set { qty = value; } }
        public string Amount { get { return amount; } set { amount = value; } }
        public string PostProcess { get { return postProcess; } set { postProcess = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSmlRtfItems_InsRec", "@ItemId", out var rv, GetInsertParameterValues());
            itemId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSmlRtfItems_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@ItemId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameter("@HeaderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameter("@LineNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.LineNumber),
            GetSqlParameter("@ProductCode", ParameterDirection.Input, SqlDbType.NVarChar, 128, this.ProductCode),
            GetSqlParameter("@ProductDescription", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.ProductDescription),
            GetSqlParameter("@Price", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.Price),
            GetSqlParameter("@Discount", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.Discount),
            GetSqlParameter("@Qty", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.Qty),
            GetSqlParameter("@Amount", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.Amount),
            GetSqlParameter("@PostProcess", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.PostProcess)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@ItemId", SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameterWithoutDirection("@HeaderId", SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameterWithoutDirection("@LineNumber", SqlDbType.Int, 4, this.LineNumber),
            GetSqlParameterWithoutDirection("@ProductCode", SqlDbType.NVarChar, 128, this.ProductCode),
            GetSqlParameterWithoutDirection("@ProductDescription", SqlDbType.NVarChar, 256, this.ProductDescription),
            GetSqlParameterWithoutDirection("@Price", SqlDbType.NVarChar, 16, this.Price),
            GetSqlParameterWithoutDirection("@Discount", SqlDbType.NVarChar, 16, this.Discount),
            GetSqlParameterWithoutDirection("@Qty", SqlDbType.NVarChar, 16, this.Qty),
            GetSqlParameterWithoutDirection("@Amount", SqlDbType.NVarChar, 16, this.Amount),
            GetSqlParameterWithoutDirection("@PostProcess", SqlDbType.NVarChar, 64, this.PostProcess)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("itemId: " + itemId).Append("\r\n");
            b.Append("headerId: " + headerId).Append("\r\n");
            b.Append("lineNumber: " + lineNumber).Append("\r\n");
            b.Append("productCode: " + productCode).Append("\r\n");
            b.Append("productDescription: " + productDescription).Append("\r\n");
            b.Append("price: " + price).Append("\r\n");
            b.Append("discount: " + discount).Append("\r\n");
            b.Append("qty: " + qty).Append("\r\n");
            b.Append("amount: " + amount).Append("\r\n");
            b.Append("postProcess: " + postProcess).Append("\r\n");
            return b.ToString();
        }
    }

    public class SmlRtfItemsCollection : BindingList<SmlRtfItems> { }
}
