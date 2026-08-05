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

CREATE TABLE [Projects] (
    [Id] uniqueidentifier NOT NULL,
    [WorkspaceId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [Budget] decimal(18,2) NULL,
    [Client] nvarchar(max) NULL,
    [ProjectManagerUserId] uniqueidentifier NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ProjectFavorites] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectFavorites] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectFavorites_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProjectJoinRequests] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [RequestingUserId] uniqueidentifier NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ResolvedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectJoinRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectJoinRequests_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProjectMembers] (
    [Id] uniqueidentifier NOT NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ProjectScopedRole] nvarchar(max) NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectMembers_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_ProjectFavorites_ProjectId_UserId] ON [ProjectFavorites] ([ProjectId], [UserId]);
GO

CREATE INDEX [IX_ProjectJoinRequests_ProjectId_RequestingUserId] ON [ProjectJoinRequests] ([ProjectId], [RequestingUserId]);
GO

CREATE UNIQUE INDEX [IX_ProjectMembers_ProjectId_UserId] ON [ProjectMembers] ([ProjectId], [UserId]);
GO

CREATE INDEX [IX_Projects_WorkspaceId] ON [Projects] ([WorkspaceId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805082602_AddProjectsAndMembersSchema', N'8.0.11');
GO

COMMIT;
GO

