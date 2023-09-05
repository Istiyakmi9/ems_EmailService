using Bot.CoreBottomHalf.CommonModal;

namespace EmailRequest.Service
{
    public interface IEmailService
    {
        Task SendEmail(EmailSenderModal emailSenderModal);
    }
}
