using System.Data;
using System.Data.Common;
using JB2026.EfCore.Data;
using Microsoft.EntityFrameworkCore;

namespace JB2026.Api.Services;

public sealed class JobOrderStoredProcedureGateway : IJobOrderStoredProcedureGateway
{
    private readonly JB5LegacyReadContext _readContext;
    private readonly JB5LegacyWriteContext _writeContext;

    public JobOrderStoredProcedureGateway(JB5LegacyReadContext readContext, JB5LegacyWriteContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<JobOrderStoredProcedureRecord?> SelectAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var connection = _readContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobOrder_SelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, orderId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new JobOrderStoredProcedureRecord(
            OrderId: reader.GetGuid(reader.GetOrdinal("OrderId")),
            OrderType: reader.GetInt32(reader.GetOrdinal("OrderType")),
            OrderNumber: GetNullableString(reader, "OrderNumber"),
            JobNumber: GetNullableInt(reader, "JobNumber"),
            CustomerName: GetNullableString(reader, "CustomerName"),
            CustomerRef: GetNullableString(reader, "CustomerRef"),
            OrderTitle: GetNullableString(reader, "OrderTitle"),
            ProductCode: GetNullableString(reader, "ProductCode"),
            ProductStyle: GetNullableString(reader, "ProductStyle"),
            ProductDetails: GetNullableString(reader, "ProductDetails"),
            OrderedOn: GetNullableDateTime(reader, "OrderedOn"),
            OrderedBy: GetNullableString(reader, "OrderedBy"),
            OutputRef: GetNullableString(reader, "OutputRef"),
            InvoiceRef: GetNullableString(reader, "InvoiceRef"),
            InvoiceAmount: GetNullableDecimal(reader, "InvoiceAmount"),
            Qty: GetNullableDecimal(reader, "Qty"),
            QtyText: GetNullableString(reader, "QtyText"),
            RequiredOn: GetNullableDateTime(reader, "RequiredOn"),
            CompletedOn: GetNullableDateTime(reader, "CompletedOn"),
            SONumber: GetNullableString(reader, "SONumber"),
            PONumber: GetNullableString(reader, "PONumber"),
            OriginalSONumber: GetNullableString(reader, "OriginalSONumber"),
            OriginalPONumber: GetNullableString(reader, "OriginalPONumber"),
            PaymentTerms: GetNullableString(reader, "PaymentTerms"),
            Remarks: GetNullableString(reader, "Remarks"),
            Status: reader.GetInt32(reader.GetOrdinal("Status")),
            CreatedOn: reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            CreatedBy: reader.GetGuid(reader.GetOrdinal("CreatedBy")),
            ModifiedOn: reader.GetDateTime(reader.GetOrdinal("ModifiedOn")),
            ModifiedBy: reader.GetGuid(reader.GetOrdinal("ModifiedBy")),
            Retired: reader.GetBoolean(reader.GetOrdinal("Retired")),
            RetiredOn: GetNullableDateTime(reader, "RetiredOn"),
            RetiredBy: GetNullableGuid(reader, "RetiredBy"));
    }

    public async Task<Guid> InsertAsync(CreateJobOrderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobOrder_InsRec";
        command.CommandType = CommandType.StoredProcedure;

        var orderIdOut = command.CreateParameter();
        orderIdOut.ParameterName = "@OrderId";
        orderIdOut.DbType = DbType.Guid;
        orderIdOut.Direction = ParameterDirection.Output;
        command.Parameters.Add(orderIdOut);

        AddJobOrderParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return orderIdOut.Value is Guid orderId
            ? orderId
            : Guid.Parse(orderIdOut.Value?.ToString() ?? throw new InvalidOperationException("Missing output OrderId."));
    }

