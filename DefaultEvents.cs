using CustomPlayerEffects;
using KibsAdminEventsPlus;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp096;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Targeting;
using UnityEngine;
using UserSettings.OtherSettings;

namespace KibsAdminEventsPlus
{
    public class Test : KAEevent
    {
        public override int Id => 1;

        public override string Name => "Debug event";

        public override string Description => "Broacast it works to everyone if it works";

        public override void RoundStart()
        {
            foreach (var item in Player.List)
            {
                item.SendConsoleMessage("Break out the champagne, this shit functions");
            }
        }
    }
    public class SkinwalkercEvent : KAEevent
    {
        public override int Id => 2;

        public override string Name => "Skinwalkers";

        public override string Description => "Skeletons and blackouts, what could go wrong?";
        public override void Loop()
        {
            Map.TurnOffLights();
        }

        public override void RoundStart()
        {
            foreach (var item in Player.List)
            {
                if (item.IsSCP)
                {
                    item.SetRole(RoleTypeId.Scp3114, RoleChangeReason.None, RoleSpawnFlags.AssignInventory);
                    item.Position = Room.Get(MapGeneration.RoomName.Hcz127).First().Position + new UnityEngine.Vector3(0, 1, 0);
                }
                else
                {
                    item.AddItem(ItemType.Lantern);
                }
            }
            Map.TurnOffLights();
        }
        public override void RoleChanged(PlayerChangedRoleEventArgs ev)
        {
            base.RoleChanged(ev);
            ev.Player.AddItem(ItemType.Lantern);
        }
    }



    public class ShyGuyApolly : KAEevent
    {
        public override int Id => 3;

        public override string Name => "096 apollyon";

        public override string Description => "Regular round but with one super 096 with infinite rage";
        Player scp96 = null;

        public override void RoundStart()
        {
            var Scps = Player.List.Where<Player>(x => x.IsSCP);
            scp96 = Scps.First();
            foreach (var item in Scps)
            {
                if (item == scp96)
                {
                    item.SetRole(RoleTypeId.Scp096, RoleChangeReason.None, RoleSpawnFlags.None);
                    item.Position = Room.Get(MapGeneration.FacilityZone.Surface).First().Position + new UnityEngine.Vector3(0, 1, 0);
                    item.MaxHealth = Player.List.Count * 500;
                    item.Heal(99999999);
                }

                else
                {
                    item.SetRole(RoleTypeId.ClassD, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
                }
            }
        }
        public override void Loop()
        {

            base.Loop();
            if (scp96 != null & scp96.Role == RoleTypeId.Scp096)
            {
                foreach (var item in Player.List)
                {
                    scp96.GameObject.GetComponent<Scp096TargetsTracker>().AddTarget(item.ReferenceHub, true);
                }
                scp96.GameObject.GetComponent<Scp096StateController>().SetRageState(Scp096RageState.Enraged);
            }
        }
    }

    public class Speedup : KAEevent
    {
        public override int Id => 4;

        public override string Name => "Speedsters";

        public override string Description => "everyone in the game passively accelerates";

        public override float Tps { get; set; } = 6;

        byte boost = 0;
        public override void RoundStart()
        {

        }
        public override void Loop()
        {
            if (boost <= 180)
            {
                boost++;
            }
            foreach (var item in Player.List)
            {
                item.EnableEffect<MovementBoost>(boost);
            }
        }
    }
    public class Blindall : KAEevent
    {
        public override int Id => 5;

        public override string Name => "Missing Glasses";

        public override string Description => "Everyone is nearsighted";

        public override void RoundStart()
        {

        }
        public override void Loop()
        {
            foreach (var item in Player.List)
            {
                item.EnableEffect<CustomPlayerEffects.FogControl>(5);
            }

        }
    }
}
