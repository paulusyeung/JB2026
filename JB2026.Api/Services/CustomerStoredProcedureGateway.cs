using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class CustomerStoredProcedureGateway : ICustomerStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public CustomerStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<CustomerStoredProcedureRecord?> SelectAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spCustomers_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@CustomerId", DbType.Guid, customerId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerStoredProcedureRecord(
            CustomerId: reader.GetGuid(reader.GetOrdinal("CustomerId")),
            CustomerName: GetNullableString(reader, "CustomerName"),
            LoginAccount: GetNullableString(reader, "LoginAccount"),
            LoginPassword: GetNullableString(reader, "LoginPassword"),
            MetadataXml: GetNullableXmlString(reader, "MetadataXml"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spCustomers_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var customerIdOut = command.CreateParameter();
        customerIdOut.ParameterName = "@CustomerId";
        customerIdOut.DbType = DbType.Guid;
        customerIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(customerIdOut);

        AddCustomerParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return customerIdOut.Value is Guid customerId
            ? customerId
            : Guid.Parse(customerIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output CustomerId."));
    }

    public async Task<bool> UpdateAsync(UpdateCustomerStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spCustomers_UpdRec";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(CreateInputParameter(command, "@CustomerId", DbType.Guid, request.CustomerId));
        AddCustomerParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spCustomers_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@CustomerId", DbType.Guid, customerId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddCustomerParameters(DbCommand command, CreateCustomerStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CustomerName", DbType.String, request.CustomerName, size: 64));
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

    private static void AddCustomerParameters(DbCommand command, UpdateCustomerStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CustomerName", DbType.String, request.CustomerName, size: 64));
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

    private static string? GetNullableXmlString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetFieldValue<object>(ordinal) switch
        {
            string value => value,
            SqlXml sqlXml => sqlXml.IsNull ? null : sqlXml.Value,
            _ => reader.GetValue(ordinal)?.ToString(),
        };
    }
}
