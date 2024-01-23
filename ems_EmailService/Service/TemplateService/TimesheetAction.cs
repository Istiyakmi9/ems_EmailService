using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using ModalLayer.Modal;
using System.Globalization;

namespace EmailRequest.Service.TemplateService
{
    public class TimesheetAction
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        public TimesheetAction(IDb db, IEmailService emailService)
        {
            _db = db;
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

            if (timesheetApprovalTemplateModel?.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (timesheetApprovalTemplateModel?.ToDate == null)
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

        public async Task SendEmailNotification(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            // validate request modal
            ValidateModal(timesheetApprovalTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            var logoPath = GetCompanyLogo(timesheetApprovalTemplateModel.CompanyId);
            if (string.IsNullOrEmpty(logoPath))
                throw HiringBellException.ThrowBadRequest("Logo path not found");

            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = emailTemplate.EmailTitle.Replace("__COMPANYNAME__", timesheetApprovalTemplateModel.CompanyName);
            emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__STATUS__", timesheetApprovalTemplateModel.ActionType)
                    .Replace("__DEVELOPERNAME__", timesheetApprovalTemplateModel.DeveloperName)
                    .Replace("__REQUESTTYPE__", timesheetApprovalTemplateModel!.RequestType);
            emailSenderModal.To = timesheetApprovalTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            string statusColor = string.Empty;
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            switch (textinfo.ToTitleCase(timesheetApprovalTemplateModel.ActionType))
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

            var html = ApplicationResource.TimesheetApplied;
            html = html.Replace("__REQUESTTYPE__", timesheetApprovalTemplateModel!.RequestType)
                .Replace("__REVEIVERNAME__", string.Empty)
                .Replace("__DEVELOPERNAME__", timesheetApprovalTemplateModel.DeveloperName)
                .Replace("__MANAGENAME__", timesheetApprovalTemplateModel.ManagerName)
                .Replace("__FROMDATE__", timesheetApprovalTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("__TODATE__", timesheetApprovalTemplateModel.ToDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("__NOOFDAYS__", timesheetApprovalTemplateModel.DayCount.ToString())
                .Replace("__STATUS__", timesheetApprovalTemplateModel.ActionType)
                .Replace("__ACTIONNAME__", $"{timesheetApprovalTemplateModel.ActionType} By")
                .Replace("__STATUSCOLOR__", statusColor)
                .Replace("__MESSAGE__", emailTemplate.EmailNote ?? string.Empty)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__COMPANYNAME__", timesheetApprovalTemplateModel.CompanyName)
                .Replace("__EMAILNOTE__", "Please write us back if you have any issue")
                .Replace("__EMAILENCLOSINGSTATEMENT__", emailTemplate.SignatureDetail)
                .Replace("__COMPANYLOGO__", logoPath)
                .Replace("__ENCLOSINGSTATEMENT__", emailTemplate.EmailClosingStatement);

            emailSenderModal.Body = html;

            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
