using System;
using System.Linq;

namespace SimplMqttClient.Utility
{
    public class PayloadMapper
    {
        public  ClientType ClientType { get; set; }

        public  string Map(string payload)
        {
            switch (ClientType)
            {
                case ClientType.DIGITAL:
                    {
                        return ConverToDigital(payload);
                    }
                case ClientType.ANALOG:
                    {
                        return ConvertToAnalog(payload);
                    }
                case ClientType.SERIAL:
                    {
                        return payload;
                    }
                default:
                    {
                        throw new ArgumentException("The declared type doesn't exists");
                    }
            }
        }

        private  string ConvertToAnalog(string payload)
        {
            try
            {
                if (payload.All(char.IsDigit))
                {
                    ushort.Parse(payload);
                    return payload;
                }
                else
                    throw new ArgumentException("The payload isn't Analog , payload received : " + payload );
            }
            catch (Exception)
            {
                throw;
            }
        }

        private  string ConverToDigital(string payload)
        {
            switch (payload.ToLower())
            {
                case "true":
                case "on":
                case "1": return "1";
                case "false":
                case "off":
                case "0": return "0";
                default: throw new ArgumentException("The payload is not digital");
            }
        }
    }
}