using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Создаёт начальную схему PostgreSQL для ASP.NET Core Identity и всех доменных сущностей ShartnomaYor.
    /// Миграция фиксирует проверенную модель частей 1–2 и позволяет безопасно инициализировать среду без EnsureCreated.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>Создаёт таблицы, ключи, ограничения, индексы и связи начальной версии схемы.</summary>
        /// <param name="migrationBuilder">Построитель операций обновления PostgreSQL.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    RefreshTokenHash = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshTokenAuthenticatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
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
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                    table.CheckConstraint("CK_AuditLogEntries_Actor", "(\"ActorType\" = 'System' AND \"ActorId\" IS NULL) OR (\"ActorType\" <> 'System' AND \"ActorId\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "ClauseBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentTj = table.Column<string>(type: "text", nullable: false),
                    ContentRu = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClauseBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDeletionRequests", x => x.Id);
                    table.CheckConstraint("CK_DataDeletionRequests_CompletionState", "(\"Status\" = 'Completed' AND \"CompletedAt\" IS NOT NULL) OR (\"Status\" <> 'Completed' AND \"CompletedAt\" IS NULL)");
                    table.CheckConstraint("CK_DataDeletionRequests_CompletionTime", "\"CompletedAt\" IS NULL OR \"CompletedAt\" >= \"RequestedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "LegislationAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LawChangedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegislationAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MaintainedByRef = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
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
                name: "LawyerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LawFirmName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SubscriptionTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Free"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LawyerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LawyerProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateClauseBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClauseBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateClauseBlocks", x => x.Id);
                    table.CheckConstraint("CK_TemplateClauseBlocks_Order_NonNegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_TemplateClauseBlocks_ClauseBlocks_ClauseBlockId",
                        column: x => x.ClauseBlockId,
                        principalTable: "ClauseBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateClauseBlocks_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Free"),
                    RequestsUsed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RequestsLimit = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageQuotas", x => x.Id);
                    table.CheckConstraint("CK_AiUsageQuotas_Period", "\"PeriodEnd\" > \"PeriodStart\"");
                    table.CheckConstraint("CK_AiUsageQuotas_RequestsUsed_NonNegative", "\"RequestsUsed\" >= 0");
                    table.CheckConstraint("CK_AiUsageQuotas_RequestsWithinLimit", "\"RequestsLimit\" IS NULL OR \"RequestsUsed\" <= \"RequestsLimit\"");
                    table.CheckConstraint("CK_AiUsageQuotas_TierLimit", "(\"Tier\" = 'Free' AND \"RequestsLimit\" IS NOT NULL AND \"RequestsLimit\" > 0) OR (\"Tier\" = 'Paid' AND \"RequestsLimit\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_AiUsageQuotas_LawyerProfiles_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.CheckConstraint("CK_Clients_ExactlyOneName", "\"DeletedAt\" IS NOT NULL OR ((\"FullName\" IS NOT NULL) <> (\"CompanyName\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Clients_LawyerProfiles_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_LawyerProfiles_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseLegislationAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegislationAlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseLegislationAlerts", x => x.Id);
                    table.CheckConstraint("CK_CaseLegislationAlerts_ReadState", "(\"IsRead\" = FALSE AND \"ReadAt\" IS NULL) OR (\"IsRead\" = TRUE AND \"ReadAt\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CaseLegislationAlerts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseLegislationAlerts_LegislationAlerts_LegislationAlertId",
                        column: x => x.LegislationAlertId,
                        principalTable: "LegislationAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiUsageQuotaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageRecords", x => x.Id);
                    table.CheckConstraint("CK_AiUsageRecords_DraftReference", "\"RequestType\" IN ('GenerateDraft', 'RegenerateDraft') OR (\"RequestType\" = 'ReviewIncomingDocument' AND \"DraftId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_AiUsageRecords_AiUsageQuotas_AiUsageQuotaId",
                        column: x => x.AiUsageQuotaId,
                        principalTable: "AiUsageQuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiUsageRecords_LawyerProfiles_LawyerId",
                        column: x => x.LawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClauseBlockReference = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentComments", x => x.Id);
                    table.CheckConstraint("CK_DocumentComments_ResolutionTime", "\"ResolvedAt\" IS NULL OR \"ResolvedAt\" >= \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_DocumentComments_ClauseBlocks_ClauseBlockReference",
                        column: x => x.ClauseBlockReference,
                        principalTable: "ClauseBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ContentStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedByLawyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.CheckConstraint("CK_DocumentVersions_VersionNumber_Positive", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_DocumentVersions_LawyerProfiles_CreatedByLawyerId",
                        column: x => x.CreatedByLawyerId,
                        principalTable: "LawyerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Drafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsibilityConfirmedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    DueRespondByDate = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drafts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Drafts_DocumentVersions_CurrentVersionId",
                        column: x => x.CurrentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Drafts_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignatureRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsentAgreementVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignatureRecords_DocumentVersions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignatureRecords_Drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "Drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageQuotas_LawyerId_PeriodStart_PeriodEnd",
                table: "AiUsageQuotas",
                columns: new[] { "LawyerId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_AiUsageQuotaId",
                table: "AiUsageRecords",
                column: "AiUsageQuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_DraftId",
                table: "AiUsageRecords",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_LawyerId",
                table: "AiUsageRecords",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageRecords_LawyerId_CreatedAt",
                table: "AiUsageRecords",
                columns: new[] { "LawyerId", "CreatedAt" });

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
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActorType_ActorId",
                table: "AuditLogEntries",
                columns: new[] { "ActorType", "ActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_EntityType_EntityId",
                table: "AuditLogEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_OccurredAt",
                table: "AuditLogEntries",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CaseLegislationAlerts_CaseId_IsRead",
                table: "CaseLegislationAlerts",
                columns: new[] { "CaseId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseLegislationAlerts_CaseId_LegislationAlertId",
                table: "CaseLegislationAlerts",
                columns: new[] { "CaseId", "LegislationAlertId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseLegislationAlerts_LegislationAlertId",
                table: "CaseLegislationAlerts",
                column: "LegislationAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ClientId",
                table: "Cases",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_LawyerId",
                table: "Cases",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_LawyerId_Status",
                table: "Cases",
                columns: new[] { "LawyerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClauseBlocks_Category",
                table: "ClauseBlocks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_LawyerId",
                table: "Clients",
                column: "LawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_LawyerId_DeletedAt",
                table: "Clients",
                columns: new[] { "LawyerId", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataDeletionRequests_Status",
                table: "DataDeletionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataDeletionRequests_TargetEntityType_TargetEntityId",
                table: "DataDeletionRequests",
                columns: new[] { "TargetEntityType", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentComments_ClauseBlockReference",
                table: "DocumentComments",
                column: "ClauseBlockReference");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentComments_DocumentVersionId",
                table: "DocumentComments",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_CreatedByLawyerId",
                table: "DocumentVersions",
                column: "CreatedByLawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DraftId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DraftId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_CaseId",
                table: "Drafts",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_CaseId_Status",
                table: "Drafts",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_CurrentVersionId",
                table: "Drafts",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_Status",
                table: "Drafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Drafts_TemplateId",
                table: "Drafts",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_LawyerProfiles_UserId",
                table: "LawyerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegislationAlerts_DetectedAt",
                table: "LegislationAlerts",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRecords_DocumentVersionId",
                table: "SignatureRecords",
                column: "DocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRecords_DraftId",
                table: "SignatureRecords",
                column: "DraftId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureRecords_DraftId_SignerType_SignerId",
                table: "SignatureRecords",
                columns: new[] { "DraftId", "SignerType", "SignerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateClauseBlocks_ClauseBlockId",
                table: "TemplateClauseBlocks",
                column: "ClauseBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateClauseBlocks_TemplateId_ClauseBlockId",
                table: "TemplateClauseBlocks",
                columns: new[] { "TemplateId", "ClauseBlockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateClauseBlocks_TemplateId_Order",
                table: "TemplateClauseBlocks",
                columns: new[] { "TemplateId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_IsActive",
                table: "Templates",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_AiUsageRecords_Drafts_DraftId",
                table: "AiUsageRecords",
                column: "DraftId",
                principalTable: "Drafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentComments_DocumentVersions_DocumentVersionId",
                table: "DocumentComments",
                column: "DocumentVersionId",
                principalTable: "DocumentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_Drafts_DraftId",
                table: "DocumentVersions",
                column: "DraftId",
                principalTable: "Drafts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <summary>Удаляет начальную схему в обратном порядке зависимостей для явного отката миграции.</summary>
        /// <param name="migrationBuilder">Построитель операций отката PostgreSQL.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_LawyerProfiles_LawyerId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_LawyerProfiles_LawyerId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_LawyerProfiles_CreatedByLawyerId",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Drafts_DraftId",
                table: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "AiUsageRecords");

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
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "CaseLegislationAlerts");

            migrationBuilder.DropTable(
                name: "DataDeletionRequests");

            migrationBuilder.DropTable(
                name: "DocumentComments");

            migrationBuilder.DropTable(
                name: "SignatureRecords");

            migrationBuilder.DropTable(
                name: "TemplateClauseBlocks");

            migrationBuilder.DropTable(
                name: "AiUsageQuotas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "LegislationAlerts");

            migrationBuilder.DropTable(
                name: "ClauseBlocks");

            migrationBuilder.DropTable(
                name: "LawyerProfiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Drafts");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
