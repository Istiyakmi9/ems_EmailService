using EmailRequest.Service.TemplateService;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly BillingTemplate _billingTemplate;
        private readonly AttendanceTemplate _attendanceTemplate;
        private readonly AutoLeaveMigrationTemplate _autoLeaveMigrationTemplate;
        private readonly AttendanceApprovalTemplate _attendanceApprovalTemplate;
        private readonly ForgotPasswordTemplate _forgotPasswordTemplate;
        private readonly LeaveApprovalTemplate _leaveApprovalTemplate;
        private readonly LeaveRequestTemplate _leaveRequestTemplate;
        private readonly NewRegistrationTemplate _newRegistrationTemplate1;
        private readonly OfferLetterTemplate _offerLetterTemplate;
        private readonly PayrollTemplate _payrollTemplate;
        private readonly TimesheetApprovalTemplate _timesheetApprovalTemplate;
        private readonly TimesheetTemplate _timesheetTemplate;

        public EmailController(BillingTemplate billingTemplate, 
            AttendanceTemplate attendanceTemplate, 
            AutoLeaveMigrationTemplate autoLeaveMigrationTemplate, 
            AttendanceApprovalTemplate attendanceApprovalTemplate, 
            ForgotPasswordTemplate forgotPasswordTemplate, 
            LeaveApprovalTemplate leaveApprovalTemplate, 
            LeaveRequestTemplate leaveRequestTemplate, 
            NewRegistrationTemplate newRegistrationTemplate1, 
            OfferLetterTemplate offerLetterTemplate, 
            PayrollTemplate payrollTemplate, 
            TimesheetApprovalTemplate timesheetApprovalTemplate, 
            TimesheetTemplate timesheetTemplate)
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
            _billingTemplate.SetupEmailTemplate(billingTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/AttendanceEmail")]
        public async Task AttendanceEmail(AttendanceTemplateModel attendanceTemplateModel)
        {
            _attendanceTemplate.SetupEmailTemplate(attendanceTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/AutoLeaveMigrationEmail")]
        public async Task AutoLeaveMigrationEmail(AutoLeaveMigrationTemplateModel autoLeaveMigrationTemplateModel)
        {
            _autoLeaveMigrationTemplate.SetupEmailTemplate(autoLeaveMigrationTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/AttendanceApprovalEmail")]
        public async Task AttendanceApprovalEmail(AttendanceApprovalTemplateModel attendanceApprovalTemplateModel)
        {
            _attendanceApprovalTemplate.SetupEmailTemplate(attendanceApprovalTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/ForgotPasswordEmail")]
        public async Task ForgotPasswordEmail(ForgotPasswordTemplateModel forgotPasswordTemplateModel)
        {
            _forgotPasswordTemplate.SetupEmailTemplate(forgotPasswordTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/LeaveApprovalEmail")]
        public async Task LeaveApprovalEmail(LeaveApprovalTemplateModel leaveApprovalTemplateModel)
        {
            _leaveApprovalTemplate.SetupEmailTemplate(leaveApprovalTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/LeaveRequestEmail")]
        public async Task LeaveRequestEmail(LeaveRequestTemplateModel leaveRequestTemplateModel)
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
            _payrollTemplate.SetupEmailTemplate(payrollTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/TimesheetApprovalEmail")]
        public async Task TimesheetApprovalEmail(TimesheetApprovalTemplateModel timesheetApprovalTemplateModel)
        {
            _timesheetApprovalTemplate.SetupEmailTemplate(timesheetApprovalTemplateModel);
            await Task.CompletedTask;
        }

        [HttpPost("Email/TimesheetEmail")]
        public async Task TimesheetEmail(TimesheetSubmittedTemplateModel timesheetSubmittedTemplateModel)
        {
            _timesheetTemplate.SetupEmailTemplate(timesheetSubmittedTemplateModel);
            await Task.CompletedTask;
        }
    }
}
