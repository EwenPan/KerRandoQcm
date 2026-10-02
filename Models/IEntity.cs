namespace KerRandoQcm.Models;

/// <summary>Marker for entities with a string identifier, usable both with MongoDB ObjectIds and the in-memory store.</summary>
public interface IEntity
{
    string Id { get; set; }
}
