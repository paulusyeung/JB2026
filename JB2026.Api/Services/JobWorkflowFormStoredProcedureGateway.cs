using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobWorkflowFormStoredProcedureGateway : IJobWorkflowFormStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobWorkflowFormStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobWorkflowFormStoredProcedureRecord?> SelectAsync(Guid jobWorkflowFormId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflowForms_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowFormId", DbType.Guid, jobWorkflowFormId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobWorkflowFormStoredProcedureRecord(
            JobWorkflowFormId: reader.GetGuid(reader.GetOrdinal("JobWorkflowFormId")),
            JobWorkflowId: reader.GetGuid(reader.GetOrdinal("JobWorkflowId")),
            FormId: GetNullableGuid(reader, "FormId"),
            SeqNumber: GetNullableInt32(reader, "SeqNumber"),
            MetadataXml: GetNullableString(reader, "MetadataXml"));
    }

    public async Task<Guid> InsertAsync(CreateJobWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflowForms_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var jobWorkflowFormIdOut = command.CreateParameter();
        jobWorkflowFormIdOut.ParameterName = "@JobWorkflowFormId";
        jobWorkflowFormIdOut.DbType = DbType.Guid;
        jobWorkflowFormIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(jobWorkflowFormIdOut);

        AddJobWorkflowFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return jobWorkflowFormIdOut.Value is Guid id
            ? id
            : Guid.Parse(jobWorkflowFormIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output JobWorkflowFormId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobWorkflowFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflowForms_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowFormId", DbType.Guid, request.JobWorkflowFormId));

        AddJobWorkflowFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid jobWorkflowFormId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobWorkflowForms_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowFormId", DbType.Guid, jobWorkflowFormId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddJobWorkflowFormParameters(DbCommand command, CreateJobWorkflowFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowId", DbType.Guid, request.JobWorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, request.FormId));
        command.Parameters.Add(CreateInputParameter(command, "@SeqNumber", DbType.Int32, request.SeqNumber));
        command.Parameters.Add(CreateXmlParameter(command, "@MetadataXml", request.MetadataXml));
    }

    private static void AddJobWorkflowFormParameters(DbCommand command, UpdateJobWorkflowFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@JobWorkflowId", DbType.Guid, request.JobWorkflowId));
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, request.FormId));
        command.Parameters.Add(CreateInputParameter(command, "@SeqNumber", DbType.Int32, request.SeqNumber));
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

    private static Guid? GetNullableGuid(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static int? GetNullableInt32(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}
