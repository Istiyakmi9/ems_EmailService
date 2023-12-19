using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
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
                throw HiringBellException.ThrowBadRequest("To address is missing.");

            if (string.IsNullOrEmpty(forgotPasswordTemplateModel.NewPassword))
                throw HiringBellException.ThrowBadRequest("Tempory password is not found");

            if (string.IsNullOrEmpty(forgotPasswordTemplateModel.CompanyName))
                throw HiringBellException.ThrowBadRequest("Company name is not found");
        }

        private EmailTemplate GetEmailTemplate()
        {
            _logger.LogInformation($"[1. Kafka] Trying to read email template from database.");
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.ForgotPassword });

            if (emailTemplate == null)
            {
                _logger.LogError($"[Kafka] Fail to read email template.");
                throw HiringBellException.ThrowBadRequest("Email template not found. Please contact to admin.");
            }

            return emailTemplate;
        }

        private string GetCompanyLogo(int companyId)
        {
            if (companyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company id");

            Files file = _db.Get<Files>("sp_company_primary_logo_get_byid", new
            {
                CompanyId = companyId,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (file == null)
                throw new HiringBellException(" Company primary logo not found. Please contact to admin.");

            string filePath = string.Empty;
            if (file.FileName.Contains("."))
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}";
            else
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}.+{file.FileExtension}";

            if (filePath.Contains("\\"))
                filePath = filePath.Replace("\\", "/");

            return filePath;
        }

        public async Task SendEmailNotification(ForgotPasswordTemplateModel forgotPasswordTemplateModel)
        {
            try
            {
                // validate request modal
                ValidateModal(forgotPasswordTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                var logoPath = GetCompanyLogo(forgotPasswordTemplateModel.CompanyId);
                if (string.IsNullOrEmpty(logoPath))
                    throw HiringBellException.ThrowBadRequest("Logo path not found");

                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", forgotPasswordTemplateModel.CompanyName);
                emailSenderModal.Subject = emailTemplate.SubjectLine;
                emailSenderModal.To = forgotPasswordTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                var html = ApplicationResource.ForgotPassword;
                html = html.Replace("__TEMPPASSWORD__", forgotPasswordTemplateModel.NewPassword)
                    .Replace("__COMPANYNAME__", forgotPasswordTemplateModel.CompanyName)
                    .Replace("__MOBILENO__", emailTemplate.ContactNo)
                    .Replace("__COMPANYLOGO__", logoPath)
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
