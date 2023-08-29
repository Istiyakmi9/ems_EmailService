using EmailRequest.EMailService.Interface;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;

namespace EmailRequest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEMailManager _eMailManager;
        public EmailController(IEMailManager eMailManager)
        {
            _eMailManager = eMailManager;
        }

        [HttpPost("email/send")]
        public async Task SendEmail(EmailSenderModal emailSenderModal)
        {
            await _eMailManager.SendMailAsync(emailSenderModal);
        }
    }
}
