using LabApi.Loader.Features.Plugins;
using System;
using LabApi;
using LabApi.Features;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MEC;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using CommandSystem;
using ICommand = CommandSystem.ICommand;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.PlayerEvents;

namespace KibsAdminEventsPlus
{
    internal class KAE : Plugin
    {

        public override string Name { get; } = "KibsAdminEvents";

        public override string Description { get; } = "Adds a bunch of automated events";

        public override string Author { get; } = "Kibs";

        public override Version Version { get; } = new Version(1, 0);

        public override Version RequiredApiVersion { get; }

        public override void Disable()
        {
            LabApi.Events.Handlers.ServerEvents.RoundStarted -= KAEmanager.Roundstart;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole -= KAEmanager.Rolechange;
        }

        public override void Enable()
        {
            KAEmanager.RegisterAll();
            LabApi.Events.Handlers.ServerEvents.RoundStarted += KAEmanager.Roundstart;
            LabApi.Events.Handlers.PlayerEvents.ChangedRole += KAEmanager.Rolechange;

        }
        /// <summary>
        /// The list of all KAEevents, registers when the pluggin is enabled
        /// </summary>
        public static List<KAEevent> Events;
        /// <summary>
        /// The event scheduled to happen the moment a new round starts. 
        /// </summary>
        public static KAEevent WaitingEvent;
        /// <summary>
        /// The event that is happening on this current active round
        /// </summary>
        public static KAEevent ActiveEvent;
    }
    public class KAEmanager
    {
        public static void Roundstart()
        {
            KAE.ActiveEvent = KAE.WaitingEvent;
            KAE.WaitingEvent = null;
            if (KAE.ActiveEvent != null)
            {
                KAE.ActiveEvent.Startall();
            }
        }
        public static void Rolechange(PlayerChangedRoleEventArgs ev)
        {
            if (KAE.ActiveEvent != null)
            {
                KAE.ActiveEvent.RoleChanged(ev);
            }
        }
        /// <summary>
        /// Automatically registers all custom admin events that use the KAEevent abstract class. (your welcome)
        /// </summary>
        public static void RegisterAll()
        {
            IEnumerable<KAEevent> GetAll()
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsSubclassOf(typeof(KAEevent)))
                    .Select(type => Activator.CreateInstance(type) as KAEevent);
            }

            KAE.Events = GetAll().ToList();
        }

    }
    /// <summary>
    /// The abstract class used to define a new automated server event. Just do (class name) : KAEevent.
    /// </summary>
    public abstract class KAEevent
    {
        public static CoroutineHandle _EvLoop;
        public abstract int Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual float Tps { get; set; } = 0.1f;
        /// <summary>
        /// Runs once when the round starts
        /// </summary>
        public abstract void RoundStart();

        /// <summary>
        /// Runs both the round start code and activates the loop code
        /// </summary>
        public virtual void Startall()
        {
            RoundStart();
            RunLoop();
        }
        /// <summary>
        /// Used to start looping the code in <see cref="Loop"/>
        /// </summary>
        public virtual void RunLoop()
        {
            _EvLoop = Timing.RunCoroutine(LoopCoro());
        }
        /// <summary>
        /// Just a filler coro, ignore it.
        /// </summary>
        public virtual IEnumerator<float> LoopCoro()
        {
            while (Round.IsRoundInProgress)
            {
                Loop();
                yield return Timing.WaitForSeconds(Tps);
            }
        }
        /// <summary>
        /// Runs every 0.1 seconds. Change the tps value to adjust ticks per second (public override float tps)
        /// </summary>
        public virtual void Loop()
        {
        }
        /// <summary>
        /// Runs when a players role changes
        /// </summary>
        public virtual void RoleChanged(PlayerChangedRoleEventArgs ev)
        {

        }

    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class KAEcommand : ParentCommand
    {
        public override string Command => "KibsAdminEvents";

        public override string[] Aliases => new[] { "KAE" };

        public override string Description => "Master command for managing all of kibs admin events";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new KaeList());
            RegisterCommand(new KaeScedule());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Specify a valid subcommand. Subcommands are:" + System.Environment.NewLine + "List (L): Gets a list of all events" +
                System.Environment.NewLine + "Check(C): Checks the active and next events" + System.Environment.NewLine + "Schedule(sc): Sets up an event to be activated";
            return true;
        }
    }
    [CommandHandler(typeof(KAEcommand))]
    public class KaeList : ICommand
    {
        public string Command => "List";

        public string[] Aliases => new[] { "L" };

        public string Description => "Lists all commands integrated";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string text = "Id | Name | Description";
            foreach (var item in KAE.Events)
            {
                text += System.Environment.NewLine + item.Id + " | " + item.Name + " | " + "   " + item.Description;
            }
            response = text;
            return true;

        }
    }
    [CommandHandler(typeof(KAEcommand))]
    public class KaeScedule : ICommand
    {
        public string Command => "Schedule";

        public string[] Aliases => new[] { "sch", "sc" };

        public string Description => "Sets up an event to be started on the next round, type in the id of the event";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                int num = int.Parse(arguments.First());
                if (KAE.Events.Exists(item => item.Id == num))
                {
                    KAEevent ka = KAE.Events.Where<KAEevent>(item => item.Id == num).First();

                    response = "Scheduled " + ka.Name + " event for next round";
                    KAE.WaitingEvent = ka;
                    return true;
                }
                else
                {
                    response = "Please specify a valid id. Use KAE list to se a list of valid events";
                    return false;
                }


            }
            catch
            {
                response = "Please type a number";
                return false;
            }


        }
    }
    [CommandHandler(typeof(KAEcommand))]
    public class KaeCurrent : ICommand
    {
        public string Command => "Check";

        public string[] Aliases => new[] { "Ch" };

        public string Description => "Checks what the current and next events are";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {

            response = "Current Event is: " + KAE.ActiveEvent.Name + System.Environment.NewLine + "Event to start at next round start: " + KAE.WaitingEvent.Name;
            return true;

        }
    }
}
