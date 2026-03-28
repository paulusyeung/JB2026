using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobPackingOnAirStoredProcedureGateway : IJobPackingOnAirStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobPackingOnAirStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobPackingOnAirStoredProcedureRecord?> SelectAsync(Guid onAirId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobPackingOnAir_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OnAirId", DbType.Guid, onAirId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobPackingOnAirStoredProcedureRecord(
            OnAirId: reader.GetGuid(reader.GetOrdinal("OnAirId")),
            OrderId: reader.GetGuid(reader.GetOrdinal("OrderId")),
            OnAiredOn: reader.GetDateTime(reader.GetOrdinal("OnAiredOn")),
            OnAiredBy: reader.GetGuid(reader.GetOrdinal("OnAiredBy")),
            Priority: GetNullableInt32(reader, "Priority"),
            Status: GetNullableInt32(reader, "Status"),
            CompletedOn: reader.GetDateTime(reader.GetOrdinal("CompletedOn")),
            CompletedBy: GetNullableGuid(reader, "CompletedBy"),
            Cancelled: GetNullableBoolean(reader, "Cancelled"),
            CancelledOn: reader.GetDateTime(reader.GetOrdinal("CancelledOn")),
            CancelledBy: GetNullableGuid(reader, "CancelledBy"),
            RescheduledCount: GetNullableInt32(reader, "RescheduledCount"),
            RescheduledOn: reader.GetDateTime(reader.GetOrdinal("RescheduledOn")),
            RescheduledBy: GetNullableGuid(reader, "RescheduledBy"));
    }

    public async Task<Guid> InsertAsync(CreateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobPackingOnAir_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var outParam = command.CreateParameter();
        outParam.ParameterName = "@OnAirId";
        outParam.DbType = DbType.Guid;
        outParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(outParam);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return outParam.Value is Guid id
            ? id
            : Guid.Parse(outParam.Value?.ToString() ?? throw new InvalidOperationException("Missing output OnAirId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobPackingOnAirStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobPackingOnAir_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OnAirId", DbType.Guid, request.OnAirId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid onAirId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobPackingOnAir_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OnAirId", DbType.Guid, onAirId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateJobPackingOnAirStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@OnAiredOn", DbType.DateTime, request.OnAiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@OnAiredBy", DbType.Guid, request.OnAiredBy));
        command.Parameters.Add(CreateInputParameter(command, "@Priority", DbType.Int32, request.Priority));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedBy", DbType.Guid, request.CompletedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Cancelled", DbType.Boolean, request.Cancelled));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledOn", DbType.DateTime, request.CancelledOn));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledBy", DbType.Guid, request.CancelledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledCount", DbType.Int32, request.RescheduledCount));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledOn", DbType.DateTime, request.RescheduledOn));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledBy", DbType.Guid, request.RescheduledBy));
    }

    private static void AddParameters(DbCommand command, UpdateJobPackingOnAirStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@OnAiredOn", DbType.DateTime, request.OnAiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@OnAiredBy", DbType.Guid, request.OnAiredBy));
        command.Parameters.Add(CreateInputParameter(command, "@Priority", DbType.Int32, request.Priority));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedBy", DbType.Guid, request.CompletedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Cancelled", DbType.Boolean, request.Cancelled));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledOn", DbType.DateTime, request.CancelledOn));
        command.Parameters.Add(CreateInputParameter(command, "@CancelledBy", DbType.Guid, request.CancelledBy));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledCount", DbType.Int32, request.RescheduledCount));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledOn", DbType.DateTime, request.RescheduledOn));
        command.Parameters.Add(CreateInputParameter(command, "@RescheduledBy", DbType.Guid, request.RescheduledBy));
    }

    private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static DbParameter CreateInputParameter(DbCommand command, string name, DbType dbType, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = value ?? DBNull.Value;
        return parameter;
    }

    private static Guid? GetNullableGuid(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static int? GetNullableInt32(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static bool? GetNullableBoolean(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }
}
