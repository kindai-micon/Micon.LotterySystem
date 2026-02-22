using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Micon.LotterySystem.Desktop.Models;

namespace Micon.LotterySystem.Desktop.Services;

public class LocalStorageService : ILocalStorageService
{
    private const string RecordsFileName = "issued_tickets.json";
    private readonly string _recordsFilePath;
    private readonly ILogger<LocalStorageService> _logger;
    private List<LotteryGroupRecords> _allRecords = [];

    public LocalStorageService(ILogger<LocalStorageService> logger)
    {
        _logger = logger;
        _recordsFilePath = Path.Combine(Directory.GetCurrentDirectory(), RecordsFileName);
        LoadRecords();
    }

    private void LoadRecords()
    {
        if (!File.Exists(_recordsFilePath))
            return;

        try
        {
            var json = File.ReadAllText(_recordsFilePath);
            _allRecords = JsonSerializer.Deserialize<List<LotteryGroupRecords>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "発行履歴の読み込みに失敗しました。履歴をクリアします。");
            _allRecords = [];
        }
    }

    private void SaveRecords()
    {
        try
        {
            var json = JsonSerializer.Serialize(_allRecords, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_recordsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "発行履歴の保存に失敗しました。");
        }
    }

    public List<IssuedTicketRecord> GetRecords(Guid lotteryGroupDisplayId)
    {
        var groupRecords = _allRecords.FirstOrDefault(r => r.LotteryGroupDisplayId == lotteryGroupDisplayId);
        return groupRecords?.Tickets.OrderByDescending(t => t.IssuedAt).ToList() ?? [];
    }

    public void AddRecords(Guid lotteryGroupDisplayId, string lotteryGroupName, IEnumerable<IssuedTicketRecord> records)
    {
        var groupRecords = _allRecords.FirstOrDefault(r => r.LotteryGroupDisplayId == lotteryGroupDisplayId);

        if (groupRecords == null)
        {
            groupRecords = new LotteryGroupRecords
            {
                LotteryGroupDisplayId = lotteryGroupDisplayId,
                LotteryGroupName = lotteryGroupName
            };
            _allRecords.Add(groupRecords);
        }
        else
        {
            groupRecords.LotteryGroupName = lotteryGroupName;
        }

        groupRecords.Tickets.AddRange(records);
        SaveRecords();
    }

    public void UpdateTicketStatus(Guid displayId, string newStatus)
    {
        foreach (var group in _allRecords)
        {
            var ticket = group.Tickets.FirstOrDefault(t => t.DisplayId == displayId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                SaveRecords();
                return;
            }
        }
    }

    public List<LotteryGroupRecords> GetAllRecords()
    {
        return _allRecords;
    }

    public void ClearRecords(Guid lotteryGroupDisplayId)
    {
        var groupRecords = _allRecords.FirstOrDefault(r => r.LotteryGroupDisplayId == lotteryGroupDisplayId);
        if (groupRecords != null)
        {
            _allRecords.Remove(groupRecords);
            SaveRecords();
        }
    }
}
