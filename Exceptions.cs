using System;
using System.Runtime.Serialization;

namespace Jannesen.Configuration.Settings
{
    public class AppSettingException: Exception
    {
        public          AppSettingException()
        {
        }
        public          AppSettingException(string message): base(message)
        {
        }
        public          AppSettingException(string message, Exception exception): base(message, exception)
        {
        }
    }
}
