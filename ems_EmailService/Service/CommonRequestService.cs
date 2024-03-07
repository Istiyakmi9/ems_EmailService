using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Kafka;

namespace EmailRequest.Service
{
    public class CommonRequestService
    {
        private readonly IEmailService _emailService;
        public CommonRequestService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendUnhandledExceptionEmailNotification(KafkaPayload kafkaPayload)
        {
            EmailSenderModal emailSenderModal = new EmailSenderModal();


            emailSenderModal.Title = "[EMSTUM] UnHandled Exception";
            emailSenderModal.Subject = $"Exception time: {kafkaPayload.UtcTimestamp}";
            emailSenderModal.To = kafkaPayload.ToAddress != null
                ?
                    kafkaPayload.ToAddress
                :
                    new List<string> { "marghub12@gmail.com", "istiyaq.mi9@gmail.com" };
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.CommonException;
            if (kafkaPayload.exceptionPayloadetail != null)
            {
                emailSenderModal.Body = html.Replace("__USERMESSAGE__", kafkaPayload.exceptionPayloadetail.UserMessage)
                .Replace("__SYSTEMMESSAGE__", kafkaPayload.exceptionPayloadetail.SystemMessage)
                .Replace("__REQUESTBODY__", kafkaPayload.exceptionPayloadetail.RequestPayload)
                .Replace("__STACKTRACE__", kafkaPayload.exceptionPayloadetail.StackTrace);
            }
            else
            {
                emailSenderModal.Body = html.Replace("__USERMESSAGE__", "No error logged. Got some problem while sending messge. Please contact to admin.")
                .Replace("__SYSTEMMESSAGE__", "NA")
                .Replace("__REQUESTBODY__", "NA")
                .Replace("__STACKTRACE__", "NA");
            }


            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }

        public async Task SendEmailNotification(KafkaPayload kafkaPayload)
        {
            EmailSenderModal emailSenderModal = new EmailSenderModal();

            emailSenderModal.Title = "[EMSTUM] Notification";
            emailSenderModal.Subject = $"Daily digest [{kafkaPayload.UtcTimestamp}]";
            emailSenderModal.To = kafkaPayload.ToAddress;
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.CommonException;
            emailSenderModal.Body = html.Replace("__BODY__", kafkaPayload.Body);

            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }

        public async Task SendDailyDigestEmailNotification(KafkaPayload kafkaPayload)
        {
            EmailSenderModal emailSenderModal = new EmailSenderModal();

            emailSenderModal.Title = "[EMSTUM]: Your daily digest";
            emailSenderModal.Subject = $"Daily digest [{kafkaPayload.UtcTimestamp}]";
            emailSenderModal.To = kafkaPayload.ToAddress != null
                ?
                    kafkaPayload.ToAddress
                :
                    new List<string> { "marghub12@gmail.com", "istiyaq.mi9@gmail.com", "kumarvivek1502@gmail.com" };
            emailSenderModal.FileLocationDetail = new FileLocationDetail();

            var html = ApplicationResource.DailyNotification;
            emailSenderModal.Body = html.Replace("__BODY__", kafkaPayload.Message);

            await Task.Run(() => _emailService.SendEmail(emailSenderModal));
        }
    }
}
