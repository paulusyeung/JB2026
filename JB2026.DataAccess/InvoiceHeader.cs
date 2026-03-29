using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class InvoiceHeader
    {
        private Guid key = Guid.Empty;
        private Guid headerId = Guid.Empty;
        private Guid customerId = Guid.Empty;
        private string billTo = string.Empty;
        private string shipTo = string.Empty;
        private DateTime invoiceDate = DateTime.Parse("1900-1-1");
        private string invoiceNumber = string.Empty;
        private decimal invoiceAmount = 0;
        private string iCNumber = string.Empty;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public InvoiceHeader() { }

        public InvoiceHeader(Guid headerId, Guid customerId, string billTo, string shipTo, DateTime invoiceDate,
            string invoiceNumber, decimal invoiceAmount, string iCNumber,
            DateTime createdOn, Guid createdBy, DateTime modifiedOn, Guid modifiedBy,
            bool retired, DateTime retiredOn, Guid retiredBy)
        {
            this.headerId = headerId; this.customerId = customerId; this.billTo = billTo; this.shipTo = shipTo;
            this.invoiceDate = invoiceDate; this.invoiceNumber = invoiceNumber; this.invoiceAmount = invoiceAmount;
            this.iCNumber = iCNumber; this.createdOn = createdOn; this.createdBy = createdBy;
            this.modifiedOn = modifiedOn; this.modifiedBy = modifiedBy; this.retired = retired;
            this.retiredOn = retiredOn; this.retiredBy = retiredBy;
        }

        public static InvoiceHeader? Load(Guid headerId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceHeader_SelRec", new SqlParameter[] { new SqlParameter("@HeaderId", headerId) });
            if (reader.Read()) { var r = new InvoiceHeader(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceHeader? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spInvoiceHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new InvoiceHeader(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static InvoiceHeaderCollection LoadCollection()
            => LoadCollection("spInvoiceHeader_SelAll", new SqlParameter[] { });

        public static InvoiceHeaderCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceHeader_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceHeaderCollection LoadCollection(string whereClause)
            => LoadCollection("spInvoiceHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static InvoiceHeaderCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spInvoiceHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static InvoiceHeaderCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new InvoiceHeaderCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new InvoiceHeader(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid headerId)
            => SqlHelper.Default.ExecuteNonQuery("spInvoiceHeader_DelRec", new SqlParameter[] { new SqlParameter("@HeaderId", headerId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) headerId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) customerId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) billTo = reader.GetString(2);
                if (!reader.IsDBNull(3)) shipTo = reader.GetString(3);
                if (!reader.IsDBNull(4)) invoiceDate = reader.GetDateTime(4);
                if (!reader.IsDBNull(5)) invoiceNumber = reader.GetString(5);
                if (!reader.IsDBNull(6)) invoiceAmount = reader.GetDecimal(6);
                if (!reader.IsDBNull(7)) iCNumber = reader.GetString(7);
                if (!reader.IsDBNull(8)) createdOn = reader.GetDateTime(8);
                if (!reader.IsDBNull(9)) createdBy = reader.GetGuid(9);
                if (!reader.IsDBNull(10)) modifiedOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) modifiedBy = reader.GetGuid(11);
                if (!reader.IsDBNull(12)) retired = reader.GetBoolean(12);
                if (!reader.IsDBNull(13)) retiredOn = reader.GetDateTime(13);
                if (!reader.IsDBNull(14)) retiredBy = reader.GetGuid(14);
            }
        }

        public void Delete() => Delete(this.HeaderId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != HeaderId) this.Delete(); Update(); }
        }

        public Guid HeaderId { get { return headerId; } set { headerId = value; } }
        public Guid CustomerId { get { return customerId; } set { customerId = value; } }
        public string BillTo { get { return billTo; } set { billTo = value; } }
        public string ShipTo { get { return shipTo; } set { shipTo = value; } }
        public DateTime InvoiceDate { get { return invoiceDate; } set { invoiceDate = value; } }
        public string InvoiceNumber { get { return invoiceNumber; } set { invoiceNumber = value; } }
        public decimal InvoiceAmount { get { return invoiceAmount; } set { invoiceAmount = value; } }
        public string ICNumber { get { return iCNumber; } set { iCNumber = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spInvoiceHeader_InsRec", "@HeaderId", out var rv, GetInsertParameterValues());
            headerId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spInvoiceHeader_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@HeaderId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameter("@CustomerId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CustomerId),
            GetSqlParameter("@BillTo", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.BillTo),
            GetSqlParameter("@ShipTo", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.ShipTo),
            GetSqlParameter("@InvoiceDate", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.InvoiceDate),
            GetSqlParameter("@InvoiceNumber", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.InvoiceNumber),
            GetSqlParameter("@InvoiceAmount", ParameterDirection.Input, SqlDbType.Decimal, 9, this.InvoiceAmount),
            GetSqlParameter("@ICNumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.ICNumber),
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
            GetSqlParameterWithoutDirection("@HeaderId", SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameterWithoutDirection("@CustomerId", SqlDbType.UniqueIdentifier, 16, this.CustomerId),
            GetSqlParameterWithoutDirection("@BillTo", SqlDbType.NVarChar, 256, this.BillTo),
            GetSqlParameterWithoutDirection("@ShipTo", SqlDbType.NVarChar, 256, this.ShipTo),
            GetSqlParameterWithoutDirection("@InvoiceDate", SqlDbType.SmallDateTime, 4, this.InvoiceDate),
            GetSqlParameterWithoutDirection("@InvoiceNumber", SqlDbType.NVarChar, 10, this.InvoiceNumber),
            GetSqlParameterWithoutDirection("@InvoiceAmount", SqlDbType.Decimal, 9, this.InvoiceAmount),
            GetSqlParameterWithoutDirection("@ICNumber", SqlDbType.NVarChar, 32, this.ICNumber),
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
            b.Append("headerId: " + headerId).Append("\r\n");
            b.Append("customerId: " + customerId).Append("\r\n");
            b.Append("billTo: " + billTo).Append("\r\n");
            b.Append("shipTo: " + shipTo).Append("\r\n");
            b.Append("invoiceDate: " + invoiceDate).Append("\r\n");
            b.Append("invoiceNumber: " + invoiceNumber).Append("\r\n");
            b.Append("invoiceAmount: " + invoiceAmount).Append("\r\n");
            b.Append("iCNumber: " + iCNumber).Append("\r\n");
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

    public class InvoiceHeaderCollection : BindingList<InvoiceHeader> { }
}
