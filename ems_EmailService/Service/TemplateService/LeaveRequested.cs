using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class LeaveRequested
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private ILogger<LeaveRequested> _logger;

        public LeaveRequested(IDb db, IEmailService emailService, ILogger<LeaveRequested> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
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

            if (leaveRequestTemplateModel?.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (leaveRequestTemplateModel?.ToDate == null)
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
            emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", leaveRequestTemplateModel.CompanyName);
            emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__FROMDATE__", leaveRequestTemplateModel?.FromDate.ToString("dd MMMM yyyy"))
                .Replace("__TODATE__", leaveRequestTemplateModel?.ToDate.ToString("dd MMMM yyyy"))
                .Replace("__REQUESTTYPE__", leaveRequestTemplateModel!.RequestType)
                .Replace("__STATUS__", leaveRequestTemplateModel.ActionType);
            emailSenderModal.To = leaveRequestTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.LeaveApplied;

            string style = string.Empty;
            if (leaveRequestTemplateModel.ActionType.ToLower() == nameof(ItemStatus.Submitted).ToLower())
                style = "blue";
            else if (leaveRequestTemplateModel.ActionType.ToLower() == nameof(ItemStatus.Rejected).ToLower())
                style = "red";
            else
                style = "green";

            html = html.Replace("__REVEIVERNAME__", string.Empty) //leaveRequestTemplateModel.DeveloperName)
                .Replace("__LEAVETYPE__", leaveRequestTemplateModel.RequestType)
                .Replace("__DEVELOPERNAME__", leaveRequestTemplateModel.DeveloperName)
                .Replace("__STATUS__", leaveRequestTemplateModel.ActionType)
                .Replace("__FROMDATE__", leaveRequestTemplateModel.FromDate.ToString("dd MMMM yyyy"))
                .Replace("__TODATE__", leaveRequestTemplateModel.ToDate.ToString("dd MMMM yyyy"))
                .Replace("__NOOFDAYS__", leaveRequestTemplateModel.DayCount.ToString())
                .Replace("__STATUSCOLOR__", style)
                .Replace("__ACTIONNAME__", "Manager Name")
                .Replace("__MANAGENAME__", leaveRequestTemplateModel.ManagerName)
                .Replace("__MESSAGE__", leaveRequestTemplateModel.Message)
                .Replace("__COMPANYNAME__", emailTemplate.EmailClosingStatement)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__EMAILNOTE__", emailTemplate.EmailNote);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);

            _logger.LogInformation($"Email send successfully to: {leaveRequestTemplateModel.ToAddress[0]}");
        }
    }
}
