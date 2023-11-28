using System.Runtime.Serialization;

namespace DellServerSilencer;
[Serializable]
internal class InvalidTemperatureReadingException : Exception
{
    public InvalidTemperatureReadingException(string reading) : base($"Invalid temperature reading: '{reading}'")
    {
    }

    //public InvalidTemperatureReadingException(string? message) : base(message)
    //{
    //}

    public InvalidTemperatureReadingException(string reading, Exception? innerException) : base($"Invalid temperature reading: '{reading}'", innerException)
    {
    }

    protected InvalidTemperatureReadingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}