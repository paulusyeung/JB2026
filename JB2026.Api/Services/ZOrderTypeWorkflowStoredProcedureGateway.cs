using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ZOrderTypeWorkflowStoredProcedureGateway : IZOrderTypeWorkflowStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ZOrderTypeWorkflowStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ZOrderTypeWorkflowStoredProcedureRecord?> SelectAsync(Guid orderTypeWorkflowId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_OrderTypeWorkflow_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OrderTypeWorkflowId", DbType.Guid, orderTypeWorkflowId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ZOrderTypeWorkflowStoredProcedureRecord(
            OrderTypeWorkflowId: reader.GetGuid(reader.GetOrdinal("OrderTypeWorkflowId")),
            WorkflowId: GetNullableGuid(reader, "WorkflowId"),
            OrderType: reader.GetInt32(reader.GetOrdinal("OrderType")),
            WorkIndex: reader.GetInt32(reader.GetOrdinal("WorkIndex")));
    }

    public async Task<Guid> InsertAsync(CreateZOrderTypeWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_OrderTypeWorkflow_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var outParam = command.CreateParameter();
        outParam.ParameterName = "@OrderTypeWorkflowId";
        outParam.DbType = DbType.Guid;
        outParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(outParam);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return outParam.Value is Guid id
            ? id
            : Guid.Parse(outParam.Value?.ToString() ?? throw new InvalidOperationException("Missing output OrderTypeWorkflowId."));
    }

    public async Task<bool> UpdateAsync(UpdateZOrderTypeWorkflowStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_OrderTypeWorkflow_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OrderTypeWorkflowId", DbType.Guid, request.OrderTypeWorkflowId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid orderTypeWorkflowId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_OrderTypeWorkflow_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OrderTypeWorkflowId", DbType.Guid, orderTypeWorkflowId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateZOrderTypeWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@OrderType", DbType.Int32, request.OrderType));
        command.Parameters.Add(CreateInputParameter(command, "@WorkIndex", DbType.Int32, request.WorkIndex));
    }

    private static void AddParameters(DbCommand command, UpdateZOrderTypeWorkflowStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@WorkflowId", DbType.Guid, request.WorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@OrderType", DbType.Int32, request.OrderType));
        command.Parameters.Add(CreateInputParameter(command, "@WorkIndex", DbType.Int32, request.WorkIndex));
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
