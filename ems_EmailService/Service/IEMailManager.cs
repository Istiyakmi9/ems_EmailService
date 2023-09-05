using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using EmailRequest.EMailService.Service;

namespace EmailRequest.EMailService.Interface
{
    public interface IEMailManager
    {
        Task SendMailAsync(EmailSenderModal emailSenderModal);
        List<InboxMailDetail> ReadMails(EmailSettingDetail emailSettingDetail);
    }
}
