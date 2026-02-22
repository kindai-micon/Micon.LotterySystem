using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Micon.LotterySystem.Desktop.Models;

public class LotteryGroupInfo
{
    [JsonPropertyName("displayId")]
    public Guid DisplayId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class IssueTicketsRequest
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("lotteryGroupId")]
    public Guid LotteryGroupId { get; set; }
}

public class IssueTicketsResponse
{
    [JsonPropertyName("tickets")]
    public List<TicketInfo> Tickets { get; set; } = [];

    [JsonPropertyName("lotteryGroupName")]
    public string? LotteryGroupName { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class TicketInfo
{
    [JsonPropertyName("displayId")]
    public Guid DisplayId { get; set; }

    [JsonPropertyName("number")]
    public long Number { get; set; }

    [JsonPropertyName("qrCodeUrl")]
    public string? QrCodeUrl { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("issuedAt")]
    public DateTimeOffset IssuedAt { get; set; }
}

public class CompleteTicketRequest
{
    [JsonPropertyName("activate")]
    public bool Activate { get; set; }
}

public class CompleteTicketResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
