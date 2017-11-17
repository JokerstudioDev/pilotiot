using System;
namespace iothub
{
    public class PushNotiModel
    {
        public string NotificationType { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int Icon { get; set; }
        public object Content { get; set; }
    }
}
