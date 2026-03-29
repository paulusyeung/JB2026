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
    public class SystemInfo
    {
        private Guid key = Guid.Empty;
        private Guid systemId = Guid.Empty;
        private string ownerName = string.Empty;
        private Dictionary<string, MetadataAttributes> metadataXml = new Dictionary<string, MetadataAttributes>();

        public SystemInfo() { }

        public SystemInfo(Guid systemId, string ownerName, Dictionary<string, MetadataAttributes> metadataXml)
        {
            this.systemId = systemId;
            this.ownerName = ownerName;
            this.metadataXml = metadataXml;
        }

        public static SystemInfo? Load(Guid systemId)
        {
            var parms = new SqlParameter[] { new SqlParameter("@SystemId", systemId) };
            using var reader = SqlHelper.Default.ExecuteReader("spSystemInfo_SelRec", parms);
            if (reader.Read()) { var r = new SystemInfo(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SystemInfo? LoadWhere(string whereClause)
        {
            var parms = new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) };
            using var reader = SqlHelper.Default.ExecuteReader("spSystemInfo_SelAll", parms);
            if (reader.Read()) { var r = new SystemInfo(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SystemInfoCollection LoadCollection()
            => LoadCollection("spSystemInfo_SelAll", new SqlParameter[] { });

        public static SystemInfoCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSystemInfo_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SystemInfoCollection LoadCollection(string whereClause)
            => LoadCollection("spSystemInfo_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SystemInfoCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSystemInfo_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SystemInfoCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SystemInfoCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new SystemInfo(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid systemId)
            => SqlHelper.Default.ExecuteNonQuery("spSystemInfo_DelRec", new SqlParameter[] { new SqlParameter("@SystemId", systemId) });

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
                    foreach (XmlNode node in dataList) attributes.Add(new MetadataAttribute(node.ChildNodes[0]!.InnerText, node.ChildNodes[1]!.InnerText));
                    if (attributes.Count > 0) metadataXml.Add("data", attributes);
                }
                else
                {
                    var attributes = new MetadataAttributes();
                    foreach (XmlNode node in metadata.SelectNodes("//" + RootNode)!)
                    {
                        if (node.HasChildNodes) ProcessingNodes(node, ref metadataXml, attributes);
                        else { foreach (XmlAttribute attr in node.Attributes!) attributes.Add(new MetadataAttribute(attr.Name, attr.Value)); if (attributes.Count > 0) metadataXml.Add("data", attributes); }
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
                if (!reader.IsDBNull(0)) systemId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) ownerName = reader.GetString(1);
                if (!reader.IsDBNull(2)) PrepareMetadataXml(reader.GetSqlXml(2), out metadataXml);
            }
        }

        public void Delete() => Delete(this.SystemId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != SystemId) this.Delete(); Update(); }
        }

        public Guid SystemId { get { return systemId; } set { systemId = value; } }
        public string OwnerName { get { return ownerName; } set { ownerName = value; } }
        public Dictionary<string, MetadataAttributes> MetadataXml { get { return metadataXml; } set { metadataXml = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSystemInfo_InsRec", "@SystemId", out var rv, GetInsertParameterValues());
            systemId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSystemInfo_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@SystemId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.SystemId),
            GetSqlParameter("@OwnerName", ParameterDirection.Input, SqlDbType.NVarChar, 255, this.OwnerName),
            GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml))
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@SystemId", SqlDbType.UniqueIdentifier, 16, this.SystemId),
            GetSqlParameterWithoutDirection("@OwnerName", SqlDbType.NVarChar, 255, this.OwnerName),
            GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.GenerateXml(this.MetadataXml))
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("systemId: " + systemId).Append("\r\n");
            b.Append("ownerName: " + ownerName).Append("\r\n");
            b.Append("metadataXml: " + GenerateXml(metadataXml)).Append("\r\n");
            return b.ToString();
        }
    }

    public class SystemInfoCollection : BindingList<SystemInfo> { }
}
