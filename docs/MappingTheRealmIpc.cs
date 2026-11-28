using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;

using MovementPolicy = (bool UseMount, bool UseMountRoulette, uint MountId, bool UseFlight, bool UseSprint, uint MinMountDistance, uint MinRunDistance);

namespace MappingTheRealm.Ipc;

public sealed class MappingTheRealmIpc
{
    private readonly ICallGateSubscriber<int> version;
    private readonly ICallGateSubscriber<bool> isPathing;
    private readonly ICallGateSubscriber<uint, Task> pathToTerritory;
    private readonly ICallGateSubscriber<uint, Vector3, Task> pathTo;
    private readonly ICallGateSubscriber<uint, Vector3, float, bool, Task> pathToArea;
    private readonly ICallGateSubscriber<MovementPolicy> getMovementPolicy;
    private readonly ICallGateSubscriber<uint, Vector3, int, Task> pathToHousing;
    private readonly ICallGateSubscriber<uint, int, int, Task> pathToHousingPlot;
    private readonly ICallGateSubscriber<uint, Vector3, uint, Task> pathToInteractable;
    private readonly ICallGateSubscriber<uint, uint, Task> pathToInteractableById;
    private readonly ICallGateSubscriber<uint, Vector3, uint, float, Task> pathToInteractableWithRange;
    private readonly ICallGateSubscriber<uint, uint, float, Task> pathToInteractableByIdWithRange;
    private readonly ICallGateSubscriber<uint, Vector3, uint[], Task> pathToAnyInteractable;
    private readonly ICallGateSubscriber<uint, Vector3, uint, int, Task> pathToHousingInteractable;
    private readonly ICallGateSubscriber<uint, Task> pathToEnpc;
    private readonly ICallGateSubscriber<uint, Vector3, CancellationToken, Task<bool>> canReach;
    private readonly ICallGateSubscriber<uint[], Task> pathToNearestEobj;
    private readonly ICallGateSubscriber<object> stop;

    public MappingTheRealmIpc(IDalamudPluginInterface pluginInterface)
    {
        version = pluginInterface.GetIpcSubscriber<int>("MappingTheRealm.ApiVersion");
        isPathing = pluginInterface.GetIpcSubscriber<bool>("MappingTheRealm.IsPathing");
        pathToTerritory = pluginInterface.GetIpcSubscriber<uint, Task>("MappingTheRealm.PathToTerritory");
        pathTo = pluginInterface.GetIpcSubscriber<uint, Vector3, Task>("MappingTheRealm.PathTo");
        pathToArea = pluginInterface.GetIpcSubscriber<uint, Vector3, float, bool, Task>("MappingTheRealm.PathToArea");
        getMovementPolicy = pluginInterface.GetIpcSubscriber<MovementPolicy>("MappingTheRealm.GetMovementPolicy");
        pathToHousing = pluginInterface.GetIpcSubscriber<uint, Vector3, int, Task>("MappingTheRealm.PathToHousing");
        pathToHousingPlot = pluginInterface.GetIpcSubscriber<uint, int, int, Task>("MappingTheRealm.PathToHousingPlot");
        pathToInteractable = pluginInterface.GetIpcSubscriber<uint, Vector3, uint, Task>("MappingTheRealm.PathToInteractable");
        pathToInteractableById = pluginInterface.GetIpcSubscriber<uint, uint, Task>("MappingTheRealm.PathToInteractableById");
        pathToInteractableWithRange = pluginInterface.GetIpcSubscriber<uint, Vector3, uint, float, Task>("MappingTheRealm.PathToInteractableWithRange");
        pathToInteractableByIdWithRange = pluginInterface.GetIpcSubscriber<uint, uint, float, Task>("MappingTheRealm.PathToInteractableByIdWithRange");
        pathToAnyInteractable = pluginInterface.GetIpcSubscriber<uint, Vector3, uint[], Task>("MappingTheRealm.PathToAnyInteractable");
        pathToHousingInteractable = pluginInterface.GetIpcSubscriber<uint, Vector3, uint, int, Task>("MappingTheRealm.PathToHousingInteractable");
        pathToEnpc = pluginInterface.GetIpcSubscriber<uint, Task>("MappingTheRealm.PathToEnpc");
        canReach = pluginInterface.GetIpcSubscriber<uint, Vector3, CancellationToken, Task<bool>>("MappingTheRealm.CanReach");
        pathToNearestEobj = pluginInterface.GetIpcSubscriber<uint[], Task>("MappingTheRealm.PathToNearestEobj");
        stop = pluginInterface.GetIpcSubscriber<object>("MappingTheRealm.Stop");
    }

