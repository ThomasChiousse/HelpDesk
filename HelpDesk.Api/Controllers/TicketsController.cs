using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Mappings;
using HelpDesk.Application.Services;
using HelpDesk.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly TicketQueryService _ticketQueryService;
    private readonly TicketCreationService _ticketCreationService;
    private readonly TicketAssignmentService _ticketAssignmentService;
    private readonly TicketCommentService _ticketCommentService;
    private readonly TicketStatusService _ticketStatusService;
    private readonly TicketUpdateService _ticketUpdateService;
    private readonly TicketPatchService _ticketPatchService;
    public TicketsController(TicketQueryService ticketQueryService, TicketCreationService ticketCreationService, TicketAssignmentService ticketAssignmentService,
        TicketCommentService ticketCommentService, TicketStatusService ticketStatusService, TicketUpdateService ticketUpdateService, TicketPatchService ticketPatchService)
    {
        _ticketQueryService = ticketQueryService;
        _ticketCreationService = ticketCreationService;
        _ticketAssignmentService = ticketAssignmentService;
        _ticketCommentService = ticketCommentService;
        _ticketStatusService = ticketStatusService;
        _ticketUpdateService = ticketUpdateService;
        _ticketPatchService = ticketPatchService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailsResponse>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketQueryService.GetByIdAsync(id, cancellationToken);
        return Ok(ticket.ToDetailsResponse());
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

    [HttpPost("{ticketId:int}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(int ticketId, CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _ticketCommentService.AddCommentAsync(ticketId, request.AuthorId, request.Content, cancellationToken);
        var response = new CommentResponse(
            comment.Id,
            comment.Content,
            comment.CreationDate,
            new UserResponse(comment.Author.Id, comment.Author.Firstname, comment.Author.Lastname));
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{ticketId:int}/status")]
    public async Task<ActionResult<TicketStatusResponse>> AdvanceStatus(int ticketId, CancellationToken cancellationToken = default)
    {
        var status = await _ticketStatusService.AdvanceStatusAsync(ticketId, cancellationToken);
        return Ok(new TicketStatusResponse(status.ToString()));
    }

    [HttpPut("{ticketId:int}")]
    public async Task<ActionResult<TicketDetailsResponse>> Update(int ticketId, UpdateTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var priority)
            || !Enum.IsDefined(priority))
        {
            ModelState.AddModelError(nameof(request.Priority), "Unknown ticket priority.");

            return ValidationProblem(ModelState);
        }

        await _ticketUpdateService.UpdateAsync(ticketId, request.Title, request.Description, priority, cancellationToken);
        var updatedTicket = await _ticketQueryService.GetByIdAsync(ticketId, cancellationToken);
        return Ok(updatedTicket.ToDetailsResponse());
    }

    [HttpPatch("{ticketId:int}")]
    public async Task<ActionResult<TicketDetailsResponse>> Patch(int ticketId, PatchTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Title is null && request.Description is null && request.Priority is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "At least one field must be provided.");

            return ValidationProblem(ModelState);
        }

        TicketPriority? priority = null;
        if (request.Priority is not null)
        {
            if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var parsedPriority) || !Enum.IsDefined(parsedPriority))
            {
                ModelState.AddModelError(
                    nameof(request.Priority),
                    "Unknown ticket priority.");

                return ValidationProblem(ModelState);
            }
            priority = parsedPriority;
        }

        await _ticketPatchService.PatchAsync(ticketId, request.Title, request.Description, priority, cancellationToken);

        var ticket = await _ticketQueryService.GetByIdAsync(ticketId, cancellationToken);
        return Ok(ticket.ToDetailsResponse());
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TicketListItemResponse>>> GetAll(
    [FromQuery] GetTicketsRequest request, CancellationToken cancellationToken = default)
    {

        var mapping = request.ToQueryOptions();

        if (!mapping.IsSuccess)
        {
            foreach (var error in mapping.Errors)
            {
                ModelState.AddModelError(
                    error.Field,
                    error.Message);
            }

            return ValidationProblem(ModelState);
        }

        var result = await _ticketQueryService.GetPagedAsync(mapping.Options!, cancellationToken);
        var items = result.Items.Select(t => new TicketListItemResponse(
          t.Id, t.Title, t.Priority.ToString(), t.Status.ToString(), t.CreationDate
           )).ToList();

        var response = new PagedResponse<TicketListItemResponse>(items, request.Page, request.PageSize, result.TotalCount);
        return Ok(response);
    }

    [HttpGet("{ticketId}/comments")]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> GetComments(
        int ticketId, [FromQuery] GetCommentsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _ticketQueryService.GetCommentsPagedAsync(ticketId, request.Page, request.PageSize, cancellationToken);

        var items = result.Items.Select(c => new CommentResponse(
            c.Id,
            c.Content,
            c.CreationDate,
            new UserResponse(c.Author.Id, c.Author.Firstname, c.Author.Lastname)
            )).ToList();
        var response = new PagedResponse<CommentResponse>(items, request.Page, request.PageSize, result.TotalCount);
        return Ok(response);
    }
}
