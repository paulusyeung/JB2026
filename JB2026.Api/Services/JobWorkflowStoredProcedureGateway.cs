using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobWorkflowStoredProcedureGateway : IJobWorkflowStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobWorkflowStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobWorkflowStoredProcedureRecord?> SelectAsync(Guid jobWorkflowId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflow_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowId", DbType.Guid, jobWorkflowId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobWorkflowStoredProcedureRecord(
            JobWorkflowId: reader.GetGuid(reader.GetOrdinal("JobWorkflowId")),
            OrderId: reader.GetGuid(reader.GetOrdinal("OrderId")),
            WorkflowId: GetNullableGuid(reader, "WorkflowId"),
            WorkIndex: reader.GetInt32(reader.GetOrdinal("WorkIndex")),
            WorkTitle: GetNullableString(reader, "WorkTitle"),
            WorkInstruction: GetNullableString(reader, "WorkInstruction"),
            WorkStatus: GetNullableInt32(reader, "WorkStatus"),
            WorkNotes: GetNullableString(reader, "WorkNotes"),
            ModifiedOn: GetNullableDateTime(reader, "ModifiedOn"),
            ModifiedBy: GetNullableGuid(reader, "ModifiedBy"));
    }

    public async Task<Guid> InsertAsync(CreateJobWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflow_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var jobWorkflowIdOut = command.CreateParameter();
        jobWorkflowIdOut.ParameterName = "@JobWorkflowId";
        jobWorkflowIdOut.DbType = DbType.Guid;
        jobWorkflowIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(jobWorkflowIdOut);

        AddJobWorkflowParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return jobWorkflowIdOut.Value is Guid id
            ? id
            : Guid.Parse(jobWorkflowIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output JobWorkflowId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflow_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowId", DbType.Guid, request.JobWorkflowId));

        AddJobWorkflowParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid jobWorkflowId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflow_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowId", DbType.Guid, jobWorkflowId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddJobWorkflowParameters(DbCommand command, CreateJobWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@WorkIndex", DbType.Int32, request.WorkIndex));
        command.Parameters.Add(CreateInputParameter(command, "@WorkTitle", DbType.String, request.WorkTitle, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@WorkInstruction", DbType.String, request.WorkInstruction, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@WorkStatus", DbType.Int32, request.WorkStatus));
        command.Parameters.Add(CreateInputParameter(command, "@WorkNotes", DbType.String, request.WorkNotes));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
    }

    private static void AddJobWorkflowParameters(DbCommand command, UpdateJobWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@WorkIndex", DbType.Int32, request.WorkIndex));
        command.Parameters.Add(CreateInputParameter(command, "@WorkTitle", DbType.String, request.WorkTitle, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@WorkInstruction", DbType.String, request.WorkInstruction, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@WorkStatus", DbType.Int32, request.WorkStatus));
        command.Parameters.Add(CreateInputParameter(command, "@WorkNotes", DbType.String, request.WorkNotes));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
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

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
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

    private static DateTime? GetNullableDateTime(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
