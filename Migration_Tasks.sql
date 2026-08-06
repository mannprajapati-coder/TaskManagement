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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083009_AddCoreTasksSchema'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083009_AddCoreTasksSchema'
)
BEGIN
    CREATE INDEX [IX_Tasks_PrimaryAssigneeUserId] ON [Tasks] ([PrimaryAssigneeUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083009_AddCoreTasksSchema'
)
BEGIN
    CREATE INDEX [IX_Tasks_ProjectId] ON [Tasks] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083009_AddCoreTasksSchema'
)
BEGIN
    CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083009_AddCoreTasksSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805083009_AddCoreTasksSchema', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    ALTER TABLE [Tasks] ADD [ParentTaskId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    CREATE TABLE [TaskAssignees] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [IsPrimary] bit NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskAssignees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskAssignees_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    CREATE TABLE [TaskWatchers] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [WatchingSince] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskWatchers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskWatchers_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    CREATE INDEX [IX_Tasks_ParentTaskId] ON [Tasks] ([ParentTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaskAssignees_TaskId_UserId] ON [TaskAssignees] ([TaskId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaskWatchers_TaskId_UserId] ON [TaskWatchers] ([TaskId], [UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Tasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [Tasks] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805083403_AddSubtasksAndAssignmentsSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805083403_AddSubtasksAndAssignmentsSchema', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805084727_AddChecklistsAndRecurringSchema'
)
BEGIN
    CREATE TABLE [ChecklistItems] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [IsCompleted] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChecklistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChecklistItems_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805084727_AddChecklistsAndRecurringSchema'
)
BEGIN
    CREATE TABLE [RecurringTaskRules] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [RecurrencePattern] nvarchar(max) NOT NULL,
        [Interval] int NOT NULL,
        [DaysOfWeek] nvarchar(max) NULL,
        [NextRunDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RecurringTaskRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecurringTaskRules_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805084727_AddChecklistsAndRecurringSchema'
)
BEGIN
    CREATE INDEX [IX_ChecklistItems_TaskId] ON [ChecklistItems] ([TaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805084727_AddChecklistsAndRecurringSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RecurringTaskRules_TaskId] ON [RecurringTaskRules] ([TaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805084727_AddChecklistsAndRecurringSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805084727_AddChecklistsAndRecurringSchema', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805121957_AddTaskSortOrder'
)
BEGIN
    ALTER TABLE [Tasks] ADD [SortOrder] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805121957_AddTaskSortOrder'
)
BEGIN
    CREATE INDEX [IX_Tasks_ProjectId_Status_SortOrder] ON [Tasks] ([ProjectId], [Status], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805121957_AddTaskSortOrder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805121957_AddTaskSortOrder', N'8.0.11');
END;
GO

COMMIT;
GO

