using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class ZFormStoredProcedureGateway : IZFormStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public ZFormStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<ZFormStoredProcedureRecord?> SelectAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Forms_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, formId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ZFormStoredProcedureRecord(
            FormId: reader.GetGuid(reader.GetOrdinal("FormId")),
            FormObjectEnum: reader.GetInt32(reader.GetOrdinal("FormObjectEnum")),
            FormName: GetNullableString(reader, "FormName"),
            FormName_Chs: GetNullableString(reader, "FormName_Chs"),
            FormName_Cht: GetNullableString(reader, "FormName_Cht"),
            MetadataXml: GetNullableString(reader, "MetadataXml"));
    }

    public async Task<Guid> InsertAsync(CreateZFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Forms_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var formIdOut = command.CreateParameter();
        formIdOut.ParameterName = "@FormId";
        formIdOut.DbType = DbType.Guid;
        formIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(formIdOut);

        AddZFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return formIdOut.Value is Guid id
            ? id
            : Guid.Parse(formIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output FormId."));
    }

    public async Task<bool> UpdateAsync(UpdateZFormStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Forms_UpdRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, request.FormId));

        AddZFormParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spZ_Forms_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@FormId", DbType.Guid, formId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddZFormParameters(DbCommand command, CreateZFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@FormObjectEnum", DbType.Int32, request.FormObjectEnum));
        command.Parameters.Add(CreateInputParameter(command, "@FormName", DbType.String, request.FormName, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@FormName_Chs", DbType.String, request.FormName_Chs, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@FormName_Cht", DbType.String, request.FormName_Cht, size: 10));
        command.Parameters.Add(CreateXmlParameter(command, "@MetadataXml", request.MetadataXml));
    }

    private static void AddZFormParameters(DbCommand command, UpdateZFormStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@FormObjectEnum", DbType.Int32, request.FormObjectEnum));
        command.Parameters.Add(CreateInputParameter(command, "@FormName", DbType.String, request.FormName, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@FormName_Chs", DbType.String, request.FormName_Chs, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@FormName_Cht", DbType.String, request.FormName_Cht, size: 10));
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
