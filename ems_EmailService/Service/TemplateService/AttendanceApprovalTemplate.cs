using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.TemplateService
{
    public class AttendanceApprovalTemplate
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly CurrentSession _currentSession;
        public AttendanceApprovalTemplate(IDb db, IEmailService emailService)
        {
            _db = db;
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
            emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", attendanceApprovalTemplateModel.CompanyName);
            emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__DATE__", attendanceApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("__REQUESTTYPE__", attendanceApprovalTemplateModel.RequestType)
                .Replace("__STATUS__", attendanceApprovalTemplateModel.ActionType);
            emailSenderModal.To = attendanceApprovalTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();
            string statusColor = attendanceApprovalTemplateModel?.ActionType?.ToLower() == "submitted" ? "#0D6EFD" : attendanceApprovalTemplateModel?.ActionType?.ToLower() == "approved" ? "#198754"
                : "#DC3545";
            var html = ApplicationResource.AttendanceApplied;
            html = html.Replace("__REQUESTTYPE__", attendanceApprovalTemplateModel.RequestType)
                .Replace("__DEVELOPERNAME__", attendanceApprovalTemplateModel.DeveloperName)
                .Replace("__MANAGENAME__", attendanceApprovalTemplateModel.ManagerName)
                .Replace("__DATE__", attendanceApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("__NOOFDAYS__", attendanceApprovalTemplateModel.DayCount.ToString())
                .Replace("__STATUS__", attendanceApprovalTemplateModel.ActionType)
                .Replace("__STATUSCOLOR__", statusColor)
                .Replace("__MESSAGE__", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__COMPANYNAME__", attendanceApprovalTemplateModel.CompanyName)
                .Replace("__EMAILENCLOSINGSTATEMENT__", emailTemplate.SignatureDetail)
                .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
