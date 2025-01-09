using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.Model;
using EmailRequest.Service;

namespace EmailRequest.Middleware
{
    public class KafkaService : IHostedService
    {
        private ILogger<KafkaService> _logger;
        private readonly IKafkaConsumerService _kafkaConsumerService;
        private readonly KafkaGreetingJobManagerService _greetingJobManagerService;
        private readonly KafkaDailyJobManagerService _dailyJobManagerService;
        private readonly KafkaUnhandleExceptionService _kafkaUnhandleExceptionService;
        public KafkaService(ILogger<KafkaService> logger,
                             IKafkaConsumerService kafkaConsumerService,
                             KafkaGreetingJobManagerService greetingJobManagerService,
                             KafkaDailyJobManagerService dailyJobManagerService,
                             KafkaUnhandleExceptionService kafkaUnhandleExceptionService)
        {
            _logger = logger;
            _kafkaConsumerService = kafkaConsumerService;
            _greetingJobManagerService = greetingJobManagerService;
            _dailyJobManagerService = dailyJobManagerService;
            _kafkaUnhandleExceptionService = kafkaUnhandleExceptionService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Kafka] Kafka listener registered successfully.");

            _kafkaConsumerService.SubscribeTopic(_dailyJobManagerService.SendEmailNotification, nameof(KafkaTopicNames.DAILY_JOBS_MANAGER));
            _kafkaConsumerService.SubscribeTopic(_greetingJobManagerService.SendEmailNotification, nameof(KafkaTopicNames.ATTENDANCE_REQUEST_ACTION));
            _kafkaConsumerService.SubscribeTopic(_kafkaUnhandleExceptionService.SendEmailNotification, nameof(KafkaTopicNames.EXCEPTION_MESSAGE_BROKER));

            await Task.CompletedTask;
        }

        //private async Task SendEmailNotification(ConsumeResult<Null, string> result)
        //{
        //    if (string.IsNullOrWhiteSpace(result.Message.Value))
        //        throw new Exception("[Kafka] Received invalid object from producer.");

        //    using (IServiceScope scope = _serviceProvider.CreateScope())
        //    {
        //        _logger.LogInformation($"[Kafka] Message received: {result.Message.Value}");

        //        KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);
        //        if (kafkaPayload == null)
        //            throw new Exception("[Kafka] Received invalid object from producer.");

        //        var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<DatabaseConfiguration>(_microserviceRegistry.DatabaseConfigurationUrl);
        //        _db.SetupConnectionString(DatabaseConfiguration.BuildConnectionString(masterDatabse));

        //        IEmailServiceRequest emailServiceRequest = null;
        //        switch (kafkaPayload.kafkaServiceName)
        //        {
        //            case KafkaServiceName.Attendance:
        //                AttendanceRequestModal attendanceTemplateModel = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
        //                if (attendanceTemplateModel == null)
        //                    throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

        //                switch (attendanceTemplateModel.ActionType)
        //                {
        //                    case AppConstants.Submitted:
        //                        emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceRequested>();
        //                        break;
        //                    case AppConstants.Approved:
        //                    case AppConstants.Rejected:
        //                        emailServiceRequest = scope.ServiceProvider.GetRequiredService<AttendanceAction>();
        //                        break;
        //                }

        //                await emailServiceRequest.SendEmailNotification(attendanceTemplateModel);
        //                break;
        //            case KafkaServiceName.Billing:
        //                BillingTemplateModel billingTemplateModel = JsonConvert.DeserializeObject<BillingTemplateModel>(result.Message.Value);
        //                if (billingTemplateModel == null)
        //                    throw new Exception("[Kafka] Received invalid object for billing template modal from producer.");

        //                var billingService = scope.ServiceProvider.GetRequiredService<BillingService>();
        //                _ = billingService.SendEmailNotification(billingTemplateModel);
        //                break;
        //            case KafkaServiceName.Leave:
        //                LeaveTemplateModel leaveTemplateModel = JsonConvert.DeserializeObject<LeaveTemplateModel>(result.Message.Value);
        //                if (leaveTemplateModel == null)
        //                    throw new Exception("[Kafka] Received invalid object for leave template modal from producer.");

        //                var leaveRequested = scope.ServiceProvider.GetRequiredService<LeaveRequested>();
        //                leaveRequested.SetupEmailTemplate(leaveTemplateModel);
        //                break;
        //            case KafkaServiceName.Payroll:
        //                PayrollTemplateModel payrollTemplateModel = JsonConvert.DeserializeObject<PayrollTemplateModel>(result.Message.Value);
        //                if (payrollTemplateModel == null)
        //                    throw new Exception("[Kafka] Received invalid object for payroll template modal from producer.");

