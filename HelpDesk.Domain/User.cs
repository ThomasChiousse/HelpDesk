namespace HelpDesk.Domain
{
    public class User
    {
        public int Id { get; private set; }
        public string Firstname { get; private set; } = null!;
        public string Lastname { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public UserRole Role { get; private set; }

        public User(int id, string firstname, string lastname, string email, UserRole role)
        {
            if (!IsStringValid(firstname)) throw new ArgumentException("Firstname must not be null or empty", nameof(firstname));
            if (!IsStringValid(lastname)) throw new ArgumentException("Lastname must not be null or empty", nameof(lastname));
            if (!IsStringValid(email)) throw new ArgumentException("Email must not be null or empty", nameof(email));
            if (!Enum.IsDefined(role)) throw new ArgumentException("Unknown user role.", nameof(role));
            if (id <= 0) throw new ArgumentException("id should be a positive integer", nameof(id));
            Id = id;
            Firstname = firstname;
            Lastname = lastname;
            Email = email;
            Role = role;
        }

        public User(string firstname, string lastname, string email, UserRole role)
        {
            if (!IsStringValid(firstname)) throw new ArgumentException("Firstname must not be null or empty", nameof(firstname));
            if (!IsStringValid(lastname)) throw new ArgumentException("Lastname must not be null or empty", nameof(lastname));
            if (!IsStringValid(email)) throw new ArgumentException("Email must not be null or empty", nameof(email));
            if (!Enum.IsDefined(role)) throw new ArgumentException("Unknown user role.", nameof(role));

            Firstname = firstname;
            Lastname = lastname;
            Email = email;
            Role = role;
        }

        private User()
        {

        }

        public void SetFirstname(string firstname)
        {
            if (!IsStringValid(firstname)) throw new ArgumentException("Firstname must not be null or empty", nameof(firstname));
            Firstname = firstname;
        }
        public void SetLastname(string lastname)
        {
            if (!IsStringValid(lastname)) throw new ArgumentException("Lastname must not be null or empty", nameof(lastname));
            Lastname = lastname;
        }
        public void SetEmail(string email)
        {
            if (!IsStringValid(email)) throw new ArgumentException("Email must not be null or empty", nameof(email));
            Email = email;
        }

        private static bool IsStringValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }


    }

    public enum UserRole
    {
        User, Technician, Administrator
    }
}
