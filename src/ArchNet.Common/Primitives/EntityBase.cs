using Visus.Cuid;

namespace ArchNet.Common.Primitives;

public abstract class EntityBase
{
    public const int IdMaxLength = 24;

    public string Id { get; private init; } = new Cuid2().ToString();
}
