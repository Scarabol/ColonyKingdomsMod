using System;
using System.Text.RegularExpressions;
using Pipliz;
using Pipliz.Chatting;
using Server.TerrainGeneration;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public class FarmChatCommand : ChatCommands.IChatCommand
  {
    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.kingdoms.farm.registercommand")]
    public static void AfterItemTypesDefined ()
    {
      ChatCommands.CommandManager.RegisterCommand (new FarmChatCommand ());
    }

    public bool IsCommand (string chat)
    {
      return chat.Equals ("/farm") || chat.StartsWith ("/farm ");
    }

    public bool TryDoCommand (Players.Player causedBy, string chattext)
    {
      try {
        if (!Permissions.PermissionsManager.CheckAndWarnPermission (causedBy, KingdomsModEntries.MOD_PREFIX + "farm")) {
          return true;
        }
        var m = Regex.Match (chattext, @"/farm( (?<size>\d+))?");
        if (!m.Success) {
          Chat.Send (causedBy, "Command didn't match, use /farm [size]");
          return true;
        }
        string strSize = m.Groups ["size"].Value;
        int size;
        if (strSize.Length > 0) {
          if (!int.TryParse (strSize, out size)) {
            Chat.Send (causedBy, "Could not parse size");
            return true;
          }
          if (size < 1 || size > NpcFarmBuilder.MAX_SIZE) {
            Chat.Send (causedBy, $"Size is out of range; min: 1; max: {NpcFarmBuilder.MAX_SIZE}");
            return true;
          }
        } else {
          size = 1 + Pipliz.Random.Next (NpcFarmBuilder.MAX_SIZE);
        }
        var farmPosition = ToFarmPosition (causedBy.Position);
        NpcKingdomFarm.Create (farmPosition, size).InitNew ();
        Chat.Send (causedBy, $"You placed a farm at {farmPosition} with size {size}");
      } catch (Exception exception) {
        Log.WriteError (string.Format ("Exception while parsing command; {0} - {1}", exception.Message, exception.StackTrace));
      }
      return true;
    }

    static Vector3Int ToFarmPosition (UnityEngine.Vector3 position)
    {
      position.y = TerrainGenerator.UsedGenerator.GetHeight (position.x, position.z) + 1;
      return new Vector3Int (position);
    }
  }
}
