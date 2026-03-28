using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class SmlRtfExtractToDNStoredProcedureGateway : ISmlRtfExtractToDNStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SmlRtfExtractToDNStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<SmlRtfExtractToDNStoredProcedureRecord?> SelectAsync(Guid dnId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfExtractToDN_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@DNId", DbType.Guid, dnId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SmlRtfExtractToDNStoredProcedureRecord(
            DNId: reader.GetGuid(reader.GetOrdinal("DNId")),
            HeaderId: reader.GetGuid(reader.GetOrdinal("HeaderId")),
            DNNumber: GetNullableString(reader, "DNNumber"),
            DNDate: reader.GetDateTime(reader.GetOrdinal("DNDate")),
            DNType: GetNullableInt32(reader, "DNType"),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")));
    }

    public async Task<Guid> InsertAsync(CreateSmlRtfExtractToDNStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfExtractToDN_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var outParam = command.CreateParameter();
        outParam.ParameterName = "@DNId";
        outParam.DbType = DbType.Guid;
        outParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(outParam);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return outParam.Value is Guid id
            ? id
            : Guid.Parse(outParam.Value?.ToString() ?? throw new InvalidOperationException("Missing output DNId."));
    }

    public async Task<bool> UpdateAsync(UpdateSmlRtfExtractToDNStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfExtractToDN_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@DNId", DbType.Guid, request.DNId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid dnId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSmlRtfExtractToDN_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@DNId", DbType.Guid, dnId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateSmlRtfExtractToDNStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@DNNumber", DbType.String, request.DNNumber, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@DNDate", DbType.DateTime, request.DNDate));
        command.Parameters.Add(CreateInputParameter(command, "@DNType", DbType.Int32, request.DNType));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
    }

    private static void AddParameters(DbCommand command, UpdateSmlRtfExtractToDNStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@HeaderId", DbType.Guid, request.HeaderId));
        command.Parameters.Add(CreateInputParameter(command, "@DNNumber", DbType.String, request.DNNumber, size: 16));
        command.Parameters.Add(CreateInputParameter(command, "@DNDate", DbType.DateTime, request.DNDate));
        command.Parameters.Add(CreateInputParameter(command, "@DNType", DbType.Int32, request.DNType));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
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

    private static int? GetNullableInt32(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}
