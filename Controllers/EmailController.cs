using EmailRequest.Service.TemplateService;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly NewRegistrationTemplate _newRegistrationTemplate;
        public EmailController(NewRegistrationTemplate newRegistrationTemplate)
        {
            _newRegistrationTemplate = newRegistrationTemplate;
        }

        [HttpPost("Email/NewRegistration")]
        public async Task NewRegistration(NewRegistrationTemplateModel newRegistrationTemplate)
        {
            _newRegistrationTemplate.SetupEmailTemplate(newRegistrationTemplate);
            await Task.CompletedTask;
        }
    }
}
