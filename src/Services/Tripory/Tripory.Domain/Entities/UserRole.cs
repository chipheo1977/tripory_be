namespace Tripory.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }

    private UserRole() { }

    internal UserRole(Guid userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
