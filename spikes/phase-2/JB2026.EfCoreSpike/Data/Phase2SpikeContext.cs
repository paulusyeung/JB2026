using System;
using System.Collections.Generic;
using JB2026.EfCoreSpike.Models;
using Microsoft.EntityFrameworkCore;

namespace JB2026.EfCoreSpike.Data;

public partial class Phase2SpikeContext : DbContext
{
    public Phase2SpikeContext(DbContextOptions<Phase2SpikeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<JobAttachment> JobAttachments { get; set; }

    public virtual DbSet<JobOrder> JobOrders { get; set; }

    public virtual DbSet<JobSchedule> JobSchedules { get; set; }

    public virtual DbSet<JobWorkflow> JobWorkflows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__JobAttac__442C64BE124F7110");

            entity.ToTable("JobAttachment");

            entity.Property(e => e.AttachmentId).ValueGeneratedNever();
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.JobAttachments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_JobAttachment_JobOrder");
        });

        modelBuilder.Entity<JobOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__JobOrder__C3905BCF60EA2D79");

            entity.ToTable("JobOrder");

            entity.Property(e => e.OrderId).ValueGeneratedNever();
            entity.Property(e => e.CustomerName).HasMaxLength(128);
            entity.Property(e => e.CustomerRef).HasMaxLength(64);
            entity.Property(e => e.OrderNumber).HasMaxLength(32);
            entity.Property(e => e.OrderTitle).HasMaxLength(200);
            entity.Property(e => e.OrderedBy).HasMaxLength(64);
            entity.Property(e => e.ProductCode).HasMaxLength(32);
            entity.Property(e => e.ProductStyle).HasMaxLength(128);
            entity.Property(e => e.Qty).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Remarks).HasMaxLength(1000);
        });

        modelBuilder.Entity<JobSchedule>(entity =>
        {
            entity.HasKey(e => e.JobScheduleId).HasName("PK__JobSched__2EB48C454B7EB0CD");

            entity.ToTable("JobSchedule");

            entity.Property(e => e.JobScheduleId).ValueGeneratedNever();
            entity.Property(e => e.MachineNumber).HasMaxLength(32);

            entity.HasOne(d => d.Order).WithMany(p => p.JobSchedules)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSchedule_JobOrder");
        });

        modelBuilder.Entity<JobWorkflow>(entity =>
        {
            entity.HasKey(e => e.JobWorkflowId).HasName("PK__JobWorkf__A9D5B79F075806EC");

            entity.ToTable("JobWorkflow");

            entity.Property(e => e.JobWorkflowId).ValueGeneratedNever();

            entity.HasOne(d => d.Order).WithMany(p => p.JobWorkflows)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobWorkflow_JobOrder");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
