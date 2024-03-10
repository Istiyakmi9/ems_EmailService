using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Service;
using ModalLayer.Modal;
using System.Net;
using System.Net.Mail;

namespace EmalRequest.Service
{
    public class EmailService : IEmailService
    {
        private readonly IDb _db;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ILogger<IEmailService> _logger;

        public EmailService(IDb db, FileLocationDetail fileLocationDetail, ILogger<IEmailService> logger)
        {
            _db = db;
            _fileLocationDetail = fileLocationDetail;
            _logger = logger;
        }

        private EmailSettingDetail GetEmailSettingDetail()
        {
            _logger.LogInformation($"[6. Kafka] Reading email setting detail.");
            var result = _db.Get<EmailSettingDetail>("sp_email_setting_detail_get", new { EmailSettingDetailId = 0 });
            if (result == null)
            {
                _logger.LogError($"[Kafka] Reading email setting fail.");
                throw HiringBellException.ThrowBadRequest("Unable to find the email template");
            }

            return result;
        }

        public async Task SendEmail(EmailSenderModal emailSenderModal)
        {
            try
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
                message.Subject = emailSenderModal.Subject;
                message.Body = emailSenderModal.Body;
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

                _logger.LogInformation($"[Kafka] Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Kafka] Sending email got exception. Messge: {ex.Message}");
                throw;
            }

            await Task.CompletedTask;
        }
    }
}
