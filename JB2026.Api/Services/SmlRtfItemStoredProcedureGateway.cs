using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SmlRtfItemStoredProcedureGateway : ISmlRtfItemStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SmlRtfItemStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<SmlRtfItemStoredProcedureRecord?> SelectAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfItems_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, itemId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SmlRtfItemStoredProcedureRecord(
            ItemId: reader.GetGuid(reader.GetOrdinal("ItemId")),
            HeaderId: reader.GetGuid(reader.GetOrdinal("HeaderId")),
            LineNumber: reader.GetInt32(reader.GetOrdinal("LineNumber")),
            ProductCode: GetNullableString(reader, "ProductCode"),
            ProductDescription: GetNullableString(reader, "ProductDescription"),
            Price: GetNullableString(reader, "Price"),
            Discount: GetNullableString(reader, "Discount"),
            Qty: GetNullableString(reader, "Qty"),
            Amount: GetNullableString(reader, "Amount"),
            PostProcess: GetNullableString(reader, "PostProcess"));
    }

    public async Task<Guid> InsertAsync(CreateSmlRtfItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfItems_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var itemIdOut = command.CreateParameter();
        itemIdOut.ParameterName = "@ItemId";
        itemIdOut.DbType = DbType.Guid;
        itemIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(itemIdOut);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return itemIdOut.Value is Guid id
            ? id
            : Guid.Parse(itemIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output ItemId."));
    }

    public async Task<bool> UpdateAsync(UpdateSmlRtfItemStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfItems_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, request.ItemId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfItems_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ItemId", DbType.Guid, itemId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateSmlRtfItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@LineNumber", DbType.Int32, request.LineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@ProductDescription", DbType.String, request.ProductDescription, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@Price", DbType.String, request.Price, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Discount", DbType.String, request.Discount, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.String, request.Qty, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Amount", DbType.String, request.Amount, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@PostProcess", DbType.String, request.PostProcess, size: 64));
    }

    private static void AddParameters(DbCommand command, UpdateSmlRtfItemStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@LineNumber", DbType.Int32, request.LineNumber));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@ProductDescription", DbType.String, request.ProductDescription, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@Price", DbType.String, request.Price, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Discount", DbType.String, request.Discount, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.String, request.Qty, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Amount", DbType.String, request.Amount, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@PostProcess", DbType.String, request.PostProcess, size: 64));
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
