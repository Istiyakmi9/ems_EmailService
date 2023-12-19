using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class TimesheetRequested
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly ILogger<TimesheetRequested> _logger;
        public TimesheetRequested(IDb db, IEmailService emailService, ILogger<TimesheetRequested> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        private void ValidateModal(TimesheetApprovalTemplateModel timesheetSubmittedTemplateModel)
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

        private string GetCompanyLogo(int companyId)
        {
            if (companyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company id");

            Files file = _db.Get<Files>("sp_company_primary_logo_get_byid", new
            {
                CompanyId = companyId,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (file == null)
                throw new HiringBellException(" Company primary logo not found. Please contact to admin.");

            string filePath = string.Empty;
            if (file.FileName.Contains("."))
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}";
            else
                filePath = $"{AppConstants.BaseImageUrl}{file.FilePath}/{file.FileName}.+{file.FileExtension}";

            if (filePath.Contains("\\"))
                filePath = filePath.Replace("\\", "/");

            return filePath;
        }

        public async Task SendEmailNotification(TimesheetApprovalTemplateModel timesheetSubmittedTemplateModel)
        {
            try
            {
                ValidateModal(timesheetSubmittedTemplateModel);
                EmailTemplate emailTemplate = GetEmailTemplate();
                var logoPath = GetCompanyLogo(timesheetSubmittedTemplateModel.CompanyId);
                if (string.IsNullOrEmpty(logoPath))
                    throw HiringBellException.ThrowBadRequest("Logo path not found");

                EmailSenderModal emailSenderModal = new EmailSenderModal();
                emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", timesheetSubmittedTemplateModel.CompanyName);
                emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__STATUS__", timesheetSubmittedTemplateModel.ActionType)
                    .Replace("__DEVELOPERNAME__", timesheetSubmittedTemplateModel.DeveloperName)
                    .Replace("__REQUESTTYPE__", "Timesheet");
                emailSenderModal.To = timesheetSubmittedTemplateModel.ToAddress;
                emailSenderModal.FileLocationDetail = new FileLocationDetail();

                var html = ApplicationResource.TimesheetApplied;

                string statusColor = string.Empty;
                switch (timesheetSubmittedTemplateModel?.ActionType?.ToLower())
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

                html = html.Replace("__REVEIVERNAME__", timesheetSubmittedTemplateModel.ManagerName)
                    .Replace("__DEVELOPERNAME__", timesheetSubmittedTemplateModel.DeveloperName)
                    .Replace("__FROMDATE__", timesheetSubmittedTemplateModel?.FromDate.ToString("dddd, dd MMMM yyyy"))
                    .Replace("__TODATE__", timesheetSubmittedTemplateModel?.ToDate.ToString("dddd, dd MMMM yyyy"))
                    .Replace("__NOOFDAYS__", timesheetSubmittedTemplateModel!.DayCount.ToString())
                    .Replace("__STATUS__", timesheetSubmittedTemplateModel.ActionType)
                    .Replace("__ACTIONNAME__", "Manager Name")
                    .Replace("__STATUSCOLOR__", statusColor)
                    .Replace("__MESSAGE__", timesheetSubmittedTemplateModel.Message ?? string.Empty)
                    .Replace("__MOBILENO__", emailTemplate.ContactNo)
                    .Replace("__COMPANYNAME__", emailTemplate.SignatureDetail)
                    .Replace("__EMAILNOTE__", "Please write us back if you have any issue")
                    .Replace("__MANAGENAME__", timesheetSubmittedTemplateModel.ManagerName)
                    .Replace("__COMPANYLOGO__", logoPath)
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
