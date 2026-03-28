using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class StockInOutStoredProcedureGateway : IStockInOutStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public StockInOutStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<StockInOutStoredProcedureRecord?> SelectAsync(Guid inOutId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spStockInOut_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@InOutId", DbType.Guid, inOutId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new StockInOutStoredProcedureRecord(
            InOutId: reader.GetGuid(reader.GetOrdinal("InOutId")),
            ProductId: GetNullableGuid(reader, "ProductId"),
            InOutDate: reader.GetDateTime(reader.GetOrdinal("InOutDate")),
            Reference: GetNullableString(reader, "Reference"),
            Qty: reader.GetInt32(reader.GetOrdinal("Qty")),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")));
    }

    public async Task<Guid> InsertAsync(CreateStockInOutStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spStockInOut_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var inOutIdOut = command.CreateParameter();
        inOutIdOut.ParameterName = "@InOutId";
        inOutIdOut.DbType = DbType.Guid;
        inOutIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(inOutIdOut);

        AddStockInOutParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return inOutIdOut.Value is Guid inOutId
            ? inOutId
            : Guid.Parse(inOutIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output InOutId."));
    }

    public async Task<bool> UpdateAsync(UpdateStockInOutStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spStockInOut_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@InOutId", DbType.Guid, request.InOutId));

        AddStockInOutParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid inOutId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spStockInOut_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@InOutId", DbType.Guid, inOutId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddStockInOutParameters(DbCommand command, CreateStockInOutStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ProductId", DbType.Guid, request.ProductId));
        command.Parameters.Add(CreateInputParameter(command, "@InOutDate", DbType.DateTime, request.InOutDate));
        command.Parameters.Add(CreateInputParameter(command, "@Reference", DbType.String, request.Reference, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.Int32, request.Qty));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
    }

    private static void AddStockInOutParameters(DbCommand command, UpdateStockInOutStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@ProductId", DbType.Guid, request.ProductId));
        command.Parameters.Add(CreateInputParameter(command, "@InOutDate", DbType.DateTime, request.InOutDate));
        command.Parameters.Add(CreateInputParameter(command, "@Reference", DbType.String, request.Reference, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.Int32, request.Qty));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
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
