﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebApi.Entities;

namespace BackArt.Migrations.ComplaintSeriesDb
{
    [DbContext(typeof(ComplaintSeriesDbContext))]
    partial class ComplaintSeriesDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.11");

            modelBuilder.Entity("WebApi.Entities.CodeAttribute", b =>
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

            modelBuilder.Entity("WebApi.Entities.CodeLink", b =>
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

            modelBuilder.Entity("WebApi.Entities.ComplaintSeries", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("DataKey")
                        .HasColumnType("longtext");

                    b.Property<string>("TenantId")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Complaints");
                });

            modelBuilder.Entity("WebApi.Entities.Image", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Data")
                        .HasColumnType("longtext");

                    b.Property<int>("TicketId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("TicketId");

                    b.ToTable("Image");
                });

            modelBuilder.Entity("WebApi.Entities.Ticket", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CodeValue")
                        .HasColumnType("longtext");

                    b.Property<int?>("ComplaintSeriesId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<bool>("HasImages")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("ComplaintSeriesId");

                    b.ToTable("Ticket");
                });

            modelBuilder.Entity("WebApi.Entities.CodeLink", b =>
                {
                    b.HasOne("WebApi.Entities.CodeLink", null)
                        .WithMany("Children")
                        .HasForeignKey("CodeLinkId");

                    b.HasOne("WebApi.Entities.Ticket", null)
                        .WithMany("codeLinks")
                        .HasForeignKey("TicketId");
                });

            modelBuilder.Entity("WebApi.Entities.Image", b =>
                {
                    b.HasOne("WebApi.Entities.Ticket", "Ticket")
                        .WithMany("Images")
                        .HasForeignKey("TicketId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();

                    b.Navigation("Ticket");
                });

            modelBuilder.Entity("WebApi.Entities.Ticket", b =>
                {
                    b.HasOne("WebApi.Entities.ComplaintSeries", null)
                        .WithMany("Tickets")
                        .HasForeignKey("ComplaintSeriesId");
                });

            modelBuilder.Entity("WebApi.Entities.CodeLink", b =>
                {
                    b.Navigation("Children");
                });

            modelBuilder.Entity("WebApi.Entities.ComplaintSeries", b =>
                {
                    b.Navigation("Tickets");
                });

            modelBuilder.Entity("WebApi.Entities.Ticket", b =>
                {
                    b.Navigation("codeLinks");

                    b.Navigation("Images");
                });
#pragma warning restore 612, 618
        }
    }
}
