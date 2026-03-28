using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class InvoiceHeaderStoredProcedureGateway : IInvoiceHeaderStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public InvoiceHeaderStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<InvoiceHeaderStoredProcedureRecord?> SelectAsync(Guid headerId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceHeader_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, headerId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InvoiceHeaderStoredProcedureRecord(
            HeaderId: reader.GetGuid(reader.GetOrdinal("HeaderId")),
            CustomerId: GetNullableGuid(reader, "CustomerId"),
            BillTo: GetNullableString(reader, "BillTo"),
            ShipTo: GetNullableString(reader, "ShipTo"),
            InvoiceDate: reader.GetDateTime(reader.GetOrdinal("InvoiceDate")),
            InvoiceNumber: GetNullableString(reader, "InvoiceNumber"),
            InvoiceAmount: GetNullableDecimal(reader, "InvoiceAmount"),
            ICNumber: GetNullableString(reader, "ICNumber"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateInvoiceHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceHeader_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var headerIdOut = command.CreateParameter();
        headerIdOut.ParameterName = "@HeaderId";
        headerIdOut.DbType = DbType.Guid;
        headerIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(headerIdOut);

        AddInvoiceHeaderParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return headerIdOut.Value is Guid headerId
            ? headerId
            : Guid.Parse(headerIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output HeaderId."));
    }

    public async Task<bool> UpdateAsync(UpdateInvoiceHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceHeader_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));

        AddInvoiceHeaderParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid headerId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spInvoiceHeader_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, headerId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddInvoiceHeaderParameters(DbCommand command, CreateInvoiceHeaderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CustomerId", DbType.Guid, request.CustomerId));
        command.Parameters.Add(CreateInputParameter(command, "@BillTo", DbType.String, request.BillTo, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@ShipTo", DbType.String, request.ShipTo, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceDate", DbType.DateTime, request.InvoiceDate));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceNumber", DbType.String, request.InvoiceNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceAmount", DbType.Decimal, request.InvoiceAmount));
        command.Parameters.Add(CreateInputParameter(command, "@ICNumber", DbType.String, request.ICNumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddInvoiceHeaderParameters(DbCommand command, UpdateInvoiceHeaderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CustomerId", DbType.Guid, request.CustomerId));
        command.Parameters.Add(CreateInputParameter(command, "@BillTo", DbType.String, request.BillTo, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@ShipTo", DbType.String, request.ShipTo, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceDate", DbType.DateTime, request.InvoiceDate));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceNumber", DbType.String, request.InvoiceNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceAmount", DbType.Decimal, request.InvoiceAmount));
        command.Parameters.Add(CreateInputParameter(command, "@ICNumber", DbType.String, request.ICNumber, size: 32));
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

    private static decimal? GetNullableDecimal(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
