﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Database.Migrations
{
    [DbContext(typeof(PeopleContext))]
    [Migration("20210622181926_AddLoggingIndexes")]
    partial class AddLoggingIndexes
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("public")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Models.Building", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Address")
                        .HasColumnName("address")
                        .HasColumnType("text");

                    b.Property<string>("City")
                        .HasColumnName("city")
                        .HasColumnType("text");

                    b.Property<string>("Code")
                        .HasColumnName("code")
                        .HasColumnType("text");

                    b.Property<string>("Country")
                        .HasColumnName("country")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("PostCode")
                        .HasColumnName("post_code")
                        .HasColumnType("text");

                    b.Property<string>("State")
                        .HasColumnName("state")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_buildings");

                    b.ToTable("buildings");
                });

            modelBuilder.Entity("Models.BuildingRelationship", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("BuildingId")
                        .HasColumnName("building_id")
                        .HasColumnType("integer");

                    b.Property<int>("UnitId")
                        .HasColumnName("unit_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_building_relationships");

                    b.HasIndex("BuildingId")
                        .HasName("ix_building_relationships_building_id");

                    b.HasIndex("UnitId")
                        .HasName("ix_building_relationships_unit_id");

                    b.ToTable("building_relationships");
                });

            modelBuilder.Entity("Models.Department", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_departments");

                    b.ToTable("departments");
                });

            modelBuilder.Entity("Models.HistoricalPerson", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.HasKey("Id")
                        .HasName("pk_historical_people");

                    b.ToTable("historical_people");
                });

            modelBuilder.Entity("Models.HrPerson", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Campus")
                        .HasColumnName("campus")
                        .HasColumnType("text");

                    b.Property<string>("CampusEmail")
                        .HasColumnName("campus_email")
                        .HasColumnType("text");

                    b.Property<string>("CampusPhone")
                        .HasColumnName("campus_phone")
                        .HasColumnType("text");

                    b.Property<string>("HrDepartment")
                        .HasColumnName("hr_department")
                        .HasColumnType("text");

                    b.Property<string>("HrDepartmentDescription")
                        .HasColumnName("hr_department_desc")
                        .HasColumnType("text");

                    b.Property<bool>("MarkedForDelete")
                        .HasColumnName("marked_for_delete")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("NameFirst")
                        .HasColumnName("name_first")
                        .HasColumnType("text");

                    b.Property<string>("NameLast")
                        .HasColumnName("name_last")
                        .HasColumnType("text");

                    b.Property<string>("Netid")
                        .HasColumnName("netid")
                        .HasColumnType("text");

                    b.Property<string>("Position")
                        .HasColumnName("position")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_hr_people");

                    b.ToTable("hr_people");
                });

            modelBuilder.Entity("Models.MemberTool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("MembershipId")
                        .HasColumnName("membership_id")
                        .HasColumnType("integer");

                    b.Property<int>("ToolId")
                        .HasColumnName("tool_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_unit_member_tools");

                    b.HasIndex("MembershipId")
                        .HasName("ix_unit_member_tools_membership_id");

                    b.HasIndex("ToolId")
                        .HasName("ix_unit_member_tools_tool_id");

                    b.ToTable("unit_member_tools");
                });

            modelBuilder.Entity("Models.Person", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Campus")
                        .IsRequired()
                        .HasColumnName("campus")
                        .HasColumnType("text");

                    b.Property<string>("CampusEmail")
                        .IsRequired()
                        .HasColumnName("campus_email")
                        .HasColumnType("text");

                    b.Property<string>("CampusPhone")
                        .IsRequired()
                        .HasColumnName("campus_phone")
                        .HasColumnType("text");

                    b.Property<int?>("DepartmentId")
                        .HasColumnName("department_id")
                        .HasColumnType("integer");

                    b.Property<string>("Expertise")
                        .HasColumnName("expertise")
                        .HasColumnType("text");

                    b.Property<bool>("IsServiceAdmin")
                        .HasColumnName("is_service_admin")
                        .HasColumnType("boolean");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasColumnName("location")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("NameFirst")
                        .HasColumnName("name_first")
                        .HasColumnType("text");

                    b.Property<string>("NameLast")
                        .HasColumnName("name_last")
                        .HasColumnType("text");

                    b.Property<string>("Netid")
                        .IsRequired()
                        .HasColumnName("netid")
                        .HasColumnType("text");

                    b.Property<string>("Notes")
                        .HasColumnName("notes")
                        .HasColumnType("text");

                    b.Property<string>("PhotoUrl")
                        .HasColumnName("photo_url")
                        .HasColumnType("text");

                    b.Property<string>("Position")
                        .IsRequired()
                        .HasColumnName("position")
                        .HasColumnType("text");

                    b.Property<int>("Responsibilities")
                        .HasColumnName("responsibilities")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_people");

                    b.HasIndex("DepartmentId")
                        .HasName("ix_people_department_id");

                    b.ToTable("people");
                });

            modelBuilder.Entity("Models.SupportRelationship", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("DepartmentId")
                        .HasColumnName("department_id")
                        .HasColumnType("integer");

                    b.Property<int>("UnitId")
                        .HasColumnName("unit_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_support_relationships");

                    b.HasIndex("DepartmentId")
                        .HasName("ix_support_relationships_department_id");

                    b.HasIndex("UnitId")
                        .HasName("ix_support_relationships_unit_id");

                    b.ToTable("support_relationships");
                });

            modelBuilder.Entity("Models.Tool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ADPath")
                        .HasColumnName("ad_path")
                        .HasColumnType("text");

                    b.Property<bool>("DepartmentScoped")
                        .HasColumnName("department_scoped")
                        .HasColumnType("boolean");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_tools");

                    b.ToTable("tools");
                });

            modelBuilder.Entity("Models.ToolPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.HasKey("Id")
                        .HasName("pk_tool_permissions");

                    b.ToTable("tool_permissions");
                });

            modelBuilder.Entity("Models.Unit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasColumnName("email")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<int?>("ParentId")
                        .HasColumnName("parent_id")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .HasColumnName("url")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_units");

                    b.HasIndex("ParentId")
                        .HasName("ix_units_parent_id");

                    b.ToTable("units");
                });

            modelBuilder.Entity("Models.UnitMember", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Notes")
                        .HasColumnName("notes")
                        .HasColumnType("text");

                    b.Property<int>("Percentage")
                        .HasColumnName("percentage")
                        .HasColumnType("integer");

                    b.Property<int>("Permissions")
                        .HasColumnName("permissions")
                        .HasColumnType("integer");

                    b.Property<int?>("PersonId")
                        .HasColumnName("person_id")
                        .HasColumnType("integer");

                    b.Property<int>("Role")
                        .HasColumnName("role")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .HasColumnName("title")
                        .HasColumnType("text");

                    b.Property<int>("UnitId")
                        .HasColumnName("unit_id")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("pk_unit_members");

                    b.HasIndex("PersonId")
                        .HasName("ix_unit_members_person_id");

                    b.HasIndex("UnitId")
                        .HasName("ix_unit_members_unit_id");

                    b.ToTable("unit_members");
                });

            modelBuilder.Entity("Models.BuildingRelationship", b =>
                {
                    b.HasOne("Models.Building", "Building")
                        .WithMany()
                        .HasForeignKey("BuildingId")
                        .HasConstraintName("fk_building_relationships_buildings_building_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Models.Unit", "Unit")
                        .WithMany("BuildingRelationships")
                        .HasForeignKey("UnitId")
                        .HasConstraintName("fk_building_relationships_units_unit_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Models.MemberTool", b =>
                {
                    b.HasOne("Models.UnitMember", "UnitMember")
                        .WithMany("MemberTools")
                        .HasForeignKey("MembershipId")
                        .HasConstraintName("fk_unit_member_tools_unit_members_membership_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Models.Tool", "Tool")
                        .WithMany("MemberTools")
                        .HasForeignKey("ToolId")
                        .HasConstraintName("fk_unit_member_tools_tools_tool_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Models.Person", b =>
                {
                    b.HasOne("Models.Department", "Department")
                        .WithMany()
                        .HasForeignKey("DepartmentId")
                        .HasConstraintName("fk_people_departments_department_id");
                });

            modelBuilder.Entity("Models.SupportRelationship", b =>
                {
                    b.HasOne("Models.Department", "Department")
                        .WithMany()
                        .HasForeignKey("DepartmentId")
                        .HasConstraintName("fk_support_relationships_departments_department_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Models.Unit", "Unit")
                        .WithMany("SupportRelationships")
                        .HasForeignKey("UnitId")
                        .HasConstraintName("fk_support_relationships_units_unit_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Models.Unit", b =>
                {
                    b.HasOne("Models.Unit", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId")
                        .HasConstraintName("fk_units_units_parent_id");
                });

            modelBuilder.Entity("Models.UnitMember", b =>
                {
                    b.HasOne("Models.Person", "Person")
                        .WithMany("UnitMemberships")
                        .HasForeignKey("PersonId")
                        .HasConstraintName("fk_unit_members_people_person_id");

                    b.HasOne("Models.Unit", "Unit")
                        .WithMany("UnitMembers")
                        .HasForeignKey("UnitId")
                        .HasConstraintName("fk_unit_members_units_unit_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
