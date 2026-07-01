using System;

namespace QRQueue.Desktop.Services;

public class TicketRenderException : Exception
{
    public TicketRenderException(string message)
        : base(message)
    {
    }

    public TicketRenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}