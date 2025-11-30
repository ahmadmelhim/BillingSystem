using System;

namespace BillingSystem.Infrastructure.Services.Email
{
    public static class EmailTemplateHelper
    {
        public static string GenerateEmailTemplate(string title, string content, string? actionUrl = null, string? actionText = null)
        {
            var actionButton = string.IsNullOrEmpty(actionUrl) || string.IsNullOrEmpty(actionText)
                ? ""
                : $@"
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{actionUrl}' style='background-color: #1a237e; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>
                            {actionText}
                        </a>
                    </div>";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1a237e 0%, #0d47a1 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; }}
        .content {{ padding: 40px 30px; background-color: #ffffff; }}
        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; border-top: 1px solid #eee; }}
        .info-box {{ background-color: #e3f2fd; border-left: 4px solid #1565c0; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div style='padding: 20px;'>
        <div class='container'>
            <div class='header'>
                <h1>Billing System</h1>
            </div>
            <div class='content'>
                <h2 style='color: #1a237e; margin-top: 0;'>{title}</h2>
                <div style='font-size: 16px; color: #444;'>
                    {content}
                </div>
                {actionButton}
                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #666;'>
                    <p>If you have any questions, please contact our support team.</p>
                </div>
            </div>
            <div class='footer'>
                <p>&copy; {DateTime.Now.Year} Billing System. All rights reserved.</p>
                <p>
                    <a href='mailto:ahmadmelhem1q@gmail.com' style='color: #666; text-decoration: none;'>ahmadmelhem1q@gmail.com</a> | 
                    <a href='tel:+972595569887' style='color: #666; text-decoration: none;'>+972 59-556-9887</a>
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}

