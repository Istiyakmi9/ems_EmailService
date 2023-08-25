using ModalLayer.Modal;

namespace EmailRequest.EMailService.Interface
{
    public interface IEMailManager
    {
        Task SendMailAsync(EmailSenderModal emailSenderModal);
        List<InboxMailDetail> ReadMails(EmailSettingDetail emailSettingDetail);
    }
}
