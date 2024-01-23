using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class LeaveApprovalTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public LeaveApprovalTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(LeaveTemplateModel leaveApprovalTemplateModel)
        {
            if (leaveApprovalTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(leaveApprovalTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(leaveApprovalTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(leaveApprovalTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (leaveApprovalTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (leaveApprovalTemplateModel?.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (leaveApprovalTemplateModel?.ToDate == null)
                throw new HiringBellException("Date is missing.");

            if (string.IsNullOrEmpty(leaveApprovalTemplateModel.ManagerName))
                throw new HiringBellException("Manager name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.LeaveApproval });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(LeaveTemplateModel leaveApprovalTemplateModel)
        {
            // validate request modal
            ValidateModal(leaveApprovalTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[DEVELOPER-NAME]]", leaveApprovalTemplateModel.DeveloperName)
                .Replace("[[ACTION-TYPE]]", leaveApprovalTemplateModel.ActionType);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = leaveApprovalTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "ApplicationFiles\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "ApplicationFiles\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";


            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", leaveApprovalTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", leaveApprovalTemplateModel.RequestType)
                .Replace("[[ACTION-TYPE]]", leaveApprovalTemplateModel.ActionType)
                .Replace("[DAYS-COUNT]]", leaveApprovalTemplateModel.DayCount.ToString())
                .Replace("[[MANAGER-NAME]]", leaveApprovalTemplateModel.ManagerName)
                .Replace("[[FROM-DATE]]", leaveApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", leaveApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
