namespace Civ2Like.Core.Cities;

/// <summary>Anything a city can build.</summary>
public interface IBuildable
{
    string Key { get; }
    int ProductionCost { get; }
    /// <summary>Optional upfront resource cost paid when queuing. Return false if cannot pay.</summary>
    bool OnQueued(Game game, City city)
        => true;
    /// <summary>Called when production completes.</summary>
    void OnCompleted(Game game, City city);
}
