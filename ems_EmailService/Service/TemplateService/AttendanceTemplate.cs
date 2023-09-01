using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;
using System.Resources;

namespace EmailRequest.Service.TemplateService
{
    public class AttendanceTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ILogger<AttendanceTemplate> _logger;

        public AttendanceTemplate(IDb db,
            IWebHostEnvironment hostingEnvironment,
            IEmailService emailService,
            FileLocationDetail fileLocationDetail,
            ILogger<AttendanceTemplate> logger)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
            _fileLocationDetail = fileLocationDetail;
            _logger = logger;
        }

        private void ValidateModal(AttendanceTemplateModel attendanceTemplateModel)
        {
            if (attendanceTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(attendanceTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (attendanceTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (attendanceTemplateModel.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (attendanceTemplateModel.ToDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            _logger.LogInformation($"[1. Kafka] Trying to read email template from database.");
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.Attendance });

            if (emailTemplate == null)
            {
                _logger.LogError($"[Kafka] Fail to read email template.");
                throw new HiringBellException("Email template not found. Please contact to admin.");
            }

            return emailTemplate;
        }

        public void SetupEmailTemplate(AttendanceTemplateModel attendanceTemplateModel)
        {
            try
            {
                ValidateModal(attendanceTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[REQUEST-TYPE]]", attendanceTemplateModel.RequestType);
                emailTemplate.SubjectLine = emailTemplate.EmailTitle;
                emailSenderModal.Title = emailTemplate.EmailTitle;
                emailSenderModal.Subject = emailTemplate.SubjectLine;
                emailSenderModal.To = attendanceTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                _logger.LogInformation($"[2. Kafka] Email setting data configured.");

                _logger.LogInformation($"[3. Kafka] Reading template content.");
                var PdfTemplatePath = Path.Combine(_fileLocationDetail.HtmlTemplatePath, "emailtemplate.html");


                var html = File.ReadAllText(PdfTemplatePath);

                _logger.LogInformation($"[4. Kafka] Converting template.");
                html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                    .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                    .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                    .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                    .Replace("[[DEVELOPER-NAME]]", attendanceTemplateModel.DeveloperName)
                    .Replace("[[REQUEST-TYPE]]", attendanceTemplateModel.RequestType)
                    .Replace("[[ACTION-TYPE]]", attendanceTemplateModel.ActionType)
                    .Replace("[DAYS-COUNT]]", attendanceTemplateModel.DayCount.ToString())
                    .Replace("[[USER-MESSAGE]]", attendanceTemplateModel.Message)
                    .Replace("[[FROM-DATE]]", attendanceTemplateModel.FromDate?.ToString("dddd, dd MMMM yyyy"))
                    .Replace("[[TO-DATE]]", attendanceTemplateModel.FromDate?.ToString("dddd, dd MMMM yyyy"))
                    .Replace("[[Signature]]", emailTemplate.SignatureDetail);

                emailSenderModal.Body = html;

                _logger.LogInformation($"[5. Kafka] Template converted.");
                _emailService.SendEmail(emailSenderModal);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Kafka] Got exception: {ex.Message}");
            }
        }
    }
}
