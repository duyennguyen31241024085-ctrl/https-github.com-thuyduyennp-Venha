using System.Net;
using System.Net.Mail;

namespace MSSV_HoVaTen.Extension;

public static class EmailServiceNew
{
    public static async Task SendAsync(IConfiguration configuration, ILogger logger, string recipient, int orderId, decimal total)
    {
        var address = configuration["Email:Address"];
        var password = configuration["Email:AppPassword"];
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(password)) return;

        try
        {
            using var mail = new MailMessage(address, recipient, $"Framebox | Đơn hàng #{orderId}",
                $"Framebox đã nhận đơn #{orderId}. Tổng tiền: {total:N0} VND.");
            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(address, password.Replace(" ", ""))
            };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await smtp.SendMailAsync(mail, timeout.Token);
        }
        catch (Exception error) when (error is SmtpException or ArgumentException or OperationCanceledException or FormatException)
        {
            logger.LogWarning("Không gửi được email cho đơn {OrderId} ({ErrorType}).", orderId, error.GetType().Name);
        }
    }
}
