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
    public class Customers
    {
        private Guid key = Guid.Empty;
        private Guid customerId = Guid.Empty;
        private string customerName = string.Empty;
        private string loginAccount = string.Empty;
        private string loginPassword = string.Empty;
        private Dictionary<string, MetadataAttributes> metadataXml = new Dictionary<string, MetadataAttributes>();
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public Customers() { }

        public Customers(Guid customerId, string customerName, string loginAccount, string loginPassword,
            Dictionary<string, MetadataAttributes> metadataXml, DateTime createdOn, Guid createdBy,
            DateTime modifiedOn, Guid modifiedBy, bool retired, DateTime retiredOn, Guid retiredBy)
        {
            this.customerId = customerId;
            this.customerName = customerName;
            this.loginAccount = loginAccount;
            this.loginPassword = loginPassword;
            this.metadataXml = metadataXml;
            this.createdOn = createdOn;
            this.createdBy = createdBy;
            this.modifiedOn = modifiedOn;
            this.modifiedBy = modifiedBy;
            this.retired = retired;
            this.retiredOn = retiredOn;
            this.retiredBy = retiredBy;
        }

        public static Customers? Load(Guid customerId)
        {
            var parms = new SqlParameter[] { new SqlParameter("@CustomerId", customerId) };
            using var reader = SqlHelper.Default.ExecuteReader("spCustomers_SelRec", parms);
            if (reader.Read())
            {
                var result = new Customers();
                result.LoadFromReader(reader);
                return result;
            }
            return null;
        }

        public static Customers? LoadWhere(string whereClause)
        {
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) };
            using var reader = SqlHelper.Default.ExecuteReader("spCustomers_SelAll", parms);
            if (reader.Read())
            {
                var result = new Customers();
                result.LoadFromReader(reader);
                return result;
            }
            return null;
        }

        public static CustomersCollection LoadCollection()
        {
            return LoadCollection("spCustomers_SelAll", new SqlParameter[] { });
        }

        public static CustomersCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var orderClause = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++)
            {
                orderClause.Append(orderByColumns[i]);
                if (i != orderByColumns.Length - 1) orderClause.Append(", ");
            }
            orderClause.Append(ascending ? " ASC" : " DESC");
            var parms = new SqlParameter[] { new SqlParameter("@OrderBy", orderClause.ToString()) };
            return LoadCollection("spCustomers_SelAll", parms);
        }

        public static CustomersCollection LoadCollection(string whereClause)
        {
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) };
            return LoadCollection("spCustomers_SelAll", parms);
        }

        public static CustomersCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var orderClause = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++)
            {
                orderClause.Append(orderByColumns[i]);
                if (i != orderByColumns.Length - 1) orderClause.Append(", ");
            }
            orderClause.Append(ascending ? " ASC" : " DESC");
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", orderClause.ToString()) };
            return LoadCollection("spCustomers_SelAll", parms);
        }

        public static CustomersCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new CustomersCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read())
            {
                var tmp = new Customers();
                tmp.LoadFromReader(reader);
                result.Add(tmp);
            }
            return result;
        }

        public static void Delete(Guid customerId)
        {
            var parms = new SqlParameter[] { new SqlParameter("@CustomerId", customerId) };
            SqlHelper.Default.ExecuteNonQuery("spCustomers_DelRec", parms);
        }

        #region XML Manipulation

        public class MetadataAttribute
        {
            private string _key = string.Empty;
            private string _value = string.Empty;

            public MetadataAttribute(string key, string value) { _key = key; _value = value; }

            public string Key { get { return _key; } set { _key = value; } }
            public string Value { get { return _value; } set { _value = value; } }
        }

        public class MetadataAttributes : BindingList<MetadataAttribute> { }

        protected string RootNode = "Metadata";

        private void PrepareMetadataXml(SqlXml dataXml, out Dictionary<string, MetadataAttributes> metadataXml)
        {
            metadataXml = new Dictionary<string, MetadataAttributes>();
            if (!dataXml.IsNull)
            {
                var metadata = new XmlDocument();
                metadata.LoadXml(dataXml.Value);
                var dataList = metadata.SelectNodes("//data")!;
                if (dataList.Count > 0)
                {
                    var attributes = new MetadataAttributes();
                    foreach (XmlNode node in dataList)
                    {
                        string k = node.ChildNodes[0]!.InnerText;
                        string v = node.ChildNodes[1]!.InnerText;
                        attributes.Add(new MetadataAttribute(k, v));
                    }
                    if (attributes.Count > 0) metadataXml.Add("data", attributes);
                }
                else
                {
                    var attributes = new MetadataAttributes();
                    var targetNode = metadata.SelectNodes("//" + RootNode)!;
                    foreach (XmlNode node in targetNode)
                    {
                        if (node.HasChildNodes)
                            ProcessingNodes(node, ref metadataXml, attributes);
                        else
                        {
                            foreach (XmlAttribute attr in node.Attributes!)
                                attributes.Add(new MetadataAttribute(attr.Name, attr.Value));
                            if (attributes.Count > 0) metadataXml.Add("data", attributes);
                        }
                    }
                }
            }
        }

        private void ProcessingNodes(XmlNode node, ref Dictionary<string, MetadataAttributes> metadataXml, MetadataAttributes attributes)
        {
            foreach (XmlNode child in node)
            {
                attributes = new MetadataAttributes();
                string metadataKey = string.Empty;
                foreach (XmlAttribute attr in child.Attributes!)
                {
                    if (attr.Name == "id") metadataKey = attr.Value;
                    else attributes.Add(new MetadataAttribute(attr.Name, attr.Value));
                }
                if (metadataKey != string.Empty) metadataXml.Add(metadataKey, attributes);
            }
        }

        public string GenerateXml(Dictionary<string, MetadataAttributes> metadataXml)
        {
            var metadata = new XmlDocument();
            var node = metadata.AppendChild(metadata.CreateElement(RootNode))!;
            foreach (var kvp in metadataXml)
            {
                var element = metadata.CreateElement("record");
                element.SetAttribute("id", kvp.Key);
                foreach (var attr in kvp.Value) element.SetAttribute(attr.Key, attr.Value);
                node.AppendChild(element);
            }
            return metadata.OuterXml;
        }

        public MetadataAttributes GetMetadataList(string id)
            => metadataXml.ContainsKey(id) ? metadataXml[id] : new MetadataAttributes();

        public string GetMetadata(string key)
        {
            var list = GetMetadataList("data");
            foreach (var attr in list) { if (attr.Key == key) return attr.Value; }
            return string.Empty;
        }

        public void SetMetadata(string key, MetadataAttributes data)
        {
            metadataXml ??= new Dictionary<string, MetadataAttributes>();
            if (metadataXml.ContainsKey(key)) metadataXml[key] = data;
            else metadataXml.Add(key, data);
        }

        public void SetMetadata(string key, MetadataAttribute data)
        {
            var attributes = GetMetadataList(key);
            if (!attributes.Contains(data)) attributes.Add(data);
            SetMetadata(key, attributes);
        }

        public void SetMetadata(string key, string data)
            => SetMetadata("data", new MetadataAttribute(key, data));

        #endregion

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) customerId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) customerName = reader.GetString(1);
                if (!reader.IsDBNull(2)) loginAccount = reader.GetString(2);
                if (!reader.IsDBNull(3)) loginPassword = reader.GetString(3);
                if (!reader.IsDBNull(4)) PrepareMetadataXml(reader.GetSqlXml(4), out metadataXml);
                if (!reader.IsDBNull(5)) createdOn = reader.GetDateTime(5);
                if (!reader.IsDBNull(6)) createdBy = reader.GetGuid(6);
                if (!reader.IsDBNull(7)) modifiedOn = reader.GetDateTime(7);
                if (!reader.IsDBNull(8)) modifiedBy = reader.GetGuid(8);
                if (!reader.IsDBNull(9)) retired = reader.GetBoolean(9);
                if (!reader.IsDBNull(10)) retiredOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) retiredBy = reader.GetGuid(11);
            }
        }

        public void Delete() => Delete(this.CustomerId);

        public void Save()
        {
            if (key == Guid.Empty)
                Insert();
            else
            {
                if (key != CustomerId) this.Delete();
                Update();
            }
        }

        public Guid CustomerId { get { return customerId; } set { customerId = value; } }
        public string CustomerName { get { return customerName; } set { customerName = value; } }
        public string LoginAccount { get { return loginAccount; } set { loginAccount = value; } }
        public string LoginPassword { get { return loginPassword; } set { loginPassword = value; } }
        public Dictionary<string, MetadataAttributes> MetadataXml { get { return metadataXml; } set { metadataXml = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            var parms = GetInsertParameterValues();
            SqlHelper.Default.ExecuteNonQuery("spCustomers_InsRec", "@CustomerId", out var returnedValue, parms);
            customerId = returnedValue != null ? (Guid)returnedValue : Guid.Empty;
            key = returnedValue != null ? (Guid)returnedValue : Guid.Empty;
        }

        private void Update()
        {
            SqlHelper.Default.ExecuteNonQuery("spCustomers_UpdRec", GetUpdateParameterValues());
        }

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
        {
            return new SqlParameter(name, dbType, size) { Value = value, Direction = direction };
        }

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
        {
            return new SqlParameter(name, dbType, size) { Value = value };
        }

        private SqlParameter[] GetInsertParameterValues()
        {
            return new SqlParameter[]
            {
                GetSqlParameter("@CustomerId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.CustomerId),
                GetSqlParameter("@CustomerName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.CustomerName),
                GetSqlParameter("@LoginAccount", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.LoginAccount),
                GetSqlParameter("@LoginPassword", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.LoginPassword),
                GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml)),
                GetSqlParameter("@CreatedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CreatedOn),
                GetSqlParameter("@CreatedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
                GetSqlParameter("@ModifiedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.ModifiedOn),
                GetSqlParameter("@ModifiedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.ModifiedBy),
                GetSqlParameter("@Retired", ParameterDirection.Input, SqlDbType.Bit, 1, this.Retired),
                GetSqlParameter("@RetiredOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.RetiredOn),
                GetSqlParameter("@RetiredBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.RetiredBy)
            };
        }

        private SqlParameter[] GetUpdateParameterValues()
        {
            return new SqlParameter[]
            {
                GetSqlParameterWithoutDirection("@CustomerId", SqlDbType.UniqueIdentifier, 16, this.CustomerId),
                GetSqlParameterWithoutDirection("@CustomerName", SqlDbType.NVarChar, 64, this.CustomerName),
                GetSqlParameterWithoutDirection("@LoginAccount", SqlDbType.NVarChar, 64, this.LoginAccount),
                GetSqlParameterWithoutDirection("@LoginPassword", SqlDbType.NVarChar, 64, this.LoginPassword),
                GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml)),
                GetSqlParameterWithoutDirection("@CreatedOn", SqlDbType.SmallDateTime, 4, this.CreatedOn),
                GetSqlParameterWithoutDirection("@CreatedBy", SqlDbType.UniqueIdentifier, 16, this.CreatedBy),
                GetSqlParameterWithoutDirection("@ModifiedOn", SqlDbType.SmallDateTime, 4, this.ModifiedOn),
                GetSqlParameterWithoutDirection("@ModifiedBy", SqlDbType.UniqueIdentifier, 16, this.ModifiedBy),
                GetSqlParameterWithoutDirection("@Retired", SqlDbType.Bit, 1, this.Retired),
                GetSqlParameterWithoutDirection("@RetiredOn", SqlDbType.SmallDateTime, 4, this.RetiredOn),
                GetSqlParameterWithoutDirection("@RetiredBy", SqlDbType.UniqueIdentifier, 16, this.RetiredBy)
            };
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("customerId: " + customerId).Append("\r\n");
            builder.Append("customerName: " + customerName).Append("\r\n");
            builder.Append("loginAccount: " + loginAccount).Append("\r\n");
            builder.Append("loginPassword: " + loginPassword).Append("\r\n");
            builder.Append("metadataXml: " + GenerateXml(metadataXml)).Append("\r\n");
            builder.Append("createdOn: " + createdOn).Append("\r\n");
            builder.Append("createdBy: " + createdBy).Append("\r\n");
            builder.Append("modifiedOn: " + modifiedOn).Append("\r\n");
            builder.Append("modifiedBy: " + modifiedBy).Append("\r\n");
            builder.Append("retired: " + retired).Append("\r\n");
            builder.Append("retiredOn: " + retiredOn).Append("\r\n");
            builder.Append("retiredBy: " + retiredBy).Append("\r\n");
            return builder.ToString();
        }
    }

    public class CustomersCollection : BindingList<Customers> { }
}
