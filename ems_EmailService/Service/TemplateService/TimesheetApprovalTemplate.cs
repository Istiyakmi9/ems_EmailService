using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.TemplateService
{
    public class TimesheetApprovalTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public TimesheetApprovalTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            if (timesheetApprovalTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(timesheetApprovalTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(timesheetApprovalTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(timesheetApprovalTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (timesheetApprovalTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (timesheetApprovalTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (timesheetApprovalTemplateModel.ToDate == null)
                throw new HiringBellException("Date is missing.");

            if (string.IsNullOrEmpty(timesheetApprovalTemplateModel.ManagerName))
                throw new HiringBellException("Manager name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.TimesheetApproval });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            // validate request modal
            ValidateModal(timesheetApprovalTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[DEVELOPER-NAME]]", timesheetApprovalTemplateModel.DeveloperName)
                .Replace("[[ACTION-TYPE]]", timesheetApprovalTemplateModel.ActionType);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = timesheetApprovalTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "Documents\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";


            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", timesheetApprovalTemplateModel.DeveloperName)
                .Replace("[[ACTION-TYPE]]", timesheetApprovalTemplateModel.ActionType)
                .Replace("[[REQUEST-TYPE]]", timesheetApprovalTemplateModel.RequestType)
                .Replace("[DAYS-COUNT]]", timesheetApprovalTemplateModel.DayCount.ToString())
                .Replace("[[MANAGER-NAME]]", timesheetApprovalTemplateModel.ManagerName)
                .Replace("[[FROM-DATE]]", timesheetApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", timesheetApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
