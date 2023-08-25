using ModalLayer.Modal;

namespace EmailRequest.Service
{
    public interface IEmailService
    {
        Task SendEmail(EmailSenderModal emailSenderModal);
    }
}
