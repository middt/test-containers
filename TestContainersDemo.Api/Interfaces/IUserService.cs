using TestContainersDemo.Api.Models;

namespace TestContainersDemo.Api.Interfaces;

public interface IUserService
{
    Task<IEnumerable<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(CreateUserRequest request);
}
