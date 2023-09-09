using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class ForgotPasswordRequested
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordRequested> _logger;
        public ForgotPasswordRequested(IDb db, 
            IEmailService emailService, 
            ILogger<ForgotPasswordRequested> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        private void ValidateModal(ForgotPasswordTemplateModel forgotPasswordTemplateModel)
        {
            if (forgotPasswordTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.ForgotPassword });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public async Task SetupEmailTemplate(ForgotPasswordTemplateModel forgotPasswordTemplateModel)
        {
            try
            {
                // validate request modal
                ValidateModal(forgotPasswordTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", forgotPasswordTemplateModel.CompanyName);
                emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__DEVELOPERNAME__", forgotPasswordTemplateModel.DeveloperName);
                emailSenderModal.To = forgotPasswordTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                var html = ApplicationResource.ForgotPassword;
                html = html.Replace("__DEVELOPERNAME__", forgotPasswordTemplateModel.DeveloperName)
                    .Replace("__COMPANYNAME__", emailTemplate.SignatureDetail)
                    .Replace("__MOBILENO__", emailTemplate.ContactNo)
                    .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

                emailSenderModal.Body = html;
                await Task.Run(() => _emailService.SendEmail(emailSenderModal));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Kafka] Got exception: {ex.Message}");
            }
        }
    }
}
