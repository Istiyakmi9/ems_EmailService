using EmailRequest.EMailService.Interface;
using EmailRequest.Service;
using EmailRequest.Service.TemplateService;
using MailKit.Net.Pop3;
using ModalLayer.Modal;
using System.Globalization;

namespace EmailRequest.EMailService.Service
{
    public class EMailManager : IEMailManager
    {
        private readonly BillingTemplate _billingTemplate;
        private readonly IEmailService _emailService;

        public EMailManager(BillingTemplate billingTemplate,
            IEmailService emailService)
        {
            _billingTemplate = billingTemplate;
            _emailService = emailService;
        }

        private List<InboxMailDetail> ReadPOP3Email(EmailSettingDetail? emailSettingDetail)
        {
            List<InboxMailDetail> inboxMailDetails = new List<InboxMailDetail>();

            // Create a new POP3 client
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect("pop.secureserver.net", 995, true);

                // Authenticate with the server
                client.Authenticate(emailSettingDetail!.EmailAddress, emailSettingDetail.Credentials);

                // Get the list of messages
                int messageCount = client.GetMessageCount();

                int i = messageCount - 1;
                int mailCounter = 0;
                // Loop through the messages
                while (i > 0 && mailCounter != 15)
                {
                    // Get the message
                    var message = client.GetMessage(i);

                    inboxMailDetails.Add(new InboxMailDetail
                    {
                        Subject = message.Subject,
                        From = message.From.ToString(),
                        Body = message.HtmlBody,
                        EMailIndex = i,
                        Text = message.GetTextBody(MimeKit.Text.TextFormat.Plain),
                        Priority = message.Priority.ToString(),
                        Date = message.Date.DateTime
                    });

                    mailCounter++;
                    i--;
                }
            }

            return inboxMailDetails;
        }

        public List<InboxMailDetail> ReadMails(EmailSettingDetail emailSettingDetail)
        {
            return ReadPOP3Email(emailSettingDetail);
        }

        private string _generateFileName(int sequence)
        {
            DateTime currentDateTime = DateTime.Now;
            return string.Format("{0}-{1:000}-{2:000}.eml",
                currentDateTime.ToString("yyyyMMddHHmmss", new CultureInfo("en-US")),
                currentDateTime.Millisecond,
                sequence);
        }

        public async Task SendMailAsync(EmailSenderModal emailSenderModal)
        {
            // Setup the email body, subject and other detail
            _billingTemplate.SetupEmailTemplate(emailSenderModal);

            await _emailService.SendEmail(emailSenderModal);
        }

        public string GetBase64StringForImage(string imgPath)
        {
            byte[] imageBytes = System.IO.File.ReadAllBytes(imgPath);
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }
    }
}
