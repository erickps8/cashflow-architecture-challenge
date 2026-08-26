using CashFlow.Launch.Domain.Enums;
namespace CashFlow.Launch.Api.Dtos.Requests;
public class UpdateRecurringEntryRequest : CreateRecurringEntryRequest { public bool IsActive { get; set; } = true; }
