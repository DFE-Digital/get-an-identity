﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TeacherIdentity.AuthServer.Models;

#nullable disable

namespace TeacherIdentity.AuthServer.Migrations
{
    [DbContext(typeof(TeacherIdentityServerDbContext))]
    [Migration("20221222113751_StaffUserTrnLookupStatus")]
    partial class StaffUserTrnLookupStatus
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Application", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ClientId")
                        .IsRequired()
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

                    b.HasAlternateKey("ClientId")
                        .HasName("ak_open_iddict_applications_client_id");

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

                    b.Property<DateTime?>("VerifiedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("verified_on");

                    b.HasKey("EmailConfirmationPinId")
                        .HasName("pk_email_confirmation_pins");

                    b.HasIndex("Email", "Pin")
                        .IsUnique()
                        .HasDatabaseName("ix_email_confirmation_pins_email_pin");

                    b.ToTable("email_confirmation_pins", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Event", b =>
                {
                    b.Property<long>("event_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("event_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("event_id"));

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created");

                    b.Property<string>("EventName")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("event_name");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("payload");

                    b.HasKey("event_id")
                        .HasName("pk_events");

                    b.HasIndex("Payload")
                        .HasDatabaseName("ix_events_payload");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Payload"), "gin");

                    b.ToTable("events", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.JourneyTrnLookupState", b =>
                {
                    b.Property<Guid>("JourneyId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("journey_id");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created");

                    b.Property<DateOnly>("DateOfBirth")
                        .HasColumnType("date")
                        .HasColumnName("date_of_birth");

                    b.Property<DateTime?>("Locked")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("locked");

                    b.Property<string>("NationalInsuranceNumber")
                        .HasMaxLength(9)
                        .HasColumnType("character(9)")
                        .HasColumnName("national_insurance_number")
                        .IsFixedLength();

                    b.Property<string>("OfficialFirstName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("official_first_name");

                    b.Property<string>("OfficialLastName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("official_last_name");

                    b.Property<string>("PreferredFirstName")
                        .HasColumnType("text")
                        .HasColumnName("preferred_first_name");

                    b.Property<string>("PreferredLastName")
                        .HasColumnType("text")
                        .HasColumnName("preferred_last_name");

                    b.Property<string>("Trn")
                        .HasMaxLength(7)
                        .HasColumnType("character(7)")
                        .HasColumnName("trn")
                        .IsFixedLength();

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("JourneyId")
                        .HasName("pk_journey_trn_lookup_states");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_journey_trn_lookup_states_user_id");

                    b.ToTable("journey_trn_lookup_states", (string)null);
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

                    b.Property<DateTime?>("CompletedTrnLookup")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("completed_trn_lookup");

                    b.Property<DateTime>("Created")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created");

                    b.Property<DateOnly?>("DateOfBirth")
                        .HasColumnType("date")
                        .HasColumnName("date_of_birth");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("email_address")
                        .UseCollation("case_insensitive");

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

                    b.Property<DateTime?>("LastSignedIn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_signed_in");

                    b.Property<string>("RegisteredWithClientId")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("registered_with_client_id");

                    b.Property<string[]>("StaffRoles")
                        .IsRequired()
                        .HasColumnType("varchar[]")
                        .HasColumnName("staff_roles");

                    b.Property<string>("Trn")
                        .HasMaxLength(7)
                        .HasColumnType("character(7)")
                        .HasColumnName("trn")
                        .IsFixedLength();

                    b.Property<int?>("TrnAssociationSource")
                        .HasColumnType("integer")
                        .HasColumnName("trn_association_source");

                    b.Property<int?>("TrnLookupStatus")
                        .HasColumnType("integer")
                        .HasColumnName("trn_lookup_status");

                    b.Property<DateTime>("Updated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated");

                    b.Property<int>("UserType")
                        .HasColumnType("integer")
                        .HasColumnName("user_type");

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
                        .HasFilter("is_deleted = false");

                    b.HasIndex("RegisteredWithClientId")
                        .HasDatabaseName("ix_users_registered_with_client_id");

                    b.HasIndex("Trn")
                        .IsUnique()
                        .HasDatabaseName("ix_users_trn")
                        .HasFilter("is_deleted = false and trn is not null");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.WebHook", b =>
                {
                    b.Property<Guid>("WebHookId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("web_hook_id");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean")
                        .HasColumnName("enabled");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("endpoint");

                    b.Property<string>("Secret")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("secret");

                    b.HasKey("WebHookId")
                        .HasName("pk_webhooks");

                    b.ToTable("webhooks", (string)null);
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.Authorization", b =>
                {
                    b.HasOne("TeacherIdentity.AuthServer.Models.Application", "Application")
                        .WithMany("Authorizations")
                        .HasForeignKey("ApplicationId")
                        .HasConstraintName("fk_authorizations_applications_application_id");

                    b.Navigation("Application");
                });

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.JourneyTrnLookupState", b =>
                {
                    b.HasOne("TeacherIdentity.AuthServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("fk_journey_trn_lookup_states_users_user_id");

                    b.Navigation("User");
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

            modelBuilder.Entity("TeacherIdentity.AuthServer.Models.User", b =>
                {
                    b.HasOne("TeacherIdentity.AuthServer.Models.Application", "RegisteredWithClient")
                        .WithMany()
                        .HasForeignKey("RegisteredWithClientId")
                        .HasPrincipalKey("ClientId")
                        .HasConstraintName("fk_users_application_registered_with_client_id");

                    b.Navigation("RegisteredWithClient");
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
