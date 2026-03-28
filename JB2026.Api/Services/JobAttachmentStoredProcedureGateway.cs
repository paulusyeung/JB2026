using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobAttachmentStoredProcedureGateway : IJobAttachmentStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobAttachmentStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobAttachmentStoredProcedureRecord?> SelectAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobAttachment_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentId", DbType.Guid, attachmentId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobAttachmentStoredProcedureRecord(
            AttachmentId: reader.GetGuid(reader.GetOrdinal("AttachmentId")),
            OrderId: reader.IsDBNull(reader.GetOrdinal("OrderId")) ? null : reader.GetGuid(reader.GetOrdinal("OrderId")),
            AttachmentType: reader.GetInt32(reader.GetOrdinal("AttachmentType")),
            AttachmentIndex: reader.GetInt32(reader.GetOrdinal("AttachmentIndex")),
            OriginalFileName: reader.IsDBNull(reader.GetOrdinal("OriginalFileName"))
                ? null
                : reader.GetString(reader.GetOrdinal("OriginalFileName")));
    }

    public async Task<Guid> InsertAsync(CreateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobAttachment_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var attachmentIdOut = command.CreateParameter();
        attachmentIdOut.ParameterName = "@AttachmentId";
        attachmentIdOut.DbType = DbType.Guid;
        attachmentIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(attachmentIdOut);

        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentType", DbType.Int32, request.AttachmentType));
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentIndex", DbType.Int32, request.AttachmentIndex));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalFileName", DbType.String, request.OriginalFileName, size: 255));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return attachmentIdOut.Value is Guid attachmentId
            ? attachmentId
            : Guid.Parse(attachmentIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output AttachmentId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobAttachmentStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobAttachment_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentId", DbType.Guid, request.AttachmentId));
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentType", DbType.Int32, request.AttachmentType));
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentIndex", DbType.Int32, request.AttachmentIndex));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalFileName", DbType.String, request.OriginalFileName, size: 255));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobAttachment_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@AttachmentId", DbType.Guid, attachmentId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
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
}
