using System;

namespace Micon.LotterySystem.Desktop.Services;

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