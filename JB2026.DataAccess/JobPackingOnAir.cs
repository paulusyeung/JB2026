using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class JobPackingOnAir
    {
        private Guid key = Guid.Empty;
        private Guid onAirId = Guid.Empty;
        private Guid orderId = Guid.Empty;
        private DateTime onAiredOn = DateTime.Parse("1900-1-1");
        private Guid onAiredBy = Guid.Empty;
        private int priority;
        private int status;
        private DateTime completedOn = DateTime.Parse("1900-1-1");
        private Guid completedBy = Guid.Empty;
        private bool cancelled;
        private DateTime cancelledOn = DateTime.Parse("1900-1-1");
        private Guid cancelledBy = Guid.Empty;
        private int rescheduledCount;
        private DateTime rescheduledOn = DateTime.Parse("1900-1-1");
        private Guid rescheduledBy = Guid.Empty;

        public JobPackingOnAir() { }

        public static JobPackingOnAir? Load(Guid onAirId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobPackingOnAir_SelRec", new SqlParameter[] { new SqlParameter("@OnAirId", onAirId) });
            if (reader.Read()) { var r = new JobPackingOnAir(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobPackingOnAir? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobPackingOnAir_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobPackingOnAir(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobPackingOnAirCollection LoadCollection()
            => LoadCollection("spJobPackingOnAir_SelAll", new SqlParameter[] { });

        public static JobPackingOnAirCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobPackingOnAir_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobPackingOnAirCollection LoadCollection(string whereClause)
            => LoadCollection("spJobPackingOnAir_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobPackingOnAirCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobPackingOnAir_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobPackingOnAirCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobPackingOnAirCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobPackingOnAir(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid onAirId)
            => SqlHelper.Default.ExecuteNonQuery("spJobPackingOnAir_DelRec", new SqlParameter[] { new SqlParameter("@OnAirId", onAirId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) onAirId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) orderId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) onAiredOn = reader.GetDateTime(2);
                if (!reader.IsDBNull(3)) onAiredBy = reader.GetGuid(3);
                if (!reader.IsDBNull(4)) priority = reader.GetInt32(4);
                if (!reader.IsDBNull(5)) status = reader.GetInt32(5);
                if (!reader.IsDBNull(6)) completedOn = reader.GetDateTime(6);
                if (!reader.IsDBNull(7)) completedBy = reader.GetGuid(7);
                if (!reader.IsDBNull(8)) cancelled = reader.GetBoolean(8);
                if (!reader.IsDBNull(9)) cancelledOn = reader.GetDateTime(9);
                if (!reader.IsDBNull(10)) cancelledBy = reader.GetGuid(10);
                if (!reader.IsDBNull(11)) rescheduledCount = reader.GetInt32(11);
                if (!reader.IsDBNull(12)) rescheduledOn = reader.GetDateTime(12);
                if (!reader.IsDBNull(13)) rescheduledBy = reader.GetGuid(13);
            }
        }

        public void Delete() => Delete(this.OnAirId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != OnAirId) this.Delete(); Update(); }
        }

        public Guid OnAirId { get { return onAirId; } set { onAirId = value; } }
        public Guid OrderId { get { return orderId; } set { orderId = value; } }
        public DateTime OnAiredOn { get { return onAiredOn; } set { onAiredOn = value; } }
        public Guid OnAiredBy { get { return onAiredBy; } set { onAiredBy = value; } }
        public int Priority { get { return priority; } set { priority = value; } }
        public int Status { get { return status; } set { status = value; } }
        public DateTime CompletedOn { get { return completedOn; } set { completedOn = value; } }
        public Guid CompletedBy { get { return completedBy; } set { completedBy = value; } }
        public bool Cancelled { get { return cancelled; } set { cancelled = value; } }
        public DateTime CancelledOn { get { return cancelledOn; } set { cancelledOn = value; } }
        public Guid CancelledBy { get { return cancelledBy; } set { cancelledBy = value; } }
        public int RescheduledCount { get { return rescheduledCount; } set { rescheduledCount = value; } }
        public DateTime RescheduledOn { get { return rescheduledOn; } set { rescheduledOn = value; } }
        public Guid RescheduledBy { get { return rescheduledBy; } set { rescheduledBy = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spJobPackingOnAir_InsRec", "@OnAirId", out var rv, GetInsertParameterValues());
            onAirId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobPackingOnAir_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@OnAirId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.OnAirId),
            GetSqlParameter("@OrderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameter("@OnAiredOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.OnAiredOn),
            GetSqlParameter("@OnAiredBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OnAiredBy),
            GetSqlParameter("@Priority", ParameterDirection.Input, SqlDbType.Int, 4, this.Priority),
            GetSqlParameter("@Status", ParameterDirection.Input, SqlDbType.Int, 4, this.Status),
            GetSqlParameter("@CompletedOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CompletedOn),
            GetSqlParameter("@CompletedBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CompletedBy),
            GetSqlParameter("@Cancelled", ParameterDirection.Input, SqlDbType.Bit, 1, this.Cancelled),
            GetSqlParameter("@CancelledOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.CancelledOn),
            GetSqlParameter("@CancelledBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CancelledBy),
            GetSqlParameter("@RescheduledCount", ParameterDirection.Input, SqlDbType.Int, 4, this.RescheduledCount),
            GetSqlParameter("@RescheduledOn", ParameterDirection.Input, SqlDbType.SmallDateTime, 4, this.RescheduledOn),
            GetSqlParameter("@RescheduledBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.RescheduledBy)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@OnAirId", SqlDbType.UniqueIdentifier, 16, this.OnAirId),
            GetSqlParameterWithoutDirection("@OrderId", SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameterWithoutDirection("@OnAiredOn", SqlDbType.SmallDateTime, 4, this.OnAiredOn),
            GetSqlParameterWithoutDirection("@OnAiredBy", SqlDbType.UniqueIdentifier, 16, this.OnAiredBy),
            GetSqlParameterWithoutDirection("@Priority", SqlDbType.Int, 4, this.Priority),
            GetSqlParameterWithoutDirection("@Status", SqlDbType.Int, 4, this.Status),
            GetSqlParameterWithoutDirection("@CompletedOn", SqlDbType.SmallDateTime, 4, this.CompletedOn),
            GetSqlParameterWithoutDirection("@CompletedBy", SqlDbType.UniqueIdentifier, 16, this.CompletedBy),
            GetSqlParameterWithoutDirection("@Cancelled", SqlDbType.Bit, 1, this.Cancelled),
            GetSqlParameterWithoutDirection("@CancelledOn", SqlDbType.SmallDateTime, 4, this.CancelledOn),
            GetSqlParameterWithoutDirection("@CancelledBy", SqlDbType.UniqueIdentifier, 16, this.CancelledBy),
            GetSqlParameterWithoutDirection("@RescheduledCount", SqlDbType.Int, 4, this.RescheduledCount),
            GetSqlParameterWithoutDirection("@RescheduledOn", SqlDbType.SmallDateTime, 4, this.RescheduledOn),
            GetSqlParameterWithoutDirection("@RescheduledBy", SqlDbType.UniqueIdentifier, 16, this.RescheduledBy)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("onAirId: " + onAirId).Append("\r\n");
            b.Append("orderId: " + orderId).Append("\r\n");
            b.Append("onAiredOn: " + onAiredOn).Append("\r\n");
            b.Append("onAiredBy: " + onAiredBy).Append("\r\n");
            b.Append("priority: " + priority).Append("\r\n");
            b.Append("status: " + status).Append("\r\n");
            b.Append("completedOn: " + completedOn).Append("\r\n");
            b.Append("completedBy: " + completedBy).Append("\r\n");
            b.Append("cancelled: " + cancelled).Append("\r\n");
            b.Append("cancelledOn: " + cancelledOn).Append("\r\n");
            b.Append("cancelledBy: " + cancelledBy).Append("\r\n");
            b.Append("rescheduledCount: " + rescheduledCount).Append("\r\n");
            b.Append("rescheduledOn: " + rescheduledOn).Append("\r\n");
            b.Append("rescheduledBy: " + rescheduledBy).Append("\r\n");
            return b.ToString();
        }
    }

    public class JobPackingOnAirCollection : BindingList<JobPackingOnAir> { }
}
