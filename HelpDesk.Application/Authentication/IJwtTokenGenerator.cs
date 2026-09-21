using HelpDesk.Domain;

namespace HelpDesk.Application.Authentication;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}