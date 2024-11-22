using Content.Server.Mech.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Interaction;

namespace Content.Server.Mech.Equipment.EntitySystems;

public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly MechSystem _mechSystem = default!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(HandleGunShotEvent);
        SubscribeLocalEvent<MechEquipmentComponent, OnEmptyGunShotEvent>(ChargeGun);
    }

    private void HandleGunShotEvent(EntityUid uid, MechEquipmentComponent component, ref GunShotEvent args)
    {
        ChargeBatteryIfNeeded(uid, component);
    }


    private void ChargeGun(EntityUid uid, MechEquipmentComponent component, ref OnEmptyGunShotEvent args)
    {
        ChargeBatteryIfNeeded(uid, component);
    }

    private void ChargeBatteryIfNeeded(EntityUid uid, MechEquipmentComponent equipment)
    {
        if (!TryGetBattery(uid, out var battery) || equipment.EquipmentOwner is null)
            return;

        if (HasComp<MechComponent>(equipment.EquipmentOwner.Value) && battery != null)
            AttemptBatteryRecharge(uid, battery, equipment.EquipmentOwner.Value);
    }

    private bool TryGetBattery(EntityUid uid, out BatteryComponent? battery)
    {
        if (!TryComp<BatteryComponent>(uid, out var batteryComponent) || batteryComponent == null)
        {
            battery = null;
            return false;
        }
        else
        {
            battery = batteryComponent;
            return true;
        }
    }

    private void AttemptBatteryRecharge(EntityUid gunUid, BatteryComponent? gunBattery, EntityUid mechUid)
    {
        if (!TryComp<MechComponent>(mechUid, out var mech) || !NeedsRecharge(gunBattery) || gunBattery == null)
            return;

        var requiredEnergy = gunBattery.MaxCharge - gunBattery.CurrentCharge;

        if (CanConsumeMechEnergy(mech, requiredEnergy) && _mechSystem.TryChangeEnergy(mechUid, -requiredEnergy, mech))
        {
            _batterySystem.SetCharge(gunUid, gunBattery.MaxCharge, gunBattery);
        }
    }

    private bool NeedsRecharge(BatteryComponent? battery)
    {
        if (battery == null)
            return false;
        return battery.CurrentCharge < battery.MaxCharge;
    }

    private bool CanConsumeMechEnergy(MechComponent mech, float energyNeeded)
    {
        return mech.Energy >= energyNeeded;
    }
}
