using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingAssistantSettingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some production databases were baselined with AddAssistantSettingsModule present in
            // __EFMigrationsHistory even though its tables were never created. Repair the physical
            // schema idempotently so both correctly migrated and baselined databases are safe.
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "AssistantAccessPolicies" (
                    "Id" uuid NOT NULL,
                    "ScopeKey" character varying(32) NOT NULL,
                    "IsEnabled" boolean NOT NULL,
                    "AllowUserManagedProviders" boolean NOT NULL,
                    "AllowedRolesCsv" character varying(512) NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NULL,
                    "LastModifiedAt" timestamp with time zone NULL,
                    "LastModifiedBy" uuid NULL,
                    CONSTRAINT "PK_AssistantAccessPolicies" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "AssistantProviderProfiles" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "Name" character varying(128) NOT NULL,
                    "ProviderKind" integer NOT NULL,
                    "BaseUrl" character varying(512) NOT NULL,
                    "Model" character varying(256) NOT NULL,
                    "ApiKeyCiphertext" character varying(4000) NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NULL,
                    "LastModifiedAt" timestamp with time zone NULL,
                    "LastModifiedBy" uuid NULL,
                    CONSTRAINT "PK_AssistantProviderProfiles" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS "AssistantUserPreferences" (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "AssistantEnabled" boolean NOT NULL,
                    "ActiveProviderProfileId" uuid NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NULL,
                    "LastModifiedAt" timestamp with time zone NULL,
                    "LastModifiedBy" uuid NULL,
                    CONSTRAINT "PK_AssistantUserPreferences" PRIMARY KEY ("Id")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssistantAccessPolicies_ScopeKey"
                    ON "AssistantAccessPolicies" ("ScopeKey");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssistantProviderProfiles_UserId_Name"
                    ON "AssistantProviderProfiles" ("UserId", "Name");
                CREATE INDEX IF NOT EXISTS "IX_AssistantUserPreferences_ActiveProviderProfileId"
                    ON "AssistantUserPreferences" ("ActiveProviderProfileId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AssistantUserPreferences_UserId"
                    ON "AssistantUserPreferences" ("UserId");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_AssistantUserPreferences_ActiveProviderProfileId'
                    ) THEN
                        ALTER TABLE "AssistantUserPreferences"
                        ADD CONSTRAINT "FK_AssistantUserPreferences_ActiveProviderProfileId"
                        FOREIGN KEY ("ActiveProviderProfileId")
                        REFERENCES "AssistantProviderProfiles" ("Id")
                        ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately non-destructive: these tables may predate this repair migration.
        }
    }
}
