using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Service;
using ModalLayer.Modal;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace EmalRequest.Service
{
    public class EmailService : IEmailService
    {
        private readonly IDb _db;
        private readonly FileLocationDetail _fileLocationDetail;

        public EmailService(IDb db, FileLocationDetail fileLocationDetail)
        {
            _db = db;
            _fileLocationDetail = fileLocationDetail;
        }

        private EmailSettingDetail GetEmailSettingDetail()
        {
            var result = _db.Get<EmailSettingDetail>("sp_email_setting_detail_get", new { EmailSettingDetailId = 0 });
            if (result == null)
                throw HiringBellException.ThrowBadRequest("Unable to find the email template");

            return result;
        }

        public async Task SendEmail(EmailSenderModal emailSenderModal)
        {
            if (emailSenderModal == null || emailSenderModal.To == null || emailSenderModal.To.Count == 0)
                throw new HiringBellException("To send email receiver address is mandatory. Receiver address not found.");

            EmailSettingDetail _emailSettingDetail = GetEmailSettingDetail();

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
            var logoPath = Path.Combine(_fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);

            // Create the HTML view  
            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(emailSenderModal.Body, Encoding.UTF8, MediaTypeNames.Text.Html);

            // Create a plain text message for client that don't support HTML  
            string mediaType = MediaTypeNames.Image.Jpeg;
            LinkedResource img = new LinkedResource(logoPath, mediaType);

            // Make sure you set all these values!!!  
            img.ContentId = "logo";
            img.ContentType.MediaType = mediaType;
            img.TransferEncoding = TransferEncoding.Base64;
            img.ContentType.Name = img.ContentId;
            img.ContentLink = new Uri("cid:" + img.ContentId);
            htmlView.LinkedResources.Add(img);
            message.AlternateViews.Add(htmlView);

            message.Subject = emailSenderModal.Subject;
            //message.Body = emailSenderModal.Body;
            message.IsBodyHtml = true;
            message.From = fromAddress;

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
                            new System.Net.Mail.Attachment(Path.Combine(_fileLocationDetail.RootPath, files.FilePath, files.FileName + ".pdf"))
                        );
                    }
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
