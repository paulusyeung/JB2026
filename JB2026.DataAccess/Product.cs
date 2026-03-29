using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Text;
using System.Xml;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class Product
    {
        private Guid key = Guid.Empty;
        private Guid productId = Guid.Empty;
        private Guid categoryId = Guid.Empty;
        private string stockNumber = string.Empty;
        private string productCode = string.Empty;
        private string productName = string.Empty;
        private string description = string.Empty;
        private string remarks = string.Empty;
        private int mOQ = 0;
        private int balance = 0;
        private decimal sellingPrice = 0;
        private decimal cOGS = 0;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public Product() { }

        public Product(Guid productId, Guid categoryId, string stockNumber, string productCode, string productName,
            string description, string remarks, int mOQ, int balance, decimal sellingPrice, decimal cOGS,
            DateTime createdOn, Guid createdBy, DateTime modifiedOn, Guid modifiedBy,
            bool retired, DateTime retiredOn, Guid retiredBy)
        {
            this.productId = productId;
            this.categoryId = categoryId;
            this.stockNumber = stockNumber;
            this.productCode = productCode;
            this.productName = productName;
            this.description = description;
            this.remarks = remarks;
            this.mOQ = mOQ;
            this.balance = balance;
            this.sellingPrice = sellingPrice;
            this.cOGS = cOGS;
            this.createdOn = createdOn;
            this.createdBy = createdBy;
            this.modifiedOn = modifiedOn;
            this.modifiedBy = modifiedBy;
            this.retired = retired;
            this.retiredOn = retiredOn;
            this.retiredBy = retiredBy;
        }

        public static Product? Load(Guid productId)
        {
            var parms = new SqlParameter[] { new SqlParameter("@ProductId", productId) };
            using var reader = SqlHelper.Default.ExecuteReader("spProduct_SelRec", parms);
            if (reader.Read()) { var r = new Product(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Product? LoadWhere(string whereClause)
        {
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) };
            using var reader = SqlHelper.Default.ExecuteReader("spProduct_SelAll", parms);
            if (reader.Read()) { var r = new Product(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static ProductCollection LoadCollection()
            => LoadCollection("spProduct_SelAll", new SqlParameter[] { });

        public static ProductCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spProduct_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static ProductCollection LoadCollection(string whereClause)
            => LoadCollection("spProduct_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static ProductCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spProduct_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static ProductCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new ProductCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Product(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid productId)
            => SqlHelper.Default.ExecuteNonQuery("spProduct_DelRec", new SqlParameter[] { new SqlParameter("@ProductId", productId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) productId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) categoryId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) stockNumber = reader.GetString(2);
                if (!reader.IsDBNull(3)) productCode = reader.GetString(3);
                if (!reader.IsDBNull(4)) productName = reader.GetString(4);
                if (!reader.IsDBNull(5)) description = reader.GetString(5);
                if (!reader.IsDBNull(6)) remarks = reader.GetString(6);
                if (!reader.IsDBNull(7)) mOQ = reader.GetInt32(7);
                if (!reader.IsDBNull(8)) balance = reader.GetInt32(8);
                if (!reader.IsDBNull(9)) sellingPrice = reader.GetDecimal(9);
                if (!reader.IsDBNull(10)) cOGS = reader.GetDecimal(10);
                if (!reader.IsDBNull(11)) createdOn = reader.GetDateTime(11);
                if (!reader.IsDBNull(12)) createdBy = reader.GetGuid(12);
                if (!reader.IsDBNull(13)) modifiedOn = reader.GetDateTime(13);
                if (!reader.IsDBNull(14)) modifiedBy = reader.GetGuid(14);
                if (!reader.IsDBNull(15)) retired = reader.GetBoolean(15);
                if (!reader.IsDBNull(16)) retiredOn = reader.GetDateTime(16);
                if (!reader.IsDBNull(17)) retiredBy = reader.GetGuid(17);
            }
        }

        public void Delete() => Delete(this.ProductId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != ProductId) this.Delete(); Update(); }
        }

        public Guid ProductId { get { return productId; } set { productId = value; } }
        public Guid CategoryId { get { return categoryId; } set { categoryId = value; } }
        public string StockNumber { get { return stockNumber; } set { stockNumber = value; } }
        public string ProductCode { get { return productCode; } set { productCode = value; } }
        public string ProductName { get { return productName; } set { productName = value; } }
        public string Description { get { return description; } set { description = value; } }
        public string Remarks { get { return remarks; } set { remarks = value; } }
        public int MOQ { get { return mOQ; } set { mOQ = value; } }
        public int Balance { get { return balance; } set { balance = value; } }
        public decimal SellingPrice { get { return sellingPrice; } set { sellingPrice = value; } }
        public decimal COGS { get { return cOGS; } set { cOGS = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spProduct_InsRec", "@ProductId", out var rv, GetInsertParameterValues());
            productId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spProduct_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@ProductId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameter("@CategoryId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CategoryId),
            GetSqlParameter("@StockNumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.StockNumber),
            GetSqlParameter("@ProductCode", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.ProductCode),
            GetSqlParameter("@ProductName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.ProductName),
            GetSqlParameter("@Description", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.Description),
            GetSqlParameter("@Remarks", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.Remarks),
            GetSqlParameter("@MOQ", ParameterDirection.Input, SqlDbType.Int, 4, this.MOQ),
            GetSqlParameter("@Balance", ParameterDirection.Input, SqlDbType.Int, 4, this.Balance),
            GetSqlParameter("@SellingPrice", ParameterDirection.Input, SqlDbType.Money, 8, this.SellingPrice),
            GetSqlParameter("@COGS", ParameterDirection.Input, SqlDbType.Money, 8, this.COGS),
            GetSqlParameter("@CreatedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameter("@CreatedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
            GetSqlParameter("@ModifiedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameter("@ModifiedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ModifiedBy),
            GetSqlParameter("@Retired", ParameterDirection.Input, SqlDbType.Bit, 1, this.Retired),
            GetSqlParameter("@RetiredOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.RetiredOn),
            GetSqlParameter("@RetiredBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.RetiredBy)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@ProductId", SqlDbType.UniqueIdentifier, 16, this.ProductId),
            GetSqlParameterWithoutDirection("@CategoryId", SqlDbType.UniqueIdentifier, 16, this.CategoryId),
            GetSqlParameterWithoutDirection("@StockNumber", SqlDbType.NVarChar, 32, this.StockNumber),
            GetSqlParameterWithoutDirection("@ProductCode", SqlDbType.NVarChar, 32, this.ProductCode),
            GetSqlParameterWithoutDirection("@ProductName", SqlDbType.NVarChar, 64, this.ProductName),
            GetSqlParameterWithoutDirection("@Description", SqlDbType.NVarChar, 512, this.Description),
            GetSqlParameterWithoutDirection("@Remarks", SqlDbType.NVarChar, 512, this.Remarks),
            GetSqlParameterWithoutDirection("@MOQ", SqlDbType.Int, 4, this.MOQ),
            GetSqlParameterWithoutDirection("@Balance", SqlDbType.Int, 4, this.Balance),
            GetSqlParameterWithoutDirection("@SellingPrice", SqlDbType.Money, 8, this.SellingPrice),
            GetSqlParameterWithoutDirection("@COGS", SqlDbType.Money, 8, this.COGS),
            GetSqlParameterWithoutDirection("@CreatedOn", SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameterWithoutDirection("@CreatedBy", SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
            GetSqlParameterWithoutDirection("@ModifiedOn", SqlDbType.SmallDateTime, 4, this.ModifiedOn),
            GetSqlParameterWithoutDirection("@ModifiedBy", SqlDbType.UniqueIdentifier, 16, this.ModifiedBy),
            GetSqlParameterWithoutDirection("@Retired", SqlDbType.Bit, 1, this.Retired),
            GetSqlParameterWithoutDirection("@RetiredOn", SqlDbType.SmallDateTime, 4, this.RetiredOn),
            GetSqlParameterWithoutDirection("@RetiredBy", SqlDbType.UniqueIdentifier, 16, this.RetiredBy)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("productId: " + productId).Append("\r\n");
            b.Append("categoryId: " + categoryId).Append("\r\n");
            b.Append("stockNumber: " + stockNumber).Append("\r\n");
            b.Append("productCode: " + productCode).Append("\r\n");
            b.Append("productName: " + productName).Append("\r\n");
            b.Append("description: " + description).Append("\r\n");
            b.Append("remarks: " + remarks).Append("\r\n");
            b.Append("mOQ: " + mOQ).Append("\r\n");
            b.Append("balance: " + balance).Append("\r\n");
            b.Append("sellingPrice: " + sellingPrice).Append("\r\n");
            b.Append("cOGS: " + cOGS).Append("\r\n");
            b.Append("createdOn: " + createdOn).Append("\r\n");
            b.Append("createdBy: " + createdBy).Append("\r\n");
            b.Append("modifiedOn: " + modifiedOn).Append("\r\n");
            b.Append("modifiedBy: " + modifiedBy).Append("\r\n");
            b.Append("retired: " + retired).Append("\r\n");
            b.Append("retiredOn: " + retiredOn).Append("\r\n");
            b.Append("retiredBy: " + retiredBy).Append("\r\n");
            return b.ToString();
        }
    }

    public class ProductCollection : BindingList<Product> { }
}
