﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VM.Domains;

#nullable disable

namespace VM.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240503130622_IdAlteredForStockClearedandRefill")]
    partial class IdAlteredForStockClearedandRefill
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("VM.Domains.AppUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedOn")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<int>("CreditLimit")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImagePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AppUsers");
                });

            modelBuilder.Entity("VM.Domains.CashRefund", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Amount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CancelOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Denomination")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MobileNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("CashRefunds");
                });

            modelBuilder.Entity("VM.Domains.Category", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("DisplayOrder")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImgPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedOn")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("VM.Domains.Company", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("GstNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Company");
                });

            modelBuilder.Entity("VM.Domains.Machine", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("AppType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MachineNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("VendorShortName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Machines");
                });

            modelBuilder.Entity("VM.Domains.MotorSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CabinId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Capacity")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MotorNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProductId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("SoldOut")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Stock")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedOn")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("MotorSettings");
                });

            modelBuilder.Entity("VM.Domains.Order", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(35)
                        .HasColumnType("TEXT");

                    b.Property<string>("DeliveryType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsPaid")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRefunded")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("OrderDate")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<string>("OrderNumber")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("TEXT");

                    b.Property<double>("PaidAmount")
                        .HasColumnType("REAL");

                    b.Property<string>("PaymentType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<double>("RefundedAmount")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("TEXT");

                    b.Property<double>("Total")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("VM.Domains.OrderItem", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(35)
                        .HasColumnType("TEXT");

                    b.Property<int>("Gst")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OrderId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Price")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<string>("ProductId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Qty")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Rate")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<DateTime>("UpdatedOn")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.Property<int>("VendQty")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.ToTable("OrderItems");
                });

            modelBuilder.Entity("VM.Domains.Product", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<double>("BaseRate")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<string>("CategoryId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Gst")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImgPath")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("TEXT");

                    b.Property<double>("Price")
                        .HasPrecision(2)
                        .HasColumnType("REAL");

                    b.Property<DateTime>("UpdatedOn")
                        .HasPrecision(0)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("VM.Domains.Refill", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MotorNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProductId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("RefilledOn")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Refills");
                });

            modelBuilder.Entity("VM.Domains.StockCleared", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ClearedOn")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsViewed")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MotorNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProductId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProductName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("StockCleared");
                });

            modelBuilder.Entity("VM.Domains.OrderItem", b =>
                {
                    b.HasOne("VM.Domains.Order", null)
                        .WithMany("Items")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("VM.Domains.Product", b =>
                {
                    b.HasOne("VM.Domains.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("VM.Domains.Category", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("VM.Domains.Order", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}
