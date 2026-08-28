using CashFlow.Launch.Domain.Entities;
using CashFlow.Launch.Domain.Interfaces;
using CashFlow.Launch.Domain.Interfaces.Services;
using CashFlow.Launch.Domain.Models;
using CashFlow.Launch.Domain.Services;
using FluentAssertions;
using Moq;

namespace CashFlow.Tests;

public class MonthlyBalanceServiceTests
{
    private readonly Mock<IEntryRepository> _entries = new();
    private readonly Mock<ICreditCardInstallmentRepository> _installments = new();
    private readonly Mock<IRecurringEntryRepository> _recurring = new();
    private readonly Mock<IMonthlyBudgetService> _budgets = new();

    public MonthlyBalanceServiceTests()
    {
        _entries.Setup(x => x.GetByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync([]);
        _installments.Setup(x => x.GetByReferenceAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);
        _recurring.Setup(x => x.GetAllAsync()).ReturnsAsync([]);
        _budgets.Setup(x => x.GetSummaryAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((MonthlyBudgetSummary?)null);
    }

    private MonthlyBalanceService CreateService() => new(_entries.Object, _installments.Object, _recurring.Object, _budgets.Object);

    [Fact]
    public async Task PlannedProjection_ShouldAddOnlyBudgetRemainingAndCarryBalanceForward()
    {
        _budgets.Setup(x => x.GetSummaryAsync(2027, 1)).ReturnsAsync(new MonthlyBudgetSummary { Year = 2027, Month = 1, PlannedAmount = 60000m, ActualAmount = 10000m });

        var result = await CreateService().GetPlannedProjectionAsync(2027, 1, 2, 10000m);

        result.Months[0].PlannedExpenseAmount.Should().Be(50000m);
        result.Months[0].ClosingBalance.Should().Be(-40000m);
        result.Months[1].OpeningBalance.Should().Be(-40000m);
        result.FinalBalance.Should().Be(-40000m);
        result.HasNegativeMonth.Should().BeTrue();
    }

    [Fact]
    public async Task PlannedProjection_ShouldNotCreateNegativeRemainingWhenActualExceedsPlan()
    {
        _budgets.Setup(x => x.GetSummaryAsync(2027, 1)).ReturnsAsync(new MonthlyBudgetSummary { Year = 2027, Month = 1, PlannedAmount = 1000m, ActualAmount = 1500m });

        var result = await CreateService().GetPlannedProjectionAsync(2027, 1, 1, 10000m);

        result.Months[0].PlannedExpenseAmount.Should().Be(0m);
        result.Months[0].ClosingBalance.Should().Be(10000m);
    }

    [Fact]
    public async Task NormalProjection_ShouldIgnoreBudgetPlanning()
    {
        _budgets.Setup(x => x.GetSummaryAsync(2027, 1)).ReturnsAsync(new MonthlyBudgetSummary { Year = 2027, Month = 1, PlannedAmount = 60000m, ActualAmount = 0m });

        var result = await CreateService().GetProjectionAsync(2027, 1, 1, 10000m);

        result.Months[0].PlannedExpenseAmount.Should().Be(0m);
        result.Months[0].ClosingBalance.Should().Be(10000m);
        _budgets.Verify(x => x.GetSummaryAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
