using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class SmlRtfHeader
    {
        private Guid key = Guid.Empty;
        private Guid headerId = Guid.Empty;
        private string rtfFileName = string.Empty;
        private string purchaseOrder = string.Empty;
        private string customerPO = string.Empty;
        private DateTime orderedOn = DateTime.Parse("1900-1-1");
        private string orderedBy = string.Empty;
        private string originalPO = string.Empty;
        private string salesOrder = string.Empty;
        private string originalSO = string.Empty;
        private string remarks = string.Empty;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public SmlRtfHeader() { }

        public static SmlRtfHeader? Load(Guid headerId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfHeader_SelRec", new SqlParameter[] { new SqlParameter("@HeaderId", headerId) });
            if (reader.Read()) { var r = new SmlRtfHeader(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfHeader? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new SmlRtfHeader(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfHeaderCollection LoadCollection()
            => LoadCollection("spSmlRtfHeader_SelAll", new SqlParameter[] { });

        public static SmlRtfHeaderCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfHeader_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfHeaderCollection LoadCollection(string whereClause)
            => LoadCollection("spSmlRtfHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SmlRtfHeaderCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfHeader_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfHeaderCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SmlRtfHeaderCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new SmlRtfHeader(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid headerId)
            => SqlHelper.Default.ExecuteNonQuery("spSmlRtfHeader_DelRec", new SqlParameter[] { new SqlParameter("@HeaderId", headerId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) headerId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) rtfFileName = reader.GetString(1);
                if (!reader.IsDBNull(2)) purchaseOrder = reader.GetString(2);
                if (!reader.IsDBNull(3)) customerPO = reader.GetString(3);
                if (!reader.IsDBNull(4)) orderedOn = reader.GetDateTime(4);
                if (!reader.IsDBNull(5)) orderedBy = reader.GetString(5);
                if (!reader.IsDBNull(6)) originalPO = reader.GetString(6);
                if (!reader.IsDBNull(7)) salesOrder = reader.GetString(7);
                if (!reader.IsDBNull(8)) originalSO = reader.GetString(8);
                if (!reader.IsDBNull(9)) remarks = reader.GetString(9);
                if (!reader.IsDBNull(10)) createdOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) createdBy = reader.GetGuid(11);
                if (!reader.IsDBNull(12)) modifiedOn = reader.GetDateTime(12);
                if (!reader.IsDBNull(13)) modifiedBy = reader.GetGuid(13);
                if (!reader.IsDBNull(14)) retired = reader.GetBoolean(14);
                if (!reader.IsDBNull(15)) retiredOn = reader.GetDateTime(15);
                if (!reader.IsDBNull(16)) retiredBy = reader.GetGuid(16);
            }
        }

        public void Delete() => Delete(this.HeaderId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != HeaderId) this.Delete(); Update(); }
        }

        public Guid HeaderId { get { return headerId; } set { headerId = value; } }
        public string RtfFileName { get { return rtfFileName; } set { rtfFileName = value; } }
        public string PurchaseOrder { get { return purchaseOrder; } set { purchaseOrder = value; } }
        public string CustomerPO { get { return customerPO; } set { customerPO = value; } }
        public DateTime OrderedOn { get { return orderedOn; } set { orderedOn = value; } }
        public string OrderedBy { get { return orderedBy; } set { orderedBy = value; } }
        public string OriginalPO { get { return originalPO; } set { originalPO = value; } }
        public string SalesOrder { get { return salesOrder; } set { salesOrder = value; } }
        public string OriginalSO { get { return originalSO; } set { originalSO = value; } }
        public string Remarks { get { return remarks; } set { remarks = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSmlRtfHeader_InsRec", "@HeaderId", out var rv, GetInsertParameterValues());
            headerId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSmlRtfHeader_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@HeaderId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameter("@RtfFileName", ParameterDirection.Input, SqlDbType.NVarChar, 256, this.RtfFileName),
            GetSqlParameter("@PurchaseOrder", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.PurchaseOrder),
            GetSqlParameter("@CustomerPO", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.CustomerPO),
            GetSqlParameter("@OrderedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.OrderedOn),
            GetSqlParameter("@OrderedBy", ParameterDirection.Input, SqlDbType.NVarChar, 32, this.OrderedBy),
            GetSqlParameter("@OriginalPO", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.OriginalPO),
            GetSqlParameter("@SalesOrder", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.SalesOrder),
            GetSqlParameter("@OriginalSO", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.OriginalSO),
            GetSqlParameter("@Remarks", ParameterDirection.Input, SqlDbType.NVarChar, 512, this.Remarks),
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
            GetSqlParameterWithoutDirection("@RtfFileName", SqlDbType.NVarChar, 256, this.RtfFileName),
            GetSqlParameterWithoutDirection("@PurchaseOrder", SqlDbType.NVarChar, 16, this.PurchaseOrder),
            GetSqlParameterWithoutDirection("@CustomerPO", SqlDbType.NVarChar, 16, this.CustomerPO),
            GetSqlParameterWithoutDirection("@OrderedOn", SqlDbType.SmallDateTime, 4, this.OrderedOn),
            GetSqlParameterWithoutDirection("@OrderedBy", SqlDbType.NVarChar, 32, this.OrderedBy),
            GetSqlParameterWithoutDirection("@OriginalPO", SqlDbType.NVarChar, 16, this.OriginalPO),
            GetSqlParameterWithoutDirection("@SalesOrder", SqlDbType.NVarChar, 16, this.SalesOrder),
            GetSqlParameterWithoutDirection("@OriginalSO", SqlDbType.NVarChar, 16, this.OriginalSO),
            GetSqlParameterWithoutDirection("@Remarks", SqlDbType.NVarChar, 512, this.Remarks),
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
            b.Append("rtfFileName: " + rtfFileName).Append("\r\n");
            b.Append("purchaseOrder: " + purchaseOrder).Append("\r\n");
            b.Append("customerPO: " + customerPO).Append("\r\n");
            b.Append("orderedOn: " + orderedOn).Append("\r\n");
            b.Append("orderedBy: " + orderedBy).Append("\r\n");
            b.Append("originalPO: " + originalPO).Append("\r\n");
            b.Append("salesOrder: " + salesOrder).Append("\r\n");
            b.Append("originalSO: " + originalSO).Append("\r\n");
            b.Append("remarks: " + remarks).Append("\r\n");
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

    public class SmlRtfHeaderCollection : BindingList<SmlRtfHeader> { }
}
