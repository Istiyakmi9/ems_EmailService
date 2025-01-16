using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using EmailRequest.Modal;
using EmailRequest.Service.Interface;
using ModalLayer.Modal;

namespace EmailRequest.Service.TemplateService
{
    public class WelcomeNotification : IWelcomeNotification
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        private readonly GitHubConnector _gitHubConnector;
        private readonly MicroserviceRegistry _microserviceRegistry;
        public WelcomeNotification(IDb db,
            IEmailService emailService,
            MicroserviceRegistry microserviceRegistry,
            GitHubConnector gitHubConnector)
        {
            _db = db;
            _emailService = emailService;
            _microserviceRegistry = microserviceRegistry;
            _gitHubConnector = gitHubConnector;
        }

        public async Task SendWelcomeNotification(WelcomeNotificationModal welcomeNotificationModal)
        {
            try
            {
                validateWelcomeNotificationDetail(welcomeNotificationModal);

                var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_microserviceRegistry.DatabaseConfigurationUrl);
                _db.SetupConnectionString(DatabaseConfiguration.BuildConnectionString(masterDatabse));

                var html = ApplicationResource.WelcomeNotification;
                html = html.Replace("__RECIPIENTNAME__", welcomeNotificationModal.RecipientName)
                    .Replace("__USERNAME__", welcomeNotificationModal.Email)
                    .Replace("__COMPANYCODE__", welcomeNotificationModal.OrgCode + welcomeNotificationModal.Code)
                    .Replace("__PASSWORD__", welcomeNotificationModal.Password)
                    .Replace("__COMPANYNAME__", welcomeNotificationModal.CompanyName);

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
