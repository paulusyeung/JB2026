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
    public class Z_Forms
    {
        private Guid key = Guid.Empty;
        private Guid formId = Guid.Empty;
        private int formObjectEnum;
        private string formName = string.Empty;
        private string formName_Chs = string.Empty;
        private string formName_Cht = string.Empty;
        private string metadataXml = string.Empty;

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

        public Z_Forms() { }

        public static Z_Forms? Load(Guid formId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Forms_SelRec", new SqlParameter[] { new SqlParameter("@FormId", formId) });
            if (reader.Read()) { var r = new Z_Forms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_Forms? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Forms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Z_Forms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_FormsCollection LoadCollection()
            => LoadCollection("spZ_Forms_SelAll", new SqlParameter[] { });

        public static Z_FormsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Forms_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_FormsCollection LoadCollection(string whereClause)
            => LoadCollection("spZ_Forms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static Z_FormsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Forms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_FormsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new Z_FormsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Z_Forms(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid formId)
            => SqlHelper.Default.ExecuteNonQuery("spZ_Forms_DelRec", new SqlParameter[] { new SqlParameter("@FormId", formId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) formId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) formObjectEnum = reader.GetInt32(1);
                if (!reader.IsDBNull(2)) formName = reader.GetString(2);
                if (!reader.IsDBNull(3)) formName_Chs = reader.GetString(3);
                if (!reader.IsDBNull(4)) formName_Cht = reader.GetString(4);
                if (!reader.IsDBNull(5))
                {
                    SqlXml sqlXml = reader.GetSqlXml(5);
                    metadataXml = sqlXml.Value;
                    ProcessingNodes(sqlXml.Value);
                }
            }
        }

        public void Delete() => Delete(this.FormId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != FormId) this.Delete(); Update(); }
        }

        public Guid FormId { get { return formId; } set { formId = value; } }
        public int FormObjectEnum { get { return formObjectEnum; } set { formObjectEnum = value; } }
        public string FormName { get { return formName; } set { formName = value; } }
        public string FormName_Chs { get { return formName_Chs; } set { formName_Chs = value; } }
        public string FormName_Cht { get { return formName_Cht; } set { formName_Cht = value; } }
        public string MetadataXml { get { return metadataXml; } set { metadataXml = value; } }

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
            SqlHelper.Default.ExecuteNonQuery("spZ_Forms_InsRec", "@FormId", out var rv, GetInsertParameterValues());
            formId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spZ_Forms_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@FormId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameter("@FormObjectEnum", ParameterDirection.Input, SqlDbType.Int, 4, this.FormObjectEnum),
            GetSqlParameter("@FormName", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.FormName),
            GetSqlParameter("@FormName_Chs", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.FormName_Chs),
            GetSqlParameter("@FormName_Cht", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.FormName_Cht),
            GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.MetadataXml)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@FormId", SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameterWithoutDirection("@FormObjectEnum", SqlDbType.Int, 4, this.FormObjectEnum),
            GetSqlParameterWithoutDirection("@FormName", SqlDbType.NVarChar, 10, this.FormName),
            GetSqlParameterWithoutDirection("@FormName_Chs", SqlDbType.NVarChar, 10, this.FormName_Chs),
            GetSqlParameterWithoutDirection("@FormName_Cht", SqlDbType.NVarChar, 10, this.FormName_Cht),
            GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.MetadataXml)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("formId: " + formId).Append("\r\n");
            b.Append("formObjectEnum: " + formObjectEnum).Append("\r\n");
            b.Append("formName: " + formName).Append("\r\n");
            b.Append("formName_Chs: " + formName_Chs).Append("\r\n");
            b.Append("formName_Cht: " + formName_Cht).Append("\r\n");
            b.Append("metadataXml: " + metadataXml).Append("\r\n");
            return b.ToString();
        }
    }

    public class Z_FormsCollection : BindingList<Z_Forms> { }
}
