using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class OfferLetterTemplate
    {
        private readonly IDb _db;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailService _emailService;
        public OfferLetterTemplate(IDb db, IWebHostEnvironment hostingEnvironment, IEmailService emailService)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private void ValidateModal(OfferLetterTemplateModel offerLetterTemplateModel)
        {
            if (offerLetterTemplateModel.ToAddress.Count == 0)
                throw new HiringBellException("To address is missing.");

            if (string.IsNullOrEmpty(offerLetterTemplateModel.RequestType))
                throw new HiringBellException("Request type is missing.");

            if (string.IsNullOrEmpty(offerLetterTemplateModel.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            if (string.IsNullOrEmpty(offerLetterTemplateModel.ActionType))
                throw new HiringBellException("Action type is missing.");

            if (offerLetterTemplateModel.DayCount < 0)
                throw new HiringBellException("Days count is missing.");

            if (offerLetterTemplateModel?.FromDate == null)
                throw new HiringBellException("Date is missing.");

            if (offerLetterTemplateModel?.ToDate == null)
                throw new HiringBellException("Date is missing.");
        }

        private EmailTemplate GetEmailTemplate()
        {
            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = (int)TemplateEnum.OfferLetter });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public void SetupEmailTemplate(OfferLetterTemplateModel offerLetterTemplateModel)
        {
            // validate request modal
            ValidateModal(offerLetterTemplateModel);
            EmailTemplate emailTemplate = GetEmailTemplate();
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[DEVELOPER-NAME]]", offerLetterTemplateModel.DeveloperName);
            emailTemplate.SubjectLine = emailTemplate.EmailTitle;
            emailSenderModal.Title = emailTemplate.EmailTitle;
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.To = offerLetterTemplateModel.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var PdfTemplatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Documents\\htmltemplates\\emailtemplate.html");
            emailSenderModal.FileLocationDetail.LogoPath = "Documents\\logos";
            emailSenderModal.FileLocationDetail.RootPath = "E:\\Marghub\\core\\ems\\OnlineDataBuilderServer\\OnlineDataBuilder";


            var html = File.ReadAllText(PdfTemplatePath);
            html = html.Replace("[[Salutation]]", emailTemplate.Salutation).Replace("[[Body]]", emailTemplate.BodyContent)
                .Replace("[[EmailClosingStatement]]", emailTemplate.EmailClosingStatement)
                .Replace("[[Note]]", emailTemplate.EmailNote != null ? $"Note: {emailTemplate.EmailNote}" : null)
                .Replace("[[ContactNo]]", emailTemplate.ContactNo)
                .Replace("[[DEVELOPER-NAME]]", offerLetterTemplateModel.DeveloperName)
                .Replace("[[REQUEST-TYPE]]", offerLetterTemplateModel.RequestType)
                .Replace("[[ACTION-TYPE]]", offerLetterTemplateModel.ActionType)
                .Replace("[DAYS-COUNT]]", offerLetterTemplateModel.DayCount.ToString())
                .Replace("[[USER-MESSAGE]]", offerLetterTemplateModel.Message)
                .Replace("[[FROM-DATE]]", offerLetterTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[TO-DATE]]", offerLetterTemplateModel.FromDate.ToString("dddd, dd MMMM yyyy"))
                .Replace("[[Signature]]", emailTemplate.SignatureDetail);

            emailSenderModal.Body = html;
            _emailService.SendEmail(emailSenderModal);
        }
    }
}
