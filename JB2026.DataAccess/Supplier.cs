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
    public class Supplier
    {
        private Guid key = Guid.Empty;
        private Guid supplierId = Guid.Empty;
        private string supplierName = string.Empty;
        private string loginAccount = string.Empty;
        private string loginPassword = string.Empty;
        private string metadataXml = string.Empty;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public class MetadataAttribute
        {
            private string name = string.Empty;
            private string value = string.Empty;
            public string Name { get { return name; } set { name = value; } }
            public string Value { get { return value; } set { this.value = value; } }
        }

        public class MetadataAttributes : BindingList<MetadataAttribute> { }

        private Dictionary<string, string> _metadataList = new Dictionary<string, string>();
        public Dictionary<string, string> MetadataList { get { return _metadataList; } }

        public Supplier() { }

        public static Supplier? Load(Guid supplierId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSupplier_SelRec", new SqlParameter[] { new SqlParameter("@SupplierId", supplierId) });
            if (reader.Read()) { var r = new Supplier(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Supplier? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSupplier_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Supplier(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SupplierCollection LoadCollection()
            => LoadCollection("spSupplier_SelAll", new SqlParameter[] { });

        public static SupplierCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSupplier_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SupplierCollection LoadCollection(string whereClause)
            => LoadCollection("spSupplier_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SupplierCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSupplier_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SupplierCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SupplierCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Supplier(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid supplierId)
            => SqlHelper.Default.ExecuteNonQuery("spSupplier_DelRec", new SqlParameter[] { new SqlParameter("@SupplierId", supplierId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) supplierId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) supplierName = reader.GetString(1);
                if (!reader.IsDBNull(2)) loginAccount = reader.GetString(2);
                if (!reader.IsDBNull(3)) loginPassword = reader.GetString(3);
                if (!reader.IsDBNull(4))
                {
                    SqlXml sqlXml = reader.GetSqlXml(4);
                    metadataXml = sqlXml.Value;
                    ProcessingNodes(sqlXml.Value);
                }
                if (!reader.IsDBNull(5)) createdOn = reader.GetDateTime(5);
                if (!reader.IsDBNull(6)) createdBy = reader.GetGuid(6);
                if (!reader.IsDBNull(7)) modifiedOn = reader.GetDateTime(7);
                if (!reader.IsDBNull(8)) modifiedBy = reader.GetGuid(8);
                if (!reader.IsDBNull(9)) retired = reader.GetBoolean(9);
                if (!reader.IsDBNull(10)) retiredOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) retiredBy = reader.GetGuid(11);
            }
        }

        public void Delete() => Delete(this.SupplierId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != SupplierId) this.Delete(); Update(); }
        }

        public Guid SupplierId { get { return supplierId; } set { supplierId = value; } }
        public string SupplierName { get { return supplierName; } set { supplierName = value; } }
        public string LoginAccount { get { return loginAccount; } set { loginAccount = value; } }
        public string LoginPassword { get { return loginPassword; } set { loginPassword = value; } }
        public string MetadataXml { get { return metadataXml; } set { metadataXml = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        public void PrepareMetadataXml() => metadataXml = GenerateXml();

        private void ProcessingNodes(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return;
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var nodeList = doc.GetElementsByTagName("data");
            _metadataList = new Dictionary<string, string>();
            foreach (XmlNode node in nodeList)
            {
                string name = node.Attributes?["name"]?.Value ?? string.Empty;
                string val = node.Attributes?["value"]?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(name)) _metadataList[name] = val;
            }
        }

        private string GenerateXml()
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            foreach (var kvp in _metadataList)
                sb.Append("<data name=\"" + kvp.Key + "\" value=\"" + kvp.Value + "\" />");
            sb.Append("</root>");
            return sb.ToString();
        }

        public MetadataAttributes GetMetadataList()
        {
            var list = new MetadataAttributes();
            foreach (var kvp in _metadataList)
                list.Add(new MetadataAttribute { Name = kvp.Key, Value = kvp.Value });
            return list;
        }

        public string GetMetadata(string name)
        {
            if (_metadataList.TryGetValue(name, out var val)) return val;
            return string.Empty;
        }

        public void SetMetadata(string name, string value) => _metadataList[name] = value;
        public void SetMetadata(string name, int value) => _metadataList[name] = value.ToString();
        public void SetMetadata(string name, bool value) => _metadataList[name] = value.ToString();

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSupplier_InsRec", "@SupplierId", out var rv, GetInsertParameterValues());
            supplierId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSupplier_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@SupplierId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.SupplierId),
            GetSqlParameter("@SupplierName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.SupplierName),
            GetSqlParameter("@LoginAccount", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.LoginAccount),
            GetSqlParameter("@LoginPassword", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.LoginPassword),
            GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.MetadataXml),
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
            GetSqlParameterWithoutDirection("@SupplierId", SqlDbType.UniqueIdentifier, 16, this.SupplierId),
            GetSqlParameterWithoutDirection("@SupplierName", SqlDbType.NVarChar, 64, this.SupplierName),
            GetSqlParameterWithoutDirection("@LoginAccount", SqlDbType.NVarChar, 64, this.LoginAccount),
            GetSqlParameterWithoutDirection("@LoginPassword", SqlDbType.NVarChar, 64, this.LoginPassword),
            GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.MetadataXml),
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
            b.Append("supplierId: " + supplierId).Append("\r\n");
            b.Append("supplierName: " + supplierName).Append("\r\n");
            b.Append("loginAccount: " + loginAccount).Append("\r\n");
            b.Append("loginPassword: " + loginPassword).Append("\r\n");
            b.Append("metadataXml: " + metadataXml).Append("\r\n");
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

    public class SupplierCollection : BindingList<Supplier> { }
}
