using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Service.Interface;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

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

        public async Task SendEmailNotification(BillingTemplateModel billingTemplateModel)
        {
            // validate request modal
            ValidateModal(billingTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
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
