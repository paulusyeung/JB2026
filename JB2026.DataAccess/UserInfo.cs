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
    public class UserInfo
    {
        private Guid key = Guid.Empty;
        private Guid userId = Guid.Empty;
        private bool primaryRec;
        private string userName = string.Empty;
        private string userPassword = string.Empty;
        private string userAlias = string.Empty;
        private int userRole = 0;
        private Dictionary<string, MetadataAttributes> metadataXml = new Dictionary<string, MetadataAttributes>();
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public UserInfo() { }

        public UserInfo(Guid userId, bool primaryRec, string userName, string userPassword, string userAlias,
            int userRole, Dictionary<string, MetadataAttributes> metadataXml,
            DateTime createdOn, Guid createdBy, DateTime modifiedOn, Guid modifiedBy,
            bool retired, DateTime retiredOn, Guid retiredBy)
        {
            this.userId = userId;
            this.primaryRec = primaryRec;
            this.userName = userName;
            this.userPassword = userPassword;
            this.userAlias = userAlias;
            this.userRole = userRole;
            this.metadataXml = metadataXml;
            this.createdOn = createdOn;
            this.createdBy = createdBy;
            this.modifiedOn = modifiedOn;
            this.modifiedBy = modifiedBy;
            this.retired = retired;
            this.retiredOn = retiredOn;
            this.retiredBy = retiredBy;
        }

        public static UserInfo? Load(Guid userId)
        {
            var parms = new SqlParameter[] { new SqlParameter("@UserId", userId) };
            using var reader = SqlHelper.Default.ExecuteReader("spUserInfo_SelRec", parms);
            if (reader.Read()) { var r = new UserInfo(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static UserInfo? LoadWhere(string whereClause)
        {
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) };
            using var reader = SqlHelper.Default.ExecuteReader("spUserInfo_SelAll", parms);
            if (reader.Read()) { var r = new UserInfo(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static UserInfoCollection LoadCollection()
            => LoadCollection("spUserInfo_SelAll", new SqlParameter[] { });

        public static UserInfoCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spUserInfo_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static UserInfoCollection LoadCollection(string whereClause)
            => LoadCollection("spUserInfo_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static UserInfoCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spUserInfo_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static UserInfoCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new UserInfoCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new UserInfo(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid userId)
            => SqlHelper.Default.ExecuteNonQuery("spUserInfo_DelRec", new SqlParameter[] { new SqlParameter("@UserId", userId) });

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
                    foreach (XmlNode node in dataList) { attributes.Add(new MetadataAttribute(node.ChildNodes[0]!.InnerText, node.ChildNodes[1]!.InnerText)); }
                    if (attributes.Count > 0) metadataXml.Add("data", attributes);
                }
                else
                {
                    var attributes = new MetadataAttributes();
                    foreach (XmlNode node in metadata.SelectNodes("//" + RootNode)!)
                    {
                        if (node.HasChildNodes) ProcessingNodes(node, ref metadataXml, attributes);
                        else
                        {
                            foreach (XmlAttribute attr in node.Attributes!) attributes.Add(new MetadataAttribute(attr.Name, attr.Value));
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
                foreach (XmlAttribute attr in child.Attributes!) { if (attr.Name == "id") metadataKey = attr.Value; else attributes.Add(new MetadataAttribute(attr.Name, attr.Value)); }
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
            foreach (var attr in GetMetadataList("data")) { if (attr.Key == key) return attr.Value; }
            return string.Empty;
        }

        public void SetMetadata(string key, MetadataAttributes data)
        {
            metadataXml ??= new Dictionary<string, MetadataAttributes>();
            if (metadataXml.ContainsKey(key)) metadataXml[key] = data; else metadataXml.Add(key, data);
        }

        public void SetMetadata(string key, MetadataAttribute data)
        {
            var attributes = GetMetadataList(key);
            if (!attributes.Contains(data)) attributes.Add(data);
            SetMetadata(key, attributes);
        }

        public void SetMetadata(string key, string data) => SetMetadata("data", new MetadataAttribute(key, data));

        #endregion

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) userId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) primaryRec = reader.GetBoolean(1);
                if (!reader.IsDBNull(2)) userName = reader.GetString(2);
                if (!reader.IsDBNull(3)) userPassword = reader.GetString(3);
                if (!reader.IsDBNull(4)) userAlias = reader.GetString(4);
                if (!reader.IsDBNull(5)) userRole = reader.GetInt32(5);
                if (!reader.IsDBNull(6)) PrepareMetadataXml(reader.GetSqlXml(6), out metadataXml);
                if (!reader.IsDBNull(7)) createdOn = reader.GetDateTime(7);
                if (!reader.IsDBNull(8)) createdBy = reader.GetGuid(8);
                if (!reader.IsDBNull(9)) modifiedOn = reader.GetDateTime(9);
                if (!reader.IsDBNull(10)) modifiedBy = reader.GetGuid(10);
                if (!reader.IsDBNull(11)) retired = reader.GetBoolean(11);
                if (!reader.IsDBNull(12)) retiredOn = reader.GetDateTime(12);
                if (!reader.IsDBNull(13)) retiredBy = reader.GetGuid(13);
            }
        }

        public void Delete() => Delete(this.UserId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != UserId) this.Delete(); Update(); }
        }

        public Guid UserId { get { return userId; } set { userId = value; } }
        public bool PrimaryRec { get { return primaryRec; } set { primaryRec = value; } }
        public string UserName { get { return userName; } set { userName = value; } }
        public string UserPassword { get { return userPassword; } set { userPassword = value; } }
        public string UserAlias { get { return userAlias; } set { userAlias = value; } }
        public int UserRole { get { return userRole; } set { userRole = value; } }
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
            SqlHelper.Default.ExecuteNonQuery("spUserInfo_InsRec", "@UserId", out var rv, GetInsertParameterValues());
            userId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spUserInfo_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@UserId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.UserId),
            GetSqlParameter("@PrimaryRec", ParameterDirection.Input, SqlDbType.Bit, 1, this.PrimaryRec),
            GetSqlParameter("@UserName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.UserName),
            GetSqlParameter("@UserPassword", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.UserPassword),
            GetSqlParameter("@UserAlias", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.UserAlias),
            GetSqlParameter("@UserRole", ParameterDirection.Input, SqlDbType.Int, 4, this.UserRole),
            GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml)),
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
            GetSqlParameterWithoutDirection("@UserId", SqlDbType.UniqueIdentifier, 16, this.UserId),
            GetSqlParameterWithoutDirection("@PrimaryRec", SqlDbType.Bit, 1, this.PrimaryRec),
            GetSqlParameterWithoutDirection("@UserName", SqlDbType.NVarChar, 64, this.UserName),
            GetSqlParameterWithoutDirection("@UserPassword", SqlDbType.NVarChar, 64, this.UserPassword),
            GetSqlParameterWithoutDirection("@UserAlias", SqlDbType.NVarChar, 64, this.UserAlias),
            GetSqlParameterWithoutDirection("@UserRole", SqlDbType.Int, 4, this.UserRole),
            GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml)),
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
            b.Append("userId: " + userId).Append("\r\n");
            b.Append("primaryRec: " + primaryRec).Append("\r\n");
            b.Append("userName: " + userName).Append("\r\n");
            b.Append("userPassword: " + userPassword).Append("\r\n");
            b.Append("userAlias: " + userAlias).Append("\r\n");
            b.Append("userRole: " + userRole).Append("\r\n");
            b.Append("metadataXml: " + GenerateXml(metadataXml)).Append("\r\n");
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

    public class UserInfoCollection : BindingList<UserInfo> { }
}
