using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class Z_Category
    {
        private Guid key = Guid.Empty;
        private Guid categoryId = Guid.Empty;
        private string categoryCode = string.Empty;
        private string categoryName = string.Empty;
        private DateTime createdOn = DateTime.Parse("1900-1-1");
        private Guid createdBy = Guid.Empty;
        private DateTime modifiedOn = DateTime.Parse("1900-1-1");
        private Guid modifiedBy = Guid.Empty;
        private bool retired;
        private DateTime retiredOn = DateTime.Parse("1900-1-1");
        private Guid retiredBy = Guid.Empty;

        public Z_Category() { }

        public static Z_Category? Load(Guid categoryId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Category_SelRec", new SqlParameter[] { new SqlParameter("@CategoryId", categoryId) });
            if (reader.Read()) { var r = new Z_Category(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_Category? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spZ_Category_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new Z_Category(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static Z_CategoryCollection LoadCollection()
            => LoadCollection("spZ_Category_SelAll", new SqlParameter[] { });

        public static Z_CategoryCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Category_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_CategoryCollection LoadCollection(string whereClause)
            => LoadCollection("spZ_Category_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static Z_CategoryCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spZ_Category_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static Z_CategoryCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new Z_CategoryCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new Z_Category(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid categoryId)
            => SqlHelper.Default.ExecuteNonQuery("spZ_Category_DelRec", new SqlParameter[] { new SqlParameter("@CategoryId", categoryId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) categoryId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) categoryCode = reader.GetString(1);
                if (!reader.IsDBNull(2)) categoryName = reader.GetString(2);
                if (!reader.IsDBNull(3)) createdOn = reader.GetDateTime(3);
                if (!reader.IsDBNull(4)) createdBy = reader.GetGuid(4);
                if (!reader.IsDBNull(5)) modifiedOn = reader.GetDateTime(5);
                if (!reader.IsDBNull(6)) modifiedBy = reader.GetGuid(6);
                if (!reader.IsDBNull(7)) retired = reader.GetBoolean(7);
                if (!reader.IsDBNull(8)) retiredOn = reader.GetDateTime(8);
                if (!reader.IsDBNull(9)) retiredBy = reader.GetGuid(9);
            }
        }

        public void Delete() => Delete(this.CategoryId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != CategoryId) this.Delete(); Update(); }
        }

        public Guid CategoryId { get { return categoryId; } set { categoryId = value; } }
        public string CategoryCode { get { return categoryCode; } set { categoryCode = value; } }
        public string CategoryName { get { return categoryName; } set { categoryName = value; } }
        public DateTime CreatedOn { get { return createdOn; } set { createdOn = value; } }
        public Guid CreatedBy { get { return createdBy; } set { createdBy = value; } }
        public DateTime ModifiedOn { get { return modifiedOn; } set { modifiedOn = value; } }
        public Guid ModifiedBy { get { return modifiedBy; } set { modifiedBy = value; } }
        public bool Retired { get { return retired; } set { retired = value; } }
        public DateTime RetiredOn { get { return retiredOn; } set { retiredOn = value; } }
        public Guid RetiredBy { get { return retiredBy; } set { retiredBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spZ_Category_InsRec", "@CategoryId", out var rv, GetInsertParameterValues());
            categoryId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spZ_Category_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@CategoryId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.CategoryId),
            GetSqlParameter("@CategoryCode", ParameterDirection.Input, SqlDbType.NVarChar, 3, this.CategoryCode),
            GetSqlParameter("@CategoryName", ParameterDirection.Input, SqlDbType.NVarChar, 64, this.CategoryName),
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
            GetSqlParameterWithoutDirection("@CategoryId", SqlDbType.UniqueIdentifier, 16, this.CategoryId),
            GetSqlParameterWithoutDirection("@CategoryCode", SqlDbType.NVarChar, 3, this.CategoryCode),
            GetSqlParameterWithoutDirection("@CategoryName", SqlDbType.NVarChar, 64, this.CategoryName),
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
            b.Append("categoryId: " + categoryId).Append("\r\n");
            b.Append("categoryCode: " + categoryCode).Append("\r\n");
            b.Append("categoryName: " + categoryName).Append("\r\n");
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

    public class Z_CategoryCollection : BindingList<Z_Category> { }
}
