using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.TemplateService
{
    public class AttendanceApprovalTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public AttendanceApprovalTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(AttendanceApprovalTemplateModel attendanceApprovalTemplateModel)
        {
            if (attendanceApprovalTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(attendanceApprovalTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(attendanceApprovalTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(attendanceApprovalTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (attendanceApprovalTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (attendanceApprovalTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (attendanceApprovalTemplateModel.ToDate == null)
                throw new HiringBellException("Date is missing.");

            if (string.IsNullOrEmpty(attendanceApprovalTemplateModel.ManagerName))
                throw new HiringBellException("Manager name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.AttendanceApproval });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(AttendanceApprovalTemplateModel attendanceApprovalTemplateModel)
        {
            // validate request modal
            ValidateModal(attendanceApprovalTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[DEVELOPER-NAME]]", attendanceApprovalTemplateModel.DeveloperName)
                .Replace("[[ACTION-TYPE]]", attendanceApprovalTemplateModel.ActionType);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = attendanceApprovalTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "Documents\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";


            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", attendanceApprovalTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", attendanceApprovalTemplateModel.RequestType)
                .Replace("[[ACTION-TYPE]]", attendanceApprovalTemplateModel.ActionType)
                .Replace("[DAYS-COUNT]]", attendanceApprovalTemplateModel.DayCount.ToString())
                .Replace("[[MANAGER-NAME]]", attendanceApprovalTemplateModel.ManagerName)
                .Replace("[[FROM-DATE]]", attendanceApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", attendanceApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
