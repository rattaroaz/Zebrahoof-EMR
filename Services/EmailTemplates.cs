namespace Zebrahoof_EMR.Services;

public static class EmailTemplates
{
    public static string PasswordReset(string userName, string resetLink, string expiresIn = "24 hours") => $"""
        Subject: Reset Your Zebrahoof EMR Password

        Hello {userName},

        We received a request to reset your password for your Zebrahoof EMR account.

        Click the link below to reset your password:
        {resetLink}

        This link will expire in {expiresIn}.

        If you didn't request this password reset, please ignore this email or contact support if you have concerns.

        Thank you,
        Zebrahoof EMR Team
        """;

    public static string EmailVerification(string userName, string verificationLink) => $"""
        Subject: Verify Your Zebrahoof EMR Account

        Hello {userName},

        Thank you for registering with Zebrahoof EMR Patient Portal.

        Please verify your email address by clicking the link below:
        {verificationLink}

        If you didn't create this account, please ignore this email.

        Thank you,
        Zebrahoof EMR Team
        """;

    public static string AccountLockout(string userName, string unlockTime, string supportContact = "support@zebrahoof.com") => $"""
        Subject: Zebrahoof EMR Account Locked

        Hello {userName},

        Your Zebrahoof EMR account has been temporarily locked due to multiple failed login attempts.

        Your account will be automatically unlocked at: {unlockTime}

        If you did not attempt these logins, please contact us immediately at {supportContact}.

        Thank you,
        Zebrahoof EMR Security Team
        """;

    public static string PasswordChanged(string userName) => $"""
        Subject: Your Zebrahoof EMR Password Was Changed

        Hello {userName},

        This email confirms that your Zebrahoof EMR password was successfully changed.

        If you did not make this change, please contact support immediately and reset your password.

        Thank you,
        Zebrahoof EMR Security Team
        """;

    public static string MfaEnabled(string userName) => $"""
        Subject: Two-Factor Authentication Enabled

        Hello {userName},

        Two-factor authentication has been enabled on your Zebrahoof EMR account.

        From now on, you'll need your authenticator app to sign in.

        If you did not enable this feature, please contact support immediately.

        Thank you,
        Zebrahoof EMR Security Team
        """;

    public static string MfaDisabled(string userName) => $"""
        Subject: Two-Factor Authentication Disabled

        Hello {userName},

        Two-factor authentication has been disabled on your Zebrahoof EMR account.

        If you did not make this change, please contact support immediately.

        Thank you,
        Zebrahoof EMR Security Team
        """;

    public static string NewLoginAlert(string userName, string deviceInfo, string location, string time) => $"""
        Subject: New Login to Your Zebrahoof EMR Account

        Hello {userName},

        A new login to your account was detected:

        Device: {deviceInfo}
        Location: {location}
        Time: {time}

        If this was you, no action is needed.

        If you don't recognize this activity, please change your password immediately and contact support.

        Thank you,
        Zebrahoof EMR Security Team
        """;
}

public static class SmsTemplates
{
    public static string PasswordResetCode(string code) =>
        $"Your Zebrahoof EMR password reset code is: {code}. This code expires in 15 minutes.";

    public static string MfaCode(string code) =>
        $"Your Zebrahoof EMR verification code is: {code}. Do not share this code with anyone.";

    public static string AccountLockout(string unlockTime) =>
        $"Your Zebrahoof EMR account has been locked. It will unlock at {unlockTime}. Contact support if needed.";

    public static string AppointmentReminder(string patientName, string appointmentTime, string provider) =>
        $"Hi {patientName}, reminder: Your appointment with {provider} is scheduled for {appointmentTime}.";
}
