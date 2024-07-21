using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Service.Interface;
using ModalLayer.Modal;
using System.Globalization;

namespace EmailRequest.Service.TemplateService
{
    public class AttendanceAction : IEmailServiceRequest
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<AttendanceAction> _logger;

        public AttendanceAction(IDb db, IEmailService emailService, ILogger<AttendanceAction> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        private void ValidateModal(AttendanceRequestModal attendanceRequestModal)
        {
            if (attendanceRequestModal.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(attendanceRequestModal.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(attendanceRequestModal.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(attendanceRequestModal.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (attendanceRequestModal.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (attendanceRequestModal?.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (attendanceRequestModal?.ToDate == null)
                throw new HiringBellException("Date is missing.");

            if (string.IsNullOrEmpty(attendanceRequestModal.ManagerName))
                throw new HiringBellException("Manager name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            try
            {
                EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.AttendanceApproval });

                if (emailTemplate == null)
                    throw new HiringBellException("Email template not found. Please contact to admin.");

                return emailTemplate;
            }
            catch (Exception ex)
            {
                throw new HiringBellException($"[Kafka Error]: Error {ex.Message}");
            }
        }

        public async Task SendEmailNotification(AttendanceRequestModal attendanceRequestModal)
        {
            _logger.LogInformation($"[Kafka]: Validating request object");
            // validate request modal
            ValidateModal(attendanceRequestModal);
            EmailTemplate emailTemplate = GetEmailTemplate();

            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", attendanceRequestModal.CompanyName);
            emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__DATE__", attendanceRequestModal.FromDate.ToString("dd MMMM yyyy"))
                .Replace("__REQUESTTYPE__", attendanceRequestModal.RequestType)
                .Replace("__DEVELOPERNAME__", attendanceRequestModal.DeveloperName);
            emailSenderModal.To = attendanceRequestModal.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            string statusColor = string.Empty;
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            switch (textinfo.ToTitleCase(attendanceRequestModal.ActionType))
            {
                case ApplicationConstants.Submitted:
                    statusColor = "#0D6EFD";
                    break;
                case ApplicationConstants.Approved:
                    statusColor = "#198754";
                    break;
                case ApplicationConstants.Rejected:
                    statusColor = "#DC3545";
                    break;
            }

            _logger.LogInformation($"[Kafka]: Preparing the email html content");
            var html = ApplicationResource.AttendanceApplied;
            html = html.Replace("__REQUESTTYPE__", attendanceRequestModal!.RequestType)
                .Replace("__REVEIVERNAME__", string.Empty)
                .Replace("__DEVELOPERNAME__", attendanceRequestModal.DeveloperName)
                .Replace("__MANAGENAME__", attendanceRequestModal.ManagerName)
                .Replace("__DATE__", attendanceRequestModal.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("__NOOFDAYS__", attendanceRequestModal.DayCount.ToString())
                .Replace("__STATUS__", attendanceRequestModal.ActionType)
                .Replace("__ACTIONNAME__", $"{attendanceRequestModal.ActionType} By")
                .Replace("__STATUSCOLOR__", statusColor)
                .Replace("__MESSAGE__", emailTemplate.EmailNote ?? string.Empty)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__COMPANYNAME__", attendanceRequestModal.CompanyName)
                .Replace("__EMAILNOTE__", "Please write us back if you have any issue")
                .Replace("__EMAILENCLOSINGSTATEMENT__", emailTemplate.SignatureDetail)
                .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

            emailSenderModal.Body = html;

            _logger.LogInformation($"[Kafka]: Sending content for email");
            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
