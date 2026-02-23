using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountsPayableEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsPayableEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountsReceivableEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsReceivableEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TableName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CounterpartyType = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SourceReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispatchNoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebitNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CounterpartyType = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SourceReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectDispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectPurchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WarrantyUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationOutboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutboxItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyType = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Terms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceEstimates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceHandovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandoverDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ItemsReturned = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PostServiceWarrantyMonths = table.Column<int>(type: "integer", nullable: true),
                    CustomerAcknowledgement = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceHandovers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EquipmentUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProblemDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    DirectPurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FreightAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RoundingAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AccountsPayableEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteAllocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountsReceivableEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountsPayableEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNoteAllocation_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "CreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnLine_CustomerReturns_CustomerReturnId",
                        column: x => x.CustomerReturnId,
                        principalTable: "CustomerReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectDispatchLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectDispatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectDispatchLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectDispatchLine_DirectDispatches_DirectDispatchId",
                        column: x => x.DirectDispatchId,
                        principalTable: "DirectDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectPurchaseLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectPurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectPurchaseLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectPurchaseLine_DirectPurchases_DirectPurchaseId",
                        column: x => x.DirectPurchaseId,
                        principalTable: "DirectPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchLine_DispatchNotes_DispatchNoteId",
                        column: x => x.DispatchNoteId,
                        principalTable: "DispatchNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLine_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemSubcategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemSubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemSubcategories_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequisitionLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequisitionLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequisitionLine_MaterialRequisitions_MaterialRequis~",
                        column: x => x.MaterialRequisitionId,
                        principalTable: "MaterialRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountsReceivableEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountsPayableEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocation_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLine_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequisitionLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequisitionLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRequisitionLine_PurchaseRequisitions_PurchaseRequis~",
                        column: x => x.PurchaseRequisitionId,
                        principalTable: "PurchaseRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestForQuoteLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestForQuoteLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestForQuoteLine_RequestForQuotes_RequestForQuoteId",
                        column: x => x.RequestForQuoteId,
                        principalTable: "RequestForQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLine_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderLine_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuoteLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesQuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuoteLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLine_SalesQuotes_SalesQuoteId",
                        column: x => x.SalesQuoteId,
                        principalTable: "SalesQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceEstimateLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceEstimateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxPercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceEstimateLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceEstimateLine_ServiceEstimates_ServiceEstimateId",
                        column: x => x.ServiceEstimateId,
                        principalTable: "ServiceEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLine_StockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "StockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLine_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLine_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReturnLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerReturnLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnLineSerial_CustomerReturnLine_CustomerReturnL~",
                        column: x => x.CustomerReturnLineId,
                        principalTable: "CustomerReturnLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectDispatchLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectDispatchLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectDispatchLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectDispatchLineSerial_DirectDispatchLine_DirectDispatchL~",
                        column: x => x.DirectDispatchLineId,
                        principalTable: "DirectDispatchLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectPurchaseLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectPurchaseLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectPurchaseLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectPurchaseLineSerial_DirectPurchaseLine_DirectPurchaseL~",
                        column: x => x.DirectPurchaseLineId,
                        principalTable: "DirectPurchaseLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchLineSerial_DispatchLine_DispatchLineId",
                        column: x => x.DispatchLineId,
                        principalTable: "DispatchLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLineSerial_GoodsReceiptLine_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TrackingType = table.Column<int>(type: "integer", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubcategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DefaultUnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_ItemCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Items_ItemSubcategories_SubcategoryId",
                        column: x => x.SubcategoryId,
                        principalTable: "ItemSubcategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequisitionLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequisitionLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequisitionLineSerial_MaterialRequisitionLine_Mater~",
                        column: x => x.MaterialRequisitionLineId,
                        principalTable: "MaterialRequisitionLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustmentLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentLineSerial_StockAdjustmentLine_StockAdjustme~",
                        column: x => x.StockAdjustmentLineId,
                        principalTable: "StockAdjustmentLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLineSerial_StockTransferLine_StockTransferLine~",
                        column: x => x.StockTransferLineId,
                        principalTable: "StockTransferLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLineSerial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierReturnLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLineSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLineSerial_SupplierReturnLine_SupplierReturnL~",
                        column: x => x.SupplierReturnLineId,
                        principalTable: "SupplierReturnLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsImage = table.Column<bool>(type: "boolean", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAttachments_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReorderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReorderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReorderSettings_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReorderSettings_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Code",
                table: "Brands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteAllocation_CreditNoteId",
                table: "CreditNoteAllocation",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_ReferenceNumber",
                table: "CreditNotes",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnLine_CustomerReturnId",
                table: "CustomerReturnLine",
                column: "CustomerReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnLineSerial_CustomerReturnLineId",
                table: "CustomerReturnLineSerial",
                column: "CustomerReturnLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnLineSerial_SerialNumber",
                table: "CustomerReturnLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturns_Number",
                table: "CustomerReturns",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Code",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_ReferenceNumber",
                table: "DebitNotes",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatches_Number",
                table: "DirectDispatches",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatchLine_DirectDispatchId",
                table: "DirectDispatchLine",
                column: "DirectDispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatchLineSerial_DirectDispatchLineId",
                table: "DirectDispatchLineSerial",
                column: "DirectDispatchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectDispatchLineSerial_SerialNumber",
                table: "DirectDispatchLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchaseLine_DirectPurchaseId",
                table: "DirectPurchaseLine",
                column: "DirectPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchaseLineSerial_DirectPurchaseLineId",
                table: "DirectPurchaseLineSerial",
                column: "DirectPurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchaseLineSerial_SerialNumber",
                table: "DirectPurchaseLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectPurchases_Number",
                table: "DirectPurchases",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchLine_DispatchNoteId",
                table: "DispatchLine",
                column: "DispatchNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchLineSerial_DispatchLineId",
                table: "DispatchLineSerial",
                column: "DispatchLineId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchLineSerial_SerialNumber",
                table: "DispatchLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchNotes_Number",
                table: "DispatchNotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSequences_DocumentType",
                table: "DocumentSequences",
                column: "DocumentType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_SerialNumber",
                table: "EquipmentUnits",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLine_GoodsReceiptId",
                table: "GoodsReceiptLine",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineSerial_GoodsReceiptLineId",
                table: "GoodsReceiptLineSerial",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLineSerial_SerialNumber",
                table: "GoodsReceiptLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_Number",
                table: "GoodsReceipts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ItemId_BatchNumber",
                table: "InventoryMovements",
                columns: new[] { "ItemId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ItemId_SerialNumber",
                table: "InventoryMovements",
                columns: new[] { "ItemId", "SerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_WarehouseId_ItemId_OccurredAt",
                table: "InventoryMovements",
                columns: new[] { "WarehouseId", "ItemId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemAttachments_ItemId_CreatedAt",
                table: "ItemAttachments",
                columns: new[] { "ItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_Code",
                table: "ItemCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Barcode",
                table: "Items",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_BrandId",
                table: "Items",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Sku",
                table: "Items",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_SubcategoryId",
                table: "Items",
                column: "SubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSubcategories_CategoryId_Code",
                table: "ItemSubcategories",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequisitionLine_MaterialRequisitionId",
                table: "MaterialRequisitionLine",
                column: "MaterialRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequisitionLineSerial_MaterialRequisitionLineId",
                table: "MaterialRequisitionLineSerial",
                column: "MaterialRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequisitionLineSerial_SerialNumber",
                table: "MaterialRequisitionLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequisitions_Number",
                table: "MaterialRequisitions",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutboxItems_Status_NextAttemptAt",
                table: "NotificationOutboxItems",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocation_PaymentId",
                table: "PaymentAllocation",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLine_PurchaseOrderId",
                table: "PurchaseOrderLine",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Number",
                table: "PurchaseOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitionLine_PurchaseRequisitionId",
                table: "PurchaseRequisitionLine",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequisitions_Number",
                table: "PurchaseRequisitions",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSettings_ItemId",
                table: "ReorderSettings",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReorderSettings_WarehouseId_ItemId",
                table: "ReorderSettings",
                columns: new[] { "WarehouseId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuoteLine_RequestForQuoteId",
                table: "RequestForQuoteLine",
                column: "RequestForQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestForQuotes_Number",
                table: "RequestForQuotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLine_SalesInvoiceId",
                table: "SalesInvoiceLine",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Number",
                table: "SalesInvoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLine_SalesOrderId",
                table: "SalesOrderLine",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Number",
                table: "SalesOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLine_SalesQuoteId",
                table: "SalesQuoteLine",
                column: "SalesQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_Number",
                table: "SalesQuotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEstimateLine_ServiceEstimateId",
                table: "ServiceEstimateLine",
                column: "ServiceEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEstimates_Number",
                table: "ServiceEstimates",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceHandovers_Number",
                table: "ServiceHandovers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceJobs_Number",
                table: "ServiceJobs",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLine_StockAdjustmentId",
                table: "StockAdjustmentLine",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLineSerial_SerialNumber",
                table: "StockAdjustmentLineSerial",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentLineSerial_StockAdjustmentLineId",
                table: "StockAdjustmentLineSerial",
                column: "StockAdjustmentLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_Number",
                table: "StockAdjustments",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLine_StockTransferId",
                table: "StockTransferLine",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineSerial_SerialNumber",
                table: "StockTransferLineSerial",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLineSerial_StockTransferLineId",
                table: "StockTransferLineSerial",
                column: "StockTransferLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Number",
                table: "StockTransfers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_Number",
                table: "SupplierInvoices",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_SupplierId_InvoiceNumber",
                table: "SupplierInvoices",
                columns: new[] { "SupplierId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLine_SupplierReturnId",
                table: "SupplierReturnLine",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLineSerial_SerialNumber",
                table: "SupplierReturnLineSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLineSerial_SupplierReturnLineId",
                table: "SupplierReturnLineSerial",
                column: "SupplierReturnLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_Number",
                table: "SupplierReturns",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_Code",
                table: "UnitOfMeasures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountsPayableEntries");

            migrationBuilder.DropTable(
                name: "AccountsReceivableEntries");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CreditNoteAllocation");

            migrationBuilder.DropTable(
                name: "CustomerReturnLineSerial");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DebitNotes");

            migrationBuilder.DropTable(
                name: "DirectDispatchLineSerial");

            migrationBuilder.DropTable(
                name: "DirectPurchaseLineSerial");

            migrationBuilder.DropTable(
                name: "DispatchLineSerial");

            migrationBuilder.DropTable(
                name: "DocumentSequences");

            migrationBuilder.DropTable(
                name: "EquipmentUnits");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLineSerial");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "ItemAttachments");

            migrationBuilder.DropTable(
                name: "MaterialRequisitionLineSerial");

            migrationBuilder.DropTable(
                name: "NotificationOutboxItems");

            migrationBuilder.DropTable(
                name: "PaymentAllocation");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLine");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitionLine");

            migrationBuilder.DropTable(
                name: "QualityChecks");

            migrationBuilder.DropTable(
                name: "ReorderSettings");

            migrationBuilder.DropTable(
                name: "RequestForQuoteLine");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLine");

            migrationBuilder.DropTable(
                name: "SalesOrderLine");

            migrationBuilder.DropTable(
                name: "SalesQuoteLine");

            migrationBuilder.DropTable(
                name: "ServiceEstimateLine");

            migrationBuilder.DropTable(
                name: "ServiceHandovers");

            migrationBuilder.DropTable(
                name: "ServiceJobs");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLineSerial");

            migrationBuilder.DropTable(
                name: "StockTransferLineSerial");

            migrationBuilder.DropTable(
                name: "SupplierInvoices");

            migrationBuilder.DropTable(
                name: "SupplierReturnLineSerial");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "UnitOfMeasures");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "CustomerReturnLine");

            migrationBuilder.DropTable(
                name: "DirectDispatchLine");

            migrationBuilder.DropTable(
                name: "DirectPurchaseLine");

            migrationBuilder.DropTable(
                name: "DispatchLine");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLine");

            migrationBuilder.DropTable(
                name: "MaterialRequisitionLine");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "PurchaseRequisitions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "RequestForQuotes");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "SalesQuotes");

            migrationBuilder.DropTable(
                name: "ServiceEstimates");

            migrationBuilder.DropTable(
                name: "StockAdjustmentLine");

            migrationBuilder.DropTable(
                name: "StockTransferLine");

            migrationBuilder.DropTable(
                name: "SupplierReturnLine");

            migrationBuilder.DropTable(
                name: "CustomerReturns");

            migrationBuilder.DropTable(
                name: "DirectDispatches");

            migrationBuilder.DropTable(
                name: "DirectPurchases");

            migrationBuilder.DropTable(
                name: "DispatchNotes");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropTable(
                name: "MaterialRequisitions");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "ItemSubcategories");

            migrationBuilder.DropTable(
                name: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropTable(
                name: "SupplierReturns");

            migrationBuilder.DropTable(
                name: "ItemCategories");
        }
    }
}
