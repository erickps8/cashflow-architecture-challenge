using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace CashFlow.Assistant.Api;

[McpServerToolType]
public sealed class CashFlowTools(CashFlowClient cashFlow)
{
    [McpServerTool, Description("Lista as contas financeiras do grupo autenticado no CashFlow.")]
    public Task<JsonElement> GetAccounts(CancellationToken cancellationToken) =>
        cashFlow.GetAsync("api/v1/accounts", cancellationToken);

    [McpServerTool, Description("Obtém o balanço financeiro de um mês do grupo autenticado, incluindo receitas, despesas, recorrências, cartão e saldo final.")]
    public Task<JsonElement> GetMonthlyBalance(
        [Description("Ano, por exemplo 2027")] int year,
        [Description("Mês de 1 a 12")] int month,
        [Description("Saldo de abertura do mês")] decimal openingBalance = 0,
        CancellationToken cancellationToken = default) =>
        cashFlow.GetAsync($"api/v1/balance/monthly/{year}/{month}?openingBalance={openingBalance.ToString(System.Globalization.CultureInfo.InvariantCulture)}", cancellationToken);

    [McpServerTool, Description("Projeta os próximos meses usando lançamentos, recorrências e parcelas de cartão já existentes no CashFlow.")]
    public Task<JsonElement> GetProjection(
        int startYear,
        int startMonth,
        [Description("Quantidade de meses, de 1 a 60")] int months = 12,
        decimal initialBalance = 0,
        CancellationToken cancellationToken = default) =>
        cashFlow.GetAsync($"api/v1/balance/projection?startYear={startYear}&startMonth={startMonth}&months={months}&initialBalance={initialBalance.ToString(System.Globalization.CultureInfo.InvariantCulture)}", cancellationToken);

    [McpServerTool, Description("Projeta o planejamento financeiro incluindo o orçamento planejado ainda não realizado.")]
    public Task<JsonElement> GetPlannedProjection(
        int startYear,
        int startMonth,
        int months = 12,
        decimal initialBalance = 0,
        CancellationToken cancellationToken = default) =>
        cashFlow.GetAsync($"api/v1/balance/planned-projection?startYear={startYear}&startMonth={startMonth}&months={months}&initialBalance={initialBalance.ToString(System.Globalization.CultureInfo.InvariantCulture)}", cancellationToken);
}
