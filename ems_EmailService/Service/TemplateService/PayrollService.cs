using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
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

            if (payrollTemplateModel.FromDate == null)
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
            emailSenderModal.FileDetails = payrollTemplateModel.FileDetails;

            var html = ApplicationResource.Payroll;
            string status = string.Empty;
            string style = string.Empty;
            StringBuilder builder = new StringBuilder();
            if (payrollTemplateModel.missingDetail.Count == 0)
            {
                style = "green";
            }
            else if (payrollTemplateModel.missingDetail.Count <= 20)
            {
                style = "red";
                status = "Partially successfull";
                foreach (var detail in payrollTemplateModel.missingDetail)
                {
                    builder.Append("<div>" + detail + "</div>");
                }
            } else
            {
                style = "red";
                status = "Many failed";
                builder.Append("<div style=\"color: red;\"><b>Alert!!!</b>  " +
                    "payroll cycle failed, more than 20 employee payroll cycle raise exception. " +
                    "For detail please check the log files. Total failed count: " + missingDetail.Count + "</div>");
            }

            html = html.Replace("__STATUS__", status)
                .Replace("__STATUSCOLOR__", style)
                .Replace("__PRESENTDATE__", DateTime.Now.ToString("dd MMMM yyyy"))
                .Replace("__COMPANYNAME__", emailTemplate.EmailClosingStatement)
                .Replace("__MOBILENO__", emailTemplate.ContactNo)
                .Replace("__EMAILNOTE__", emailTemplate.EmailNote)
                .Replace("__ALERT__", builder.ToString());

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
