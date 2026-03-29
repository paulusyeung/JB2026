using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace JB2026.DataAccess
{
    public class JobSchedule
    {
        private Guid key = Guid.Empty;
        private Guid scheduleId = Guid.Empty;
        private Guid orderId = Guid.Empty;
        private DateTime scheduledOn = DateTime.Parse("1900-1-1");
        private int status;
        private int priority;
        private string machineNumber = string.Empty;
        private DateTime completedOn = DateTime.Parse("1900-1-1");
        private bool shouldReview;
        private int urgencyLevel;
        private bool cancelled;
        private DateTime cancelledOn = DateTime.Parse("1900-1-1");
        private Guid cancelledBy = Guid.Empty;
        private int rescheduledCount;
        private Guid rescheduledBy = Guid.Empty;
        private DateTime rescheduledOn = DateTime.Parse("1900-1-1");

        public JobSchedule() { }

        public static JobSchedule? Load(Guid scheduleId)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobSchedule_SelRec", new SqlParameter[] { new SqlParameter("@ScheduleId", scheduleId) });
            if (reader.Read()) { var r = new JobSchedule(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobSchedule? LoadWhere(string whereClause)
        {
            using var reader = SqlHelper.Default.ExecuteReader("spJobSchedule_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });
            if (reader.Read()) { var r = new JobSchedule(); r.LoadFromReader(reader); return r; }
            return null;
        }

        public static JobScheduleCollection LoadCollection()
            => LoadCollection("spJobSchedule_SelAll", new SqlParameter[] { });

        public static JobScheduleCollection LoadCollection(string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobSchedule_SelAll", new SqlParameter[] { new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobScheduleCollection LoadCollection(string whereClause)
            => LoadCollection("spJobSchedule_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause) });

        public static JobScheduleCollection LoadCollection(string whereClause, string[] orderByColumns, bool ascending)
        {
            var ob = new StringBuilder();
            for (int i = 0; i < orderByColumns.Length; i++) { ob.Append(orderByColumns[i]); if (i != orderByColumns.Length - 1) ob.Append(", "); }
            ob.Append(ascending ? " ASC" : " DESC");
            return LoadCollection("spJobSchedule_SelAll", new SqlParameter[] { new SqlParameter("@WhereClause", whereClause), new SqlParameter("@OrderBy", ob.ToString()) });
        }

        public static JobScheduleCollection LoadCollection(string spName, SqlParameter[] parms)
        {
            var result = new JobScheduleCollection();
            using var reader = SqlHelper.Default.ExecuteReader(spName, parms);
            while (reader.Read()) { var tmp = new JobSchedule(); tmp.LoadFromReader(reader); result.Add(tmp); }
            return result;
        }

        public static void Delete(Guid scheduleId)
            => SqlHelper.Default.ExecuteNonQuery("spJobSchedule_DelRec", new SqlParameter[] { new SqlParameter("@ScheduleId", scheduleId) });

        public void LoadFromReader(SqlDataReader reader)
        {
            if (reader != null && !reader.IsClosed)
            {
                key = reader.GetGuid(0);
                if (!reader.IsDBNull(0)) scheduleId = reader.GetGuid(0);
                if (!reader.IsDBNull(1)) orderId = reader.GetGuid(1);
                if (!reader.IsDBNull(2)) scheduledOn = reader.GetDateTime(2);
                if (!reader.IsDBNull(3)) status = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) priority = reader.GetInt32(4);
                if (!reader.IsDBNull(5)) machineNumber = reader.GetString(5);
                if (!reader.IsDBNull(6)) completedOn = reader.GetDateTime(6);
                if (!reader.IsDBNull(7)) shouldReview = reader.GetBoolean(7);
                if (!reader.IsDBNull(8)) urgencyLevel = reader.GetInt32(8);
                if (!reader.IsDBNull(9)) cancelled = reader.GetBoolean(9);
                if (!reader.IsDBNull(10)) cancelledOn = reader.GetDateTime(10);
                if (!reader.IsDBNull(11)) cancelledBy = reader.GetGuid(11);
                if (!reader.IsDBNull(12)) rescheduledCount = reader.GetInt32(12);
                if (!reader.IsDBNull(13)) rescheduledBy = reader.GetGuid(13);
                if (!reader.IsDBNull(14)) rescheduledOn = reader.GetDateTime(14);
            }
        }

        public void Delete() => Delete(this.ScheduleId);

        public void Save()
        {
            if (key == Guid.Empty) Insert();
            else { if (key != ScheduleId) this.Delete(); Update(); }
        }

        public Guid ScheduleId { get { return scheduleId; } set { scheduleId = value; } }
        public Guid OrderId { get { return orderId; } set { orderId = value; } }
        public DateTime ScheduledOn { get { return scheduledOn; } set { scheduledOn = value; } }
        public int Status { get { return status; } set { status = value; } }
        public int Priority { get { return priority; } set { priority = value; } }
        public string MachineNumber { get { return machineNumber; } set { machineNumber = value; } }
        public DateTime CompletedOn { get { return completedOn; } set { completedOn = value; } }
        public bool ShouldReview { get { return shouldReview; } set { shouldReview = value; } }
        public int UrgencyLevel { get { return urgencyLevel; } set { urgencyLevel = value; } }
        public bool Cancelled { get { return cancelled; } set { cancelled = value; } }
        public DateTime CancelledOn { get { return cancelledOn; } set { cancelledOn = value; } }
        public Guid CancelledBy { get { return cancelledBy; } set { cancelledBy = value; } }
        public int RescheduledCount { get { return rescheduledCount; } set { rescheduledCount = value; } }
        public Guid RescheduledBy { get { return rescheduledBy; } set { rescheduledBy = value; } }
        public DateTime RescheduledOn { get { return rescheduledOn; } set { rescheduledOn = value; } }

        private void Insert()
        {
            SqlHelper.Default.ExecuteNonQuery("spJobSchedule_InsRec", "@ScheduleId", out var rv, GetInsertParameterValues());
            scheduleId = rv != null ? (Guid)rv : Guid.Empty;
            key = rv != null ? (Guid)rv : Guid.Empty;
        }

        private void Update() => SqlHelper.Default.ExecuteNonQuery("spJobSchedule_UpdRec", GetUpdateParameterValues());

        private SqlParameter GetSqlParameter(string name, ParameterDirection direction, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value, Direction = direction };

        private SqlParameter GetSqlParameterWithoutDirection(string name, SqlDbType dbType, int size, object value)
            => new SqlParameter(name, dbType, size) { Value = value };

        private SqlParameter[] GetInsertParameterValues() => new SqlParameter[]
        {
            GetSqlParameter("@ScheduleId", ParameterDirection.Output, SqlDbType.UniqueIdentifier, 16, this.ScheduleId),
            GetSqlParameter("@OrderId", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameter("@ScheduledOn", ParameterDirection.Input, SqlDbType.DateTime, 8, this.ScheduledOn),
            GetSqlParameter("@Status", ParameterDirection.Input, SqlDbType.Int, 4, this.Status),
            GetSqlParameter("@Priority", ParameterDirection.Input, SqlDbType.Int, 4, this.Priority),
            GetSqlParameter("@MachineNumber", ParameterDirection.Input, SqlDbType.NVarChar, 10, this.MachineNumber),
            GetSqlParameter("@CompletedOn", ParameterDirection.Input, SqlDbType.DateTime, 8, this.CompletedOn),
            GetSqlParameter("@ShouldReview", ParameterDirection.Input, SqlDbType.Bit, 1, this.ShouldReview),
            GetSqlParameter("@UrgencyLevel", ParameterDirection.Input, SqlDbType.Int, 4, this.UrgencyLevel),
            GetSqlParameter("@Cancelled", ParameterDirection.Input, SqlDbType.Bit, 1, this.Cancelled),
            GetSqlParameter("@CancelledOn", ParameterDirection.Input, SqlDbType.DateTime, 8, this.CancelledOn),
            GetSqlParameter("@CancelledBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.CancelledBy),
            GetSqlParameter("@RescheduledCount", ParameterDirection.Input, SqlDbType.Int, 4, this.RescheduledCount),
            GetSqlParameter("@RescheduledBy", ParameterDirection.Input, SqlDbType.UniqueIdentifier, 16, this.RescheduledBy),
            GetSqlParameter("@RescheduledOn", ParameterDirection.Input, SqlDbType.DateTime, 8, this.RescheduledOn)
        };

        private SqlParameter[] GetUpdateParameterValues() => new SqlParameter[]
        {
            GetSqlParameterWithoutDirection("@ScheduleId", SqlDbType.UniqueIdentifier, 16, this.ScheduleId),
            GetSqlParameterWithoutDirection("@OrderId", SqlDbType.UniqueIdentifier, 16, this.OrderId),
            GetSqlParameterWithoutDirection("@ScheduledOn", SqlDbType.DateTime, 8, this.ScheduledOn),
            GetSqlParameterWithoutDirection("@Status", SqlDbType.Int, 4, this.Status),
            GetSqlParameterWithoutDirection("@Priority", SqlDbType.Int, 4, this.Priority),
            GetSqlParameterWithoutDirection("@MachineNumber", SqlDbType.NVarChar, 10, this.MachineNumber),
            GetSqlParameterWithoutDirection("@CompletedOn", SqlDbType.DateTime, 8, this.CompletedOn),
            GetSqlParameterWithoutDirection("@ShouldReview", SqlDbType.Bit, 1, this.ShouldReview),
            GetSqlParameterWithoutDirection("@UrgencyLevel", SqlDbType.Int, 4, this.UrgencyLevel),
            GetSqlParameterWithoutDirection("@Cancelled", SqlDbType.Bit, 1, this.Cancelled),
            GetSqlParameterWithoutDirection("@CancelledOn", SqlDbType.DateTime, 8, this.CancelledOn),
            GetSqlParameterWithoutDirection("@CancelledBy", SqlDbType.UniqueIdentifier, 16, this.CancelledBy),
            GetSqlParameterWithoutDirection("@RescheduledCount", SqlDbType.Int, 4, this.RescheduledCount),
            GetSqlParameterWithoutDirection("@RescheduledBy", SqlDbType.UniqueIdentifier, 16, this.RescheduledBy),
            GetSqlParameterWithoutDirection("@RescheduledOn", SqlDbType.DateTime, 8, this.RescheduledOn)
        };

        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("scheduleId: " + scheduleId).Append("\r\n");
            b.Append("orderId: " + orderId).Append("\r\n");
            b.Append("scheduledOn: " + scheduledOn).Append("\r\n");
            b.Append("status: " + status).Append("\r\n");
            b.Append("priority: " + priority).Append("\r\n");
            b.Append("machineNumber: " + machineNumber).Append("\r\n");
            b.Append("completedOn: " + completedOn).Append("\r\n");
            b.Append("shouldReview: " + shouldReview).Append("\r\n");
            b.Append("urgencyLevel: " + urgencyLevel).Append("\r\n");
            b.Append("cancelled: " + cancelled).Append("\r\n");
            b.Append("cancelledOn: " + cancelledOn).Append("\r\n");
            b.Append("cancelledBy: " + cancelledBy).Append("\r\n");
            b.Append("rescheduledCount: " + rescheduledCount).Append("\r\n");
            b.Append("rescheduledBy: " + rescheduledBy).Append("\r\n");
            b.Append("rescheduledOn: " + rescheduledOn).Append("\r\n");
            return b.ToString();
        }
    }

    public class JobScheduleCollection : BindingList<JobSchedule> { }
}
