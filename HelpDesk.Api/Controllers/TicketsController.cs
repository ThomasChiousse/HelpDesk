using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Application.Services;
using HelpDesk.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketQueryService _ticketQueryService;
        private readonly TicketCreationService _ticketCreationService;
        private readonly TicketAssignmentService _ticketAssignmentService;

        public TicketsController(TicketQueryService ticketQueryService, TicketCreationService ticketCreationService, TicketAssignmentService ticketAssignmentService)
        {
            _ticketQueryService = ticketQueryService;
            _ticketCreationService = ticketCreationService;
            _ticketAssignmentService = ticketAssignmentService;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDetailsResponse>> GetById(int id, CancellationToken cancellationToken = default)
        {
            var ticket = await _ticketQueryService.GetByIdAsync(id, cancellationToken);

            var response = new TicketDetailsResponse(ticket.Id, ticket.Title, ticket.Description, ticket.Priority.ToString(), ticket.Status.ToString(), ticket.CreationDate,
                ticket.AssignedUser is null ? null : new UserResponse(ticket.AssignedUser.Id, ticket.AssignedUser.Firstname, ticket.AssignedUser.Lastname),
                ticket.Comments.Select(
                    c => new CommentResponse(
                        c.Id, c.Content, c.CreationDate, new UserResponse(
                            c.Author.Id, c.Author.Firstname, c.Author.Lastname))).ToList());
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<TicketDetailsResponse>> Create(CreateTicketRequest request, CancellationToken cancellationToken = default)
        {
            if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var priority)
                || !Enum.IsDefined(priority))
            {
                ModelState.AddModelError(nameof(request.Priority), "Unknown ticket priority.");

                return ValidationProblem(ModelState);
            }

            var ticket = await _ticketCreationService.CreateAsync(request.Title, request.Description, priority, cancellationToken);

            var response = new TicketDetailsResponse(
                ticket.Id,
                ticket.Title,
                ticket.Description,
                ticket.Priority.ToString(),
                ticket.Status.ToString(),
                ticket.CreationDate,
                null,
                []);

            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, response);
        }

        [HttpPut("{ticketId:int}/assignee/{userId:int}")]
        public async Task<IActionResult> AssignUser(int ticketId, int userId, CancellationToken cancellationToken = default)
        {
            await _ticketAssignmentService.AssignUserAsync(ticketId, userId, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{ticketId:int}/assignee/{userId:int}")]
        public async Task<IActionResult> UnassignUser(int ticketId, int userId, CancellationToken cancellationToken = default)
        {
            await _ticketAssignmentService.UnassignUserAsync(ticketId, userId, cancellationToken);
            return NoContent();
        }
    }
}
