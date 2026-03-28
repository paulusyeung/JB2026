using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ZWorkflowStoredProcedureGateway : IZWorkflowStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ZWorkflowStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ZWorkflowStoredProcedureRecord?> SelectAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Workflow_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, workflowId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ZWorkflowStoredProcedureRecord(
            WorkflowId: reader.GetGuid(reader.GetOrdinal("WorkflowId")),
            WorkflowName: GetNullableString(reader, "WorkflowName"),
            WorkTitle: GetNullableString(reader, "WorkTitle"),
            WorkInstruction: GetNullableString(reader, "WorkInstruction"));
    }

    public async Task<Guid> InsertAsync(CreateZWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Workflow_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var workflowIdOut = command.CreateParameter();
        workflowIdOut.ParameterName = "@WorkflowId";
        workflowIdOut.DbType = DbType.Guid;
        workflowIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(workflowIdOut);

        AddZWorkflowParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return workflowIdOut.Value is Guid id
            ? id
            : Guid.Parse(workflowIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output WorkflowId."));
    }

    public async Task<bool> UpdateAsync(UpdateZWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Workflow_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));

        AddZWorkflowParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Workflow_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, workflowId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddZWorkflowParameters(DbCommand command, CreateZWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowName", DbType.String, request.WorkflowName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@WorkTitle", DbType.String, request.WorkTitle, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@WorkInstruction", DbType.String, request.WorkInstruction, size: 512));
    }

    private static void AddZWorkflowParameters(DbCommand command, UpdateZWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowName", DbType.String, request.WorkflowName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@WorkTitle", DbType.String, request.WorkTitle, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@WorkInstruction", DbType.String, request.WorkInstruction, size: 512));
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
}
