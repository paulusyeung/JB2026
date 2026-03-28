using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ZCategoryStoredProcedureGateway : IZCategoryStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ZCategoryStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ZCategoryStoredProcedureRecord?> SelectAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Category_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@CategoryId", DbType.Guid, categoryId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ZCategoryStoredProcedureRecord(
            CategoryId: reader.GetGuid(reader.GetOrdinal("CategoryId")),
            CategoryCode: GetNullableString(reader, "CategoryCode"),
            CategoryName: GetNullableString(reader, "CategoryName"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateZCategoryStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Category_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var categoryIdOut = command.CreateParameter();
        categoryIdOut.ParameterName = "@CategoryId";
        categoryIdOut.DbType = DbType.Guid;
        categoryIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(categoryIdOut);

        AddZCategoryParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return categoryIdOut.Value is Guid id
            ? id
            : Guid.Parse(categoryIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output CategoryId."));
    }

    public async Task<bool> UpdateAsync(UpdateZCategoryStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Category_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@CategoryId", DbType.Guid, request.CategoryId));

        AddZCategoryParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Category_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@CategoryId", DbType.Guid, categoryId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddZCategoryParameters(DbCommand command, CreateZCategoryStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CategoryCode", DbType.String, request.CategoryCode, size: 3));
        command.Parameters.Add(CreateInputParameter(command, "@CategoryName", DbType.String, request.CategoryName, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddZCategoryParameters(DbCommand command, UpdateZCategoryStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@CategoryCode", DbType.String, request.CategoryCode, size: 3));
        command.Parameters.Add(CreateInputParameter(command, "@CategoryName", DbType.String, request.CategoryName, size: 64));
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

    private static DateTime? GetNullableDateTime(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
