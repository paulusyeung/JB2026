using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ZWorkflowFormStoredProcedureGateway : IZWorkflowFormStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ZWorkflowFormStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ZWorkflowFormStoredProcedureRecord?> SelectAsync(Guid workflowFormId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_WorkflowForms_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowFormId", DbType.Guid, workflowFormId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ZWorkflowFormStoredProcedureRecord(
            WorkflowFormId: reader.GetGuid(reader.GetOrdinal("WorkflowFormId")),
            WorkflowId: GetNullableGuid(reader, "WorkflowId"),
            FormId: GetNullableGuid(reader, "FormId"),
            SeqNumber: reader.GetInt32(reader.GetOrdinal("SeqNumber")));
    }

    public async Task<Guid> InsertAsync(CreateZWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_WorkflowForms_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var workflowFormIdOut = command.CreateParameter();
        workflowFormIdOut.ParameterName = "@WorkflowFormId";
        workflowFormIdOut.DbType = DbType.Guid;
        workflowFormIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(workflowFormIdOut);

        AddZWorkflowFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return workflowFormIdOut.Value is Guid id
            ? id
            : Guid.Parse(workflowFormIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output WorkflowFormId."));
    }

    public async Task<bool> UpdateAsync(UpdateZWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_WorkflowForms_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowFormId", DbType.Guid, request.WorkflowFormId));

        AddZWorkflowFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid workflowFormId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_WorkflowForms_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowFormId", DbType.Guid, workflowFormId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddZWorkflowFormParameters(DbCommand command, CreateZWorkflowFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, request.FormId));
        command.Parameters.Add(CreateInputParameter(command, "@SeqNumber", DbType.Int32, request.SeqNumber));
    }

    private static void AddZWorkflowFormParameters(DbCommand command, UpdateZWorkflowFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, request.FormId));
        command.Parameters.Add(CreateInputParameter(command, "@SeqNumber", DbType.Int32, request.SeqNumber));
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

    private static Guid? GetNullableGuid(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }
}
