﻿// <auto-generated />
using System;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackArt.Migrations.ComplaintSeriesDb
{
    [DbContext(typeof(ComplaintSeriesDbContext))]
    [Migration("20220329115605_initial")]
    partial class initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.11");

            modelBuilder.Entity("DataAccess.Entities.Attachment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("ContentType")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Data")
                        .HasColumnType("longtext");

                    b.Property<string>("Extension")
                        .HasColumnType("longtext");

                    b.Property<string>("StorageType")
                        .HasColumnType("longtext");

                    b.Property<int>("TicketId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("TicketId");

                    b.ToTable("Attachment");
                });

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

                    b.ToTable("CodeAttributeSnapshot");
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

                    b.Property<int?>("TicketId")
                        .HasColumnType("int");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("args")
                        .HasColumnType("longtext");

                    b.Property<bool>("isRoot")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("CodeLinkId");

                    b.HasIndex("TicketId");

                    b.ToTable("CodeLinkSnapshot");
                });

            modelBuilder.Entity("DataAccess.Entities.ComplaintSeries", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("DataKeyId")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("NrComanda")
                        .HasColumnType("longtext");

                    b.Property<string>("Status")
                        .HasColumnType("longtext");

                    b.Property<string>("TenantId")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("isDeleted")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("DataKeyId");

                    b.ToTable("Complaints");
                });

            modelBuilder.Entity("DataAccess.Entities.DataKeyLocation", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("locationCode")
                        .HasColumnType("longtext");

                    b.Property<string>("name")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("DataKeyLocation", t => t.ExcludeFromMigrations());
                });

            modelBuilder.Entity("DataAccess.Entities.Ticket", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CodeValue")
                        .HasColumnType("longtext");

                    b.Property<int>("ComplaintId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<bool>("HasAttachments")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("isDeleted")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("ComplaintId");

                    b.ToTable("Ticket");
                });

            modelBuilder.Entity("DataAccess.Entities.Attachment", b =>
                {
                    b.HasOne("DataAccess.Entities.Ticket", "Ticket")
                        .WithMany("Attachments")
                        .HasForeignKey("TicketId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();

                    b.Navigation("Ticket");
                });

            modelBuilder.Entity("DataAccess.Entities.CodeLink", b =>
                {
                    b.HasOne("DataAccess.Entities.CodeLink", null)
                        .WithMany("Children")
                        .HasForeignKey("CodeLinkId");

                    b.HasOne("DataAccess.Entities.Ticket", null)
                        .WithMany("CodeLinks")
                        .HasForeignKey("TicketId");
                });

            modelBuilder.Entity("DataAccess.Entities.ComplaintSeries", b =>
                {
                    b.HasOne("DataAccess.Entities.DataKeyLocation", "DataKey")
                        .WithMany()
                        .HasForeignKey("DataKeyId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("DataKey");
                });

            modelBuilder.Entity("DataAccess.Entities.Ticket", b =>
                {
                    b.HasOne("DataAccess.Entities.ComplaintSeries", "Complaint")
                        .WithMany("Tickets")
                        .HasForeignKey("ComplaintId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();

                    b.Navigation("Complaint");
                });

            modelBuilder.Entity("DataAccess.Entities.CodeLink", b =>
                {
                    b.Navigation("Children");
                });

            modelBuilder.Entity("DataAccess.Entities.ComplaintSeries", b =>
                {
                    b.Navigation("Tickets");
                });

            modelBuilder.Entity("DataAccess.Entities.Ticket", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("CodeLinks");
                });
#pragma warning restore 612, 618
        }
    }
}