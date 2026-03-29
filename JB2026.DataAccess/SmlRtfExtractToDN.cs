using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class SmlRtfExtractToDN
    {
        private Guid key = Guid.Empty;
        private Guid dNId = Guid.Empty;
        private Guid headerId = Guid.Empty;
        private string dNNumber = string.Empty;
        private DateTime dNDate = DateTime.Parse("1900-1-1");
        private int dNType;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;

        public SmlRtfExtractToDN() { }

        public static SmlRtfExtractToDN? Load(Guid dNId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfExtractToDN_SelRec", new SqlParameter[] { new SqlParameter("@DNId", dNId) });
            if (reader.Read()) { var r = new SmlRtfExtractToDN(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfExtractToDN? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spSmlRtfExtractToDN_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new SmlRtfExtractToDN(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static SmlRtfExtractToDNCollection LoadCollection()
            => LoadCollection("spSmlRtfExtractToDN_SelAll", new SqlParameter[] { });

        public static SmlRtfExtractToDNCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfExtractToDN_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfExtractToDNCollection LoadCollection(string whereClause)
            => LoadCollection("spSmlRtfExtractToDN_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static SmlRtfExtractToDNCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spSmlRtfExtractToDN_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static SmlRtfExtractToDNCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new SmlRtfExtractToDNCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new SmlRtfExtractToDN(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid dNId)
            => SqlHelper.Default.ExecuteNonQuery("spSmlRtfExtractToDN_DelRec", new SqlParameter[] { new SqlParameter("@DNId", dNId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) dNId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) headerId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) dNNumber = reader.GetString(2);
                if (!reader.IsDBNull(3)) dNDate = reader.GetDateTime(3);
                if (!reader.IsDBNull(4)) dNType = reader.GetInt32(4);
                if (!reader.IsDBNull(5)) createdOn = reader.GetDateTime(5);
                if (!reader.IsDBNull(6)) createdBy = reader.GetGuid(6);
            }
        }

        public void Delete() => Delete(this.DNId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != DNId) this.Delete(); Update(); }
        }

        public Guid DNId { get { return dNId; } set { dNId = value; } }
        public Guid HeaderId { get { return headerId; } set { headerId = value; } }
        public string DNNumber { get { return dNNumber; } set { dNNumber = value; } }
        public DateTime DNDate { get { return dNDate; } set { dNDate = value; } }
        public int DNType { get { return dNType; } set { dNType = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spSmlRtfExtractToDN_InsRec", "@DNId", out var rv, GetInsertParameterValues());
            dNId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spSmlRtfExtractToDN_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@DNId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.DNId),
            GetSqlParameter("@HeaderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameter("@DNNumber", ParameterDirection.Input, SqlDbType.NVarChar, 16, this.DNNumber),
            GetSqlParameter("@DNDate", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.DNDate),
            GetSqlParameter("@DNType", ParameterDirection.Input, SqlDbType.Int, 4, this.DNType),
            GetSqlParameter("@CreatedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameter("@CreatedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CreatedBy)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@DNId", SqlDbType.UniqueIdentifier, 16, this.DNId),
            GetSqlParameterWithoutDirection("@HeaderId", SqlDbType.UniqueIdentifier, 16, this.HeaderId),
            GetSqlParameterWithoutDirection("@DNNumber", SqlDbType.NVarChar, 16, this.DNNumber),
            GetSqlParameterWithoutDirection("@DNDate", SqlDbType.SmallDateTime, 4, this.DNDate),
            GetSqlParameterWithoutDirection("@DNType", SqlDbType.Int, 4, this.DNType),
            GetSqlParameterWithoutDirection("@CreatedOn", SqlDbType.SmallDateTime, 4, this.CreatedOn),
            GetSqlParameterWithoutDirection("@CreatedBy", SqlDbType.UniqueIdentifier, 16, this.CreatedBy)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("dNId: " + dNId).Append("\r\n");
            b.Append("headerId: " + headerId).Append("\r\n");
            b.Append("dNNumber: " + dNNumber).Append("\r\n");
            b.Append("dNDate: " + dNDate).Append("\r\n");
            b.Append("dNType: " + dNType).Append("\r\n");
            b.Append("createdOn: " + createdOn).Append("\r\n");
            b.Append("createdBy: " + createdBy).Append("\r\n");
            return b.ToString();
        }
    }

    public class SmlRtfExtractToDNCollection : BindingList<SmlRtfExtractToDN> { }
}
