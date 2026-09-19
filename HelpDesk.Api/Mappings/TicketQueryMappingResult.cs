using HelpDesk.Application.Tickets.Queries;

namespace HelpDesk.Api.Mappings
{
    public sealed record ValidationError(
        string Field,
        string Message);

    public sealed record TicketQueryMappingResult(
        TicketQueryOptions? Options,
        IReadOnlyCollection<ValidationError> Errors)
    {
        public bool IsSuccess => Errors.Count == 0;
    }
}
