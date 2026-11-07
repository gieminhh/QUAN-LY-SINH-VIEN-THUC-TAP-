using System.Net;
using System.Net.Mail;
namespace InternshipManagement.Web.Services;

public class PasswordRecoveryMailer(IConfiguration config, IWebHostEnvironment env)
{
    public async Task SendAsync(string email, string token)
    {
        var baseUrl = config["Mail:BaseUrl"] ?? (env.IsDevelopment() ? "http://localhost:5064" : throw new InvalidOperationException("Mail:BaseUrl chưa cấu hình"));
        var url = baseUrl.TrimEnd('/') + "/Account/ResetPassword?email=" + Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token);
        var text = "Yêu cầu đặt lại mật khẩu. Mở liên kết sau: " + url + "\nNếu bạn không yêu cầu, hãy bỏ qua email này.";
        if (env.IsDevelopment() && string.IsNullOrWhiteSpace(config["Mail:Host"]))
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data", "MailOutbox"); Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, Guid.NewGuid() + ".txt"), "To: " + email + "\n" + text); return;
        }
        using var smtp = new SmtpClient(config["Mail:Host"] ?? throw new InvalidOperationException("Chưa cấu hình SMTP"), config.GetValue<int?>("Mail:Port") ?? 587) { EnableSsl = true, Credentials = new NetworkCredential(config["Mail:User"], config["Mail:Password"]) };
        using var message = new MailMessage(config["Mail:From"]!, email, "Khôi phục mật khẩu USMART", text);
        await smtp.SendMailAsync(message);
    }
}
