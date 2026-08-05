using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Modules.UserManagement.Domain.Entities;
using Modules.UserManagement.Domain.IServices;
using Modules.UserManagement.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.User;

namespace Modules.UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManagementDbContext _dbContext;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(UserManagementDbContext dbContext)
        {
            _dbContext = dbContext;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<UserProfileViewModel> GetProfileAsync(Guid userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found.");
            }

            return MapToProfile(user);
        }

        public async Task<UserProfileViewModel> UpdateProfileAsync(Guid userId, UpdateProfileRequestViewModel model)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found.");
            }

            user.FullName = model.FullName;
            user.Bio = model.Bio;
            user.JobTitle = model.JobTitle;

            await _dbContext.SaveChangesAsync();
            return MapToProfile(user);
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestViewModel model)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found.");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", model.CurrentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                throw new DomainException("Current password is incorrect.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<UserPreferenceViewModel> GetPreferencesAsync(Guid userId)
        {
            var prefs = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (prefs == null)
            {
                return new UserPreferenceViewModel();
            }

            return new UserPreferenceViewModel
            {
                TimeZone = prefs.TimeZone,
                Language = prefs.Language,
                NotificationChannelPrefsJson = prefs.NotificationChannelPrefsJson
            };
        }

        public async Task<UserPreferenceViewModel> UpdatePreferencesAsync(Guid userId, UserPreferenceViewModel model)
        {
            var prefs = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
            if (prefs == null)
            {
                prefs = new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TimeZone = model.TimeZone,
                    Language = model.Language,
                    NotificationChannelPrefsJson = model.NotificationChannelPrefsJson,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.UserPreferences.Add(prefs);
            }
            else
            {
                prefs.TimeZone = model.TimeZone;
                prefs.Language = model.Language;
                prefs.NotificationChannelPrefsJson = model.NotificationChannelPrefsJson;
                prefs.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return new UserPreferenceViewModel
            {
                TimeZone = prefs.TimeZone,
                Language = prefs.Language,
                NotificationChannelPrefsJson = prefs.NotificationChannelPrefsJson
            };
        }

        public async Task<List<ActiveSessionViewModel>> GetActiveSessionsAsync(Guid userId, string? currentTokenHash = null)
        {
            var sessions = await _dbContext.ActiveSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LastSeenAt)
                .ToListAsync();

            return sessions.Select(s => new ActiveSessionViewModel
            {
                SessionId = s.Id.ToString(),
                DeviceInfo = s.DeviceInfo,
                IpAddress = s.IpAddress,
                LastSeenAt = s.LastSeenAt,
                IsCurrentSession = s.Id.ToString() == currentTokenHash
            }).ToList();
        }

        public async Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId)
        {
            var session = await _dbContext.ActiveSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session != null)
            {
                _dbContext.ActiveSessions.Remove(session);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        private static UserProfileViewModel MapToProfile(User user)
        {
            return new UserProfileViewModel
            {
                UserId = user.Id.ToString(),
                Email = user.Email ?? "",
                FullName = user.FullName,
                Bio = user.Bio,
                JobTitle = user.JobTitle,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsEmailVerified = user.IsEmailVerified
            };
        }
    }
}
