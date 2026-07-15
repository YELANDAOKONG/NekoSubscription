using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NekoSubscription.Core.Subscriptions.Migrations
{
    /// <inheritdoc />
    public partial class InitialSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountIdentifier = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProfiles", x => x.Id);
                    table.CheckConstraint("CK_PaymentProfiles_ArchiveState", "(IsArchived = 0 AND ArchivedAtUtc IS NULL) OR (IsArchived = 1 AND ArchivedAtUtc IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    BillingAmount = table.Column<decimal>(type: "TEXT", precision: 28, scale: 18, nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CurrencyKind = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingCadence = table.Column<int>(type: "INTEGER", nullable: false),
                    BillingIntervalUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    BillingIntervalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextBillingOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AutomaticallyRenews = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfirmationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LifecycleStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderGracePeriodDays = table.Column<int>(type: "INTEGER", nullable: true),
                    BudgetToleranceDays = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IncludedWithSubscriptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ManagementUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.CheckConstraint("CK_Subscriptions_ArchiveState", "(IsArchived = 0 AND ArchivedAtUtc IS NULL) OR (IsArchived = 1 AND ArchivedAtUtc IS NOT NULL)");
                    table.CheckConstraint("CK_Subscriptions_DeleteState", "(IsDeleted = 0 AND DeletedAtUtc IS NULL) OR (IsDeleted = 1 AND DeletedAtUtc IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Subscriptions_PaymentProfiles_PaymentProfileId",
                        column: x => x.PaymentProfileId,
                        principalTable: "PaymentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Subscriptions_IncludedWithSubscriptionId",
                        column: x => x.IncludedWithSubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CloudServiceSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BillingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantIdentifier = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ProjectIdentifier = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudServiceSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloudServiceSubscriptions_Subscriptions_Id",
                        column: x => x.Id,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomSubscriptions_Subscriptions_Id",
                        column: x => x.Id,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DomainSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DomainName = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false, collation: "NOCASE"),
                    RegisteredOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainSubscriptions_Subscriptions_Id",
                        column: x => x.Id,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdinarySubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdinarySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdinarySubscriptions_Subscriptions_Id",
                        column: x => x.Id,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumberSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PhoneNumberType = table.Column<int>(type: "INTEGER", nullable: false),
                    CarrierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RegionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsPrepaid = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumberSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNumberSubscriptions_Subscriptions_Id",
                        column: x => x.Id,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTags",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTags", x => new { x.SubscriptionId, x.TagId });
                    table.ForeignKey(
                        name: "FK_SubscriptionTags_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomSubscriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FieldType = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TextValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NumberValue = table.Column<decimal>(type: "TEXT", precision: 28, scale: 8, nullable: true),
                    BooleanValue = table.Column<bool>(type: "INTEGER", nullable: true),
                    DateValue = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UrlValue = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFields", x => x.Id);
                    table.CheckConstraint("CK_CustomFields_TypedValue", "(FieldType = 0 AND TextValue IS NOT NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NULL) OR (FieldType = 1 AND TextValue IS NULL AND NumberValue IS NOT NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NULL) OR (FieldType = 2 AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NOT NULL AND DateValue IS NULL AND UrlValue IS NULL) OR (FieldType = 3 AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NOT NULL AND UrlValue IS NULL) OR (FieldType = 4 AND TextValue IS NULL AND NumberValue IS NULL AND BooleanValue IS NULL AND DateValue IS NULL AND UrlValue IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CustomFields_CustomSubscriptions_CustomSubscriptionId",
                        column: x => x.CustomSubscriptionId,
                        principalTable: "CustomSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudServiceSubscriptions_ProjectIdentifier",
                table: "CloudServiceSubscriptions",
                column: "ProjectIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_CloudServiceSubscriptions_TenantIdentifier",
                table: "CloudServiceSubscriptions",
                column: "TenantIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_CustomSubscriptionId_SortOrder",
                table: "CustomFields",
                columns: new[] { "CustomSubscriptionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_FieldType",
                table: "CustomFields",
                column: "FieldType");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSubscriptions_DomainName",
                table: "DomainSubscriptions",
                column: "DomainName");

            migrationBuilder.CreateIndex(
                name: "IX_DomainSubscriptions_ExpiresOn",
                table: "DomainSubscriptions",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProfiles_Channel",
                table: "PaymentProfiles",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProfiles_IsArchived",
                table: "PaymentProfiles",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumberSubscriptions_PhoneNumber",
                table: "PhoneNumberSubscriptions",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ConfirmationStatus",
                table: "Subscriptions",
                column: "ConfirmationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IncludedWithSubscriptionId",
                table: "Subscriptions",
                column: "IncludedWithSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsArchived",
                table: "Subscriptions",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextBillingOn",
                table: "Subscriptions",
                column: "NextBillingOn");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentProfileId",
                table: "Subscriptions",
                column: "PaymentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTags_TagId",
                table: "SubscriptionTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudServiceSubscriptions");

            migrationBuilder.DropTable(
                name: "CustomFields");

            migrationBuilder.DropTable(
                name: "DomainSubscriptions");

            migrationBuilder.DropTable(
                name: "OrdinarySubscriptions");

            migrationBuilder.DropTable(
                name: "PhoneNumberSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionTags");

            migrationBuilder.DropTable(
                name: "CustomSubscriptions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "PaymentProfiles");
        }
    }
}
