# MappingTheRealm IPC

Copy `MappingTheRealmIpc.cs` into your Dalamud plugin. It only needs Dalamud; it does not depend on Underlings or MappingTheRealm assemblies. The provider must be installed and enabled. Check `IsAvailable` before making calls.

```csharp
using MappingTheRealm.Ipc;

private readonly MappingTheRealmIpc navigator;

public Plugin(IDalamudPluginInterface pluginInterface)
{
    navigator = new MappingTheRealmIpc(pluginInterface);
}

public async Task GoToVendor(uint enpcId, CancellationToken token)
{
    if (!navigator.IsAvailable || navigator.ApiVersion < 1)
        throw new InvalidOperationException("MappingTheRealm is unavailable.");

    await navigator.PathToEnpcAsync(enpcId, token);
}
```

All route methods complete when travel finishes or fail with an exception. Only one route can run at a time. Cancelling the token passed to a route method calls `Stop`; `Stop` only cancels a route started by your plugin. `IsPathing` reports any active MappingTheRealm route. Housing wards are numbered 1 through 30 and plots 1 through 60 in IPC calls.

| Method | Purpose |
| --- | --- |
| `PathToTerritoryAsync(uint territoryId, CancellationToken token)` | Enter a territory. |
| `PathToAsync(uint territoryId, Vector3 position, CancellationToken token)` | Move to a position, including travel across territories. |
| `PathToHousingAsync(uint territoryId, Vector3 position, int ward, CancellationToken token)` | Move to housing coordinates in a ward. |
| `PathToHousingPlotAsync(uint territoryId, int ward, int plot, CancellationToken token)` | Travel to the specified ward and stop at the plot placard. |
| `PathToInteractableAsync(uint territoryId, Vector3 position, uint baseId, CancellationToken token, float range = 0)` | Move into range of an actor. Pass `Vector3.Zero` to look up the position by ID. |
| `PathToInteractableAsync(uint territoryId, uint baseId, CancellationToken token, float range = 0)` | Look up an indexed aetheryte, ENPC, or EOBJ in the specified territory and stop within range. |
| `PathToAnyInteractableAsync(uint territoryId, Vector3 position, uint[] baseIds, CancellationToken token)` | Move to the supplied destination and stop in range of any listed actor base ID. It does not search zones or interact. |
| `PathToHousingInteractableAsync(uint territoryId, Vector3 position, uint baseId, int ward, CancellationToken token)` | Travel to the ward and stop in range of the supplied actor base ID near the supplied coordinates. Does not select a plot or interact. |
| `PathToEnpcAsync(uint enpcId, CancellationToken token)` | Find and move to an ENPC, preferring its placement in the current territory. |
| `PathToNearestEobjAsync(uint[] eobjIds, CancellationToken token)` | Search known locations across territories, rank by route cost, and move to the first reachable EOBJ. |
| `CanReachAsync(uint territoryId, Vector3 position, CancellationToken token)` | Check route reachability without moving. |
| `Stop()` | Cancel the active route if your plugin started it. |

Automatic ENPC and EOBJ searches skip restricted territory placements outside the current territory. For an intentional visit, pass the exact territory ID to `PathToInteractableAsync`. The route needs a supported entrance and any required unlocks.

`ApiVersion` returns 1 for this release. `IsAvailable` checks whether the IPC provider is registered. These calls plan and execute movement; they do not automatically interact with the destination actor.

The overload without coordinates calls `MappingTheRealm.PathToInteractableById`. For example, `PathToInteractableAsync(128, 48, token)` and `PathToInteractableAsync(128, Vector3.Zero, 48, token)` both look up the same aethernet shard. The lookup fails if the specified territory has no indexed placement for that ID. If an ID has several indexed placements in a territory, the lookup uses the first match. Pass explicit nonzero coordinates to choose a specific placement.

For either overload, `range: 0` uses the game's interaction range check. A positive range measures world distance in yalms from the player to a loaded, targetable object with the supplied base ID. For example, `await navigator.PathToInteractableAsync(128, 48, token, range: 10f);` stops within 10 yalms of the shard. Direct IPC callers can pass the extra `float` to `MappingTheRealm.PathToInteractableWithRange` or `MappingTheRealm.PathToInteractableByIdWithRange`.
