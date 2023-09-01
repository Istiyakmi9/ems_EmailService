using ModalLayer.Modal.HtmlTemplateModel;

namespace EmailRequest.Service.Interface
{
    public interface IEmailServiceRequest
    {
        void SetupEmailTemplate(AttendanceRequestModal attendanceRequestModal);
    }
}
