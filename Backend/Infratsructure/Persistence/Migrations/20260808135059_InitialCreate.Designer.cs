// Сгенерировано EF Core и дополнено русскоязычной документацией.
using System;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Хранит сгенерированную EF Core целевую модель начальной миграции для вычисления последующих изменений схемы.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260808135059_InitialCreate")]
    partial class InitialCreate
    {
        /// <summary>Восстанавливает полную целевую модель схемы, соответствующую миграции InitialCreate.</summary>
        /// <param name="modelBuilder">Построитель метаданных EF Core.</param>
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Domain.Entities.AiUsageQuota", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("LawyerId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("PeriodEnd")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset>("PeriodStart")
                        .HasColumnType("timestamptz");

                    b.Property<int?>("RequestsLimit")
                        .HasColumnType("integer");

                    b.Property<int>("RequestsUsed")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0);

                    b.Property<string>("Tier")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("Free");

                    b.HasKey("Id");

                    b.HasIndex("LawyerId", "PeriodStart", "PeriodEnd")
                        .IsUnique();

                    b.ToTable("AiUsageQuotas", null, t =>
                        {
                            t.HasCheckConstraint("CK_AiUsageQuotas_Period", "\"PeriodEnd\" > \"PeriodStart\"");

                            t.HasCheckConstraint("CK_AiUsageQuotas_RequestsUsed_NonNegative", "\"RequestsUsed\" >= 0");

                            t.HasCheckConstraint("CK_AiUsageQuotas_RequestsWithinLimit", "\"RequestsLimit\" IS NULL OR \"RequestsUsed\" <= \"RequestsLimit\"");

                            t.HasCheckConstraint("CK_AiUsageQuotas_TierLimit", "(\"Tier\" = 'Free' AND \"RequestsLimit\" IS NOT NULL AND \"RequestsLimit\" > 0) OR (\"Tier\" = 'Paid' AND \"RequestsLimit\" IS NULL)");
                        });
                });

            modelBuilder.Entity("Domain.Entities.AiUsageRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AiUsageQuotaId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid?>("DraftId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("LawyerId")
                        .HasColumnType("uuid");

                    b.Property<string>("RequestType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<bool>("Succeeded")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("AiUsageQuotaId");

                    b.HasIndex("DraftId");

                    b.HasIndex("LawyerId");

                    b.HasIndex("LawyerId", "CreatedAt");

                    b.ToTable("AiUsageRecords", null, t =>
                        {
                            t.HasCheckConstraint("CK_AiUsageRecords_DraftReference", "\"RequestType\" IN ('GenerateDraft', 'RegenerateDraft') OR (\"RequestType\" = 'ReviewIncomingDocument' AND \"DraftId\" IS NULL)");
                        });
                });

            modelBuilder.Entity("Domain.Entities.AuditLogEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid?>("ActorId")
                        .HasColumnType("uuid");

                    b.Property<string>("ActorType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid>("EntityId")
                        .HasColumnType("uuid");

                    b.Property<string>("EntityType")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Metadata")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset>("OccurredAt")
                        .HasColumnType("timestamptz");

                    b.HasKey("Id");

                    b.HasIndex("OccurredAt");

                    b.HasIndex("ActorType", "ActorId");

                    b.HasIndex("EntityType", "EntityId");

                    b.ToTable("AuditLogEntries", null, t =>
                        {
                            t.HasCheckConstraint("CK_AuditLogEntries_Actor", "(\"ActorType\" = 'System' AND \"ActorId\" IS NULL) OR (\"ActorType\" <> 'System' AND \"ActorId\" IS NOT NULL)");
                        });
                });

            modelBuilder.Entity("Domain.Entities.Case", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ClosedAt")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<Guid>("LawyerId")
                        .HasColumnType("uuid");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("Open");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.HasIndex("LawyerId");

                    b.HasIndex("LawyerId", "Status");

                    b.ToTable("Cases", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.CaseLegislationAlert", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("CaseId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsRead")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<Guid>("LegislationAlertId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ReadAt")
                        .HasColumnType("timestamptz");

                    b.HasKey("Id");

                    b.HasIndex("LegislationAlertId");

                    b.HasIndex("CaseId", "IsRead");

                    b.HasIndex("CaseId", "LegislationAlertId")
                        .IsUnique();

                    b.ToTable("CaseLegislationAlerts", null, t =>
                        {
                            t.HasCheckConstraint("CK_CaseLegislationAlerts_ReadState", "(\"IsRead\" = FALSE AND \"ReadAt\" IS NULL) OR (\"IsRead\" = TRUE AND \"ReadAt\" IS NOT NULL)");
                        });
                });

            modelBuilder.Entity("Domain.Entities.ClauseBlock", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("ContentRu")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ContentTj")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamptz");

                    b.HasKey("Id");

                    b.HasIndex("Category");

                    b.ToTable("ClauseBlocks", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.Client", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("CompanyName")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<string>("ContactEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ContactPhone")
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset?>("DeletedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("FullName")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<Guid>("LawyerId")
                        .HasColumnType("uuid");

                    b.Property<string>("Notes")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("LawyerId");

                    b.HasIndex("LawyerId", "DeletedAt");

                    b.ToTable("Clients", null, t =>
                        {
                            t.HasCheckConstraint("CK_Clients_ExactlyOneName", "\"DeletedAt\" IS NOT NULL OR ((\"FullName\" IS NOT NULL) <> (\"CompanyName\" IS NOT NULL))");
                        });
                });

            modelBuilder.Entity("Domain.Entities.DataDeletionRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("CompletedAt")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset>("RequestedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid>("RequestedById")
                        .HasColumnType("uuid");

                    b.Property<string>("RequestedByType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("Pending");

                    b.Property<Guid>("TargetEntityId")
                        .HasColumnType("uuid");

                    b.Property<string>("TargetEntityType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("Id");

                    b.HasIndex("Status");

                    b.HasIndex("TargetEntityType", "TargetEntityId");

                    b.ToTable("DataDeletionRequests", null, t =>
                        {
                            t.HasCheckConstraint("CK_DataDeletionRequests_CompletionState", "(\"Status\" = 'Completed' AND \"CompletedAt\" IS NOT NULL) OR (\"Status\" <> 'Completed' AND \"CompletedAt\" IS NULL)");

                            t.HasCheckConstraint("CK_DataDeletionRequests_CompletionTime", "\"CompletedAt\" IS NULL OR \"CompletedAt\" >= \"RequestedAt\"");
                        });
                });

            modelBuilder.Entity("Domain.Entities.DocumentComment", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<string>("AuthorType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid?>("ClauseBlockReference")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid>("DocumentVersionId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ResolvedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ClauseBlockReference");

                    b.HasIndex("DocumentVersionId");

                    b.ToTable("DocumentComments", null, t =>
                        {
                            t.HasCheckConstraint("CK_DocumentComments_ResolutionTime", "\"ResolvedAt\" IS NULL OR \"ResolvedAt\" >= \"CreatedAt\"");
                        });
                });

            modelBuilder.Entity("Domain.Entities.DocumentVersion", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("ChangeSummary")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("ContentStorageKey")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid>("CreatedByLawyerId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("DraftId")
                        .HasColumnType("uuid");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<int>("VersionNumber")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CreatedByLawyerId");

                    b.HasIndex("DraftId", "VersionNumber")
                        .IsUnique();

                    b.ToTable("DocumentVersions", null, t =>
                        {
                            t.HasCheckConstraint("CK_DocumentVersions_VersionNumber_Positive", "\"VersionNumber\" > 0");
                        });
                });

            modelBuilder.Entity("Domain.Entities.Draft", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("ArchivedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid>("CaseId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid?>("CurrentVersionId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset?>("DueRespondByDate")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset?>("ResponsibilityConfirmedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("Draft");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamptz");

                    b.HasKey("Id");

                    b.HasIndex("CaseId");

                    b.HasIndex("CurrentVersionId");

                    b.HasIndex("Status");

                    b.HasIndex("TemplateId");

                    b.HasIndex("CaseId", "Status");

                    b.ToTable("Drafts", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.LawyerProfile", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("LawFirmName")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<string>("SubscriptionTier")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("Free");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("LawyerProfiles", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.LegislationAlert", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("DetectedAt")
                        .HasColumnType("timestamptz");

                    b.Property<DateTimeOffset?>("LawChangedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("SourceUrl")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.HasKey("Id");

                    b.HasIndex("DetectedAt");

                    b.ToTable("LegislationAlerts", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.SignatureRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<string>("ConsentAgreementVersion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<Guid>("DocumentVersionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("DraftId")
                        .HasColumnType("uuid");

                    b.Property<string>("IpAddress")
                        .IsRequired()
                        .HasMaxLength(45)
                        .HasColumnType("character varying(45)");

                    b.Property<string>("Method")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("SignedAt")
                        .HasColumnType("timestamptz");

                    b.Property<Guid>("SignerId")
                        .HasColumnType("uuid");

                    b.Property<string>("SignerType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.HasKey("Id");

                    b.HasIndex("DocumentVersionId");

                    b.HasIndex("DraftId");

                    b.HasIndex("DraftId", "SignerType", "SignerId")
                        .IsUnique();

                    b.ToTable("SignatureRecords", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.Template", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamptz");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("MaintainedByRef")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamptz");

                    b.HasKey("Id");

                    b.HasIndex("IsActive");

                    b.ToTable("Templates", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.TemplateClauseBlock", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClauseBlockId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsDefault")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("TemplateId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ClauseBlockId");

                    b.HasIndex("TemplateId", "ClauseBlockId")
                        .IsUnique();

                    b.HasIndex("TemplateId", "Order")
                        .IsUnique();

                    b.ToTable("TemplateClauseBlocks", null, t =>
                        {
                            t.HasCheckConstraint("CK_TemplateClauseBlocks_Order_NonNegative", "\"Order\" >= 0");
                        });
                });

            modelBuilder.Entity("Infrastructure.Identity.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("PhoneNumber")
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("RefreshTokenAuthenticatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("RefreshTokenExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("RefreshTokenHash")
                        .HasMaxLength(64)
                        .IsUnicode(false)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .IsUnique()
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("Domain.Entities.AiUsageQuota", b =>
                {
                    b.HasOne("Domain.Entities.LawyerProfile", null)
                        .WithMany()
                        .HasForeignKey("LawyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.AiUsageRecord", b =>
                {
                    b.HasOne("Domain.Entities.AiUsageQuota", null)
                        .WithMany()
                        .HasForeignKey("AiUsageQuotaId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Draft", null)
                        .WithMany()
                        .HasForeignKey("DraftId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Domain.Entities.LawyerProfile", null)
                        .WithMany()
                        .HasForeignKey("LawyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Case", b =>
                {
                    b.HasOne("Domain.Entities.Client", null)
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.LawyerProfile", null)
                        .WithMany()
                        .HasForeignKey("LawyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.CaseLegislationAlert", b =>
                {
                    b.HasOne("Domain.Entities.Case", null)
                        .WithMany()
                        .HasForeignKey("CaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domain.Entities.LegislationAlert", null)
                        .WithMany()
                        .HasForeignKey("LegislationAlertId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Client", b =>
                {
                    b.HasOne("Domain.Entities.LawyerProfile", null)
                        .WithMany()
                        .HasForeignKey("LawyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.DocumentComment", b =>
                {
                    b.HasOne("Domain.Entities.ClauseBlock", null)
                        .WithMany()
                        .HasForeignKey("ClauseBlockReference")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Domain.Entities.DocumentVersion", null)
                        .WithMany()
                        .HasForeignKey("DocumentVersionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.DocumentVersion", b =>
                {
                    b.HasOne("Domain.Entities.LawyerProfile", null)
                        .WithMany()
                        .HasForeignKey("CreatedByLawyerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Draft", null)
                        .WithMany()
                        .HasForeignKey("DraftId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Draft", b =>
                {
                    b.HasOne("Domain.Entities.Case", null)
                        .WithMany()
                        .HasForeignKey("CaseId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.DocumentVersion", null)
                        .WithMany()
                        .HasForeignKey("CurrentVersionId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Domain.Entities.Template", null)
                        .WithMany()
                        .HasForeignKey("TemplateId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.LawyerProfile", b =>
                {
                    b.HasOne("Infrastructure.Identity.ApplicationUser", null)
                        .WithOne()
                        .HasForeignKey("Domain.Entities.LawyerProfile", "UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.SignatureRecord", b =>
                {
                    b.HasOne("Domain.Entities.DocumentVersion", null)
                        .WithMany()
                        .HasForeignKey("DocumentVersionId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Draft", null)
                        .WithMany()
                        .HasForeignKey("DraftId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.TemplateClauseBlock", b =>
                {
                    b.HasOne("Domain.Entities.ClauseBlock", null)
                        .WithMany()
                        .HasForeignKey("ClauseBlockId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Template", null)
                        .WithMany()
                        .HasForeignKey("TemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("Infrastructure.Identity.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
