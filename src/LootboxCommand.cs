using System;
using Pipliz;
using Permissions;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public class LootboxChatCommand : ChatCommands.IChatCommand
  {
    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.kingdoms.lootbox.registercommand")]
    public static void AfterItemTypesDefined ()
    {
      ChatCommands.CommandManager.RegisterCommand (new LootboxChatCommand ());
    }

    public bool IsCommand (string chat)
    {
      return chat.Equals ("/lootbox");
    }

    public bool TryDoCommand (Players.Player causedBy, string chattext)
    {
      try {
        if (PermissionsManager.CheckAndWarnPermission (causedBy, KingdomsModEntries.MOD_PREFIX + "lootbox")) {
          Lootbox.DoGamble (causedBy);
        }
      } catch (Exception exception) {
        Log.WriteError ($"Exception while parsing command; {exception.Message} - {exception.StackTrace}");
      }
      return true;
    }
  }
}
