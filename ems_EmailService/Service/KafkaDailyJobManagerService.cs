using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.Model;
using Bt.Lib.Common.Service.Services;
using Confluent.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EmailRequest.Modal;
using EmailRequest.Service.Interface;
using EmailRequest.Service.TemplateService;
using Newtonsoft.Json;

namespace EmailRequest.Service
{
    public class KafkaDailyJobManagerService(IServiceProvider _serviceProvider,
                                        ILogger<KafkaDailyJobManagerService> _logger,
                                        IDb _db,
                                        MicroserviceRegistry _microserviceRegistry,
                                        GitHubConnector _gitHubConnector)
    {
        public async Task SendEmailNotification(ConsumeResult<Ignore, string> result)
        {
            if (string.IsNullOrWhiteSpace(result.Message.Value))
                throw new Exception("[Kafka] Received invalid object from producer.");

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");

                KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);
                if (kafkaPayload == null)
                    throw new Exception("[Kafka] Received invalid object from producer.");

                var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_microserviceRegistry.DatabaseConfigurationUrl);
                _db.SetupConnectionString(DatabaseConfiguration.BuildConnectionString(masterDatabse));

                IEmailServiceRequest emailServiceRequest = null;
                switch (kafkaPayload.kafkaServiceName)
                {
                    case KafkaServiceName.DailyGreetingJob:
                        var commonNotificationRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                        _ = commonNotificationRequestService.SendDailyDigestEmailNotification(kafkaPayload);
                        _logger.LogInformation($"[Kafka] Message send: ");
                        break;
                    case KafkaServiceName.Attendance:
                        AttendanceRequestModal attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                        if (attendanceTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                        switch (attendanceTemplateModel.ActionType)
                        {
                            case AppConstants.Submitted:
                                emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceRequested>();
                                break;
                            case AppConstants.Approved:
                            case AppConstants.Rejected:
                                emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceAction>();
                                break;
                        }

                        await emailServiceRequest.SendEmailNotification(attendanceTemplateModel);
                        break;
                    case KafkaServiceName.Billing:
                        BillingTemplateModel billingTemplateModel = JsonConvert.DeserializeObject<BillingTemplateModel>(result.Message.Value);
                        if (billingTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for billing template modal from producer.");

                        var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
                        _ = billingService.SendEmailNotification(billingTemplateModel);
                        break;
                    case KafkaServiceName.Leave:
                        LeaveTemplateModel leaveTemplateModel = JsonConvert.DeserializeObject<LeaveTemplateModel>(result.Message.Value);
                        if (leaveTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for leave template modal from producer.");

                        var leaveRequested = scope.ServiceProvider.GetRequiredService<LeaveRequested>();
                        leaveRequested.SetupEmailTemplate(leaveTemplateModel);
                        break;
                    case KafkaServiceName.Payroll:
                        PayrollTemplateModel payrollTemplateModel = JsonConvert.DeserializeObject<PayrollTemplateModel>(result.Message.Value);
                        if (payrollTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for payroll template modal from producer.");

                        var payrollService = scope.ServiceProvider.GetRequiredService<PayrollService>();
                        payrollService.SendEmailNotification(payrollTemplateModel);
                        break;
                    case KafkaServiceName.BlockAttendance:
                        _logger.LogInformation($"[Kafka] Message received: Block attendance get");
                        AttendanceRequestModal attendanceTemplate = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
                        if (attendanceTemplate == null)
                            throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                        _logger.LogInformation($"[Kafka] Message received: {attendanceTemplate.ActionType}");
                        switch (attendanceTemplate.ActionType)
                        {
                            case AppConstants.Submitted:
                                emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceActionRequested>();
                                break;
                            case AppConstants.Approved:
                            case AppConstants.Rejected:
                                emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceAction>();
                                break;
                        }

                        _logger.LogInformation($"[Kafka] Starting sending request.");
                        await emailServiceRequest!.SendEmailNotification(attendanceTemplate);
                        break;
                    case KafkaServiceName.ForgotPassword:
                        _logger.LogInformation($"[Kafka] Message received: Forgot password get");
                        ForgotPasswordTemplateModel forgotPasswordTemplateModel = JsonConvert.DeserializeObject<ForgotPasswordTemplateModel>(result.Message.Value);
                        if (forgotPasswordTemplateModel == null)
                            throw new Exception("[Kafka] Received invalid object for forgot password template modal from producer.");

                        var forgotpasswordService = scope.ServiceProvider.GetRequiredService<ForgotPasswordRequested>();
                        _ = forgotpasswordService.SendEmailNotification(forgotPasswordTemplateModel);
                        break;
                    case KafkaServiceName.Timesheet:
                        _logger.LogInformation($"[Kafka] Message received: Timesheet get");
                        TimesheetApprovalTemplateModel timesheetSubmittedTemplate = JsonConvert.DeserializeObject<TimesheetApprovalTemplateModel>(result.Message.Value);
                        if (timesheetSubmittedTemplate == null)
                            throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

                        _logger.LogInformation($"[Kafka] Message received: {timesheetSubmittedTemplate.ActionType}");
                        switch (timesheetSubmittedTemplate.ActionType)
                        {
                            case AppConstants.Submitted:
                                var timesheetRequestedService = scope.ServiceProvider.GetRequiredService<TimesheetRequested>();
                                _logger.LogInformation($"[Kafka] Starting sending request.");
                                _ = timesheetRequestedService.SendEmailNotification(timesheetSubmittedTemplate);
                                break;
                            case AppConstants.Approved:
                            case AppConstants.Rejected:
                                var timesheetActionService = scope.ServiceProvider.GetRequiredService<TimesheetAction>();
                                _logger.LogInformation($"[Kafka] Starting sending request.");
                                _ = timesheetActionService.SendEmailNotification(timesheetSubmittedTemplate);
                                break;
                        }

                        break;
                    case KafkaServiceName.UnhandledException:
                        _logger.LogInformation($"[Kafka] Got unhandled exception");

                        if (kafkaPayload == null)
                            throw new Exception("[Kafka] Received invalid object. Getting null value.");

                        var comService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
                        _ = comService.SendUnhandledExceptionEmailNotification(kafkaPayload);
                        break;
                }
            }
        }
    }
}
