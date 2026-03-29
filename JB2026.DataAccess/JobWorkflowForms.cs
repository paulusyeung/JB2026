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
    public class JobWorkflowForms
    {
        private Guid key = Guid.Empty;
        private Guid jobWorkflowFormId = Guid.Empty;
        private Guid jobWorkflowId = Guid.Empty;
        private Guid formId = Guid.Empty;
        private int seqNumber;
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

        public JobWorkflowForms() { }

        public static JobWorkflowForms? Load(Guid jobWorkflowFormId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobWorkflowForms_SelRec", new SqlParameter[] { new SqlParameter("@JobWorkflowFormId", jobWorkflowFormId) });
            if (reader.Read()) { var r = new JobWorkflowForms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobWorkflowForms? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobWorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobWorkflowForms(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobWorkflowFormsCollection LoadCollection()
            => LoadCollection("spJobWorkflowForms_SelAll", new SqlParameter[] { });

        public static JobWorkflowFormsCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobWorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobWorkflowFormsCollection LoadCollection(string whereClause)
            => LoadCollection("spJobWorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobWorkflowFormsCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobWorkflowForms_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobWorkflowFormsCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobWorkflowFormsCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobWorkflowForms(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid jobWorkflowFormId)
            => SqlHelper.Default.ExecuteNonQuery("spJobWorkflowForms_DelRec", new SqlParameter[] { new SqlParameter("@JobWorkflowFormId", jobWorkflowFormId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) jobWorkflowFormId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) jobWorkflowId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) formId = reader.GetGuid(2);
                if (!reader.IsDBNull(3)) seqNumber = reader.GetInt32(3);
                if (!reader.IsDBNull(4))
                {
                    SqlXml sqlXml = reader.GetSqlXml(4);
                    metadataXml = sqlXml.Value;
                    ProcessingNodes(sqlXml.Value);
                }
            }
        }

        public void Delete() => Delete(this.JobWorkflowFormId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != JobWorkflowFormId) this.Delete(); Update(); }
        }

        public Guid JobWorkflowFormId { get { return jobWorkflowFormId; } set { jobWorkflowFormId = value; } }
        public Guid JobWorkflowId { get { return jobWorkflowId; } set { jobWorkflowId = value; } }
        public Guid FormId { get { return formId; } set { formId = value; } }
        public int SeqNumber { get { return seqNumber; } set { seqNumber = value; } }
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
            SqlHelper.Default.ExecuteNonQuery("spJobWorkflowForms_InsRec", "@JobWorkflowFormId", out var rv, GetInsertParameterValues());
            jobWorkflowFormId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobWorkflowForms_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@JobWorkflowFormId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.JobWorkflowFormId),
            GetSqlParameter("@JobWorkflowId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.JobWorkflowId),
            GetSqlParameter("@FormId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameter("@SeqNumber", ParameterDirection.Input, SqlDbType.Int, 4, this.SeqNumber),
            GetSqlParameter("@MetadataXml", ParameterDirection.Input, SqlDbType.Xml, -1, this.MetadataXml)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@JobWorkflowFormId", SqlDbType.UniqueIdentifier, 16, this.JobWorkflowFormId),
            GetSqlParameterWithoutDirection("@JobWorkflowId", SqlDbType.UniqueIdentifier, 16, this.JobWorkflowId),
            GetSqlParameterWithoutDirection("@FormId", SqlDbType.UniqueIdentifier, 16, this.FormId),
            GetSqlParameterWithoutDirection("@SeqNumber", SqlDbType.Int, 4, this.SeqNumber),
            GetSqlParameterWithoutDirection("@MetadataXml", SqlDbType.Xml, -1, this.MetadataXml)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("jobWorkflowFormId: " + jobWorkflowFormId).Append("\r\n");
            b.Append("jobWorkflowId: " + jobWorkflowId).Append("\r\n");
            b.Append("formId: " + formId).Append("\r\n");
            b.Append("seqNumber: " + seqNumber).Append("\r\n");
            b.Append("metadataXml: " + metadataXml).Append("\r\n");
            return b.ToString();
        }
    }

    public class JobWorkflowFormsCollection : BindingList<JobWorkflowForms> { }
}
