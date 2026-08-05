using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlatform.Shared.ViewModels.User
{
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? JobTitle { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsEmailVerified { get; set; }
    }

    public class UpdateProfileRequestViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        [StringLength(100, ErrorMessage = "Job Title cannot exceed 100 characters.")]
        public string? JobTitle { get; set; }
    }

    public class ChangePasswordRequestViewModel
    {
        [Required(ErrorMessage = "Current Password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm New Password is required.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserPreferenceViewModel
    {
        public string TimeZone { get; set; } = "UTC";
        public string Language { get; set; } = "en";
        public string NotificationChannelPrefsJson { get; set; } = "{}";
    }

    public class ActiveSessionViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastSeenAt { get; set; }
        public bool IsCurrentSession { get; set; }
    }
}
