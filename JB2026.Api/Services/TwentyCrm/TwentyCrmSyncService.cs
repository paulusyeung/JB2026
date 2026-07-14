using JB2026.Api.Options;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Text.Json;

namespace JB2026.Api.Services.TwentyCrm;

public sealed class TwentyCrmSyncService : ITwentyCrmSyncService
{
    private readonly string _connectionString;
    private readonly string _workspaceId;
    private readonly ILogger<TwentyCrmSyncService> _logger;

    public TwentyCrmSyncService(
        IOptions<TwentyCrmOptions> options,
        ILogger<TwentyCrmSyncService> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _workspaceId = options.Value.WorkspaceId;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Guid? UserId)> SyncMemberAsync(
        string email,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return (false, "Twenty CRM is not configured. Set TwentyCrm:ConnectionString.", null);
        }

        if (string.IsNullOrWhiteSpace(_workspaceId))
        {
            return (false, "Twenty CRM workspace ID is not configured. Set TwentyCrm:WorkspaceId.", null);
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var plainPassword = Guid.NewGuid().ToString("N")[..12];
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
            var normalizedEmail = email.ToLowerInvariant();

            Guid userId;
            await using (var lookupCmd = new NpgsqlCommand(
                """SELECT id FROM core."user" WHERE email = @email LIMIT 1;""", conn, tx))
            {
                lookupCmd.Parameters.AddWithValue("email", normalizedEmail);
                var existing = await lookupCmd.ExecuteScalarAsync();
                if (existing is Guid existingId)
                {
                    userId = existingId;
                    await using (var updateCmd = new NpgsqlCommand("""
                        UPDATE core."user" SET
                            "passwordHash" = @passwordHash,
                            "firstName" = @firstName,
                            "lastName" = @lastName,
                            "isEmailVerified" = true,
                            "updatedAt" = NOW()
                        WHERE id = @id;
                        """, conn, tx))
                    {
                        updateCmd.Parameters.AddWithValue("id", userId);
                        updateCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                        updateCmd.Parameters.AddWithValue("firstName", firstName ?? "");
                        updateCmd.Parameters.AddWithValue("lastName", lastName ?? "");
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    userId = Guid.NewGuid();
                    await using (var insertCmd = new NpgsqlCommand("""
                        INSERT INTO core."user" (
                            id, email, "passwordHash", "firstName", "lastName",
                            "isEmailVerified", disabled, "canImpersonate",
                            "canAccessFullAdminPanel", "updatedAt", locale
                        )
                        VALUES (
                            @id, @email, @passwordHash, @firstName, @lastName,
                            true, false, false, false,
                            NOW(), 'en'
                        );
                        """, conn, tx))
                    {
                        insertCmd.Parameters.AddWithValue("id", userId);
                        insertCmd.Parameters.AddWithValue("email", normalizedEmail);
                        insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                        insertCmd.Parameters.AddWithValue("firstName", firstName ?? "");
                        insertCmd.Parameters.AddWithValue("lastName", lastName ?? "");
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            string? workspaceSchema = null;
            if (!string.IsNullOrWhiteSpace(_workspaceId))
            {
                try
                {
                    var workspaceId = Guid.Parse(_workspaceId);

                    await using (var schemaCmd = new NpgsqlCommand(
                        """SELECT "databaseSchema" FROM core.workspace WHERE id = @workspaceId;""", conn, tx))
                    {
                        schemaCmd.Parameters.AddWithValue("workspaceId", workspaceId);
                        var schemaResult = await schemaCmd.ExecuteScalarAsync();
                        workspaceSchema = schemaResult as string ?? "public";
                    }

                    _logger.LogInformation("Resolved workspace schema '{Schema}' for workspace {WorkspaceId}", workspaceSchema, workspaceId);

                    var wsTable = $"\"{workspaceSchema}\".\"workspaceMember\"";

                    await using (var lookupWsCmd = new NpgsqlCommand(
                        $"""SELECT id FROM {wsTable} WHERE "userId" = @userId LIMIT 1;""", conn, tx))
                    {
                        lookupWsCmd.Parameters.AddWithValue("userId", userId);
                        var existingWs = await lookupWsCmd.ExecuteScalarAsync();
                        if (existingWs is Guid existingWsId)
                        {
                            await using (var updateWsCmd = new NpgsqlCommand($"""
                                UPDATE {wsTable} SET
                                    "nameFirstName" = @nameFirstName,
                                    "nameLastName" = @nameLastName,
                                    "userEmail" = @userEmail,
                                    locale = 'en',
                                    "colorScheme" = 'Light',
                                    "updatedAt" = NOW()
                                WHERE id = @id;
                                """, conn, tx))
                            {
                                updateWsCmd.Parameters.AddWithValue("id", existingWsId);
                                updateWsCmd.Parameters.AddWithValue("nameFirstName", firstName ?? "");
                                updateWsCmd.Parameters.AddWithValue("nameLastName", lastName ?? "");
                                updateWsCmd.Parameters.AddWithValue("userEmail", normalizedEmail);
                                await updateWsCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            await using (var insertWsCmd = new NpgsqlCommand($"""
                                INSERT INTO {wsTable} (
                                    id, "userId", "nameFirstName", "nameLastName",
                                    "userEmail", locale, "colorScheme", "createdAt", "updatedAt"
                                )
                                VALUES (
                                    @id, @userId, @nameFirstName, @nameLastName,
                                    @userEmail, 'en', 'Light', NOW(), NOW()
                                );
                                """, conn, tx))
                            {
                                insertWsCmd.Parameters.AddWithValue("id", Guid.NewGuid());
                                insertWsCmd.Parameters.AddWithValue("userId", userId);
                                insertWsCmd.Parameters.AddWithValue("nameFirstName", firstName ?? "");
                                insertWsCmd.Parameters.AddWithValue("nameLastName", lastName ?? "");
                                insertWsCmd.Parameters.AddWithValue("userEmail", normalizedEmail);
                                await insertWsCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                catch (PostgresException ex) when (ex.SqlState is "42P01" or "42703")
                {
                    _logger.LogWarning(ex, "workspaceMember table or column not found in schema '{Schema}'; skipping workspace membership for user {UserId}. Check the table's column names via: SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'workspaceMember'", workspaceSchema ?? "?", userId, workspaceSchema ?? "?");
                }
            }

            await tx.CommitAsync();

            _logger.LogInformation("Synced user {Email} to Twenty CRM (userId={UserId})", email, userId);
            return (true, $"User '{email}' synced to Twenty CRM successfully.", userId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to sync user {Email} to Twenty CRM", email);
            return (false, $"Failed to sync to Twenty CRM: {ex.Message}", null);
        }
    }
}
