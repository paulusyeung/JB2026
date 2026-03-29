using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class JobOrder
    {
        private Guid key = Guid.Empty;
        private Guid orderId = Guid.Empty;
        private int orderType;
        private string orderNumber = string.Empty;
        private int jobNumber;
        private string customerName = string.Empty;
        private string customerRef = string.Empty;
        private string orderTitle = string.Empty;
        private string productCode = string.Empty;
        private string productStyle = string.Empty;
        private string productDetails = string.Empty;
        private DateTime orderedOn = DateTime.Parse("1900-1-1");
        private Guid orderedBy = Guid.Empty;
        private string outputRef = string.Empty;
        private string invoiceRef = string.Empty;
        private decimal invoiceAmount;
        private decimal qty;
        private string qtyText = string.Empty;
        private DateTime requiredOn = DateTime.Parse("1900-1-1");
        private DateTime completedOn = DateTime.Parse("1900-1-1");
        private string sONumber = string.Empty;
        private string pONumber = string.Empty;
        private string originalSONumber = string.Empty;
        private string originalPONumber = string.Empty;
        private string paymentTerms = string.Empty;
        private string remarks = string.Empty;
        private int status;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public JobOrder() { }

        public static JobOrder? Load(Guid orderId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobOrder_SelRec", new SqlParameter[] { new SqlParameter("@OrderId", orderId) });
            if (reader.Read()) { var r = new JobOrder(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobOrder? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobOrder_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobOrder(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobOrderCollection LoadCollection()
            => LoadCollection("spJobOrder_SelAll", new SqlParameter[] { });

        public static JobOrderCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobOrder_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobOrderCollection LoadCollection(string whereClause)
            => LoadCollection("spJobOrder_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobOrderCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobOrder_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobOrderCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobOrderCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobOrder(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid orderId)
            => SqlHelper.Default.ExecuteNonQuery("spJobOrder_DelRec", new SqlParameter[] { new SqlParameter("@OrderId", orderId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) orderId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) orderType = reader.GetInt32(1);
                if (!reader.IsDBNull(2)) orderNumber = reader.GetString(2);
                if (!reader.IsDBNull(3)) jobNumber = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) customerName = reader.GetString(4);
                if (!reader.IsDBNull(5)) customerRef = reader.GetString(5);
                if (!reader.IsDBNull(6)) orderTitle = reader.GetString(6);
                if (!reader.IsDBNull(7)) productCode = reader.GetString(7);
                if (!reader.IsDBNull(8)) productStyle = reader.GetString(8);
                if (!reader.IsDBNull(9)) productDetails = reader.GetString(9);
                if (!reader.IsDBNull(10)) orderedOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) orderedBy = reader.GetGuid(11);
                if (!reader.IsDBNull(12)) outputRef = reader.GetString(12);
                if (!reader.IsDBNull(13)) invoiceRef = reader.GetString(13);
                if (!reader.IsDBNull(14)) invoiceAmount = reader.GetDecimal(14);
                if (!reader.IsDBNull(15)) qty = reader.GetDecimal(15);
                if (!reader.IsDBNull(16)) qtyText = reader.GetString(16);
                if (!reader.IsDBNull(17)) requiredOn = reader.GetDateTime(17);
                if (!reader.IsDBNull(18)) completedOn = reader.GetDateTime(18);
                if (!reader.IsDBNull(19)) sONumber = reader.GetString(19);
                if (!reader.IsDBNull(20)) pONumber = reader.GetString(20);
                if (!reader.IsDBNull(21)) originalSONumber = reader.GetString(21);
                if (!reader.IsDBNull(22)) originalPONumber = reader.GetString(22);
                if (!reader.IsDBNull(23)) paymentTerms = reader.GetString(23);
                if (!reader.IsDBNull(24)) remarks = reader.GetString(24);
                if (!reader.IsDBNull(25)) status = reader.GetInt32(25);
                if (!reader.IsDBNull(26)) createdOn = reader.GetDateTime(26);
                if (!reader.IsDBNull(27)) createdBy = reader.GetGuid(27);
                if (!reader.IsDBNull(28)) modifiedOn = reader.GetDateTime(28);
                if (!reader.IsDBNull(29)) modifiedBy = reader.GetGuid(29);
                if (!reader.IsDBNull(30)) retired = reader.GetBoolean(30);
                if (!reader.IsDBNull(31)) retiredOn = reader.GetDateTime(31);
                if (!reader.IsDBNull(32)) retiredBy = reader.GetGuid(32);
            }
        }

        public void Delete() => Delete(this.OrderId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != OrderId) this.Delete(); Update(); }
        }

        public Guid OrderId { get { return orderId; } set { orderId = value; } }
        public int OrderType { get { return orderType; } set { orderType = value; } }
        public string OrderNumber { get { return orderNumber; } set { orderNumber = value; } }
        public int JobNumber { get { return jobNumber; } set { jobNumber = value; } }
        public string CustomerName { get { return customerName; } set { customerName = value; } }
        public string CustomerRef { get { return customerRef; } set { customerRef = value; } }
        public string OrderTitle { get { return orderTitle; } set { orderTitle = value; } }
        public string ProductCode { get { return productCode; } set { productCode = value; } }
        public string ProductStyle { get { return productStyle; } set { productStyle = value; } }
        public string ProductDetails { get { return productDetails; } set { productDetails = value; } }
        public DateTime OrderedOn { get { return orderedOn; } set { orderedOn = value; } }
        public Guid OrderedBy { get { return orderedBy; } set { orderedBy = value; } }
        public string OutputRef { get { return outputRef; } set { outputRef = value; } }
        public string InvoiceRef { get { return invoiceRef; } set { invoiceRef = value; } }
        public decimal InvoiceAmount { get { return invoiceAmount; } set { invoiceAmount = value; } }
        public decimal Qty { get { return qty; } set { qty = value; } }
        public string QtyText { get { return qtyText; } set { qtyText = value; } }
        public DateTime RequiredOn { get { return requiredOn; } set { requiredOn = value; } }
        public DateTime CompletedOn { get { return completedOn; } set { completedOn = value; } }
        public string SONumber { get { return sONumber; } set { sONumber = value; } }
        public string PONumber { get { return pONumber; } set { pONumber = value; } }
        public string OriginalSONumber { get { return originalSONumber; } set { originalSONumber = value; } }
        public string OriginalPONumber { get { return originalPONumber; } set { originalPONumber = value; } }
        public string PaymentTerms { get { return paymentTerms; } set { paymentTerms = value; } }
        public string Remarks { get { return remarks; } set { remarks = value; } }
        public int Status { get { return status; } set { status = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spJobOrder_InsRec", "@OrderId", out var rv, GetInsertParameterValues());
            orderId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobOrder_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@OrderId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameter("@OrderType", ParameterDirection.Input, SqlDbType.Int, 4, this.OrderType),
            GetSqlParameter("@OrderNumber", ParameterDirection.Input, SqlDbType.VarChar, 32, this.OrderNumber),
            GetSqlParameter("@JobNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.JobNumber),
            GetSqlParameter("@CustomerName", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.CustomerName),
            GetSqlParameter("@CustomerRef", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.CustomerRef),
            GetSqlParameter("@OrderTitle", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.OrderTitle),
            GetSqlParameter("@ProductCode", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.ProductCode),
            GetSqlParameter("@ProductStyle", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.ProductStyle),
            GetSqlParameter("@ProductDetails", ParameterDirection.Input, SqlDbType.NVarChar, -1, this.ProductDetails),
            GetSqlParameter("@OrderedOn", ParameterDirection.Input, SqlDbType.DateTime, 8, this.OrderedOn),
            GetSqlParameter("@OrderedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OrderedBy),
            GetSqlParameter("@OutputRef", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.OutputRef),
            GetSqlParameter("@InvoiceRef", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.InvoiceRef),
            GetSqlParameter("@InvoiceAmount", ParameterDirection.Input, SqlDbType.Decimal, 9, this.InvoiceAmount),
            GetSqlParameter("@Qty", ParameterDirection.Input, SqlDbType.Decimal, 9, this.Qty),
            GetSqlParameter("@QtyText", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.QtyText),
            GetSqlParameter("@RequiredOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.RequiredOn),
            GetSqlParameter("@CompletedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CompletedOn),
            GetSqlParameter("@SONumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.SONumber),
            GetSqlParameter("@PONumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.PONumber),
            GetSqlParameter("@OriginalSONumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.OriginalSONumber),
            GetSqlParameter("@OriginalPONumber", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.OriginalPONumber),
            GetSqlParameter("@PaymentTerms", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.PaymentTerms),
            GetSqlParameter("@Remarks", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.Remarks),
            GetSqlParameter("@Status", ParameterDirection.Input, SqlDbType.Int, 4, this.Status),
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
            GetSqlParameterWithoutDirection("@OrderId", SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameterWithoutDirection("@OrderType", SqlDbType.Int, 4, this.OrderType),
            GetSqlParameterWithoutDirection("@OrderNumber", SqlDbType.VarChar, 32, this.OrderNumber),
            GetSqlParameterWithoutDirection("@JobNumber", SqlDbType.Int, 4, this.JobNumber),
            GetSqlParameterWithoutDirection("@CustomerName", SqlDbType.NVarChar, 256, this.CustomerName),
            GetSqlParameterWithoutDirection("@CustomerRef", SqlDbType.NVarChar, 64, this.CustomerRef),
            GetSqlParameterWithoutDirection("@OrderTitle", SqlDbType.NVarChar, 512, this.OrderTitle),
            GetSqlParameterWithoutDirection("@ProductCode", SqlDbType.NVarChar, 32, this.ProductCode),
            GetSqlParameterWithoutDirection("@ProductStyle", SqlDbType.NVarChar, 64, this.ProductStyle),
            GetSqlParameterWithoutDirection("@ProductDetails", SqlDbType.NVarChar, -1, this.ProductDetails),
            GetSqlParameterWithoutDirection("@OrderedOn", SqlDbType.DateTime, 8, this.OrderedOn),
            GetSqlParameterWithoutDirection("@OrderedBy", SqlDbType.UniqueIdentifier, 16, this.OrderedBy),
            GetSqlParameterWithoutDirection("@OutputRef", SqlDbType.NVarChar, 64, this.OutputRef),
            GetSqlParameterWithoutDirection("@InvoiceRef", SqlDbType.NVarChar, 64, this.InvoiceRef),
            GetSqlParameterWithoutDirection("@InvoiceAmount", SqlDbType.Decimal, 9, this.InvoiceAmount),
            GetSqlParameterWithoutDirection("@Qty", SqlDbType.Decimal, 9, this.Qty),
            GetSqlParameterWithoutDirection("@QtyText", SqlDbType.NVarChar, 32, this.QtyText),
            GetSqlParameterWithoutDirection("@RequiredOn", SqlDbType.SmallDateTime, 4, this.RequiredOn),
            GetSqlParameterWithoutDirection("@CompletedOn", SqlDbType.SmallDateTime, 4, this.CompletedOn),
            GetSqlParameterWithoutDirection("@SONumber", SqlDbType.NVarChar, 32, this.SONumber),
            GetSqlParameterWithoutDirection("@PONumber", SqlDbType.NVarChar, 32, this.PONumber),
            GetSqlParameterWithoutDirection("@OriginalSONumber", SqlDbType.NVarChar, 32, this.OriginalSONumber),
            GetSqlParameterWithoutDirection("@OriginalPONumber", SqlDbType.NVarChar, 32, this.OriginalPONumber),
            GetSqlParameterWithoutDirection("@PaymentTerms", SqlDbType.NVarChar, 256, this.PaymentTerms),
            GetSqlParameterWithoutDirection("@Remarks", SqlDbType.NVarChar, 512, this.Remarks),
            GetSqlParameterWithoutDirection("@Status", SqlDbType.Int, 4, this.Status),
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
            b.Append("orderId: " + orderId).Append("\r\n");
            b.Append("orderType: " + orderType).Append("\r\n");
            b.Append("orderNumber: " + orderNumber).Append("\r\n");
            b.Append("jobNumber: " + jobNumber).Append("\r\n");
            b.Append("customerName: " + customerName).Append("\r\n");
            b.Append("customerRef: " + customerRef).Append("\r\n");
            b.Append("orderTitle: " + orderTitle).Append("\r\n");
            b.Append("productCode: " + productCode).Append("\r\n");
            b.Append("productStyle: " + productStyle).Append("\r\n");
            b.Append("productDetails: " + productDetails).Append("\r\n");
            b.Append("orderedOn: " + orderedOn).Append("\r\n");
            b.Append("orderedBy: " + orderedBy).Append("\r\n");
            b.Append("outputRef: " + outputRef).Append("\r\n");
            b.Append("invoiceRef: " + invoiceRef).Append("\r\n");
            b.Append("invoiceAmount: " + invoiceAmount).Append("\r\n");
            b.Append("qty: " + qty).Append("\r\n");
            b.Append("qtyText: " + qtyText).Append("\r\n");
            b.Append("requiredOn: " + requiredOn).Append("\r\n");
            b.Append("completedOn: " + completedOn).Append("\r\n");
            b.Append("sONumber: " + sONumber).Append("\r\n");
            b.Append("pONumber: " + pONumber).Append("\r\n");
            b.Append("originalSONumber: " + originalSONumber).Append("\r\n");
            b.Append("originalPONumber: " + originalPONumber).Append("\r\n");
            b.Append("paymentTerms: " + paymentTerms).Append("\r\n");
            b.Append("remarks: " + remarks).Append("\r\n");
            b.Append("status: " + status).Append("\r\n");
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

    public class JobOrderCollection : BindingList<JobOrder> { }
}
