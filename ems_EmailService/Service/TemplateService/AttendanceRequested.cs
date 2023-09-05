using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Service.Interface;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class AttendanceRequested : IEmailServiceRequest
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ILogger<AttendanceRequested> _logger;

        public AttendanceRequested(IDb db,
            IWebHostEnvironment hostingEnvironment,
            IEmailService emailService,
            FileLocationDetail fileLocationDetail,
            ILogger<AttendanceRequested> logger)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
            _fileLocationDetail = fileLocationDetail;
            _logger = logger;
        }

        private void ValidateModal(AttendanceRequestModal attendanceTemplateModel)
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

            if (attendanceTemplateModel?.FromDate == null)
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

        public async Task SendEmailNotification(AttendanceRequestModal attendanceTemplateModel)
        {
            try
            {
                ValidateModal(attendanceTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", attendanceTemplateModel.CompanyName);
                emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__DATE__", attendanceTemplateModel?.FromDate.ToString("dddd, dd MMMM yyyy"))
                    .Replace("__REQUESTTYPE__", attendanceTemplateModel!.RequestType)
                    .Replace("__STATUS__", attendanceTemplateModel.ActionType);
                emailSenderModal.To = attendanceTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                var html = ApplicationResource.AttendanceApplied;

                string statusColor = string.Empty;
                switch (attendanceTemplateModel?.ActionType?.ToLower())
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

                html = html.Replace("__REQUESTTYPE__", attendanceTemplateModel!.RequestType)
                    .Replace("__REVEIVERNAME__", attendanceTemplateModel.ManagerName)
                    .Replace("__DEVELOPERNAME__", attendanceTemplateModel.DeveloperName)
                    .Replace("__DATE__", attendanceTemplateModel?.FromDate.ToString("dddd, dd MMMM yyyy"))
                    .Replace("__NOOFDAYS__", attendanceTemplateModel!.DayCount.ToString())
                    .Replace("__STATUS__", attendanceTemplateModel.ActionType)
                    .Replace("__ACTIONNAME__", "Manager Name")
                    .Replace("__STATUSCOLOR__", statusColor)
                    .Replace("__MESSAGE__", emailTemplate.EmailNote ?? string.Empty)
                    .Replace("__MOBILENO__", emailTemplate.ContactNo)
                    .Replace("__COMPANYNAME__", emailTemplate.SignatureDetail)
                    .Replace("__EMAILNOTE__", "Please write us back if you have any issue")
                    .Replace("__MANAGENAME__", attendanceTemplateModel.ManagerName)
                    .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

                emailSenderModal.Body = html;

                _logger.LogInformation($"[5. Kafka] Template converted.");
                await Task.Run(() => _emailService.SendEmail(emailSenderModal));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Kafka] Got exception: {ex.Message}");
            }
        }
    }
}
