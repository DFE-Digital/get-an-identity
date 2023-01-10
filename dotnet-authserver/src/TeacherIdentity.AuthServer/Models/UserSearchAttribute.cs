namespace TeacherIdentity.AuthServer.Models;

public class UserSearchAttribute
{
    public const string UserIdIndexName = "ix_user_search_attributes_user_id";

    public const string AttributeTypeAndValueIndexName = "ix_user_search_attributes_attribute_type_and_value";

    public required Guid UserSearchAttributeId { get; init; }

    public required Guid UserId { get; init; }

    public required string AttributeType { get; init; }

    public required string AttributeValue { get; init; }
}
