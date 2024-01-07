using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class BillingService
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public BillingService(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(BillingTemplateModel billingTemplateModel)
        {
            if (billingTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (billingTemplateModel.Year == 0)
                throw new HiringBellException("Year is missing.");

            if (string.IsNullOrEmpty(billingTemplateModel.Month))
                throw new HiringBellException("Month is missing.");

            if (string.IsNullOrEmpty(billingTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.Billing });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

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

        public async Task SendEmailNotification(BillingTemplateModel billingTemplateModel)
        {
            // validate request modal
            ValidateModal(billingTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            var logoPath = GetCompanyLogo(billingTemplateModel.CompanyId);
            if (string.IsNullOrEmpty(logoPath))
                throw HiringBellException.ThrowBadRequest("Logo path not found");

            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", billingTemplateModel.CompanyName);
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = billingTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();
            emailSenderModal.FileDetails = billingTemplateModel.FileDetails;

            var html = ApplicationResource.EmployeeBill;
            html = html.Replace("__MONTH__", billingTemplateModel.Month)
                .Replace("__YEAR__", billingTemplateModel.Year.ToString())
                .Replace("__DEVELOPERANAME__", billingTemplateModel.DeveloperName)
                .Replace("__ROLE__", billingTemplateModel.Role)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__COMPANYNAME__", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
