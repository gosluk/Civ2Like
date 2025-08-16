using Civ2Like.Core;
using System.Text.Json.Serialization;

namespace Civ2Like.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(GameStartedEvent), "GameStarted")]
[JsonDerivedType(typeof(UnitMovedEvent), "UnitMoved")]
[JsonDerivedType(typeof(CityFoundedEvent), "CityFounded")]
[JsonDerivedType(typeof(TurnEndedEvent), "TurnEnded")]
public interface IGameEvent { void Apply(Game game); }
