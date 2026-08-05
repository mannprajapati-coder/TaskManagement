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

CREATE TABLE [Workspaces] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [OwnerUserId] uniqueidentifier NOT NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Workspaces] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [WorkspaceInvites] (
    [Id] uniqueidentifier NOT NULL,
    [WorkspaceId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(450) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [MaxUses] int NOT NULL,
    [UseCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkspaceInvites] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkspaceInvites_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WorkspaceMembers] (
    [Id] uniqueidentifier NOT NULL,
    [WorkspaceId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkspaceMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkspaceMembers_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_WorkspaceInvites_TokenHash] ON [WorkspaceInvites] ([TokenHash]);
GO

CREATE INDEX [IX_WorkspaceInvites_WorkspaceId] ON [WorkspaceInvites] ([WorkspaceId]);
GO

CREATE UNIQUE INDEX [IX_WorkspaceMembers_WorkspaceId_UserId] ON [WorkspaceMembers] ([WorkspaceId], [UserId]);
GO

CREATE INDEX [IX_Workspaces_OwnerUserId] ON [Workspaces] ([OwnerUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260805071733_InitialWorkspacesSchema', N'8.0.11');
GO

COMMIT;
GO

