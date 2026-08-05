namespace TaskPlatform.Shared.Constants
{
    public static class ApiEndpoint
    {
        public static class Auth
        {
            public const string Register = "api/v1/Auth/Register";
            public const string VerifyEmail = "api/v1/Auth/VerifyEmail";
            public const string Login = "api/v1/Auth/Login";
            public const string RefreshToken = "api/v1/Auth/RefreshToken";
            public const string ForgotPassword = "api/v1/Auth/ForgotPassword";
            public const string ResetPassword = "api/v1/Auth/ResetPassword";
            public const string GoogleLogin = "api/v1/Auth/GoogleLogin";
            public const string EnableMfa = "api/v1/Auth/EnableMfa";
            public const string VerifyMfa = "api/v1/Auth/VerifyMfa";
            public const string Logout = "api/v1/Auth/Logout";
        }
    }
}
