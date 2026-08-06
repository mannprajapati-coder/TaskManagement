-- =====================================================================
-- Task Management Platform - Database Reset & Data Purge Script
-- Usage: Run this script against the TaskManagement database to 
-- delete all test data safely and reset auto-increments/entities.
-- =====================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

USE [TaskManagement];
GO

PRINT 'Starting TaskManagement Database Data Purge...';

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Disable all foreign key constraints temporarily
    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
    PRINT '[1/4] Foreign key constraints disabled.';

    -- 2. Delete all records from child/leaf tables in logical order
    DELETE FROM [ChecklistItems];
    DELETE FROM [RecurringTaskRules];
    DELETE FROM [TaskWatchers];
    DELETE FROM [TaskAssignees];
    DELETE FROM [Tasks];
    PRINT '[2/4] Task & sub-item tables cleared.';

    DELETE FROM [ProjectFavorites];
    DELETE FROM [ProjectJoinRequests];
    DELETE FROM [ProjectMembers];
    DELETE FROM [Projects];
    PRINT '[2/4] Project & membership tables cleared.';

    DELETE FROM [WorkspaceInvites];
    DELETE FROM [WorkspaceMembers];
    DELETE FROM [Workspaces];
    PRINT '[2/4] Workspace & invite tables cleared.';

    DELETE FROM [ActiveSessions];
    DELETE FROM [UserPreferences];
    DELETE FROM [RefreshTokens];
    DELETE FROM [PasswordResetTokens];
    DELETE FROM [EmailVerificationTokens];
    DELETE FROM [MfaSecrets];
    DELETE FROM [AspNetUserClaims];
    DELETE FROM [AspNetUserRoles];
    DELETE FROM [AspNetUserLogins];
    DELETE FROM [AspNetUserTokens];
    DELETE FROM [AspNetRoleClaims];
    DELETE FROM [Users];
    PRINT '[2/4] Auth, user profile, and session tables cleared.';

    -- 3. Seed default roles if they do not exist
    IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [Name] = 'Admin')
    BEGIN
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    END

    IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [Name] = 'ProjectManager')
    BEGIN
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (NEWID(), 'ProjectManager', 'PROJECTMANAGER', NEWID());
    END

    IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [Name] = 'Member')
    BEGIN
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (NEWID(), 'Member', 'MEMBER', NEWID());
    END
    PRINT '[3/4] Default system roles ensured (Admin, ProjectManager, Member).';

    -- 4. Re-enable all foreign key constraints
    EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
    PRINT '[4/4] Foreign key constraints re-enabled successfully.';

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: TaskManagement database has been completely reset to a clean state!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: Database reset failed! Details below:';
    PRINT ERROR_MESSAGE();
END CATCH;
GO
