using DMS.Domain.Entities;
using DMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DMS.UnitTests;

public sealed class SalesOrderDomainTests
{
    [Fact]
    public void Recalculate_totals_sums_line_amounts_and_vat()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m },
                new SalesOrderDetail { Quantity = 1.5m, UnitPrice = 20m, VatRate = 10m }
            }
        };

        order.RecalculateTotals();

        order.SubTotal.Should().Be(230m);
        order.VatAmount.Should().Be(19m);
        order.GrandTotal.Should().Be(249m);
    }

    [Fact]
    public void Submit_moves_draft_order_to_submitted()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m }
            }
        };

        order.Submit();

        order.Status.Should().Be(SalesOrderStatus.Submitted);
    }

    [Fact]
    public void Submit_rejects_non_draft_order()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m }
            }
        };
        order.Submit();

        Action action = order.Submit;

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft sales orders can be submitted.");
    }

    [Fact]
    public void Submit_rejects_order_without_lines()
    {
        var order = new SalesOrder();

        Action action = order.Submit;

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Sales order must contain at least one line.");
    }

    [Fact]
    public void Approve_moves_submitted_order_to_approved()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m }
            }
        };
        order.Submit();

        order.Approve();

        order.Status.Should().Be(SalesOrderStatus.Approved);
    }

    [Fact]
    public void Approve_rejects_non_submitted_order()
    {
        var order = new SalesOrder();

        Action action = order.Approve;

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only submitted sales orders can be approved.");
    }

    [Fact]
    public void Cancel_moves_submitted_order_to_cancelled()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m }
            }
        };
        order.Submit();

        order.Cancel();

        order.Status.Should().Be(SalesOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_rejects_approved_order()
    {
        var order = new SalesOrder
        {
            Details =
            {
                new SalesOrderDetail { Quantity = 2m, UnitPrice = 100m, VatRate = 8m }
            }
        };
        order.Submit();
        order.Approve();

        Action action = order.Cancel;

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only draft or submitted sales orders can be cancelled.");
    }
}
