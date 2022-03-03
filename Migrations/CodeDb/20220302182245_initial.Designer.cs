﻿// <auto-generated />
using System;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackArt.Migrations.CodeDb
{
    [DbContext(typeof(CodeDbContext))]
    [Migration("20220302182245_initial")]
    partial class initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.11");

            modelBuilder.Entity("DataAccess.Entities.CodeAttribute", b =>
                {
                    b.Property<string>("Tag")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("InnerValue")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("DisplayValue")
                        .HasColumnType("longtext");

                    b.Property<string>("Id")
                        .HasColumnType("longtext");

                    b.Property<string>("TenantId")
                        .HasColumnType("longtext");

                    b.HasKey("Tag", "InnerValue")
                        .HasName("Id");

                    b.ToTable("CodeAttribute");
                });

            modelBuilder.Entity("DataAccess.Entities.CodeLink", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("AttributeTags")
                        .HasColumnType("longtext");

                    b.Property<string>("CodeDisplay")
                        .HasColumnType("longtext");

                    b.Property<int?>("CodeLinkId")
                        .HasColumnType("int");

                    b.Property<string>("CodeValue")
                        .HasColumnType("longtext");

                    b.Property<string>("CodeValueFormat")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("TenantId")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("args")
                        .HasColumnType("longtext");

                    b.Property<bool>("isRoot")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("CodeLinkId");

                    b.ToTable("Codes");
                });

            modelBuilder.Entity("DataAccess.Entities.CodeLink", b =>
                {
                    b.HasOne("DataAccess.Entities.CodeLink", null)
                        .WithMany("Children")
                        .HasForeignKey("CodeLinkId");
                });

            modelBuilder.Entity("DataAccess.Entities.CodeLink", b =>
                {
                    b.Navigation("Children");
                });
#pragma warning restore 612, 618
        }
    }
}