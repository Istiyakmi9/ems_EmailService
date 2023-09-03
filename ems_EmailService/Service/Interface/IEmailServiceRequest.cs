using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.Interface
{
    public interface IEmailServiceRequest
    {
        Task SendEmailNotification(AttendanceRequestModal attendanceRequestModal);
    }
}
