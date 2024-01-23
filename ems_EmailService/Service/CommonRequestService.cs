using Bot.CoreBottomHalf.CommonModal;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;

namespace EmailRequest.Service
{
    public class CommonRequestService
    {
        private readonly IEmailService _emailService;
        public CommonRequestService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendEmailNotification(CommonFields commonFields)
        {
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = "EMSTUM Application Exception";
            emailSenderModal.Subject = "Exception message and reason";
            emailSenderModal.To = new List<string> { "marghub12@gmail.com", "istiyaq.mi9@gmail.com" };
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.CommonException;
            emailSenderModal.Body = html.Replace("__BODY__", commonFields.Body);

            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
