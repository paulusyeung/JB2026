using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ProductStoredProcedureGateway : IProductStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ProductStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ProductStoredProcedureRecord?> SelectAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spProduct_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ProductId", DbType.Guid, productId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ProductStoredProcedureRecord(
            ProductId: reader.GetGuid(reader.GetOrdinal("ProductId")),
            CategoryId: GetNullableGuid(reader, "CategoryId"),
            StockNumber: GetNullableString(reader, "StockNumber"),
            ProductCode: GetNullableString(reader, "ProductCode"),
            ProductName: GetNullableString(reader, "ProductName"),
            Description: GetNullableString(reader, "Description"),
            Remarks: GetNullableString(reader, "Remarks"),
            MOQ: reader.GetInt32(reader.GetOrdinal("MOQ")),
            Balance: reader.GetInt32(reader.GetOrdinal("Balance")),
            SellingPrice: reader.GetDecimal(reader.GetOrdinal("SellingPrice")),
            COGS: reader.GetDecimal(reader.GetOrdinal("COGS")),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateProductStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spProduct_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var productIdOut = command.CreateParameter();
        productIdOut.ParameterName = "@ProductId";
        productIdOut.DbType = DbType.Guid;
        productIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(productIdOut);

        AddProductParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return productIdOut.Value is Guid productId
            ? productId
            : Guid.Parse(productIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output ProductId."));
    }

    public async Task<bool> UpdateAsync(UpdateProductStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spProduct_UpdRec";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(CreateInputParameter(command, "@ProductId", DbType.Guid, request.ProductId));
        AddProductParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spProduct_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@ProductId", DbType.Guid, productId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddProductParameters(DbCommand command, CreateProductStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CategoryId", DbType.Guid, request.CategoryId));
        command.Parameters.Add(CreateInputParameter(command, "@StockNumber", DbType.String, request.StockNumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductName", DbType.String, request.ProductName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@Description", DbType.String, request.Description, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@MOQ", DbType.Int32, request.MOQ));
        command.Parameters.Add(CreateInputParameter(command, "@Balance", DbType.Int32, request.Balance));
        command.Parameters.Add(CreateInputParameter(command, "@SellingPrice", DbType.Decimal, request.SellingPrice));
        command.Parameters.Add(CreateInputParameter(command, "@COGS", DbType.Decimal, request.COGS));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddProductParameters(DbCommand command, UpdateProductStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CategoryId", DbType.Guid, request.CategoryId));
        command.Parameters.Add(CreateInputParameter(command, "@StockNumber", DbType.String, request.StockNumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductName", DbType.String, request.ProductName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@Description", DbType.String, request.Description, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@MOQ", DbType.Int32, request.MOQ));
        command.Parameters.Add(CreateInputParameter(command, "@Balance", DbType.Int32, request.Balance));
        command.Parameters.Add(CreateInputParameter(command, "@SellingPrice", DbType.Decimal, request.SellingPrice));
        command.Parameters.Add(CreateInputParameter(command, "@COGS", DbType.Decimal, request.COGS));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
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
