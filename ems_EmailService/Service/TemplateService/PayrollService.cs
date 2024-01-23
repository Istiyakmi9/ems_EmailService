using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using ModalLayer.Modal;
using System.Text;

namespace EmailRequest.Service.TemplateService
{
    public class PayrollService
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        public PayrollService(IDb db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        private void ValidateModal(PayrollTemplateModel payrollTemplateModel)
        {
            if (payrollTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (payrollTemplateModel?.FromDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.Payroll });

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

        public void SendEmailNotification(PayrollTemplateModel payrollTemplateModel)
        {
            // validate request modal
            ValidateModal(payrollTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            var logoPath = GetCompanyLogo(payrollTemplateModel.CompanyId);
            if (string.IsNullOrEmpty(logoPath))
                throw HiringBellException.ThrowBadRequest("Logo path not found");

            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine.Replace("__PRESENTDATE__", DateTime.Now.ToString("MMMM"));
            emailSenderModal.To = payrollTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.Payroll;
            html = html.Replace("__BODY__", payrollTemplateModel.Body)
                .Replace("__PRESENTDATE__", DateTime.Now.ToString("dd MMMM yyyy"))
                .Replace("__COMPANYNAME__", emailTemplate.EmailClosingStatement)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__COMPANYLOGO__", logoPath)
                .Replace("__EMAILNOTE__", emailTemplate.EmailNote);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
