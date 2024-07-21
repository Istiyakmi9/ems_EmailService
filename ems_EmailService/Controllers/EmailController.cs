using Bot.CoreBottomHalf.CommonModal.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using EmailRequest.Service;
using EmailRequest.Service.Interface;
using EmailRequest.Service.TemplateService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailRequest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly BillingService _billingTemplate;
        private readonly AttendanceRequested _attendanceTemplate;
        private readonly AutoLeaveMigrationTemplate _autoLeaveMigrationTemplate;
        private readonly AttendanceAction _attendanceApprovalTemplate;
        private readonly ForgotPasswordRequested _forgotPasswordTemplate;
        private readonly LeaveApprovalTemplate _leaveApprovalTemplate;
        private readonly LeaveRequested _leaveRequestTemplate;
        private readonly NewRegistrationTemplate _newRegistrationTemplate1;
        private readonly OfferLetterTemplate _offerLetterTemplate;
        private readonly PayrollService _payrollTemplate;
        private readonly TimesheetAction _timesheetApprovalTemplate;
        private readonly TimesheetRequested _timesheetTemplate;
        private readonly CommonRequestService _commonRequestService;
        private readonly IWelcomeNotification _welcomeNotification;
        public EmailController(BillingService billingTemplate,
            AttendanceRequested attendanceTemplate,
            AutoLeaveMigrationTemplate autoLeaveMigrationTemplate,
            AttendanceAction attendanceApprovalTemplate,
            ForgotPasswordRequested forgotPasswordTemplate,
            LeaveApprovalTemplate leaveApprovalTemplate,
            LeaveRequested leaveRequestTemplate,
            NewRegistrationTemplate newRegistrationTemplate1,
            OfferLetterTemplate offerLetterTemplate,
            PayrollService payrollTemplate,
            TimesheetAction timesheetApprovalTemplate,
            TimesheetRequested timesheetTemplate,
            CommonRequestService commonRequestService,
            IWelcomeNotification welcomeNotification)
        {
            _billingTemplate = billingTemplate;
            _attendanceTemplate = attendanceTemplate;
            _autoLeaveMigrationTemplate = autoLeaveMigrationTemplate;
            _attendanceApprovalTemplate = attendanceApprovalTemplate;
            _forgotPasswordTemplate = forgotPasswordTemplate;
            _leaveApprovalTemplate = leaveApprovalTemplate;
            _leaveRequestTemplate = leaveRequestTemplate;
            _newRegistrationTemplate1 = newRegistrationTemplate1;
            _offerLetterTemplate = offerLetterTemplate;
            _payrollTemplate = payrollTemplate;
            _timesheetApprovalTemplate = timesheetApprovalTemplate;
            _timesheetTemplate = timesheetTemplate;
            _commonRequestService = commonRequestService;
            _welcomeNotification = welcomeNotification;
        }

        [HttpPost("Email/NewRegistration")]
        public async Task NewRegistration(NewRegistrationTemplateModel newRegistrationTemplate)
        {
            _newRegistrationTemplate1.SetupEmailTemplate(newRegistrationTemplate);
            await Task.CompletedTask;
        }

        [HttpPost("Email/BillingEmail")]
        public async Task BillingEmail(BillingTemplateModel billingTemplateModel)
        {
            await _billingTemplate.SendEmailNotification(billingTemplateModel);
        }

        [HttpPost("Email/AttendanceEmail")]
        public async Task AttendanceEmail(AttendanceRequestModal attendanceTemplateModel)
        {
            await _attendanceTemplate.SendEmailNotification(attendanceTemplateModel);
        }

        [HttpPost("Email/AutoLeaveMigrationEmail")]
        public async Task AutoLeaveMigrationEmail(AutoLeaveMigrationTemplateModel autoLeaveMigrationTemplateModel)
        {
            _autoLeaveMigrationTemplate.SetupEmailTemplate(autoLeaveMigrationTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/AttendanceApprovalEmail")]
        public async Task AttendanceApprovalEmail(AttendanceRequestModal attendanceRequestModal)
        {
            await _attendanceApprovalTemplate.SendEmailNotification(attendanceRequestModal);
        }

        [HttpPost("Email/ForgotPasswordEmail")]
        public async Task ForgotPasswordEmail(ForgotPasswordTemplateModel forgotPasswordTemplateModel)
        {
            await _forgotPasswordTemplate.SendEmailNotification(forgotPasswordTemplateModel);
        }

        [HttpPost("Email/LeaveApprovalEmail")]
        public async Task LeaveApprovalEmail(LeaveTemplateModel leaveApprovalTemplateModel)
        {
            _leaveApprovalTemplate.SetupEmailTemplate(leaveApprovalTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/LeaveRequestEmail")]
        public async Task LeaveRequestEmail(LeaveTemplateModel leaveRequestTemplateModel)
        {
            _leaveRequestTemplate.SetupEmailTemplate(leaveRequestTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/OfferLetterEmail")]
        public async Task OfferLetterEmail(OfferLetterTemplateModel offerLetterTemplateModel)
        {
            _offerLetterTemplate.SetupEmailTemplate(offerLetterTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/PayrollEmail")]
        public async Task PayrollEmail(PayrollTemplateModel payrollTemplateModel)
        {
            _payrollTemplate.SendEmailNotification(payrollTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/TimesheetApprovalEmail")]
        public async Task TimesheetApprovalEmail(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            await _timesheetApprovalTemplate.SendEmailNotification(timesheetApprovalTemplateModel);
        }

        [HttpPost("Email/TimesheetEmail")]
        public async Task TimesheetEmail(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            await _timesheetTemplate.SendEmailNotification(timesheetApprovalTemplateModel);
        }

        [HttpPost("Email/Exception")]
        public async Task TimesheetEmail(KafkaPayload kafkaPayload)
        {
            await _commonRequestService.SendEmailNotification(kafkaPayload);
        }

        [AllowAnonymous]
        [HttpPost("SendWelcomeNotification")]
        public async Task SendWelcomeNotification([FromBody] WelcomeNotificationModal welcomeNotificationModal)
        {
            await _welcomeNotification.SendWelcomeNotification(welcomeNotificationModal);
        }
    }
}
