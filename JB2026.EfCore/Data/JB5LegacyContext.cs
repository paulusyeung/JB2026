using System;
using System.Collections.Generic;
using JB2026.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCore.Data;

public partial class JB5LegacyContext : DbContext
{
    public JB5LegacyContext(DbContextOptions options)
        : base(options)
    {
    }

    public virtual DbSet<AggregatedCounter> AggregatedCounters { get; set; }

    public virtual DbSet<Counter> Counters { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<FCMHistory> FCMHistories { get; set; }

    public virtual DbSet<Hash> Hashes { get; set; }

    public virtual DbSet<InvoiceHeader> InvoiceHeaders { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<InvoiceSubItem> InvoiceSubItems { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobAttachment> JobAttachments { get; set; }

    public virtual DbSet<JobOrder> JobOrders { get; set; }

    public virtual DbSet<JobPackingOnAir> JobPackingOnAirs { get; set; }

    public virtual DbSet<JobParameter> JobParameters { get; set; }

    public virtual DbSet<JobQueue> JobQueues { get; set; }

    public virtual DbSet<JobSchedule> JobSchedules { get; set; }

    public virtual DbSet<JobWorkflow> JobWorkflows { get; set; }

    public virtual DbSet<JobWorkflowForm> JobWorkflowForms { get; set; }

    public virtual DbSet<List> Lists { get; set; }

    public virtual DbSet<Log4Net> Log4Nets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAttachment> ProductAttachments { get; set; }

    public virtual DbSet<QtDetail> QtDetails { get; set; }

    public virtual DbSet<QtHeader> QtHeaders { get; set; }

    public virtual DbSet<QtItem> QtItems { get; set; }

    public virtual DbSet<QtItemGroup> QtItemGroups { get; set; }

    public virtual DbSet<Schema> Schemas { get; set; }

    public virtual DbSet<Server> Servers { get; set; }

    public virtual DbSet<Set> Sets { get; set; }

    public virtual DbSet<SmlRtfExtractToDN> SmlRtfExtractToDNs { get; set; }

    public virtual DbSet<SmlRtfHeader> SmlRtfHeaders { get; set; }

    public virtual DbSet<SmlRtfItem> SmlRtfItems { get; set; }

    public virtual DbSet<SmlRtfSubItem> SmlRtfSubItems { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<StockInOut> StockInOuts { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SystemInfo> SystemInfos { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuth> UserAuths { get; set; }

    public virtual DbSet<UserInfo> UserInfos { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }

    public virtual DbSet<Z_Category> Z_Categories { get; set; }

    public virtual DbSet<Z_Form> Z_Forms { get; set; }

    public virtual DbSet<Z_OrderTypeWorkflow> Z_OrderTypeWorkflows { get; set; }

    public virtual DbSet<Z_Workflow> Z_Workflows { get; set; }

    public virtual DbSet<Z_WorkflowForm> Z_WorkflowForms { get; set; }

    public virtual DbSet<vwAvailableJobOrderList> vwAvailableJobOrderLists { get; set; }

    public virtual DbSet<vwAvailableJobPackingList> vwAvailableJobPackingLists { get; set; }

    public virtual DbSet<vwAvailableJobPackingOnAirList> vwAvailableJobPackingOnAirLists { get; set; }

    public virtual DbSet<vwAvailableJobScheduleList> vwAvailableJobScheduleLists { get; set; }

    public virtual DbSet<vwCustomerList> vwCustomerLists { get; set; }

    public virtual DbSet<vwCustomerList_Active> vwCustomerList_Actives { get; set; }

    public virtual DbSet<vwDashboard_StatJob_All> vwDashboard_StatJob_Alls { get; set; }

    public virtual DbSet<vwDashboard_StatJob_Average> vwDashboard_StatJob_Averages { get; set; }

    public virtual DbSet<vwDashboard_StatJob_OneStep4All> vwDashboard_StatJob_OneStep4Alls { get; set; }

    public virtual DbSet<vwDashboard_StatJob_Staff> vwDashboard_StatJob_Staffs { get; set; }

    public virtual DbSet<vwDashboard_StatSML_Invoice> vwDashboard_StatSML_Invoices { get; set; }

    public virtual DbSet<vwDashboard_StatSML_Order> vwDashboard_StatSML_Orders { get; set; }

    public virtual DbSet<vwInvoiceList> vwInvoiceLists { get; set; }

    public virtual DbSet<vwJobList> vwJobLists { get; set; }

    public virtual DbSet<vwJobListWithWorkflow> vwJobListWithWorkflows { get; set; }

    public virtual DbSet<vwJobList_Active> vwJobList_Actives { get; set; }

    public virtual DbSet<vwJobOrder_PackingList> vwJobOrder_PackingLists { get; set; }

    public virtual DbSet<vwJobOrder_PendingList> vwJobOrder_PendingLists { get; set; }

    public virtual DbSet<vwJobScheduleList> vwJobScheduleLists { get; set; }

    public virtual DbSet<vwJobScheduleList_OnAir> vwJobScheduleList_OnAirs { get; set; }

    public virtual DbSet<vwJobSchedule_AvailableList> vwJobSchedule_AvailableLists { get; set; }

    public virtual DbSet<vwJobSchedule_OnAirList> vwJobSchedule_OnAirLists { get; set; }

    public virtual DbSet<vwJobSchedule_PendingList> vwJobSchedule_PendingLists { get; set; }

    public virtual DbSet<vwJobStatCoG> vwJobStatCoGs { get; set; }

    public virtual DbSet<vwJobStatGrossProfit> vwJobStatGrossProfits { get; set; }

    public virtual DbSet<vwOlapInvoiceStat> vwOlapInvoiceStats { get; set; }

    public virtual DbSet<vwOlapSmlRtf> vwOlapSmlRtfs { get; set; }

    public virtual DbSet<vwOrderDetailList> vwOrderDetailLists { get; set; }

    public virtual DbSet<vwOrderList> vwOrderLists { get; set; }

    public virtual DbSet<vwProductList> vwProductLists { get; set; }

    public virtual DbSet<vwQtHeaderList> vwQtHeaderLists { get; set; }

    public virtual DbSet<vwQtItemList> vwQtItemLists { get; set; }

    public virtual DbSet<vwRptQuotation> vwRptQuotations { get; set; }

    public virtual DbSet<vwRtfHeaderList> vwRtfHeaderLists { get; set; }

    public virtual DbSet<vwRtfHeaderList_Active> vwRtfHeaderList_Actives { get; set; }

    public virtual DbSet<vwRtfItemList> vwRtfItemLists { get; set; }

    public virtual DbSet<vwSupplierList> vwSupplierLists { get; set; }

    public virtual DbSet<vwSupplierList_Active> vwSupplierList_Actives { get; set; }

    public virtual DbSet<vwUserList> vwUserLists { get; set; }

    public virtual DbSet<vwUserList_Active> vwUserList_Actives { get; set; }

    public virtual DbSet<vwUserNotificationList> vwUserNotificationLists { get; set; }

    public virtual DbSet<vwUserPreferenceList> vwUserPreferenceLists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AggregatedCounter>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("PK_HangFire_CounterAggregated");

            entity.ToTable("AggregatedCounter", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_AggregatedCounter_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_Counter");

            entity.ToTable("Counter", "HangFire");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.CustomerId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedBy).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
            entity.Property(e => e.ModifiedBy).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ModifiedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
        });

        modelBuilder.Entity<FCMHistory>(entity =>
        {
            entity.ToTable("FCMHistory");

            entity.Property(e => e.FCMHistoryId).ValueGeneratedNever();
            entity.Property(e => e.DeliveredOn).HasColumnType("datetime");
            entity.Property(e => e.MessageBody).HasMaxLength(256);
            entity.Property(e => e.MessageTitle).HasMaxLength(64);
            entity.Property(e => e.RecipientList).HasMaxLength(1024);
            entity.Property(e => e.Topic).HasMaxLength(64);
            entity.Property(e => e.UserIdList).HasMaxLength(512);
        });

        modelBuilder.Entity<Hash>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Field }).HasName("PK_HangFire_Hash");

            entity.ToTable("Hash", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Hash_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Field).HasMaxLength(100);
        });

