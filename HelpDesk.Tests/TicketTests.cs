using HelpDesk.Domain;
using HelpDesk.Domain.Exceptions;

namespace HelpDesk.Tests;

public class TicketTests
{
    private static Ticket CreateTicket(int id = 1)
    {
        return new Ticket(id,
            "Printer broken",
            "The printer doesn't work",
            TicketPriority.Normal);
    }

    [Fact]
    public void NewTicket_ShouldHaveOpenStatus()
    {
        // Arrange
        var ticket = CreateTicket();

        // Act
        var status = ticket.Status;

        // Assert
        Assert.Equal(TicketStatus.Open, status);
    }

    [Fact]
    public void NewTicket_ShouldHaveNoComments()
    {
        Ticket ticket = CreateTicket();
        Assert.Empty(ticket.Comments);
    }


    [Fact]
    public void NewTicket_ShouldSetCreationDate()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var ticket = CreateTicket();

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(
            ticket.CreationDate,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    [InlineData(null)]
    public void NewTicket_WithInvalidTitle_ShouldThrowArgumentException(string? title)
    {
        Assert.Throws<ArgumentException>(() =>
            new Ticket(1,
                title,
                "Description",
                TicketPriority.Normal));
    }

    [Fact]
    public void NewTicket_ShouldHaveNoAffectedUser()
    {
        var ticket = CreateTicket();
        Assert.Null(ticket.AssignedUser);
    }

    [Fact]
    public void AdvanceStatus_WhenOpen_ShouldBecomeInProgress()
    {
        var ticket = CreateTicket();
        ticket.AdvanceStatus();
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }
    [Fact]
    public void AdvanceStatus_WhenInProgress_ShouldBecomeResolved()
    {
        // arrange
        var ticket = CreateTicket();
        ticket.AdvanceStatus(); // open -> in progress

        // act
        ticket.AdvanceStatus(); // in progress -> Resolved

        // assert
        Assert.Equal(TicketStatus.Resolved, ticket.Status);
    }

    [Fact]
    public void AdvanceStatus_WhenResolved_ShouldBecomeClosed()
    {
        // arrange
        var ticket = CreateTicket();
        ticket.AdvanceStatus(); // open -> in progress
        ticket.AdvanceStatus(); // in progress -> resolved

        //act
        ticket.AdvanceStatus(); // resolved -> closed

        // assert
        Assert.Equal(TicketStatus.Closed, ticket.Status);
    }

    [Fact]
    public void AdvanceStatus_WhenClosed_ShouldThrowTicketAlreadyClosedException()
    {
        // arrange
        var ticket = CreateTicket();
        ticket.AdvanceStatus(); // open -> in progress
        ticket.AdvanceStatus(); // in progress -> resolved
        ticket.AdvanceStatus(); // resolved -> closed

        // act + assert
        Assert.Throws<TicketAlreadyClosedException>(() => ticket.AdvanceStatus());
    }


    [Fact]
    public void SetTitle_WithValidTitle_ShouldUpdateTitle()
    {
        var ticket = CreateTicket();
        const string newTitle = "Printer engulfed in flames";
        ticket.SetTitle(newTitle);
        Assert.Equal(newTitle, ticket.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTitle_WithInvalidTitle_ShouldThrowArgumentException(string? title)
    {
        var ticket = CreateTicket();
        string currentTitle = ticket.Title;

        Assert.Throws<ArgumentException>(() => ticket.SetTitle(title));
        Assert.Equal(currentTitle, ticket.Title);
    }



    [Fact]
    public void SetDescription_ShouldUpdateDescription()
    {
        var ticket = CreateTicket();
        const string newDescription = "Printer stuck in tree";
        ticket.SetDescription(newDescription);
        Assert.Equal(newDescription, ticket.Description);
    }

    [Fact]
    public void AssignUser_ShouldAssignUser()
    {
        var ticket = CreateTicket();
        string firstname = "Thomas";
        string lastname = "Banana";
        string email = "email@domain.com";
        UserRole role = UserRole.Technician;
        User user = new(12, firstname, lastname, email, role);
        ticket.AssignUser(user);
        Assert.Equal(user, ticket.AssignedUser);
    }

    [Fact]
    public void NewTicket_WithAffectedUser_ShouldSetAffectedUser()
    {

        string firstname = "Thomas";
        string lastname = "Banana";
        string email = "email@domain.com";
        UserRole role = UserRole.Technician;
        User user = new(12, firstname, lastname, email, role);
        var ticket = new Ticket(1, "Printer broken", "The printer doesn't work", TicketPriority.Normal, user);
        Assert.Equal(user, ticket.AssignedUser);
    }

    [Fact]
    public void UnassignUser_WhenUserIsAssigned_ShouldRemoveUser()
    {
        string firstname = "Thomas";
        string lastname = "Banana";
        string email = "email@domain.com";
        UserRole role = UserRole.Technician;
        User user = new(12, firstname, lastname, email, role);
        var ticket = new Ticket(1, "Printer broken", "The printer doesn't work", TicketPriority.Normal, user);
        ticket.UnassignUser(user.Id);
        Assert.Null(ticket.AssignedUser);
    }

    [Fact]
    public void AssignUser_WithNullUser_ShouldThrowArgumentNullException()
    {
        var ticket = CreateTicket();

        Assert.Throws<ArgumentNullException>(
            () => ticket.AssignUser(null!));
    }

    [Fact]
    public void AddComment_ShouldAddComment()
    {
        string content = "content of comment";
        Comment comment = new(1, new User(2, "Thomas", "Banana", "email@domain.com", UserRole.Technician), content);
        Ticket ticket = CreateTicket();

        ticket.AddComment(comment);

        Assert.Single(ticket.Comments);
        Assert.Contains(comment, ticket.Comments);
    }

    [Fact]
    public void AddComment_WithNullComment_ShouldThrowArgumentNullException()
    {
        Ticket ticket = CreateTicket();
        Assert.Throws<ArgumentNullException>(() => ticket.AddComment(null!));
    }

    [Fact]
    public void AddComment_WhenTicketIsClosed_ShouldThrowTicketClosedException()
    {
        Comment comment = new(1, new User(2, "Thomas", "Banana", "email@domain.com", UserRole.Technician), "content");
        Ticket ticket = CreateTicket();
        ticket.AdvanceStatus(); // open -> in progress
        ticket.AdvanceStatus(); // in progress -> resolved
        ticket.AdvanceStatus(); // resolved -> closed

        Assert.Throws<TicketClosedException>(() => ticket.AddComment(comment));
        Assert.DoesNotContain(comment, ticket.Comments);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-4589)]
    public void NewTicketWithInvalidId_ShouldThrowArgumentException(int id)
    {
        Assert.Throws<ArgumentException>(() => CreateTicket(id));
    }

    [Fact]
    public void GetCommentsByAuthor_ShouldReturnOnlyCommentsFromRequestedAuthor()
    {
        User user1 = new(1, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
        Comment comment1 = new(1, user1, "content");
        Comment comment2 = new(2, new User(2, "Tom", "Bana", "email@domain2.com", UserRole.Technician), "content2");
        Comment comment3 = new(3, user1, "content2");
        Ticket ticket = CreateTicket();
        ticket.AddComment(comment1);
        ticket.AddComment(comment2);
        ticket.AddComment(comment3);
        int id = 1;
        var result = ticket.GetCommentsByAuthor(id);
        Assert.Equal(2, result.Count);
        Assert.Contains(comment1, result);
        Assert.Contains(comment3, result);
        Assert.DoesNotContain(comment2, result);
        Assert.All(result, comment => Assert.Equal(user1.Id, comment.Author.Id));
    }

    [Fact]
    public void GetCommentsByAuthor_WhenNoCommentsMatch_ShouldReturnEmptyCollection()
    {
        User user1 = new(1, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
        Comment comment1 = new(1, user1, "content");
        Comment comment2 = new(2, new User(2, "Tom", "Bana", "email@domain2.com", UserRole.Technician), "content2");
        Comment comment3 = new(3, user1, "content2");
        Ticket ticket = CreateTicket();
        ticket.AddComment(comment1);
        ticket.AddComment(comment2);
        ticket.AddComment(comment3);
        int id = 15600;
        Assert.Empty(ticket.GetCommentsByAuthor(id));
    }

    [Fact]
    public void GetLatestComment_ShouldReturnMostRecentComment()
    {
        // Arrange
        var clock = new TestTimeProvider(
            new DateTimeOffset(
                2026, 9, 13,
                10, 0, 0,
                TimeSpan.Zero));

        var user = new User(
            1,
            "Thomas",
            "Banana",
            "email@domain.com",
            UserRole.Technician);

        var ticket = CreateTicket();

        var comment1 = new Comment(
            1,
            user,
            "First",
            clock);

        ticket.AddComment(comment1);

        clock.Advance(TimeSpan.FromMinutes(10));

        var comment2 = new Comment(
            2,
            user,
            "Second",
            clock);

        ticket.AddComment(comment2);

        clock.Advance(TimeSpan.FromMinutes(10));

        var comment3 = new Comment(
            3,
            user,
            "Third",
            clock);

        ticket.AddComment(comment3);

        // Act
        var latestComment = ticket.GetLatestComment();

        // Assert
        Assert.Equal(comment3, latestComment);
    }

    [Fact]
    public void GetLatestComment_WhenThereAreNoComments_ShouldReturnNull()
    {
        Ticket ticket = CreateTicket();
        Assert.Null(ticket.GetLatestComment());
    }

    [Fact]
    public void HasCommentsFromAuthor_WhenTicketHasCommentsFromAuthor_ShouldReturnTrue()
    {
        // arrange
        int userId = 1;
        Ticket ticket = CreateTicket();
        var user = new User(
           userId,
           "Thomas",
           "Banana",
           "email@domain.com",
           UserRole.Technician);
        Comment comment = new(12, user, "comment text");
        ticket.AddComment(comment);

        //act, assert
        Assert.True(ticket.HasCommentsFrom(userId));
    }

    [Fact]
    public void HasCommentsFrom_WhenTicketHasNoCommentsFromAuthor_ShouldReturnFalse()
    {
        var ticket = CreateTicket();

        Assert.False(ticket.HasCommentsFrom(999));
    }

    [Fact]
    public void GetCommentCountFromAuthor_ShouldReturnCommentCountFromAuthor()
    {
        // arrange
        int userId = 1;
        Ticket ticket = CreateTicket();
        var user1 = new User(
           userId,
           "Thomas",
           "Banana",
           "email@domain.com",
           UserRole.Technician);

        var user2 = new User(
           userId + 1,
           "Thomas",
           "Banana",
           "email@domain.com",
           UserRole.Technician);

        Comment comment1 = new(12, user1, "comment text");
        Comment comment2 = new(13, user2, "comment text 2");
        Comment comment3 = new(14, user1, "comment text 3");
        ticket.AddComment(comment1);
        ticket.AddComment(comment2);
        ticket.AddComment(comment3);

        //act, assert
        Assert.Equal(2, ticket.GetCommentCountByAuthor(userId));
        Assert.Equal(1, ticket.GetCommentCountByAuthor(userId + 1));
    }

    [Fact]
    public void GetCommentAuthors_ShouldReturnDistinctCommentAuthors()
    {
        Ticket ticket = CreateTicket();
        var user1 = new User(
            1,
            "Thomas",
            "Banana",
            "email@domain.com",
            UserRole.Technician);

        var sameUserDifferentInstance = new User(
            1,
            "Tom",
            "Banana",
            "new@email.com",
            UserRole.Technician);

        ticket.AddComment(new Comment(1, user1, "First"));
        ticket.AddComment(new Comment(2, sameUserDifferentInstance, "Second"));

        Assert.Single(ticket.GetCommentAuthors());
    }

    [Fact]
    public void NewTicket_WithInvalidPriority_ShouldThrowArgumentException()
    {
        var invalidPriority = (TicketPriority)999;

        Assert.Throws<ArgumentException>(() =>
            new Ticket(
                "Test",
                "Description",
                invalidPriority));
    }
}
