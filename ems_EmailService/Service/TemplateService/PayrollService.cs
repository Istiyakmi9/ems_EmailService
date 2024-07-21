using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

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

        public void SendEmailNotification(PayrollTemplateModel payrollTemplateModel)
        {
            // validate request modal
            ValidateModal(payrollTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
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
                .Replace("__EMAILNOTE__", emailTemplate.EmailNote);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
