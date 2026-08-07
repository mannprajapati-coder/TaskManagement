# Script to update local SQL Server database for TaskPlatform across all modules
Write-Host "Updating TaskPlatform Database (Server=localhost;Database=TaskManagement)..." -ForegroundColor Cyan

Write-Host "`n1. Updating Authentication Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Authentication --startup-project TaskPlatform.Api --context AuthenticationDbContext

Write-Host "`n2. Updating User Management Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/UserManagement --startup-project TaskPlatform.Api --context UserManagementDbContext

Write-Host "`n3. Updating Workspaces Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Workspaces --startup-project TaskPlatform.Api --context WorkspacesDbContext

Write-Host "`n4. Updating Projects Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Projects --startup-project TaskPlatform.Api --context ProjectsDbContext

Write-Host "`n5. Updating Tasks Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Tasks --startup-project TaskPlatform.Api --context TasksDbContext

Write-Host "`n6. Updating Collaboration Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Collaboration --startup-project TaskPlatform.Api --context CollaborationDbContext

Write-Host "`n7. Updating Notifications Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/Notifications --startup-project TaskPlatform.Api --context NotificationsDbContext

Write-Host "`n8. Updating Time Tracking Module..." -ForegroundColor Yellow
dotnet ef database update --project Modules/TimeTracking --startup-project TaskPlatform.Api --context TimeTrackingDbContext

Write-Host "`nAll Database Migrations Applied Successfully!" -ForegroundColor Green
