using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class TimesheetTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public TimesheetTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(TimesheetSubmittedTemplateModel timesheetSubmittedTemplateModel)
        {
            if (timesheetSubmittedTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(timesheetSubmittedTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(timesheetSubmittedTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(timesheetSubmittedTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (timesheetSubmittedTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (timesheetSubmittedTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (timesheetSubmittedTemplateModel.ToDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.TimesheetSubmitted });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");
            return emailTemplate;
        }

        public void SetupEmailTemplate(TimesheetSubmittedTemplateModel timesheetSubmittedTemplateModel)
        {
            // validate request modal
            ValidateModal(timesheetSubmittedTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[REQUEST-TYPE]]", timesheetSubmittedTemplateModel.RequestType);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = timesheetSubmittedTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "Documents\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";


            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", timesheetSubmittedTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", timesheetSubmittedTemplateModel.RequestType)
                .Replace("[[ACTION-TYPE]]", timesheetSubmittedTemplateModel.ActionType)
                .Replace("[DAYS-COUNT]]", timesheetSubmittedTemplateModel.DayCount.ToString())
                .Replace("[[USER-MESSAGE]]", timesheetSubmittedTemplateModel.Message)
                .Replace("[[FROM-DATE]]", timesheetSubmittedTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", timesheetSubmittedTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
