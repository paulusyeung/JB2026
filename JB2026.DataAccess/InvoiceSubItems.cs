using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class InvoiceSubItems
    {
        private Guid key = Guid.Empty;
        private Guid subItemId = Guid.Empty;
        private Guid itemId = Guid.Empty;
        private int subLineNumber;
        private string description = string.Empty;
        private decimal quantity;
        private string uoM = string.Empty;
        private decimal price;
        private decimal amount;

        public InvoiceSubItems() { }

        public InvoiceSubItems(Guid subItemId, Guid itemId, int subLineNumber, string description,
            decimal quantity, string uoM, decimal price, decimal amount)
        {
            this.subItemId = subItemId; this.itemId = itemId; this.subLineNumber = subLineNumber;
            this.description = description; this.quantity = quantity; this.uoM = uoM;
            this.price = price; this.amount = amount;
        }

        public static InvoiceSubItems? Load(Guid subItemId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceSubItems_SelRec", new SqlParameter[] { new SqlParameter("@SubItemId", subItemId) });
            if (reader.Read()) { var r = new InvoiceSubItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceSubItems? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new InvoiceSubItems(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceSubItemsCollection LoadCollection()
            => LoadCollection("spInvoiceSubItems_SelAll", new SqlParameter[] { });

        public static InvoiceSubItemsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceSubItems_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceSubItemsCollection LoadCollection(string whereClause)
            => LoadCollection("spInvoiceSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static InvoiceSubItemsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceSubItems_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceSubItemsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new InvoiceSubItemsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new InvoiceSubItems(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid subItemId)
            => SqlHelper.Default.ExecuteNonQuery("spInvoiceSubItems_DelRec", new SqlParameter[] { new SqlParameter("@SubItemId", subItemId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) subItemId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) itemId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) subLineNumber = reader.GetInt32(2);
                if (!reader.IsDBNull(3)) description = reader.GetString(3);
                if (!reader.IsDBNull(4)) quantity = reader.GetDecimal(4);
                if (!reader.IsDBNull(5)) uoM = reader.GetString(5);
                if (!reader.IsDBNull(6)) price = reader.GetDecimal(6);
                if (!reader.IsDBNull(7)) amount = reader.GetDecimal(7);
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
        public string Description { get { return description; } set { description = value; } }
        public decimal Quantity { get { return quantity; } set { quantity = value; } }
        public string UoM { get { return uoM; } set { uoM = value; } }
        public decimal Price { get { return price; } set { price = value; } }
        public decimal Amount { get { return amount; } set { amount = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spInvoiceSubItems_InsRec", "@SubItemId", out var rv, GetInsertParameterValues());
            subItemId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spInvoiceSubItems_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@SubItemId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.SubItemId),
            GetSqlParameter("@ItemId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameter("@SubLineNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.SubLineNumber),
            GetSqlParameter("@Description", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.Description),
            GetSqlParameter("@Quantity", ParameterDirection.Input, SqlDbType.Decimal, 9, this.Quantity),
            GetSqlParameter("@UoM", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.UoM),
            GetSqlParameter("@Price", ParameterDirection.Input, SqlDbType.Decimal, 9, this.Price),
            GetSqlParameter("@Amount", ParameterDirection.Input, SqlDbType.Decimal, 9, this.Amount)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@SubItemId", SqlDbType.UniqueIdentifier, 16, this.SubItemId),
            GetSqlParameterWithoutDirection("@ItemId", SqlDbType.UniqueIdentifier, 16, this.ItemId),
            GetSqlParameterWithoutDirection("@SubLineNumber", SqlDbType.Int, 4, this.SubLineNumber),
            GetSqlParameterWithoutDirection("@Description", SqlDbType.NVarChar, 64, this.Description),
            GetSqlParameterWithoutDirection("@Quantity", SqlDbType.Decimal, 9, this.Quantity),
            GetSqlParameterWithoutDirection("@UoM", SqlDbType.NVarChar, 10, this.UoM),
            GetSqlParameterWithoutDirection("@Price", SqlDbType.Decimal, 9, this.Price),
            GetSqlParameterWithoutDirection("@Amount", SqlDbType.Decimal, 9, this.Amount)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("subItemId: " + subItemId).Append("\r\n");
            b.Append("itemId: " + itemId).Append("\r\n");
            b.Append("subLineNumber: " + subLineNumber).Append("\r\n");
            b.Append("description: " + description).Append("\r\n");
            b.Append("quantity: " + quantity).Append("\r\n");
            b.Append("uoM: " + uoM).Append("\r\n");
            b.Append("price: " + price).Append("\r\n");
            b.Append("amount: " + amount).Append("\r\n");
            return b.ToString();
        }
    }

    public class InvoiceSubItemsCollection : BindingList<InvoiceSubItems> { }
}
