using HelpDesk.Domain;

namespace HelpDesk.Tests
{
    public class UserTests
    {
        private static User CreateUser(int id = 12)
        {
            return new User(id, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
        }

        [Fact]
        public void NewUser_WithValidData_ShouldSetProperties()
        {
            // arrange
            string firstname = "Thomas";
            string lastname = "Banana";
            string email = "email@domain.com";
            UserRole role = UserRole.Technician;

            // act
            User user = new(12, firstname, lastname, email, role);

            // assert
            Assert.Equal(12, user.Id);
            Assert.Equal(firstname, user.Firstname);
            Assert.Equal(lastname, user.Lastname);
            Assert.Equal(email, user.Email);
            Assert.Equal(role, user.Role);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-1564051607)]
        public void NewUser_WithNegativeId_ShouldThrowArgumentException(int id)
        {
            Assert.Throws<ArgumentException>(() => CreateUser(id));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void NewUser_WithInvalidFirstName_ShouldThrowArgumentException(string? firstname)
        {
            Assert.Throws<ArgumentException>(() => new User(12, firstname, "Banana", "email@domain.com", UserRole.Technician));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void NewUser_WithInvalidLastName_ShouldThrowArgumentException(string? lastname)
        {
            Assert.Throws<ArgumentException>(() => new User(12, "Thomas", lastname, "email@domain.com", UserRole.Technician));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void NewUser_WithInvalidEmail_ShouldThrowArgumentException(string? email)
        {
            Assert.Throws<ArgumentException>(() => new User(12, "Thomas", "Banana", email, UserRole.Technician));
        }

        [Fact]
        public void SetEmail_WithValidEmail_ShouldUpdateEmail()
        {
            var user = CreateUser();
            string email = "newmail@domain.com";
            user.SetEmail(email);
            Assert.Equal(email, user.Email);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetEmail_WithInvalidEmail_ShouldThrowArgumentException(string? email)
        {
            var user = CreateUser();
            var currentEmail = user.Email;

            Assert.Throws<ArgumentException>(() => user.SetEmail(email!));
            Assert.Equal(currentEmail, user.Email);
        }

        [Fact]
        public void SetFirstname_WithValidFirstname_ShouldUpdateFirstname()
        {
            var user = CreateUser();

            string newFirstname = "new firstname";
            user.SetFirstname(newFirstname);

            Assert.Equal(newFirstname, user.Firstname);
        }

        [Theory]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData(null)]
        public void SetFirstname_WithInvalidFirstname_ShouldThrowArgumentException(string? firstname)
        {
            var user = CreateUser();
            var currentFirstname = user.Firstname;
            Assert.Throws<ArgumentException>(() => user.SetFirstname(firstname));
            Assert.Equal(currentFirstname, user.Firstname);
        }

        [Fact]
        public void SetLastname_WithValidLastname_ShouldUpdateLastname()
        {
            var user = CreateUser();

            string newLastname = "new lastname";
            user.SetFirstname(newLastname);

            Assert.Equal(newLastname, user.Firstname);
        }

        [Theory]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData(null)]
        public void SetLastname_WithInvalidLastname_ShouldThrowArgumentException(string? lastname)
        {
            var user = CreateUser();
            var currentLastname = user.Lastname;
            Assert.Throws<ArgumentException>(() => user.SetLastname(lastname));
            Assert.Equal(currentLastname, user.Lastname);
        }

        [Fact]
        public void NewUser_WithInvalidRole_ShouldThrowArgumentException()
        {
            var invalidRole = (UserRole)999;

            Assert.Throws<ArgumentException>(() =>
                new User(
                    12,
                    "Thomas",
                    "Banana",
                    "email@domain.com",
                    invalidRole));
        }

        #region temporary
        [Fact]
        public void User_WithSameReference_ShouldBeEqual()
        {
            var user1 = CreateUser();
            var user2 = user1;
            Assert.Same(user1, user2);
            Assert.Equal(user1, user2);
        }

        [Fact]
        public void User_WithSameValuesButDifferentInstances_ShouldNotBeEqual()
        {
            var user1 = CreateUser();
            var user2 = CreateUser();
            Assert.NotSame(user1, user2);
            Assert.NotEqual(user1, user2);
        }

        [Fact]
        public void User_WithSameIdButDifferentInstances_ShouldNotBeEqual()
        {
            User user1 = new User(1, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
            User user2 = new User(1, "Tom", "Bana", "email@domain2.com", UserRole.Technician);
            Assert.NotSame(user1, user2);
            Assert.NotEqual(user1, user2);
        }
        #endregion
    }
}
