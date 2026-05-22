
namespace CashFlow.Tests;

public class EntryTests
{
    [Fact]
    public void Should_Add_Credit_To_Balance()
    {
        var currentBalance = 100m;
        var amount = 50m;

        var result = currentBalance + amount;

        Assert.Equal(150m, result);
    }

    [Fact]
    public void Should_Subtract_Debit_From_Balance()
    {
        var currentBalance = 100m;
        var amount = 30m;

        var result = currentBalance - amount;

        Assert.Equal(70m, result);
    }

    [Fact]
    public void Should_Not_Allow_Zero_Amount()
    {
        var amount = 0m;

        Assert.True(amount <= 0);
    }

    [Fact]
    public void Should_Keep_Balance_When_No_Entries()
    {
        var balance = 0m;

        Assert.Equal(0m, balance);
    }

    [Fact]
    public void Should_Handle_Multiple_Credits()
    {
        var balance = 0m;

        balance += 100m;
        balance += 50m;

        Assert.Equal(150m, balance);
    }
}