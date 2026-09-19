using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Mappings;
using HelpDesk.Application.Sorting;
using HelpDesk.Domain;

namespace HelpDesk.Api.Tests;

public class TicketQueryMappingTests
{
    [Fact]
    public void ToQueryOptions_WithValidRequest_ShouldMapValues()
    {
        var request = new GetTicketsRequest
        {
            Status = "resolved",
            Priority = "high",
            SortBy = "title",
            SortDirection = "asc",
            Page = 2,
            PageSize = 10
        };

        var result = request.ToQueryOptions();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Options);

        Assert.Equal(TicketStatus.Resolved, result.Options.Status);
        Assert.Equal(TicketPriority.High, result.Options.Priority);
        Assert.Equal(TicketSortField.Title, result.Options.SortField);
        Assert.Equal(SortDirection.Ascending, result.Options.SortDirection);
        Assert.Equal(2, result.Options.Page);
        Assert.Equal(10, result.Options.PageSize);
    }

    [Fact]
    public void ToQueryOptions_WithNoSortingOption_ShouldSortByDescCreationDate()
    {
        var request = new GetTicketsRequest
        {
            Status = "resolved",
            Priority = "high",
            Page = 2,
            PageSize = 10
        };

        var result = request.ToQueryOptions();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Options);

        Assert.Equal(TicketStatus.Resolved, result.Options.Status);
        Assert.Equal(TicketPriority.High, result.Options.Priority);
        Assert.Equal(TicketSortField.CreationDate, result.Options.SortField);
        Assert.Equal(SortDirection.Descending, result.Options.SortDirection);
        Assert.Equal(2, result.Options.Page);
        Assert.Equal(10, result.Options.PageSize);
    }

    [Fact]
    public void ToQueryOptions_WithMultipleRequestErrors_ShouldReturnFailureAndMultipleErrors()
    {
        var request = new GetTicketsRequest
        {
            Status = "unexistantStatus",
            Priority = "unexistantPriority",
            SortBy = "giraffes",
            SortDirection = "quadratic",
            Page = 2,
            PageSize = 10
        };

        var result = request.ToQueryOptions();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Options);
        Assert.Equal(4, result.Errors.Count);
        Assert.Null(result.Options.Status);
        Assert.Null(result.Options.Priority);
        Assert.Equal(TicketSortField.CreationDate, result.Options.SortField); // because ToQueryOptions initializes sortField = 0 (0 => CreationDate in the SortField enum)
        Assert.Equal(SortDirection.Ascending, result.Options.SortDirection); // because ToQueryOptions initializes sortDirection = 0 (0 => Ascending in the SortDirection enum)
        Assert.Equal(2, result.Options.Page);
        Assert.Equal(10, result.Options.PageSize);
    }

    [Fact]
    public void ToQueryOptions_WhenHasAssigneeIsFalseAndAssignedUserIdIsPresent_ShouldReturnFailureAndErrorOnAssignedUserId()
    {
        var request = new GetTicketsRequest
        {
            HasAssignee = false,
            AssignedUserId = 1
        };

        var result = request.ToQueryOptions();
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Options);
        Assert.Single(result.Errors);
        Assert.Equal("AssignedUserId", result.Errors.First().Field);
        Assert.Equal("AssignedUserId cannot be used when HasAssignee is false.", result.Errors.First().Message);
    }
}