    public bool IsAvailable => version.HasFunction;
    public int ApiVersion => version.InvokeFunc();
    public bool IsPathing => isPathing.InvokeFunc();

    public Task PathToTerritoryAsync(uint territory, CancellationToken token) =>
        RunRouteAsync(() => pathToTerritory.InvokeFunc(territory), token);

    public Task PathToAsync(uint territory, Vector3 position, CancellationToken token) =>
        RunRouteAsync(() => pathTo.InvokeFunc(territory, position), token);

    public Task PathToAreaAsync(uint territory, Vector3 center, float radius, bool includeHeight, CancellationToken token) =>
        RunRouteAsync(() => pathToArea.InvokeFunc(territory, center, radius, includeHeight), token, minimumVersion: 4);

    public MovementPolicy GetMovementPolicy()
    {
        RequireVersion(4);
        return getMovementPolicy.InvokeFunc();
    }

    public Task PathToHousingAsync(uint territory, Vector3 position, int ward, CancellationToken token) =>
        RunRouteAsync(() => pathToHousing.InvokeFunc(territory, position, ward), token);

    public Task PathToHousingPlotAsync(uint territory, int ward, int plot, CancellationToken token) =>
        RunRouteAsync(() => pathToHousingPlot.InvokeFunc(territory, ward, plot), token);

    public Task PathToInteractableAsync(uint territory, Vector3 position, uint baseId, CancellationToken token, float range = 0) =>
        RunRouteAsync(() => range == 0
            ? pathToInteractable.InvokeFunc(territory, position, baseId)
            : pathToInteractableWithRange.InvokeFunc(territory, position, baseId, range), token);

    public Task PathToInteractableAsync(uint territory, uint baseId, CancellationToken token, float range = 0) =>
        RunRouteAsync(() => range == 0
            ? pathToInteractableById.InvokeFunc(territory, baseId)
            : pathToInteractableByIdWithRange.InvokeFunc(territory, baseId, range), token);

    public Task PathToAnyInteractableAsync(uint territory, Vector3 position, uint[] baseIds, CancellationToken token) =>
        RunRouteAsync(() => pathToAnyInteractable.InvokeFunc(territory, position, baseIds), token);

    public Task PathToHousingInteractableAsync(uint territory, Vector3 position, uint baseId, int ward, CancellationToken token) =>
        RunRouteAsync(() => pathToHousingInteractable.InvokeFunc(territory, position, baseId, ward), token);

    public Task PathToEnpcAsync(uint enpcId, CancellationToken token) =>
        RunRouteAsync(() => pathToEnpc.InvokeFunc(enpcId), token);

    public Task PathToNearestEobjAsync(uint[] eobjIds, CancellationToken token) =>
        RunRouteAsync(() => pathToNearestEobj.InvokeFunc(eobjIds), token);

    public Task<bool> CanReachAsync(uint territory, Vector3 position, CancellationToken token)
    {
        RequireVersion();
        return canReach.InvokeFunc(territory, position, token).WaitAsync(token);
    }

    private async Task RunRouteAsync(Func<Task> start, CancellationToken token, int minimumVersion = 1)
    {
        token.ThrowIfCancellationRequested();
        RequireVersion(minimumVersion);
        var route = start();
        using var cancellation = token.Register(Stop);
        await route.WaitAsync(token);
    }

    private void RequireVersion(int minimumVersion = 1)
    {
        if (version.InvokeFunc() < minimumVersion)
            throw new InvalidOperationException($"This call requires MappingTheRealm IPC version {minimumVersion} or later.");
    }

    public void Stop()
    {
        try { stop.InvokeAction(); }
        catch (IpcNotReadyError) { }
    }
}
