using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobScheduleStoredProcedureGateway : IJobScheduleStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobScheduleStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobScheduleStoredProcedureRecord?> SelectAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobSchedule_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ScheduleId", DbType.Guid, scheduleId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobScheduleStoredProcedureRecord(
            ScheduleId: reader.GetGuid(reader.GetOrdinal("ScheduleId")),
            OrderId: reader.GetGuid(reader.GetOrdinal("OrderId")),
            ScheduledOn: GetNullableDateTime(reader, "ScheduledOn"),
            Status: GetNullableInt(reader, "Status"),
            Priority: GetNullableInt(reader, "Priority"),
            MachineNumber: GetNullableString(reader, "MachineNumber"),
            CompletedOn: GetNullableDateTime(reader, "CompletedOn"),
            ShouldReview: GetNullableBool(reader, "ShouldReview"),
            UrgencyLevel: reader.GetInt32(reader.GetOrdinal("UrgencyLevel")),
            Cancelled: GetNullableBool(reader, "Cancelled"),
            CancelledOn: GetNullableDateTime(reader, "CancelledOn"),
            CancelledBy: GetNullableGuid(reader, "CancelledBy"),
            RescheduledCount: GetNullableInt(reader, "RescheduledCount"),
            RescheduledBy: GetNullableGuid(reader, "RescheduledBy"),
            RescheduledOn: GetNullableDateTime(reader, "RescheduledOn"));
    }

    public async Task<Guid> InsertAsync(CreateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobSchedule_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var scheduleIdOut = command.CreateParameter();
        scheduleIdOut.ParameterName = "@ScheduleId";
        scheduleIdOut.DbType = DbType.Guid;
        scheduleIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(scheduleIdOut);

        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@ScheduledOn", DbType.DateTime, request.ScheduledOn));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
        command.Parameters.Add(CreateInputParameter(command, "@Priority", DbType.Int32, request.Priority));
        command.Parameters.Add(CreateInputParameter(command, "@MachineNumber", DbType.String, request.MachineNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ShouldReview", DbType.Boolean, request.ShouldReview));
        command.Parameters.Add(CreateInputParameter(command, "@UrgencyLevel", DbType.Int32, request.UrgencyLevel));
        command.Parameters.Add(CreateInputParameter(command, "@Cancelled", DbType.Boolean, request.Cancelled));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledOn", DbType.DateTime, request.CancelledOn));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledBy", DbType.Guid, request.CancelledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledCount", DbType.Int32, request.RescheduledCount));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledBy", DbType.Guid, request.RescheduledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledOn", DbType.DateTime, request.RescheduledOn));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return scheduleIdOut.Value is Guid scheduleId
            ? scheduleId
            : Guid.Parse(scheduleIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output ScheduleId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobScheduleStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobSchedule_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ScheduleId", DbType.Guid, request.ScheduleId));
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@ScheduledOn", DbType.DateTime, request.ScheduledOn));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
        command.Parameters.Add(CreateInputParameter(command, "@Priority", DbType.Int32, request.Priority));
        command.Parameters.Add(CreateInputParameter(command, "@MachineNumber", DbType.String, request.MachineNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ShouldReview", DbType.Boolean, request.ShouldReview));
        command.Parameters.Add(CreateInputParameter(command, "@UrgencyLevel", DbType.Int32, request.UrgencyLevel));
        command.Parameters.Add(CreateInputParameter(command, "@Cancelled", DbType.Boolean, request.Cancelled));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledOn", DbType.DateTime, request.CancelledOn));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledBy", DbType.Guid, request.CancelledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledCount", DbType.Int32, request.RescheduledCount));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledBy", DbType.Guid, request.RescheduledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledOn", DbType.DateTime, request.RescheduledOn));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobSchedule_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ScheduleId", DbType.Guid, scheduleId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static DbParameter CreateInputParameter(DbCommand command, string name, DbType dbType, object? value, int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = value ?? DBNull.Value;
        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }

        return parameter;
    }

    private static int? GetNullableInt(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static bool? GetNullableBool(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static Guid? GetNullableGuid(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
