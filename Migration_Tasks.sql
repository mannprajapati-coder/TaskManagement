IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Tasks] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(450) NOT NULL,
    [Priority] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NULL,
    [DueDate] datetime2 NULL,
    [EstimatedHours] decimal(18,2) NULL,
    [ActualHours] decimal(18,2) NULL,
    [PrimaryAssigneeUserId] uniqueidentifier NULL,
    [CreatorUserId] uniqueidentifier NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Tasks_PrimaryAssigneeUserId] ON [Tasks] ([PrimaryAssigneeUserId]);
GO

CREATE INDEX [IX_Tasks_ProjectId] ON [Tasks] ([ProjectId]);
GO

CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805083009_AddCoreTasksSchema', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Tasks] ADD [ParentTaskId] uniqueidentifier NULL;
GO

CREATE TABLE [TaskAssignees] (
    [Id] uniqueidentifier NOT NULL,
    [TaskId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [IsPrimary] bit NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskAssignees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskAssignees_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TaskWatchers] (
    [Id] uniqueidentifier NOT NULL,
    [TaskId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [WatchingSince] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskWatchers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskWatchers_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Tasks_ParentTaskId] ON [Tasks] ([ParentTaskId]);
GO

CREATE UNIQUE INDEX [IX_TaskAssignees_TaskId_UserId] ON [TaskAssignees] ([TaskId], [UserId]);
GO

CREATE UNIQUE INDEX [IX_TaskWatchers_TaskId_UserId] ON [TaskWatchers] ([TaskId], [UserId]);
GO

ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Tasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [Tasks] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805083403_AddSubtasksAndAssignmentsSchema', N'8.0.11');
GO

COMMIT;
GO

