using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Pipliz;
using Pipliz.Chatting;
using Server.TerrainGeneration;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public class KingdomListChatCommand : ChatCommands.IChatCommand
  {
    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.kingdoms.kingdomlist.registercommand")]
    public static void AfterItemTypesDefined ()
    {
      ChatCommands.CommandManager.RegisterCommand (new KingdomListChatCommand ());
    }

    public bool IsCommand (string chat)
    {
      return chat.Equals ("/kingdomlist") || chat.StartsWith ("/kingdomlist ");
    }

    public bool TryDoCommand (Players.Player causedBy, string chattext)
    {
      try {
        if (!Permissions.PermissionsManager.CheckAndWarnPermission (causedBy, KingdomsModEntries.MOD_PREFIX + "listfarms")) {
          return true;
        }
        var m = Regex.Match (chattext, @"/kingdomlist( (?<kingdomType>.+))?");
        if (!m.Success) {
          Chat.Send (causedBy, "Command didn't match, use /kingdomlist [kingdomType]");
          return true;
        }
        var kingdomType = m.Groups ["kingdomType"].Value;
        List<NpcKingdom> kingdoms;
        if (kingdomType.Length < 1 || "all".Equals (kingdomType)) {
          kingdoms = KingdomsTracker.GetAll ();
        } else {
          kingdoms = KingdomsTracker.GetAllByType (kingdomType);
        }
        var msg = string.Join ("\n", kingdoms.Select (f => $"{kingdomType} at {f.Origin}").ToArray ());
        if (msg.Length < 1) {
          Chat.Send (causedBy, $"No '{kingdomType}' kingdoms found");
        } else {
          Chat.Send (causedBy, msg);
        }
      } catch (Exception exception) {
        Log.WriteError ($"Exception while parsing command; {exception.Message} - {exception.StackTrace}");
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
