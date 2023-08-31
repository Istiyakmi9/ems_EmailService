using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.TemplateService
{
    public class PayrollTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public PayrollTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(PayrollTemplateModel payrollTemplateModel)
        {
            if (payrollTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (payrollTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.Payroll });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(PayrollTemplateModel payrollTemplateModel)
        {
            // validate request modal
            ValidateModal(payrollTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = payrollTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "Documents\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";

            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[FROM-DATE]]", payrollTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
