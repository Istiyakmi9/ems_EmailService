using ModalLayer.Modal;
using System.Net.Mail;
using System.Net;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Service;

namespace EmalRequest.Service
{
    public class EmailService : IEmailService
    {
        private readonly IDb _db;
        private EmailSettingDetail _emailSettingDetail { get; set; }

        public EmailService(IDb db)
        {
            _db = db;
        }

        private void GetEmailSettingDetail()
        {
            var result = _db.Get<EmailSettingDetail>("sp_email_setting_detail_get", new { EmailSettingDetailId = 0 });
            if (result == null)
                throw HiringBellException.ThrowBadRequest("Unable to find the email template");

            _emailSettingDetail = result;
        }

        public async Task SendEmail(EmailSenderModal emailSenderModal)
        {
            if (emailSenderModal == null || emailSenderModal.To == null || emailSenderModal.To.Count == 0)
                throw new HiringBellException("To send email receiver address is mandatory. Receiver address not found.");

            FileLocationDetail fileLocationDetail = emailSenderModal.FileLocationDetail;

            GetEmailSettingDetail();

            var fromAddress = new System.Net.Mail.MailAddress(_emailSettingDetail!.EmailAddress, emailSenderModal.Title);

            var smtp = new SmtpClient
            {
                Host = _emailSettingDetail.EmailHost,
                Port = _emailSettingDetail.PortNo,
                EnableSsl = _emailSettingDetail.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = _emailSettingDetail.UserDefaultCredentials,
                Credentials = new NetworkCredential(fromAddress.Address, _emailSettingDetail.Credentials)
            };


            var message = new MailMessage();
            message.Subject = emailSenderModal.Subject;
            message.Body = emailSenderModal.Body;
            message.IsBodyHtml = true;
            message.From = fromAddress;
            //message.AlternateViews.Add(CreateHtmlMessage(emailSenderModal.Body, logoPath));

            foreach (var emailAddress in emailSenderModal.To)
                message.To.Add(emailAddress);

            if (emailSenderModal.CC != null && emailSenderModal.CC.Count > 0)
                foreach (var emailAddress in emailSenderModal.CC)
                    message.CC.Add(emailAddress);

            if (emailSenderModal.BCC != null && emailSenderModal.BCC.Count > 0)
                foreach (var emailAddress in emailSenderModal.BCC)
                    message.Bcc.Add(emailAddress);

            try
            {
                if (emailSenderModal.FileDetails != null && emailSenderModal.FileDetails.Count > 0)
                {
                    foreach (var files in emailSenderModal.FileDetails)
                    {
                        message.Attachments.Add(
                            new System.Net.Mail.Attachment(Path.Combine(fileLocationDetail.RootPath, files.FilePath, files.FileName + ".pdf"))
                        );
                    }
                }

                var logoPath = Path.Combine(fileLocationDetail.RootPath, fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
                if (File.Exists(logoPath))
                {
                    var attachment = new System.Net.Mail.Attachment(logoPath);
                    attachment.ContentId = ApplicationConstants.LogoContentId;
                    message.Attachments.Add(attachment);
                }

                smtp.Send(message);
            }
            catch (Exception ex)
            {
                var _e = ex;
                throw;
            }

            await Task.CompletedTask;
        }
    }
}
