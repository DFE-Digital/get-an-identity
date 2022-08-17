﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TeacherIdentity.AuthServer.Models;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    [DbContext(typeof(TeacherIdentityServerDbContext))]
    partial class TeacherIdentityServerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Application", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ClientId")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("client_id");

                    b.Property<string>("ClientSecret")
                        .HasColumnType("text")
                        .HasColumnName("client_secret");

                    b.Property<string>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("concurrency_token");

                    b.Property<string>("ConsentType")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("consent_type");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text")
                        .HasColumnName("display_name");

                    b.Property<string>("DisplayNames")
                        .HasColumnType("text")
                        .HasColumnName("display_names");

                    b.Property<string>("Permissions")
                        .HasColumnType("text")
                        .HasColumnName("permissions");

                    b.Property<string>("PostLogoutRedirectUris")
                        .HasColumnType("text")
                        .HasColumnName("post_logout_redirect_uris");

                    b.Property<string>("Properties")
                        .HasColumnType("text")
                        .HasColumnName("properties");

                    b.Property<string>("RedirectUris")
                        .HasColumnType("text")
                        .HasColumnName("redirect_uris");

                    b.Property<string>("Requirements")
                        .HasColumnType("text")
                        .HasColumnName("requirements");

                    b.Property<string>("ServiceUrl")
                        .HasColumnType("text")
                        .HasColumnName("service_url");

                    b.Property<string>("Type")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_applications");

                    b.HasIndex("ClientId")
                        .IsUnique()
                        .HasDatabaseName("ix_applications_client_id");

                    b.ToTable("applications", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Authorization", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ApplicationId")
                        .HasColumnType("text")
                        .HasColumnName("application_id");

                    b.Property<string>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("concurrency_token");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("creation_date");

                    b.Property<string>("Properties")
                        .HasColumnType("text")
                        .HasColumnName("properties");

                    b.Property<string>("Scopes")
                        .HasColumnType("text")
                        .HasColumnName("scopes");

                    b.Property<string>("Status")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("status");

                    b.Property<string>("Subject")
                        .HasMaxLength(400)
                        .HasColumnType("character varying(400)")
                        .HasColumnName("subject");

                    b.Property<string>("Type")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_authorizations");

                    b.HasIndex("ApplicationId", "Status", "Subject", "Type")
                        .HasDatabaseName("ix_authorizations_application_id_status_subject_type");

                    b.ToTable("authorizations", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.EmailConfirmationPin", b =>
                {
                    b.Property<long>("EmailConfirmationPinId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("email_confirmation_pin_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("EmailConfirmationPinId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("email");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean")
                        .HasColumnName("is_active");

                    b.Property<string>("Pin")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("character(6)")
                        .HasColumnName("pin")
                        .IsFixedLength();

                    b.HasKey("EmailConfirmationPinId")
                        .HasName("pk_email_confirmation_pins");

                    b.HasIndex("Email", "Pin")
                        .IsUnique()
                        .HasDatabaseName("ix_email_confirmation_pins_email_pin");

                    NpgsqlIndexBuilderExtensions.IncludeProperties(b.HasIndex("Email", "Pin"), new[] { "Expires", "IsActive" });

                    b.ToTable("email_confirmation_pins", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Scope", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("concurrency_token");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Descriptions")
                        .HasColumnType("text")
                        .HasColumnName("descriptions");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text")
                        .HasColumnName("display_name");

                    b.Property<string>("DisplayNames")
                        .HasColumnType("text")
                        .HasColumnName("display_names");

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("name");

                    b.Property<string>("Properties")
                        .HasColumnType("text")
                        .HasColumnName("properties");

                    b.Property<string>("Resources")
                        .HasColumnType("text")
                        .HasColumnName("resources");

                    b.HasKey("Id")
                        .HasName("pk_scopes");

                    b.HasIndex("Name")
                        .IsUnique()
                        .HasDatabaseName("ix_scopes_name");

                    b.ToTable("scopes", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Token", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ApplicationId")
                        .HasColumnType("text")
                        .HasColumnName("application_id");

                    b.Property<string>("AuthorizationId")
                        .HasColumnType("text")
                        .HasColumnName("authorization_id");

                    b.Property<string>("ConcurrencyToken")
                        .IsConcurrencyToken()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("concurrency_token");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("creation_date");

                    b.Property<DateTime?>("ExpirationDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expiration_date");

                    b.Property<string>("Payload")
                        .HasColumnType("text")
                        .HasColumnName("payload");

                    b.Property<string>("Properties")
                        .HasColumnType("text")
                        .HasColumnName("properties");

                    b.Property<DateTime?>("RedemptionDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("redemption_date");

                    b.Property<string>("ReferenceId")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("reference_id");

                    b.Property<string>("Status")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("status");

                    b.Property<string>("Subject")
                        .HasMaxLength(400)
                        .HasColumnType("character varying(400)")
                        .HasColumnName("subject");

                    b.Property<string>("Type")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_tokens");

                    b.HasIndex("AuthorizationId")
                        .HasDatabaseName("ix_tokens_authorization_id");

                    b.HasIndex("ReferenceId")
                        .IsUnique()
                        .HasDatabaseName("ix_tokens_reference_id");

                    b.HasIndex("ApplicationId", "Status", "Subject", "Type")
                        .HasDatabaseName("ix_tokens_application_id_status_subject_type");

                    b.ToTable("tokens", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<DateOnly>("DateOfBirth")
                        .HasColumnType("date")
                        .HasColumnName("date_of_birth");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("email_address");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("first_name");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("last_name");

                    b.Property<bool>("is_deleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.HasKey("UserId")
                        .HasName("pk_users");

                    b.HasIndex("EmailAddress")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email_address")
                        .HasFilter("is_deleted = true");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Authorization", b =>
                {
                    b.HasOne("TeacherIdentity.AuthServer.Models.Application", "Application")
                        .WithMany("Authorizations")
                        .HasForeignKey("ApplicationId")
                        .HasConstraintName("fk_authorizations_applications_application_id");

                    b.Navigation("Application");
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Token", b =>
                {
                    b.HasOne("TeacherIdentity.AuthServer.Models.Application", "Application")
                        .WithMany("Tokens")
                        .HasForeignKey("ApplicationId")
                        .HasConstraintName("fk_tokens_applications_application_id");

                    b.HasOne("TeacherIdentity.AuthServer.Models.Authorization", "Authorization")
                        .WithMany("Tokens")
                        .HasForeignKey("AuthorizationId")
                        .HasConstraintName("fk_tokens_authorizations_authorization_id");

                    b.Navigation("Application");

                    b.Navigation("Authorization");
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Application", b =>
                {
                    b.Navigation("Authorizations");

                    b.Navigation("Tokens");
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Authorization", b =>
                {
                    b.Navigation("Tokens");
                });
#pragma warning restore 612, 618
        }
    }
}
