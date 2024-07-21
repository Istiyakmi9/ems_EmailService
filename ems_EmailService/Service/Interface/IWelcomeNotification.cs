using EmailRequest.Modal;

namespace EmailRequest.Service.Interface
{
    public interface IWelcomeNotification
    {
        Task SendWelcomeNotification(WelcomeNotificationModal welcomeNotificationModal);
    }
}
