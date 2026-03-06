using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Micon.LotterySystem.Desktop.Views;

public partial class MessageDialog : Window
{
    public List<FailedTicketDisplay> FailedTickets { get; }

    public MessageDialog()
    {
        InitializeComponent();
        FailedTickets = [];
        DataContext = this;
    }

    public MessageDialog(List<(long Number, string Reason)> failedTickets) : this()
    {
        FailedTickets = failedTickets.ConvertAll(t => new FailedTicketDisplay
        {
            Number = t.Number,
            Reason = t.Reason
        });
    }

    [RelayCommand]
    private void CloseDialog()
    {
        this.Close();
    }
}

public class FailedTicketDisplay
{
    public long Number { get; set; }
    public string Reason { get; set; } = string.Empty;
}
