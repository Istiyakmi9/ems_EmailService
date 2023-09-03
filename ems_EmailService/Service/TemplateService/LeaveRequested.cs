using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.TemplateService
{
    public class LeaveRequested
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public LeaveRequested(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(LeaveTemplateModel leaveRequestTemplateModel)
        {
            if (leaveRequestTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(leaveRequestTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(leaveRequestTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(leaveRequestTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (leaveRequestTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (leaveRequestTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (leaveRequestTemplateModel.ToDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.LeaveRequest });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(LeaveTemplateModel leaveRequestTemplateModel)
        {
            // validate request modal
            ValidateModal(leaveRequestTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[DEVELOPER-NAME]]", leaveRequestTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", leaveRequestTemplateModel.RequestType);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = leaveRequestTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");

            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", leaveRequestTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", leaveRequestTemplateModel.RequestType)
                .Replace("[[ACTION-TYPE]]", leaveRequestTemplateModel.ActionType)
                .Replace("[DAYS-COUNT]]", leaveRequestTemplateModel.DayCount.ToString())
                .Replace("[[USER-MESSAGE]]", leaveRequestTemplateModel.Message)
                .Replace("[[FROM-DATE]]", leaveRequestTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", leaveRequestTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
