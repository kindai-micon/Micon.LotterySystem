using System;
using System.Collections.Generic;
using QRQueue.Desktop.Models;
using QRQueue.Desktop.ViewModels;

namespace QRQueue.Desktop.Services;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }

    void NavigateToLogin();
    void NavigateToMain();
    void NavigateToReceipt(LotteryGroupInfo lotteryGroup);

    event Action? CurrentViewModelChanged;
    event Action<List<FailedTicketInfo>>? ShowFailedTicketsDialog;
}