        //                var payrollService = scope.ServiceProvider.GetRequiredService<PayrollService>();
        //                payrollService.SendEmailNotification(payrollTemplateModel);
        //                break;
        //            case KafkaServiceName.BlockAttendance:
        //                _logger.LogInformation($"[Kafka] Message received: Block attendance get");
        //                AttendanceRequestModal attendanceTemplate = JsonConvert.DeserializeObject<AttendanceRequestModal>(result.Message.Value);
        //                if (attendanceTemplate == null)
        //                    throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

        //                _logger.LogInformation($"[Kafka] Message received: {attendanceTemplate.ActionType}");
        //                switch (attendanceTemplate.ActionType)
        //                {
        //                    case AppConstants.Submitted:
        //                        emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceActionRequested>();
        //                        break;
        //                    case AppConstants.Approved:
        //                    case AppConstants.Rejected:
        //                        emailServiceRequest = scope.ServiceProvider.GetRequiredService<BlockAttendanceAction>();
        //                        break;
        //                }

        //                _logger.LogInformation($"[Kafka] Starting sending request.");
        //                await emailServiceRequest!.SendEmailNotification(attendanceTemplate);
        //                break;
        //            case KafkaServiceName.ForgotPassword:
        //                _logger.LogInformation($"[Kafka] Message received: Forgot password get");
        //                ForgotPasswordTemplateModel forgotPasswordTemplateModel = JsonConvert.DeserializeObject<ForgotPasswordTemplateModel>(result.Message.Value);
        //                if (forgotPasswordTemplateModel == null)
        //                    throw new Exception("[Kafka] Received invalid object for forgot password template modal from producer.");

        //                var forgotpasswordService = scope.ServiceProvider.GetRequiredService<ForgotPasswordRequested>();
        //                _ = forgotpasswordService.SendEmailNotification(forgotPasswordTemplateModel);
        //                break;
        //            case KafkaServiceName.Timesheet:
        //                _logger.LogInformation($"[Kafka] Message received: Timesheet get");
        //                TimesheetApprovalTemplateModel timesheetSubmittedTemplate = JsonConvert.DeserializeObject<TimesheetApprovalTemplateModel>(result.Message.Value);
        //                if (timesheetSubmittedTemplate == null)
        //                    throw new Exception("[Kafka] Received invalid object for attendance template modal from producer.");

        //                _logger.LogInformation($"[Kafka] Message received: {timesheetSubmittedTemplate.ActionType}");
        //                switch (timesheetSubmittedTemplate.ActionType)
        //                {
        //                    case AppConstants.Submitted:
        //                        var timesheetRequestedService = scope.ServiceProvider.GetRequiredService<TimesheetRequested>();
        //                        _logger.LogInformation($"[Kafka] Starting sending request.");
        //                        _ = timesheetRequestedService.SendEmailNotification(timesheetSubmittedTemplate);
        //                        break;
        //                    case AppConstants.Approved:
        //                    case AppConstants.Rejected:
        //                        var timesheetActionService = scope.ServiceProvider.GetRequiredService<TimesheetAction>();
        //                        _logger.LogInformation($"[Kafka] Starting sending request.");
        //                        _ = timesheetActionService.SendEmailNotification(timesheetSubmittedTemplate);
        //                        break;
        //                }

        //                break;
        //            case KafkaServiceName.Common:
        //                _logger.LogInformation($"[Kafka] Message received: Timesheet get");
        //                if (kafkaPayload == null)
        //                    throw new Exception("[Kafka] Received invalid object. Getting null value.");

        //                var commonRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
        //                _logger.LogInformation($"[Kafka] Starting sending request.");

        //                _ = commonRequestService.SendDailyDigestEmailNotification(kafkaPayload);

        //                _logger.LogInformation($"[Kafka] Message received: ");

        //                break;
        //            case KafkaServiceName.UnhandledException:
        //                _logger.LogInformation($"[Kafka] Got unhandled exception");

        //                if (kafkaPayload == null)
        //                    throw new Exception("[Kafka] Received invalid object. Getting null value.");

        //                var comService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
        //                _ = comService.SendUnhandledExceptionEmailNotification(kafkaPayload);
        //                break;
        //            case KafkaServiceName.DailyGreetingJob:
        //                _logger.LogInformation($"[Kafka] Message received: DailyGreetingJob");
        //                if (kafkaPayload == null)
        //                    throw new Exception("[Kafka] Received invalid object. Getting null value.");

        //                var commonNotificationRequestService = scope.ServiceProvider.GetRequiredService<CommonRequestService>();
        //                _logger.LogInformation($"[Kafka] Starting sending request.");

        //                _ = commonNotificationRequestService.SendDailyDigestEmailNotification(kafkaPayload);

        //                _logger.LogInformation($"[Kafka] Message received: ");
        //                break;
        //        }
        //    }
        //}

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping service.");
            return Task.CompletedTask;
        }
    }
}
