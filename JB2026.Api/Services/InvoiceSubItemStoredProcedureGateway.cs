using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class InvoiceSubItemStoredProcedureGateway : IInvoiceSubItemStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public InvoiceSubItemStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<InvoiceSubItemStoredProcedureRecord?> SelectAsync(Guid subItemId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceSubItems_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, subItemId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InvoiceSubItemStoredProcedureRecord(
            SubItemId: reader.GetGuid(reader.GetOrdinal("SubItemId")),
            ItemId: reader.GetGuid(reader.GetOrdinal("ItemId")),
            SubLineNumber: reader.GetInt32(reader.GetOrdinal("SubLineNumber")),
            Description: GetNullableString(reader, "Description"),
            Quantity: GetNullableDecimal(reader, "Quantity"),
            UoM: GetNullableString(reader, "UoM"),
            Price: GetNullableDecimal(reader, "Price"),
            Amount: GetNullableDecimal(reader, "Amount"));
    }

    public async Task<Guid> InsertAsync(CreateInvoiceSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceSubItems_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var subItemIdOut = command.CreateParameter();
        subItemIdOut.ParameterName = "@SubItemId";
        subItemIdOut.DbType = DbType.Guid;
        subItemIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(subItemIdOut);

        AddInvoiceSubItemParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return subItemIdOut.Value is Guid subItemId
            ? subItemId
            : Guid.Parse(subItemIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output SubItemId."));
    }

    public async Task<bool> UpdateAsync(UpdateInvoiceSubItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceSubItems_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, request.SubItemId));

        AddInvoiceSubItemParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid subItemId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceSubItems_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SubItemId", DbType.Guid, subItemId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddInvoiceSubItemParameters(DbCommand command, CreateInvoiceSubItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));
        command.Parameters.Add(CreateInputParameter(command, "@SubLineNumber", DbType.Int32, request.SubLineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Description", DbType.String, request.Description, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@Quantity", DbType.Decimal, request.Quantity));
        command.Parameters.Add(CreateInputParameter(command, "@UoM", DbType.String, request.UoM, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@Price", DbType.Decimal, request.Price));
        command.Parameters.Add(CreateInputParameter(command, "@Amount", DbType.Decimal, request.Amount));
    }

    private static void AddInvoiceSubItemParameters(DbCommand command, UpdateInvoiceSubItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));
        command.Parameters.Add(CreateInputParameter(command, "@SubLineNumber", DbType.Int32, request.SubLineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@Description", DbType.String, request.Description, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@Quantity", DbType.Decimal, request.Quantity));
        command.Parameters.Add(CreateInputParameter(command, "@UoM", DbType.String, request.UoM, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@Price", DbType.Decimal, request.Price));
        command.Parameters.Add(CreateInputParameter(command, "@Amount", DbType.Decimal, request.Amount));
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

    private static decimal? GetNullableDecimal(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }
}
