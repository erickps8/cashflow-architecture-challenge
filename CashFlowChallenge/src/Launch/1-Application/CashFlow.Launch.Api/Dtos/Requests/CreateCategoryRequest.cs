using CashFlow.Launch.Domain.Enums;

namespace CashFlow.Launch.Api.Dtos.Requests;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public EntryType Type { get; set; }
}
