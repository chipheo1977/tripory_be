using BuildingBlocks.Core.Domains.Abstractions;
using Tripory.Domain.Enums;

namespace Tripory.Domain.Entities;

public class Role : EntityBase<int>
{
    public String Name { get; private set;} = string.Empty;
    public String Description {get; private set;} = string.Empty;

    private Role() { }

    public Role(UserRoleType roleType, String description)
    {
        Id = (int)roleType;
        Name = roleType.ToString();
        Description = description;
    }
}