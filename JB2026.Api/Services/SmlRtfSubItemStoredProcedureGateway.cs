using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SmlRtfSubItemStoredProcedureGateway : ISmlRtfSubItemStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SmlRtfSubItemStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<SmlRtfSubItemStoredProcedureRecord?> SelectAsync(Guid subItemId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfSubItems_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, subItemId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SmlRtfSubItemStoredProcedureRecord(
            SubItemId: reader.GetGuid(reader.GetOrdinal("SubItemId")),
            ItemId: reader.GetGuid(reader.GetOrdinal("ItemId")),
            SubLineNumber: reader.GetInt32(reader.GetOrdinal("SubLineNumber")),
            Start_End: GetNullableString(reader, "Start_End"),
            ReferenceNumber: GetNullableString(reader, "ReferenceNumber"),
            LabelSize: GetNullableString(reader, "LabelSize"),
            Qty: GetNullableString(reader, "Qty"));
    }

    public async Task<Guid> InsertAsync(CreateSmlRtfSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfSubItems_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var outParam = command.CreateParameter();
        outParam.ParameterName = "@SubItemId";
        outParam.DbType = DbType.Guid;
        outParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(outParam);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return outParam.Value is Guid id
            ? id
            : Guid.Parse(outParam.Value?.ToString() ?? throw new InvalidOperationException("Missing output SubItemId."));
    }

    public async Task<bool> UpdateAsync(UpdateSmlRtfSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfSubItems_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, request.SubItemId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid subItemId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfSubItems_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, subItemId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateSmlRtfSubItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));
        command.Parameters.Add(CreateInputParameter(command, "@SubLineNumber", DbType.Int32, request.SubLineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Start_End", DbType.String, request.Start_End, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@ReferenceNumber", DbType.String, request.ReferenceNumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@LabelSize", DbType.String, request.LabelSize, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.String, request.Qty, size: 10));
    }

    private static void AddParameters(DbCommand command, UpdateSmlRtfSubItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));
        command.Parameters.Add(CreateInputParameter(command, "@SubLineNumber", DbType.Int32, request.SubLineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Start_End", DbType.String, request.Start_End, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@ReferenceNumber", DbType.String, request.ReferenceNumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@LabelSize", DbType.String, request.LabelSize, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.String, request.Qty, size: 10));
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
