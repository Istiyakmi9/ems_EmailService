using CoreBottomHalf.CommonModal.HtmlTemplateModel;

namespace EmailRequest.Service.Interface
{
    public interface IEmailServiceRequest
    {
        Task SendEmailNotification(AttendanceRequestModal attendanceRequestModal);
    }
}
