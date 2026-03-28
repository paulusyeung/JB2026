IF DB_ID(N'JB2026_Phase2Spike') IS NOT NULL
BEGIN
    ALTER DATABASE [JB2026_Phase2Spike] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [JB2026_Phase2Spike];
END
GO

CREATE DATABASE [JB2026_Phase2Spike];
GO

USE [JB2026_Phase2Spike];
GO

CREATE TABLE [dbo].[JobOrder] (
    [OrderId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [OrderType] INT NOT NULL,
    [OrderNumber] NVARCHAR(32) NOT NULL,
    [JobNumber] INT NULL,
    [CustomerName] NVARCHAR(128) NOT NULL,
    [CustomerRef] NVARCHAR(64) NULL,
    [OrderTitle] NVARCHAR(200) NOT NULL,
    [ProductCode] NVARCHAR(32) NULL,
    [ProductStyle] NVARCHAR(128) NULL,
    [OrderedOn] DATETIME2 NULL,
    [OrderedBy] NVARCHAR(64) NOT NULL,
    [RequiredOn] DATETIME2 NULL,
    [Remarks] NVARCHAR(1000) NULL,
    [Qty] DECIMAL(12, 2) NULL,
    [Status] INT NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL,
    [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
    [ModifiedOn] DATETIME2 NOT NULL,
    [ModifiedBy] UNIQUEIDENTIFIER NOT NULL,
    [Retired] BIT NOT NULL DEFAULT 0,
    [RetiredOn] DATETIME2 NULL,
    [RetiredBy] UNIQUEIDENTIFIER NULL
);
GO

CREATE TABLE [dbo].[JobAttachment] (
    [AttachmentId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [OrderId] UNIQUEIDENTIFIER NULL,
    [AttachmentType] INT NOT NULL,
    [AttachmentIndex] INT NOT NULL,
    [OriginalFileName] NVARCHAR(255) NOT NULL,
    CONSTRAINT [FK_JobAttachment_JobOrder] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[JobOrder]([OrderId])
);
GO

CREATE TABLE [dbo].[JobSchedule] (
    [JobScheduleId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [OrderId] UNIQUEIDENTIFIER NOT NULL,
    [MachineNumber] NVARCHAR(32) NULL,
    [ScheduledOn] DATETIME2 NULL,
    [CompletedOn] DATETIME2 NULL,
    [Status] INT NOT NULL,
    [Priority] INT NOT NULL,
    CONSTRAINT [FK_JobSchedule_JobOrder] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[JobOrder]([OrderId])
);
GO

CREATE TABLE [dbo].[JobWorkflow] (
    [JobWorkflowId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [OrderId] UNIQUEIDENTIFIER NOT NULL,
    [WorkStatus] INT NOT NULL,
    [WorkIndex] INT NOT NULL,
    [WorkNotes] NVARCHAR(MAX) NULL,
    CONSTRAINT [FK_JobWorkflow_JobOrder] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[JobOrder]([OrderId])
);
GO

INSERT INTO [dbo].[JobOrder] (
    [OrderId], [OrderType], [OrderNumber], [JobNumber], [CustomerName], [CustomerRef], [OrderTitle],
    [ProductCode], [ProductStyle], [OrderedOn], [OrderedBy], [RequiredOn], [Remarks], [Qty], [Status],
    [CreatedOn], [CreatedBy], [ModifiedOn], [ModifiedBy], [Retired], [RetiredOn], [RetiredBy])
VALUES
(
    '1e84b2e5-3f73-4d60-9d0d-08dc50c00001', 6, 'JB2401', 12, 'Acme Retail', 'ACM-4471', 'Spring launch flyer',
    'FLY-A4', 'Flyer Matte A4', '2026-03-25T10:15:00', 'mchan', '2026-03-29T09:00:00', 'Requires colour proof before press release.', 1500, 2,
    '2026-03-25T08:00:00', 'f31c57ea-7f08-4a05-b5b5-58b2cdab1001', '2026-03-25T10:15:00', 'f31c57ea-7f08-4a05-b5b5-58b2cdab1001', 0, NULL, NULL
),
(
    '1e84b2e5-3f73-4d60-9d0d-08dc50c00002', 7, 'JB2403', 3, 'Northwind Foods', 'NWF-8820', 'Shelf wobblers refresh',
    'WOB-01', 'Diecut Wobbler', '2026-03-21T08:30:00', 'ajohnson', '2026-03-27T15:00:00', 'Keep dieline unchanged from February release.', 3200, 1,
    '2026-03-21T08:00:00', 'f31c57ea-7f08-4a05-b5b5-58b2cdab1001', '2026-03-21T08:30:00', 'f31c57ea-7f08-4a05-b5b5-58b2cdab1001', 0, NULL, NULL
);
GO

INSERT INTO [dbo].[JobAttachment] ([AttachmentId], [OrderId], [AttachmentType], [AttachmentIndex], [OriginalFileName])
VALUES
('2f84b2e5-3f73-4d60-9d0d-08dc50c00001', '1e84b2e5-3f73-4d60-9d0d-08dc50c00001', 1, 1, 'spring-flyer-proof.pdf'),
('2f84b2e5-3f73-4d60-9d0d-08dc50c00002', '1e84b2e5-3f73-4d60-9d0d-08dc50c00001', 2, 2, 'acme-brand-guidelines.pdf');
GO

INSERT INTO [dbo].[JobSchedule] ([JobScheduleId], [OrderId], [MachineNumber], [ScheduledOn], [CompletedOn], [Status], [Priority])
VALUES
('3f84b2e5-3f73-4d60-9d0d-08dc50c00001', '1e84b2e5-3f73-4d60-9d0d-08dc50c00001', 'MACHINE-A', '2026-03-28T08:00:00', NULL, 2, 1),
('3f84b2e5-3f73-4d60-9d0d-08dc50c00002', '1e84b2e5-3f73-4d60-9d0d-08dc50c00002', 'MACHINE-B', '2026-03-26T14:00:00', NULL, 1, 2);
GO

INSERT INTO [dbo].[JobWorkflow] ([JobWorkflowId], [OrderId], [WorkStatus], [WorkIndex], [WorkNotes])
VALUES
('4f84b2e5-3f73-4d60-9d0d-08dc50c00001', '1e84b2e5-3f73-4d60-9d0d-08dc50c00001', 1, 10, 'Proof approved and queued for print.'),
('4f84b2e5-3f73-4d60-9d0d-08dc50c00002', '1e84b2e5-3f73-4d60-9d0d-08dc50c00002', 0, 20, 'Waiting for customer sign-off.');
GO

CREATE OR ALTER PROCEDURE [dbo].[spJobAttachment_SelRec]
    @AttachmentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        [AttachmentId],
        [OrderId],
        [AttachmentType],
        [AttachmentIndex],
        [OriginalFileName]
    FROM [dbo].[JobAttachment]
    WHERE [AttachmentId] = @AttachmentId;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spJobAttachment_InsRec]
    @AttachmentId UNIQUEIDENTIFIER OUTPUT,
    @OrderId UNIQUEIDENTIFIER,
    @AttachmentType INT,
    @AttachmentIndex INT,
    @OriginalFileName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GeneratedId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[JobAttachment] (
        [AttachmentId],
        [OrderId],
        [AttachmentType],
        [AttachmentIndex],
        [OriginalFileName])
    VALUES (
        @GeneratedId,
        @OrderId,
        @AttachmentType,
        @AttachmentIndex,
        @OriginalFileName);

    SET @AttachmentId = @GeneratedId;
END
GO