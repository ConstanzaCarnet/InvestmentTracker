using System;

namespace Users.Domain.Common;

public abstract class BaseEntity
{
    // Usamos Guid en lugar de int para microservicios.
    // Los IDs numéricos (1, 2, 3) son predecibles y difíciles de sincronizar entre BDs.
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }
}