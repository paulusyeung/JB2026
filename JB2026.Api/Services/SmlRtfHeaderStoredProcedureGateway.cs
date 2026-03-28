using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SmlRtfHeaderStoredProcedureGateway : ISmlRtfHeaderStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SmlRtfHeaderStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<SmlRtfHeaderStoredProcedureRecord?> SelectAsync(Guid headerId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfHeader_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, headerId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SmlRtfHeaderStoredProcedureRecord(
            HeaderId: reader.GetGuid(reader.GetOrdinal("HeaderId")),
            RtfFileName: GetNullableString(reader, "RtfFileName"),
            PurchaseOrder: GetNullableString(reader, "PurchaseOrder"),
            CustomerPO: GetNullableString(reader, "CustomerPO"),
            OrderedOn: reader.GetDateTime(reader.GetOrdinal("OrderedOn")),
            OrderedBy: GetNullableString(reader, "OrderedBy"),
            OriginalPO: GetNullableString(reader, "OriginalPO"),
            SalesOrder: GetNullableString(reader, "SalesOrder"),
            OriginalSO: GetNullableString(reader, "OriginalSO"),
            Remarks: GetNullableString(reader, "Remarks"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: GetNullableGuid(reader, "CreatedBy"),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: reader.GetDateTime(reader.GetOrdinal("RetiredOn")),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateSmlRtfHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfHeader_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var headerIdOut = command.CreateParameter();
        headerIdOut.ParameterName = "@HeaderId";
        headerIdOut.DbType = DbType.Guid;
        headerIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(headerIdOut);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return headerIdOut.Value is Guid id
            ? id
            : Guid.Parse(headerIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output HeaderId."));
    }

    public async Task<bool> UpdateAsync(UpdateSmlRtfHeaderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfHeader_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid headerId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfHeader_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, headerId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateSmlRtfHeaderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@RtfFileName", DbType.String, request.RtfFileName, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@PurchaseOrder", DbType.String, request.PurchaseOrder, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerPO", DbType.String, request.CustomerPO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedOn", DbType.DateTime, request.OrderedOn));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedBy", DbType.String, request.OrderedBy, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalPO", DbType.String, request.OriginalPO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@SalesOrder", DbType.String, request.SalesOrder, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalSO", DbType.String, request.OriginalSO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddParameters(DbCommand command, UpdateSmlRtfHeaderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@RtfFileName", DbType.String, request.RtfFileName, size: 256));
        command.Parameters.Add(CreateInputParameter(command, "@PurchaseOrder", DbType.String, request.PurchaseOrder, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerPO", DbType.String, request.CustomerPO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedOn", DbType.DateTime, request.OrderedOn));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedBy", DbType.String, request.OrderedBy, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalPO", DbType.String, request.OriginalPO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@SalesOrder", DbType.String, request.SalesOrder, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalSO", DbType.String, request.OriginalSO, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
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

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static Guid? GetNullableGuid(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }
}
