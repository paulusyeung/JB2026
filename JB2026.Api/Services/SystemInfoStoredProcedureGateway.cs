using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JB2026.Api.Services;

public sealed class SystemInfoStoredProcedureGateway : ISystemInfoStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public SystemInfoStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

        public async Task<SystemInfoStoredProcedureRecord?> SelectFirstAsync(CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSystemInfo_SelAll";
        command.CommandType = CommandType.StoredProcedure;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SystemInfoStoredProcedureRecord(
            SystemId: reader.GetGuid(reader.GetOrdinal("SystemId")),
            OwnerName: GetNullableString(reader, "OwnerName"),
            MetadataXml: GetNullableString(reader, "MetadataXml"));
    }

    public async Task<SystemInfoStoredProcedureRecord?> SelectAsync(Guid systemId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSystemInfo_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SystemId", DbType.Guid, systemId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SystemInfoStoredProcedureRecord(
            SystemId: reader.GetGuid(reader.GetOrdinal("SystemId")),
            OwnerName: GetNullableString(reader, "OwnerName"),
            MetadataXml: GetNullableString(reader, "MetadataXml"));
    }

    public async Task<Guid> InsertAsync(CreateSystemInfoStoredProcedureRequest request, CancellationToken cancellationToken = default, DbTransaction? transaction = null)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSystemInfo_InsRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = transaction;

        var outParam = command.CreateParameter();
        outParam.ParameterName = "@SystemId";
        outParam.DbType = DbType.Guid;
        outParam.Direction = ParameterDirection.Output;
        command.Parameters.Add(outParam);

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return outParam.Value is Guid id
            ? id
            : Guid.Parse(outParam.Value?.ToString() ?? throw new InvalidOperationException("Missing output SystemId."));
    }

    public async Task<bool> UpdateAsync(UpdateSystemInfoStoredProcedureRequest request, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSystemInfo_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = transaction;
        command.Parameters.Add(CreateInputParameter(command, "@SystemId", DbType.Guid, request.SystemId));

        AddParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<SystemInfoStoredProcedureRecord?> SelectFirstForUpdateAsync(DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT TOP 1 [SystemId], [OwnerName], [MetadataXml] FROM [dbo].[SystemInfo] WITH (UPDLOCK, HOLDLOCK)";
        command.CommandType = CommandType.Text;
        command.Transaction = transaction;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SystemInfoStoredProcedureRecord(
            SystemId: reader.GetGuid(reader.GetOrdinal("SystemId")),
            OwnerName: GetNullableString(reader, "OwnerName"),
            MetadataXml: GetNullableString(reader, "MetadataXml"));
    }

    public async Task<string?> MutateMetadataAsync(Func<string?, string?> mutate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        await using var transaction = await _writeContext.Database.BeginTransactionAsync(cancellationToken);
        var dbTransaction = transaction.GetDbTransaction();

        try
        {
            var snapshot = await SelectFirstForUpdateAsync(dbTransaction, cancellationToken);
            var metadataXml = mutate(snapshot?.MetadataXml);

            if (metadataXml is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return snapshot?.MetadataXml;
            }

            if (snapshot is null)
            {
                await InsertAsync(
                    new CreateSystemInfoStoredProcedureRequest(OwnerName: null, MetadataXml: metadataXml),
                    cancellationToken,
                    dbTransaction);
            }
            else
            {
                await UpdateAsync(
                    new UpdateSystemInfoStoredProcedureRequest(
                        SystemId: snapshot.SystemId,
                        OwnerName: snapshot.OwnerName,
                        MetadataXml: metadataXml),
                    dbTransaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return snapshot?.MetadataXml;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid systemId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spSystemInfo_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@SystemId", DbType.Guid, systemId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddParameters(DbCommand command, CreateSystemInfoStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OwnerName", DbType.String, request.OwnerName, size: 255));
        command.Parameters.Add(CreateXmlParameter(command, "@MetadataXml", request.MetadataXml));
    }

    private static void AddParameters(DbCommand command, UpdateSystemInfoStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OwnerName", DbType.String, request.OwnerName, size: 255));
        command.Parameters.Add(CreateXmlParameter(command, "@MetadataXml", request.MetadataXml));
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

    private static DbParameter CreateXmlParameter(DbCommand command, string name, string? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Xml;
        parameter.Direction = ParameterDirection.Input;
        parameter.Value = value is not null ? (object)value : DBNull.Value;
        return parameter;
    }

    private static string? GetNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
