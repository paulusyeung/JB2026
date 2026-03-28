using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SupplierStoredProcedureGateway : ISupplierStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SupplierStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<SupplierStoredProcedureRecord?> SelectAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSupplier_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SupplierId", DbType.Guid, supplierId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SupplierStoredProcedureRecord(
            SupplierId: reader.GetGuid(reader.GetOrdinal("SupplierId")),
            SupplierName: GetNullableString(reader, "SupplierName"),
            LoginAccount: GetNullableString(reader, "LoginAccount"),
            LoginPassword: GetNullableString(reader, "LoginPassword"),
            MetadataXml: GetNullableString(reader, "MetadataXml"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateSupplierStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSupplier_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var supplierIdOut = command.CreateParameter();
        supplierIdOut.ParameterName = "@SupplierId";
        supplierIdOut.DbType = DbType.Guid;
        supplierIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(supplierIdOut);

        AddSupplierParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return supplierIdOut.Value is Guid supplierId
            ? supplierId
            : Guid.Parse(supplierIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output SupplierId."));
    }

    public async Task<bool> UpdateAsync(UpdateSupplierStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSupplier_UpdRec";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(CreateInputParameter(command, "@SupplierId", DbType.Guid, request.SupplierId));
        AddSupplierParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSupplier_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SupplierId", DbType.Guid, supplierId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddSupplierParameters(DbCommand command, CreateSupplierStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@SupplierName", DbType.String, request.SupplierName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@LoginAccount", DbType.String, request.LoginAccount, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@LoginPassword", DbType.String, request.LoginPassword, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@MetadataXml", DbType.Xml, request.MetadataXml));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddSupplierParameters(DbCommand command, UpdateSupplierStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@SupplierName", DbType.String, request.SupplierName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@LoginAccount", DbType.String, request.LoginAccount, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@LoginPassword", DbType.String, request.LoginPassword, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@MetadataXml", DbType.Xml, request.MetadataXml));
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
