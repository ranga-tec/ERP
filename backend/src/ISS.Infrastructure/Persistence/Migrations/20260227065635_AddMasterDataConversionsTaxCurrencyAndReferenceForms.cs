using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataConversionsTaxCurrencyAndReferenceForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Payments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Payments",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTypeId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    MinorUnits = table.Column<int>(type: "integer", nullable: false),
                    IsBase = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RouteTemplate = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsInclusive = table.Column<bool>(type: "boolean", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitConversions_UnitOfMeasures_FromUnitOfMeasureId",
                        column: x => x.FromUnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversions_UnitOfMeasures_ToUnitOfMeasureId",
                        column: x => x.ToUnitOfMeasureId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToCurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    RateType = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyRates_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaxConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTaxCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTaxCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxConversions_TaxCodes_SourceTaxCodeId",
                        column: x => x.SourceTaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaxConversions_TaxCodes_TargetTaxCodeId",
                        column: x => x.TargetTaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            var seededAt = new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero);
            var usdCurrencyId = new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca001");
            var eurCurrencyId = new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca002");
            var gbpCurrencyId = new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca003");

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[]
                {
                    "Id", "Code", "Name", "Symbol", "MinorUnits", "IsBase", "IsActive", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
                },
                values: new object[,]
                {
                    { usdCurrencyId, "USD", "US Dollar", "$", 2, true, true, seededAt, null, null, null },
                    { eurCurrencyId, "EUR", "Euro", "EUR", 2, false, true, seededAt, null, null, null },
                    { gbpCurrencyId, "GBP", "Pound Sterling", "GBP", 2, false, true, seededAt, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "CurrencyRates",
                columns: new[]
                {
                    "Id", "FromCurrencyId", "ToCurrencyId", "Rate", "RateType", "EffectiveFrom", "Source", "IsActive", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
                },
                values: new object[,]
                {
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca101"), eurCurrencyId, usdCurrencyId, 1.08000000m, 1, seededAt, "Seed default", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca102"), gbpCurrencyId, usdCurrencyId, 1.27000000m, 1, seededAt, "Seed default", true, seededAt, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "PaymentTypes",
                columns: new[]
                {
                    "Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
                },
                values: new object[,]
                {
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca201"), "CASH", "Cash", "Physical cash receipt or disbursement.", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca202"), "BANK_TRANSFER", "Bank Transfer", "Electronic bank-to-bank transfer.", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca203"), "CHEQUE", "Cheque", "Cheque or demand draft payment.", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca204"), "CARD", "Card", "Card swipe or online card payment.", true, seededAt, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "TaxCodes",
                columns: new[]
                {
                    "Id", "Code", "Name", "RatePercent", "IsInclusive", "Scope", "Description", "IsActive", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
                },
                values: new object[,]
                {
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca301"), "VAT0", "Zero Rated", 0m, false, 3, "Zero-rated tax.", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca302"), "VAT5", "VAT 5%", 5m, false, 3, "General reduced VAT rate.", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca303"), "VAT15", "VAT 15%", 15m, false, 3, "General standard VAT rate.", true, seededAt, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "ReferenceForms",
                columns: new[]
                {
                    "Id", "Code", "Name", "Module", "RouteTemplate", "IsActive", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
                },
                values: new object[,]
                {
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca401"), "PR", "Purchase Requisition", "Procurement", "/procurement/purchase-requisitions/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca402"), "RFQ", "Request for Quote", "Procurement", "/procurement/rfqs/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca403"), "PO", "Purchase Order", "Procurement", "/procurement/purchase-orders/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca404"), "GRN", "Goods Receipt", "Procurement", "/procurement/goods-receipts/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca405"), "DPR", "Direct Purchase", "Procurement", "/procurement/direct-purchases/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca406"), "SINV", "Supplier Invoice", "Procurement", "/procurement/supplier-invoices/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca407"), "SR", "Supplier Return", "Procurement", "/procurement/supplier-returns/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca408"), "SQ", "Sales Quote", "Sales", "/sales/quotes/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca409"), "SO", "Sales Order", "Sales", "/sales/orders/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca410"), "DN", "Dispatch Note", "Sales", "/sales/dispatches/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca411"), "DDN", "Direct Dispatch", "Sales", "/sales/direct-dispatches/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca412"), "INV", "Sales Invoice", "Sales", "/sales/invoices/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca413"), "CRTN", "Customer Return", "Sales", "/sales/customer-returns/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca414"), "SJ", "Service Job", "Service", "/service/jobs/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca415"), "SE", "Service Estimate", "Service", "/service/estimates/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca416"), "WO", "Work Order", "Service", "/service/work-orders/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca417"), "MR", "Material Requisition", "Service", "/service/material-requisitions/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca418"), "QC", "Quality Check", "Service", "/service/quality-checks/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca419"), "SH", "Service Handover", "Service", "/service/handovers/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca420"), "EUNIT", "Equipment Unit", "Service", "/service/equipment-units/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca421"), "ADJ", "Stock Adjustment", "Inventory", "/inventory/stock-adjustments/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca422"), "TRF", "Stock Transfer", "Inventory", "/inventory/stock-transfers/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca423"), "PAY", "Payment", "Finance", "/finance/payments/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca424"), "CN", "Credit Note", "Finance", "/finance/credit-notes/{id}", true, seededAt, null, null, null },
                    { new Guid("0f67e0f6-cb31-47a2-8ec0-7fc6f34ca425"), "DBN", "Debit Note", "Finance", "/finance/debit-notes/{id}", true, seededAt, null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentTypeId",
                table: "Payments",
                column: "PaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBase",
                table: "Currencies",
                column: "IsBase",
                filter: "\"IsBase\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_FromCurrencyId_ToCurrencyId_RateType_Effectiv~",
                table: "CurrencyRates",
                columns: new[] { "FromCurrencyId", "ToCurrencyId", "RateType", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_ToCurrencyId",
                table: "CurrencyRates",
                column: "ToCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTypes_Code",
                table: "PaymentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceForms_Code",
                table: "ReferenceForms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_Code",
                table: "TaxCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxConversions_SourceTaxCodeId_TargetTaxCodeId",
                table: "TaxConversions",
                columns: new[] { "SourceTaxCodeId", "TargetTaxCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxConversions_TargetTaxCodeId",
                table: "TaxConversions",
                column: "TargetTaxCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_FromUnitOfMeasureId_ToUnitOfMeasureId",
                table: "UnitConversions",
                columns: new[] { "FromUnitOfMeasureId", "ToUnitOfMeasureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_ToUnitOfMeasureId",
                table: "UnitConversions",
                column: "ToUnitOfMeasureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentTypes_PaymentTypeId",
                table: "Payments",
                column: "PaymentTypeId",
                principalTable: "PaymentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentTypes_PaymentTypeId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.DropTable(
                name: "PaymentTypes");

            migrationBuilder.DropTable(
                name: "ReferenceForms");

            migrationBuilder.DropTable(
                name: "TaxConversions");

            migrationBuilder.DropTable(
                name: "UnitConversions");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "TaxCodes");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentTypeId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentTypeId",
                table: "Payments");
        }
    }
}
