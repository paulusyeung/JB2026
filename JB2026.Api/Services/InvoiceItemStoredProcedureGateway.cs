using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class InvoiceItemStoredProcedureGateway : IInvoiceItemStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public InvoiceItemStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<InvoiceItemStoredProcedureRecord?> SelectAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceItems_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, itemId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InvoiceItemStoredProcedureRecord(
            ItemId: reader.GetGuid(reader.GetOrdinal("ItemId")),
            HeaderId: reader.GetGuid(reader.GetOrdinal("HeaderId")),
            SmlRtfHeaderId: GetNullableGuid(reader, "SmlRtfHeaderId"),
            LineNumber: reader.GetInt32(reader.GetOrdinal("LineNumber")),
            Notes: GetNullableString(reader, "Notes"));
    }

    public async Task<Guid> InsertAsync(CreateInvoiceItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceItems_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var itemIdOut = command.CreateParameter();
        itemIdOut.ParameterName = "@ItemId";
        itemIdOut.DbType = DbType.Guid;
        itemIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(itemIdOut);

        AddInvoiceItemParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return itemIdOut.Value is Guid itemId
            ? itemId
            : Guid.Parse(itemIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output ItemId."));
    }

    public async Task<bool> UpdateAsync(UpdateInvoiceItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceItems_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));

        AddInvoiceItemParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceItems_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, itemId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddInvoiceItemParameters(DbCommand command, CreateInvoiceItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@SmlRtfHeaderId", DbType.Guid, request.SmlRtfHeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@LineNumber", DbType.Int32, request.LineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Notes", DbType.String, request.Notes, size: 128));
    }

    private static void AddInvoiceItemParameters(DbCommand command, UpdateInvoiceItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@SmlRtfHeaderId", DbType.Guid, request.SmlRtfHeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@LineNumber", DbType.Int32, request.LineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Notes", DbType.String, request.Notes, size: 128));
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

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
