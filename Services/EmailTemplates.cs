namespace PingCRM.Services;

public static class EmailTemplates
{
    public static string PasswordReset(string resetUrl)
    {
        return Wrap("Reset Your Password", $"""
            <p>You are receiving this email because we received a password reset request for your account.</p>
            <p style="text-align: center; margin: 30px 0;">
                <a href="{resetUrl}" style="background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600;">
                    Reset Password
                </a>
            </p>
            <p>This password reset link will expire in 60 minutes.</p>
            <p>If you did not request a password reset, no further action is required.</p>
            """);
    }

    public static string EmailVerification(string verifyUrl)
    {
        return Wrap("Verify Your Email Address", $"""
            <p>Please click the button below to verify your email address.</p>
            <p style="text-align: center; margin: 30px 0;">
                <a href="{verifyUrl}" style="background-color: #4f46e5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600;">
                    Verify Email Address
                </a>
            </p>
            <p>If you did not create an account, no further action is required.</p>
            """);
    }

    private static string Wrap(string title, string content)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px; color: #374151;">
                <h2 style="color: #111827;">{title}</h2>
                {content}
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;" />
                <p style="font-size: 12px; color: #9ca3af;">This email was sent by PingCRM.</p>
            </body>
            </html>
            """;
    }
}
