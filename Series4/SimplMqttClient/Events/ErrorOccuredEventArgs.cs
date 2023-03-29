using System;

namespace SimplMqttClient.Events
{
    public class ErrorOccuredEventArgs : EventArgs
    {
        public string ErrorMessage { get; private set; }

        public ErrorOccuredEventArgs()
        {
            ;
        }

        public ErrorOccuredEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}