    public async Task<bool> UpdateAsync(UpdateJobOrderStoredProcedureRequest request, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobOrder_UpdRec";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, request.OrderId));
        AddJobOrderParameters(command, request);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var connection = _writeContext.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "spJobOrder_DelRec";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(CreateInputParameter(command, "@OrderId", DbType.Guid, orderId));

        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private static void AddJobOrderParameters(DbCommand command, CreateJobOrderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderType", DbType.Int32, request.OrderType));
        command.Parameters.Add(CreateInputParameter(command, "@OrderNumber", DbType.String, request.OrderNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@JobNumber", DbType.Int32, request.JobNumber));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerName", DbType.String, request.CustomerName, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerRef", DbType.String, request.CustomerRef, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OrderTitle", DbType.String, request.OrderTitle, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductStyle", DbType.String, request.ProductStyle, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductDetails", DbType.String, request.ProductDetails));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedOn", DbType.DateTime, request.OrderedOn));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedBy", DbType.String, request.OrderedBy, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@OutputRef", DbType.String, request.OutputRef, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceRef", DbType.String, request.InvoiceRef, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceAmount", DbType.Decimal, request.InvoiceAmount));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.Decimal, request.Qty));
        command.Parameters.Add(CreateInputParameter(command, "@QtyText", DbType.String, request.QtyText, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@RequiredOn", DbType.DateTime, request.RequiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@SONumber", DbType.String, request.SONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@PONumber", DbType.String, request.PONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalSONumber", DbType.String, request.OriginalSONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalPONumber", DbType.String, request.OriginalPONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@PaymentTerms", DbType.String, request.PaymentTerms, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedOn", DbType.DateTime, request.CreatedOn));
        command.Parameters.Add(CreateInputParameter(command, "@CreatedBy", DbType.Guid, request.CreatedBy));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedOn", DbType.DateTime, request.ModifiedOn));
        command.Parameters.Add(CreateInputParameter(command, "@ModifiedBy", DbType.Guid, request.ModifiedBy));
        command.Parameters.Add(CreateInputParameter(command, "@Retired", DbType.Boolean, request.Retired));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredOn", DbType.DateTime, request.RetiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@RetiredBy", DbType.Guid, request.RetiredBy));
    }

    private static void AddJobOrderParameters(DbCommand command, UpdateJobOrderStoredProcedureRequest request)
    {
        command.Parameters.Add(CreateInputParameter(command, "@OrderType", DbType.Int32, request.OrderType));
        command.Parameters.Add(CreateInputParameter(command, "@OrderNumber", DbType.String, request.OrderNumber, size: 10));
        command.Parameters.Add(CreateInputParameter(command, "@JobNumber", DbType.Int32, request.JobNumber));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerName", DbType.String, request.CustomerName, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@CustomerRef", DbType.String, request.CustomerRef, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OrderTitle", DbType.String, request.OrderTitle, size: 128));
        command.Parameters.Add(CreateInputParameter(command, "@ProductCode", DbType.String, request.ProductCode, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductStyle", DbType.String, request.ProductStyle, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@ProductDetails", DbType.String, request.ProductDetails));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedOn", DbType.DateTime, request.OrderedOn));
        command.Parameters.Add(CreateInputParameter(command, "@OrderedBy", DbType.String, request.OrderedBy, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@OutputRef", DbType.String, request.OutputRef, size: 64));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceRef", DbType.String, request.InvoiceRef, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@InvoiceAmount", DbType.Decimal, request.InvoiceAmount));
        command.Parameters.Add(CreateInputParameter(command, "@Qty", DbType.Decimal, request.Qty));
        command.Parameters.Add(CreateInputParameter(command, "@QtyText", DbType.String, request.QtyText, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@RequiredOn", DbType.DateTime, request.RequiredOn));
        command.Parameters.Add(CreateInputParameter(command, "@CompletedOn", DbType.DateTime, request.CompletedOn));
        command.Parameters.Add(CreateInputParameter(command, "@SONumber", DbType.String, request.SONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@PONumber", DbType.String, request.PONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalSONumber", DbType.String, request.OriginalSONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@OriginalPONumber", DbType.String, request.OriginalPONumber, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@PaymentTerms", DbType.String, request.PaymentTerms, size: 32));
        command.Parameters.Add(CreateInputParameter(command, "@Remarks", DbType.String, request.Remarks, size: 512));
        command.Parameters.Add(CreateInputParameter(command, "@Status", DbType.Int32, request.Status));
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

    private static int? GetNullableInt(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
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