        modelBuilder.Entity<InvoiceHeader>(entity =>
        {
            entity.HasKey(e => e.HeaderId);

            entity.ToTable("InvoiceHeader");

            entity.Property(e => e.HeaderId).ValueGeneratedNever();
            entity.Property(e => e.BillTo).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ICNumber).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InvoiceDate).HasColumnType("smalldatetime");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(10);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ShipTo).HasMaxLength(256);

            entity.HasOne(d => d.Customer).WithMany(p => p.InvoiceHeaders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Customer_InvoiceHeader");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId).ValueGeneratedNever();
            entity.Property(e => e.Notes).HasMaxLength(128);

            entity.HasOne(d => d.Header).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceHeader_InvoiceItems");

            entity.HasOne(d => d.SmlRtfHeader).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.SmlRtfHeaderId)
                .HasConstraintName("FK_SmlRtfHeader_InvoiceItems");
        });

        modelBuilder.Entity<InvoiceSubItem>(entity =>
        {
            entity.HasKey(e => e.SubItemId);

            entity.Property(e => e.SubItemId).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Description).HasMaxLength(64);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.UoM).HasMaxLength(10);

            entity.HasOne(d => d.Item).WithMany(p => p.InvoiceSubItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InvoiceItems_InvoiceSubItems");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Job");

            entity.ToTable("Job", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Job_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => e.StateName, "IX_HangFire_Job_StateName").HasFilter("([StateName] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
            entity.Property(e => e.StateName).HasMaxLength(20);
        });

        modelBuilder.Entity<JobAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).IsClustered(false);

            entity.ToTable("JobAttachment");

            entity.Property(e => e.AttachmentId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.JobAttachments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrder_JobAttachment");
        });

        modelBuilder.Entity<JobOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).IsClustered(false);

            entity.ToTable("JobOrder");

            entity.HasIndex(e => e.OrderNumber, "IX_JobOrderA");

            entity.Property(e => e.OrderId).ValueGeneratedNever();
            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.InvoiceRef).HasMaxLength(32);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OriginalPONumber).HasMaxLength(32);
            entity.Property(e => e.OriginalSONumber).HasMaxLength(32);
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.PONumber).HasMaxLength(32);
            entity.Property(e => e.PaymentTerms).HasMaxLength(32);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(32);
            entity.Property(e => e.Qty).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.QtyText).HasMaxLength(32);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.SONumber).HasMaxLength(32);
        });

        modelBuilder.Entity<JobPackingOnAir>(entity =>
        {
            entity.HasKey(e => e.OnAirId);

            entity.ToTable("JobPackingOnAir");

            entity.Property(e => e.OnAirId).ValueGeneratedNever();
            entity.Property(e => e.CancelledOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CompletedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OnAiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RescheduledOn).HasColumnType("smalldatetime");

            entity.HasOne(d => d.Order).WithMany(p => p.JobPackingOnAirs)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrder_JobPackingOnAir");
        });

        modelBuilder.Entity<JobParameter>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Name }).HasName("PK_HangFire_JobParameter");

            entity.ToTable("JobParameter", "HangFire");

            entity.Property(e => e.Name).HasMaxLength(40);

            entity.HasOne(d => d.Job).WithMany(p => p.JobParameters)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_JobParameter_Job");
        });

        modelBuilder.Entity<JobQueue>(entity =>
        {
            entity.HasKey(e => new { e.Queue, e.Id }).HasName("PK_HangFire_JobQueue");

            entity.ToTable("JobQueue", "HangFire");

            entity.Property(e => e.Queue).HasMaxLength(50);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FetchedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<JobSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.ToTable("JobSchedule");

            entity.Property(e => e.ScheduleId).ValueGeneratedNever();
            entity.Property(e => e.CancelledOn).HasColumnType("datetime");
            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.MachineNumber).HasMaxLength(10);
            entity.Property(e => e.RescheduledOn).HasColumnType("datetime");
            entity.Property(e => e.ScheduledOn).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.JobSchedules)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrder_JobSchedule");
        });

        modelBuilder.Entity<JobWorkflow>(entity =>
        {
            entity.HasKey(e => e.JobWorkflowId).IsClustered(false);

            entity.ToTable("JobWorkflow");

            entity.Property(e => e.JobWorkflowId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.WorkInstruction).HasMaxLength(128);
            entity.Property(e => e.WorkTitle).HasMaxLength(64);

            entity.HasOne(d => d.Order).WithMany(p => p.JobWorkflows)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobOrder_JobWorkflow");

            entity.HasOne(d => d.Workflow).WithMany(p => p.JobWorkflows)
                .HasForeignKey(d => d.WorkflowId)
                .HasConstraintName("FK_Z_Workflow_JobWorkflow");
        });

        modelBuilder.Entity<JobWorkflowForm>(entity =>
        {
            entity.Property(e => e.JobWorkflowFormId).ValueGeneratedNever();
            entity.Property(e => e.MetadataXml).HasColumnType("xml");

            entity.HasOne(d => d.Form).WithMany(p => p.JobWorkflowForms)
                .HasForeignKey(d => d.FormId)
                .HasConstraintName("FK_Z_Forms_JobWorkflowForms");

            entity.HasOne(d => d.JobWorkflow).WithMany(p => p.JobWorkflowForms)
                .HasForeignKey(d => d.JobWorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobWorkflow_JobWorkflowForms");
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Id }).HasName("PK_HangFire_List");

            entity.ToTable("List", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_List_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Log4Net>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Log4Net");

            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Exception)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Level)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Logger)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Message)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.Thread)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.HasIndex(e => e.StockNumber, "IX_ProductA");

            entity.HasIndex(e => e.ProductCode, "IX_ProductB");

            entity.Property(e => e.ProductId).ValueGeneratedNever();
            entity.Property(e => e.COGS).HasColumnType("money");
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductName).HasMaxLength(64);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.SellingPrice).HasColumnType("money");
            entity.Property(e => e.StockNumber).HasMaxLength(32);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Z_Category_Product");
        });

        modelBuilder.Entity<ProductAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);

            entity.ToTable("ProductAttachment");

            entity.Property(e => e.AttachmentId).ValueGeneratedNever();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttachments)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_ProductAttachment");
        });

        modelBuilder.Entity<QtDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId);

            entity.Property(e => e.DetailId).ValueGeneratedNever();
            entity.Property(e => e.CostA).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostB).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostC).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostD).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Description).HasMaxLength(64);
            entity.Property(e => e.Minimum).HasMaxLength(32);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Zone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Header).WithMany(p => p.QtDetails)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QtHeader_QtDetails");

            entity.HasOne(d => d.Item).WithMany(p => p.QtDetails)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK_QtItem_QtDetails");
        });

        modelBuilder.Entity<QtHeader>(entity =>
        {
            entity.HasKey(e => e.HeaderId);

            entity.ToTable("QtHeader");

            entity.Property(e => e.HeaderId).ValueGeneratedNever();
            entity.Property(e => e.ApprovedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(256);
            entity.Property(e => e.MaterialCost).HasMaxLength(128);
            entity.Property(e => e.MaterialName).HasMaxLength(128);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.PageHeight).HasMaxLength(32);
            entity.Property(e => e.PageWidth).HasMaxLength(32);
            entity.Property(e => e.PaperSheetSize).HasMaxLength(128);
            entity.Property(e => e.PaperSheetSizeAlias).HasMaxLength(128);
            entity.Property(e => e.PrintPerPageEx).HasMaxLength(64);
            entity.Property(e => e.PrintTitle).HasMaxLength(256);
            entity.Property(e => e.PrintsColor).HasMaxLength(64);
            entity.Property(e => e.PrintsQty).HasMaxLength(64);
            entity.Property(e => e.PrintsSize).HasMaxLength(64);
            entity.Property(e => e.QuotedOn).HasColumnType("datetime");
            entity.Property(e => e.Remarks).HasMaxLength(256);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.TotalCostA).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostB).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostC).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostD).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.UnitCostA).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostB).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostC).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostD).HasColumnType("decimal(10, 4)");

            entity.HasOne(d => d.Customer).WithMany(p => p.QtHeaders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Customer_QtHeader");
        });

        modelBuilder.Entity<QtItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.ToTable("QtItem");

            entity.Property(e => e.ItemId).ValueGeneratedNever();
            entity.Property(e => e.CostRounding).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.ItemNameChs).HasMaxLength(64);
            entity.Property(e => e.ItemNameCht).HasMaxLength(64);
            entity.Property(e => e.ItemNameEn).HasMaxLength(64);
            entity.Property(e => e.Minimum).HasMaxLength(32);
            entity.Property(e => e.MinimumCost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Zone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.ItemGroup).WithMany(p => p.QtItems)
                .HasForeignKey(d => d.ItemGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QtItemGroup_QtItem");
        });

        modelBuilder.Entity<QtItemGroup>(entity =>
        {
            entity.HasKey(e => e.ItemGroupId);

            entity.ToTable("QtItemGroup");

            entity.Property(e => e.ItemGroupId).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.GroupNameChs).HasMaxLength(64);
            entity.Property(e => e.GroupNameCht).HasMaxLength(64);
            entity.Property(e => e.GroupNameEn).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.Zone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Schema>(entity =>
        {
            entity.HasKey(e => e.Version).HasName("PK_HangFire_Schema");

            entity.ToTable("Schema", "HangFire");

            entity.Property(e => e.Version).ValueGeneratedNever();
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HangFire_Server");

            entity.ToTable("Server", "HangFire");

            entity.HasIndex(e => e.LastHeartbeat, "IX_HangFire_Server_LastHeartbeat");

            entity.Property(e => e.Id).HasMaxLength(200);
            entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.HasKey(e => new { e.Key, e.Value }).HasName("PK_HangFire_Set");

            entity.ToTable("Set", "HangFire");

            entity.HasIndex(e => e.ExpireAt, "IX_HangFire_Set_ExpireAt").HasFilter("([ExpireAt] IS NOT NULL)");

            entity.HasIndex(e => new { e.Key, e.Score }, "IX_HangFire_Set_Score");

            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(256);
            entity.Property(e => e.ExpireAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<SmlRtfExtractToDN>(entity =>
        {
            entity.HasKey(e => e.DNId);

            entity.ToTable("SmlRtfExtractToDN");

            entity.Property(e => e.DNId).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.DNDate).HasColumnType("smalldatetime");
            entity.Property(e => e.DNNumber).HasMaxLength(16);

            entity.HasOne(d => d.Header).WithMany(p => p.SmlRtfExtractToDNs)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SmlRftHeader_SmlRtfExtractToDN");
        });

        modelBuilder.Entity<SmlRtfHeader>(entity =>
        {
            entity.HasKey(e => e.HeaderId);

            entity.ToTable("SmlRtfHeader");

            entity.Property(e => e.HeaderId).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerPO).HasMaxLength(16);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OrderedBy).HasMaxLength(32);
            entity.Property(e => e.OrderedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OriginalPO).HasMaxLength(16);
            entity.Property(e => e.OriginalSO).HasMaxLength(16);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(16);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RtfFileName).HasMaxLength(256);
            entity.Property(e => e.SalesOrder).HasMaxLength(16);
        });

        modelBuilder.Entity<SmlRtfItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasMaxLength(16);
            entity.Property(e => e.Discount).HasMaxLength(16);
            entity.Property(e => e.PostProcess).HasMaxLength(64);
            entity.Property(e => e.Price).HasMaxLength(16);
            entity.Property(e => e.ProductCode).HasMaxLength(128);
            entity.Property(e => e.ProductDescription).HasMaxLength(256);
            entity.Property(e => e.Qty).HasMaxLength(16);

            entity.HasOne(d => d.Header).WithMany(p => p.SmlRtfItems)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SmlRtfHeader_SmlRtfItems");
        });

        modelBuilder.Entity<SmlRtfSubItem>(entity =>
        {
            entity.HasKey(e => e.SubItemId);

            entity.Property(e => e.SubItemId).ValueGeneratedNever();
            entity.Property(e => e.LabelSize).HasMaxLength(32);
            entity.Property(e => e.Qty).HasMaxLength(10);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(32);
            entity.Property(e => e.Start_End).HasMaxLength(256);

            entity.HasOne(d => d.Item).WithMany(p => p.SmlRtfSubItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SmlRtfItems_SmlRtfSubItems");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.Id }).HasName("PK_HangFire_State");

            entity.ToTable("State", "HangFire");

            entity.HasIndex(e => e.CreatedAt, "IX_HangFire_State_CreatedAt");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(100);

            entity.HasOne(d => d.Job).WithMany(p => p.States)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_HangFire_State_Job");
        });

        modelBuilder.Entity<StockInOut>(entity =>
        {
            entity.HasKey(e => e.InOutId);

            entity.ToTable("StockInOut");

            entity.Property(e => e.InOutId).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.InOutDate).HasColumnType("smalldatetime");
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.Reference).HasMaxLength(32);

            entity.HasOne(d => d.Product).WithMany(p => p.StockInOuts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Product_StockInOut");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierId).ValueGeneratedNever();
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.SupplierName).HasMaxLength(64);
        });

        modelBuilder.Entity<SystemInfo>(entity =>
        {
            entity.HasKey(e => e.SystemId).IsClustered(false);

            entity.ToTable("SystemInfo");

            entity.Property(e => e.SystemId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
            entity.Property(e => e.OwnerName).HasMaxLength(255);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Alias).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.LoginName).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<UserAuth>(entity =>
        {
            entity.HasKey(e => e.AuthId);

            entity.ToTable("UserAuth");

            entity.Property(e => e.AuthId).ValueGeneratedNever();
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");

            entity.HasOne(d => d.User).WithMany(p => p.UserAuths)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_UserAuth");
        });

        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.HasKey(e => e.UserId).IsClustered(false);

            entity.ToTable("UserInfo");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedBy).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("smalldatetime");
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
            entity.Property(e => e.ModifiedBy).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ModifiedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.UserAlias).HasMaxLength(64);
            entity.Property(e => e.UserName).HasMaxLength(64);
            entity.Property(e => e.UserPassword).HasMaxLength(64);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.NotifyId);

            entity.ToTable("UserNotification");

            entity.Property(e => e.NotifyId).ValueGeneratedNever();
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_UserNotification");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.PreferenceId);

            entity.ToTable("UserPreference");

            entity.Property(e => e.PreferenceId).ValueGeneratedNever();
            entity.Property(e => e.MetadataXml).HasColumnType("xml");

            entity.HasOne(d => d.User).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_UserPreference");
        });

        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WebhookS__3214EC07242A524C");

            entity.HasIndex(e => e.IsActive, "IX_WebhookSubscriptions_IsActive");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EventTypes).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Url).HasMaxLength(2048);
        });

        modelBuilder.Entity<Z_Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("Z_Category");

            entity.Property(e => e.CategoryId).ValueGeneratedNever();
            entity.Property(e => e.CategoryCode).HasMaxLength(3);
            entity.Property(e => e.CategoryName).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
        });

        modelBuilder.Entity<Z_Form>(entity =>
        {
            entity.HasKey(e => e.FormId);

            entity.Property(e => e.FormId).ValueGeneratedNever();
            entity.Property(e => e.FormName).HasMaxLength(10);
            entity.Property(e => e.FormName_Chs).HasMaxLength(10);
            entity.Property(e => e.FormName_Cht).HasMaxLength(10);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
        });

        modelBuilder.Entity<Z_OrderTypeWorkflow>(entity =>
        {
            entity.HasKey(e => e.OrderTypeWorkflowId).IsClustered(false);

            entity.ToTable("Z_OrderTypeWorkflow");

            entity.Property(e => e.OrderTypeWorkflowId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Workflow).WithMany(p => p.Z_OrderTypeWorkflows)
                .HasForeignKey(d => d.WorkflowId)
                .HasConstraintName("FK_Z_Workflow_Z_OrderTypeWorkflow");
        });

        modelBuilder.Entity<Z_Workflow>(entity =>
        {
            entity.HasKey(e => e.WorkflowId).IsClustered(false);

            entity.ToTable("Z_Workflow");

            entity.Property(e => e.WorkflowId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.WorkInstruction).HasMaxLength(512);
            entity.Property(e => e.WorkTitle).HasMaxLength(512);
            entity.Property(e => e.WorkflowName).HasMaxLength(64);
        });

        modelBuilder.Entity<Z_WorkflowForm>(entity =>
        {
            entity.HasKey(e => e.WorkflowFormId);

            entity.Property(e => e.WorkflowFormId).ValueGeneratedNever();
            entity.Property(e => e.WorkflowId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Form).WithMany(p => p.Z_WorkflowForms)
                .HasForeignKey(d => d.FormId)
                .HasConstraintName("FK_Z_Forms_Z_WorkflowForms");

            entity.HasOne(d => d.Workflow).WithMany(p => p.Z_WorkflowForms)
                .HasForeignKey(d => d.WorkflowId)
                .HasConstraintName("FK_Z_Workflow_Z_WorkflowForms");
        });

        modelBuilder.Entity<vwAvailableJobOrderList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwAvailableJobOrderList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
        });

        modelBuilder.Entity<vwAvailableJobPackingList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwAvailableJobPackingList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
        });

        modelBuilder.Entity<vwAvailableJobPackingOnAirList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwAvailableJobPackingOnAirList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
        });

        modelBuilder.Entity<vwAvailableJobScheduleList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwAvailableJobScheduleList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.MachineNumber).HasMaxLength(10);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
        });

        modelBuilder.Entity<vwCustomerList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCustomerList");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
        });

        modelBuilder.Entity<vwCustomerList_Active>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwCustomerList_Active");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
        });

        modelBuilder.Entity<vwDashboard_StatJob_All>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatJob_All");

            entity.Property(e => e.SalesRep).HasMaxLength(64);
            entity.Property(e => e.TBill).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TCost).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<vwDashboard_StatJob_Average>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatJob_Average");

            entity.Property(e => e.SalesRep)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.TBill).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.TCost).HasColumnType("decimal(38, 6)");
        });

        modelBuilder.Entity<vwDashboard_StatJob_OneStep4All>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatJob_OneStep4All");

            entity.Property(e => e.SalesRep).HasMaxLength(64);
            entity.Property(e => e.TBill).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TCost).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<vwDashboard_StatJob_Staff>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatJob_Staff");

            entity.Property(e => e.SalesRep).HasMaxLength(64);
            entity.Property(e => e.TBill).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TCost).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<vwDashboard_StatSML_Invoice>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatSML_Invoice");

            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.TAmount).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<vwDashboard_StatSML_Order>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwDashboard_StatSML_Order");

            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.TAmount).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<vwInvoiceList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwInvoiceList");

            entity.Property(e => e.BillTo).HasMaxLength(256);
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.ICNumber).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InvoiceDate).HasColumnType("smalldatetime");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(10);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ShipTo).HasMaxLength(256);
        });

        modelBuilder.Entity<vwJobList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobList");

            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.InvoiceRef).HasMaxLength(32);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.PaymentTerms).HasMaxLength(32);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(32);
            entity.Property(e => e.Qty).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.SONumber).HasMaxLength(32);
        });

        modelBuilder.Entity<vwJobListWithWorkflow>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobListWithWorkflow");

            entity.Property(e => e.Attachment_Product).HasMaxLength(255);
            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.InvoiceRef).HasMaxLength(32);
            entity.Property(e => e.JobOrderName).HasMaxLength(2);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.PaymentTerms).HasMaxLength(32);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(32);
            entity.Property(e => e.Qty).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.QtyText).HasMaxLength(32);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.WorkInstruction).HasMaxLength(128);
            entity.Property(e => e.WorkTitle).HasMaxLength(64);
            entity.Property(e => e.WorkflowName).HasMaxLength(64);
        });

        modelBuilder.Entity<vwJobList_Active>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobList_Active");

            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.InvoiceRef).HasMaxLength(32);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.PaymentTerms).HasMaxLength(32);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(32);
            entity.Property(e => e.Qty).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.SONumber).HasMaxLength(32);
        });

        modelBuilder.Entity<vwJobOrder_PackingList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobOrder_PackingList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.JobOrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwJobOrder_PendingList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobOrder_PendingList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.JobOrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwJobScheduleList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobScheduleList");

            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.MachineNumber).HasMaxLength(10);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.ScheduledOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwJobScheduleList_OnAir>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobScheduleList_OnAir");

            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.MachineNumber).HasMaxLength(10);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.ScheduledOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwJobSchedule_AvailableList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobSchedule_AvailableList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
        });

        modelBuilder.Entity<vwJobSchedule_OnAirList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobSchedule_OnAirList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.MachineNumber).HasMaxLength(10);
            entity.Property(e => e.OrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
        });

        modelBuilder.Entity<vwJobSchedule_PendingList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobSchedule_PendingList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.JobOrderNumber).HasMaxLength(13);
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwJobStatCoG>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobStatCoGS");

            entity.Property(e => e.Cost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.InvDate).HasColumnType("datetime");
            entity.Property(e => e.InvNumber).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.JobNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(32);
            entity.Property(e => e.SalesRep).HasMaxLength(64);
        });

        modelBuilder.Entity<vwJobStatGrossProfit>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwJobStatGrossProfit");

            entity.Property(e => e.Cost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.GrossProfit).HasColumnType("numeric(32, 17)");
            entity.Property(e => e.InvDate).HasColumnType("datetime");
            entity.Property(e => e.InvNumber).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.JobNumber).HasMaxLength(13);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(32);
            entity.Property(e => e.SalesRep).HasMaxLength(64);
        });

        modelBuilder.Entity<vwOlapInvoiceStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwOlapInvoiceStats");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerName).HasMaxLength(64);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(10);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.ProductCode).HasMaxLength(14);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(10);
            entity.Property(e => e.Qty).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.Unit).HasMaxLength(10);
        });

        modelBuilder.Entity<vwOlapSmlRtf>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwOlapSmlRtf");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerPO).HasMaxLength(16);
            entity.Property(e => e.Discount).HasMaxLength(16);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OriginalPO).HasMaxLength(16);
            entity.Property(e => e.OriginalSO).HasMaxLength(16);
            entity.Property(e => e.Price).HasMaxLength(16);
            entity.Property(e => e.ProductCode).HasMaxLength(14);
            entity.Property(e => e.ProductDescription).HasMaxLength(256);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(16);
            entity.Property(e => e.Qty).HasMaxLength(16);
            entity.Property(e => e.SalesOrder).HasMaxLength(16);
        });

        modelBuilder.Entity<vwOrderDetailList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwOrderDetailList");

            entity.Property(e => e.CompletedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(32);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.InvoiceRef).HasMaxLength(32);
            entity.Property(e => e.JobOrderNumber)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.OutputRef).HasMaxLength(64);
            entity.Property(e => e.PaymentTerms).HasMaxLength(32);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(32);
            entity.Property(e => e.Qty).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.SONumber).HasMaxLength(32);
        });

        modelBuilder.Entity<vwOrderList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwOrderList");

            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.InvoiceAmount).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.OrderTitle).HasMaxLength(128);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.OrderedOn).HasColumnType("datetime");
            entity.Property(e => e.RequiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwProductList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwProductList");

            entity.Property(e => e.COGS).HasColumnType("money");
            entity.Property(e => e.CategoryCode).HasMaxLength(3);
            entity.Property(e => e.CategoryName).HasMaxLength(64);
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductName).HasMaxLength(64);
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.SellingPrice).HasColumnType("money");
            entity.Property(e => e.StockNumber).HasMaxLength(32);
        });

        modelBuilder.Entity<vwQtHeaderList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwQtHeaderList");

            entity.Property(e => e.ApprovedBy).HasMaxLength(64);
            entity.Property(e => e.ApprovedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(256);
            entity.Property(e => e.MaterialCost).HasMaxLength(128);
            entity.Property(e => e.MaterialName).HasMaxLength(128);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.PageHeight).HasMaxLength(32);
            entity.Property(e => e.PageWidth).HasMaxLength(32);
            entity.Property(e => e.PaperSheetSize).HasMaxLength(128);
            entity.Property(e => e.PaperSheetSizeAlias).HasMaxLength(128);
            entity.Property(e => e.PrintPerPageEx).HasMaxLength(64);
            entity.Property(e => e.PrintTitle).HasMaxLength(256);
            entity.Property(e => e.PrintsColor).HasMaxLength(64);
            entity.Property(e => e.PrintsQty).HasMaxLength(64);
            entity.Property(e => e.PrintsSize).HasMaxLength(64);
            entity.Property(e => e.QuotedBy).HasMaxLength(64);
            entity.Property(e => e.QuotedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.TotalCostA).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostB).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostC).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostD).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.UnitCostA).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostB).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostC).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostD).HasColumnType("decimal(10, 4)");
        });

        modelBuilder.Entity<vwQtItemList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwQtItemList");

            entity.Property(e => e.CostRounding).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.GroupNameChs).HasMaxLength(64);
            entity.Property(e => e.GroupNameCht).HasMaxLength(64);
            entity.Property(e => e.GroupNameEn).HasMaxLength(64);
            entity.Property(e => e.ItemGroupZone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ItemNameChs).HasMaxLength(64);
            entity.Property(e => e.ItemNameCht).HasMaxLength(64);
            entity.Property(e => e.ItemNameEn).HasMaxLength(64);
            entity.Property(e => e.Minimum).HasMaxLength(32);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Zone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<vwRptQuotation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRptQuotation");

            entity.Property(e => e.ApprovedBy).HasMaxLength(64);
            entity.Property(e => e.ApprovedOn).HasColumnType("datetime");
            entity.Property(e => e.CostA).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostB).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostC).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CostD).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CustomerName).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(64);
            entity.Property(e => e.MaterialCost).HasMaxLength(128);
            entity.Property(e => e.MaterialName).HasMaxLength(128);
            entity.Property(e => e.Minimum).HasMaxLength(32);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.PageHeight).HasMaxLength(32);
            entity.Property(e => e.PageWidth).HasMaxLength(32);
            entity.Property(e => e.PaperSheetSize).HasMaxLength(128);
            entity.Property(e => e.PaperSheetSizeAlias).HasMaxLength(128);
            entity.Property(e => e.PrintPerPageEx).HasMaxLength(64);
            entity.Property(e => e.PrintTitle).HasMaxLength(256);
            entity.Property(e => e.PrintsColor).HasMaxLength(64);
            entity.Property(e => e.PrintsQty).HasMaxLength(64);
            entity.Property(e => e.PrintsSize).HasMaxLength(64);
            entity.Property(e => e.QuotedBy).HasMaxLength(64);
            entity.Property(e => e.QuotedOn).HasColumnType("datetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
            entity.Property(e => e.TotalCostA).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostB).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostC).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.TotalCostD).HasColumnType("decimal(12, 4)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostA).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostB).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostC).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.UnitCostD).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Zone)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ZoneName).HasMaxLength(64);
        });

        modelBuilder.Entity<vwRtfHeaderList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRtfHeaderList");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerPO).HasMaxLength(16);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OrderedBy).HasMaxLength(32);
            entity.Property(e => e.OrderedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OriginalPO).HasMaxLength(16);
            entity.Property(e => e.OriginalSO).HasMaxLength(16);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(16);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RtfFileName).HasMaxLength(256);
            entity.Property(e => e.SalesOrder).HasMaxLength(16);
        });

        modelBuilder.Entity<vwRtfHeaderList_Active>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRtfHeaderList_Active");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.CustomerPO).HasMaxLength(16);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OrderedBy).HasMaxLength(32);
            entity.Property(e => e.OrderedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OriginalPO).HasMaxLength(16);
            entity.Property(e => e.OriginalSO).HasMaxLength(16);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(16);
            entity.Property(e => e.Remarks).HasMaxLength(512);
            entity.Property(e => e.RtfFileName).HasMaxLength(256);
            entity.Property(e => e.SalesOrder).HasMaxLength(16);
        });

        modelBuilder.Entity<vwRtfItemList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRtfItemList");

            entity.Property(e => e.Amount).HasMaxLength(16);
            entity.Property(e => e.CustomerPO).HasMaxLength(16);
            entity.Property(e => e.Discount).HasMaxLength(16);
            entity.Property(e => e.OrderedBy).HasMaxLength(32);
            entity.Property(e => e.OrderedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.OriginalPO).HasMaxLength(16);
            entity.Property(e => e.OriginalSO).HasMaxLength(16);
            entity.Property(e => e.Price).HasMaxLength(16);
            entity.Property(e => e.ProductCode).HasMaxLength(128);
            entity.Property(e => e.ProductDescription).HasMaxLength(256);
            entity.Property(e => e.PurchaseOrder).HasMaxLength(16);
            entity.Property(e => e.Qty).HasMaxLength(16);
            entity.Property(e => e.SalesOrder).HasMaxLength(16);
        });

        modelBuilder.Entity<vwSupplierList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwSupplierList");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.SupplierName).HasMaxLength(64);
        });

        modelBuilder.Entity<vwSupplierList_Active>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwSupplierList_Active");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.LoginAccount).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.SupplierName).HasMaxLength(64);
        });

        modelBuilder.Entity<vwUserList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwUserList");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("smalldatetime");
            entity.Property(e => e.UserAlias).HasMaxLength(64);
            entity.Property(e => e.UserName).HasMaxLength(64);
            entity.Property(e => e.UserPassword).HasMaxLength(64);
        });

        modelBuilder.Entity<vwUserList_Active>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwUserList_Active");

            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("smalldatetime");
            entity.Property(e => e.UserAlias).HasMaxLength(64);
            entity.Property(e => e.UserName).HasMaxLength(64);
            entity.Property(e => e.UserPassword).HasMaxLength(64);
        });

        modelBuilder.Entity<vwUserNotificationList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwUserNotificationList");

            entity.Property(e => e.Alias).HasMaxLength(64);
            entity.Property(e => e.AuthXml).HasColumnType("xml");
            entity.Property(e => e.CreatedBy).HasMaxLength(64);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.LoginName).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.ModifiedBy).HasMaxLength(64);
            entity.Property(e => e.ModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.NotifyXml).HasColumnType("xml");
            entity.Property(e => e.RetiredBy).HasMaxLength(64);
            entity.Property(e => e.RetiredOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<vwUserPreferenceList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwUserPreferenceList");

            entity.Property(e => e.Alias).HasMaxLength(64);
            entity.Property(e => e.LoginName).HasMaxLength(64);
            entity.Property(e => e.LoginPassword).HasMaxLength(64);
            entity.Property(e => e.MetadataXml).HasColumnType("xml");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
