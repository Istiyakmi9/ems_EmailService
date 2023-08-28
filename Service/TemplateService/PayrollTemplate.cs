using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer;
using System.Text;

namespace EmailRequest.Service.TemplateService
{
    public class PayrollTemplate
    {
        private readonly IDb _db;
        private EmailSettingDetail _emailSettingDetail { get; set; }

        public PayrollTemplate(IDb db)
        {
            _db = db;
        }

        private EmailRequestModal GetRequestModal()
        {
            return new EmailRequestModal();
        }

        private void ValidateModal(EmailRequestModal emailRequestModal)
        {
            if (emailRequestModal.TemplateId <= 0)
                throw new HiringBellException("No email template has been selected.");

            if (string.IsNullOrEmpty(emailRequestModal.ManagerName))
                throw new HiringBellException("Manager name is missing.");

            if (string.IsNullOrEmpty(emailRequestModal.DeveloperName))
                throw new HiringBellException("Developer name is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = TemplateEnum.Billing });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(EmailSenderModal emailSenderModal)
        {
            FileLocationDetail fileLocationDetail = emailSenderModal.FileLocationDetail;
            var emailRequestModal = GetRequestModal();

            // validate request modal
            ValidateModal(emailRequestModal);


            EmailTemplate emailTemplate = GetEmailTemplate();

            var footer = new StringBuilder();
            footer.Append($"<div>{emailTemplate.EmailClosingStatement}</div>");
            footer.Append($"<div>{emailTemplate.SignatureDetail}</div>");
            footer.Append($"<div>{emailTemplate.ContactNo}</div>");

            var logoPath = Path.Combine(fileLocationDetail.RootPath, fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
            if (File.Exists(logoPath))
            {
                footer.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
            }


            emailTemplate.Footer = footer.ToString();

            emailTemplate.SubjectLine = emailTemplate.EmailTitle
                .Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType)
                .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType);

            emailTemplate.BodyContent = emailTemplate.BodyContent
                .Replace("[[DEVELOPER-NAME]]", emailRequestModal.DeveloperName)
                .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType)
                .Replace("[[DAYS-COUNT]]", emailRequestModal.TotalNumberOfDays.ToString())
                .Replace("[[FROM-DATE]]", emailRequestModal.FromDate.ToString("dd MMM, yyy"))
                .Replace("[[TO-DATE]]", emailRequestModal.ToDate.ToString("dd MMM, yyy"))
                .Replace("[[MANAGER-NAME]]", emailRequestModal.ManagerName)
                .Replace("[[USER-MESSAGE]]", emailRequestModal.Message)
                .Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType);

            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType)
                                    .Replace("[[DEVELOPER-NAME]]", emailRequestModal.DeveloperName)
                                    .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType);

            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.Body = string.Concat(emailTemplate.BodyContent, emailTemplate.Footer);
        }
    }
}
