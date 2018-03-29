using System;
using Pipliz;
using Permissions;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public abstract class KillKingdomChatCommand : ChatCommands.IChatCommand
  {
    protected abstract String KingdomType { get; }

    public bool IsCommand (string chat)
    {
      return chat.Equals ($"/kill{KingdomType}") || chat.StartsWith ($"/kill{KingdomType} ");
    }

    public bool TryDoCommand (Players.Player causedBy, string chattext)
    {
      try {
        if (PermissionsManager.CheckAndWarnPermission (causedBy, KingdomsModEntries.MOD_PREFIX + $"kill{KingdomType}")) {
          NpcKingdom closestFarm;
          if (chattext.Equals ($"/kill{KingdomType} all")) {
            KingdomsTracker.GetAllByType (KingdomType).ForEach (kingdom => kingdom.Kill ());
            KingdomsTracker.SendNotification ($"killed all {KingdomType}");
          } else if (KingdomsTracker.TryGetClosest (KingdomType, causedBy.VoxelPosition, out closestFarm)) {
            closestFarm.Kill ();
            KingdomsTracker.SendNotification ($"Killed {KingdomType} at {closestFarm.Origin}");
          }
        }
      } catch (Exception exception) {
        Log.WriteError ($"Exception while parsing command; {exception.Message} - {exception.StackTrace}");
      }
      return true;
    }
  }

  public class KillFarmCommand : KillKingdomChatCommand
  {
    protected override string KingdomType => "farm";

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.kingdoms.killfarm.registercommand")]
    public static void AfterItemTypesDefined ()
    {
      ChatCommands.CommandManager.RegisterCommand (new KillFarmCommand ());
    }
  }
}
