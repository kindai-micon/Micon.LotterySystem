using System;
using System.Collections.Generic;
using Micon.LotterySystem.Desktop.Models;
using Micon.LotterySystem.Desktop.ViewModels;

namespace Micon.LotterySystem.Desktop.Services;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }

    void NavigateToLogin();
    void NavigateToMain();
    void NavigateToReceipt(LotteryGroupInfo lotteryGroup);

    event Action? CurrentViewModelChanged;
    event Action<List<FailedTicketInfo>>? ShowFailedTicketsDialog;
}
