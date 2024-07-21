using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EmailRequest.Modal;
using EmailRequest.Modal.Common;
using EmailRequest.Service.Interface;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class WelcomeNotification : IWelcomeNotification
    {
        private readonly IDb _db;
        private readonly MasterDatabase _masterDatabase;
        private readonly IEmailService _emailService;

        public WelcomeNotification(IDb db, IOptions<MasterDatabase> masterDatabase, IEmailService emailService)
        {
            _db = db;
            _masterDatabase = masterDatabase.Value;
            _emailService = emailService;
        }

        public async Task SendWelcomeNotification(WelcomeNotificationModal welcomeNotificationModal)
        {
            try
            {
                validateWelcomeNotificationDetail(welcomeNotificationModal);

                _db.SetupConnectionString(MasterDatabase.BuildConnectionString(_masterDatabase));

                var html = ApplicationResource.WelcomeNotification;
                html = html.Replace("__RECIPIENTNAME__", welcomeNotificationModal.RecipientName)
                    .Replace("__USERNAME__", welcomeNotificationModal.Email)
                    .Replace("__COMPANYCODE__", welcomeNotificationModal.OrgCode + welcomeNotificationModal.Code)
                    .Replace("__PASSWORD__", welcomeNotificationModal.Password)
                    .Replace("__COMPANYNAME__ ", welcomeNotificationModal.CompanyName);

                var title = $"Welcome Aboard! Your Account Details for {welcomeNotificationModal.CompanyName}";
                EmailSenderModal emailSenderModal = new EmailSenderModal
                {
                    Title = title,
                    Subject = title,
                    Body = html,
                    To = new List<string> { welcomeNotificationModal.Email },
                    FileLocationDetail = new FileLocationDetail()
                };

                await Task.Run(() => _emailService.SendEmail(emailSenderModal));

            }
            catch (Exception ex)
            {
                throw HiringBellException.ThrowBadRequest(ex.Message);
            }
        }

        private void validateWelcomeNotificationDetail(WelcomeNotificationModal welcomeNotificationModal)
        {
            if (string.IsNullOrEmpty(welcomeNotificationModal.OrgCode))
                throw HiringBellException.ThrowBadRequest("Invalid organization code");

            if (string.IsNullOrEmpty(welcomeNotificationModal.Code))
                throw HiringBellException.ThrowBadRequest("Invalid company code");

            if (string.IsNullOrEmpty(welcomeNotificationModal.Password))
                throw HiringBellException.ThrowBadRequest("Password is invalid");

            if (string.IsNullOrEmpty(welcomeNotificationModal.Email))
                throw HiringBellException.ThrowBadRequest("Email is invalid");

            if (string.IsNullOrEmpty(welcomeNotificationModal.RecipientName))
                throw HiringBellException.ThrowBadRequest("Recipient name is invalid");

            if (string.IsNullOrEmpty(welcomeNotificationModal.CompanyName))
                throw HiringBellException.ThrowBadRequest("Company name is invalid");
        }
    }
}
