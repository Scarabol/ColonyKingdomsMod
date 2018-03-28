using System.Collections.Generic;
using System.Linq;
using Pipliz;
using Pipliz.JSON;
using Pipliz.Chatting;
using Permissions;
using Server.Science;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class Lootbox
  {
    public static string ITEMKEY = KingdomsModEntries.MOD_PREFIX + "lootbox";

    static string SPECIAL_SCIENCE = "special:completeactivescience";
    static string SPECIAL_DEATH = "special:death";

    static List<GambleItem> Items = new List<GambleItem> {
      new GambleItem (SPECIAL_SCIENCE, 1, 1, 1),
      new GambleItem (SPECIAL_DEATH, 0, 1, 1),
      new GambleItem ("sciencebagcolony", 15, 1, 1),
      new GambleItem ("ironwrought", 25, 1, 3),
      new GambleItem ("steelingot", 25, 1, 5),
      new GambleItem ("sciencebagadvanced", 50, 1, 1),
      new GambleItem ("lanternred", 8, 1, 2),
      new GambleItem ("lanternblue", 8, 1, 2),
      new GambleItem ("lanterncyan", 8, 1, 2),
      new GambleItem ("lanternpink", 8, 1, 2),
      new GambleItem ("lanterngreen", 8, 1, 2),
      new GambleItem ("lanternwhite", 8, 1, 2),
      new GambleItem ("lanternorange", 8, 1, 2),
      new GambleItem ("lanternyellow", 8, 1, 2),
      new GambleItem ("copper", 75, 10, 20),
      new GambleItem ("silveringot", 75, 5, 10),
      new GambleItem ("bread", 125, 10, 20),
      new GambleItem ("air", 545, 1, 1)
    };

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.kingdoms.lootbox.addrawtypes")]
    public static void AfterAddingBaseTypes (Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
    {
      itemTypes.Add (ITEMKEY, new ItemTypesServer.ItemTypeRaw (ITEMKEY, new JSONNode ()
          .SetAs ("onPlaceAudio", "woodPlace")
          .SetAs ("onRemoveAudio", "woodDeleteLight")
          .SetAs ("sideall", "crate")
          .SetAs ("npcLimit", "0")
          .SetAs ("onRemove", new JSONNode (NodeType.Array))
      ));
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.kingdoms.lootbox.registertypes")]
    public static void AfterItemTypesDefined ()
    {
      ItemTypesServer.RegisterOnRemove (ITEMKEY, OnLootboxPickup);
    }

    static void OnLootboxPickup (Vector3Int position, ushort type, Players.Player player)
    {
      DoGamble (player);
    }

    public static void DoGamble (Players.Player player)
    {
      if (player != null) {
        var priceItem = PickPrice ();
        if (SPECIAL_SCIENCE.Equals (priceItem.Typename)) {
          Chat.Send (player, "Jackpot! Just found the solution to your current research");
          ScienceManager.GetPlayerManager (player).AddActiveResearchProgress (1000000);
        } else if (SPECIAL_DEATH.Equals (priceItem.Typename)) {
          Chat.Send (player, "Fatal! Something inside killed you");
          Players.OnDeath (player);
        } else if (!ItemTypes.IndexLookup.TryGetIndex (priceItem.Typename, out ushort priceType)) {
          Log.WriteError ($"Unknown gambling price {priceItem.Typename} won by {player}");
          Chat.Send (player, "I have bad feelings about this");
        } else if (priceType == BuiltinBlocks.Air) {
          Chat.Send (player, "You found an empty box :-(");
        } else {
          Chat.Send (player, $"You found {priceItem.Amount} x {priceItem.Typename} as loot!");
          Stockpile.GetStockPile (player).Add (priceType, priceItem.Amount);
        }
      }
    }

    public static JSONNode GetJson ()
    {
      JSONNode result = new JSONNode ();
      foreach (GambleItem item in Items) {
        JSONNode itemNode = new JSONNode ();
        itemNode.SetAs ("Quantity", item.Quantity);
        itemNode.SetAs ("MinStackSize", item.MinStackSize);
        itemNode.SetAs ("MaxStackSize", item.MaxStackSize);
        result.SetAs (item.Typename, itemNode);
      }
      return result;
    }

    public static void SetFromJson (JSONNode jsonNode)
    {
      Items.Clear ();
      if (jsonNode != null && jsonNode.NodeType == NodeType.Object) {
        foreach (var item in jsonNode.LoopObject ()) {
          var jsonItem = item.Value;
          Items.Add (new GambleItem (item.Key, jsonItem.GetAsOrDefault ("Quantity", 0), jsonItem.GetAsOrDefault ("MinStackSize", 1), jsonItem.GetAsOrDefault ("MaxStackSize", 1)));
        }
      }
    }

    static GamblePrice PickPrice ()
    {
      var index = Random.Next (Items.Sum (item => item.Quantity));
      foreach (GambleItem item in Items) {
        index -= item.Quantity;
        if (index <= 0) {
          var amount = item.MinStackSize + Random.Next (item.MaxStackSize - item.MinStackSize);
          return new GamblePrice (item.Typename, amount);
        }
      }
      return new GamblePrice ("air", 0);
    }

    class GamblePrice
    {
      public readonly string Typename;
      public readonly int Amount;

      public GamblePrice (string typename, int amount)
      {
        Typename = typename;
        Amount = amount;
      }
    }

    class GambleItem
    {
      public readonly string Typename;
      public readonly int Quantity;
      public readonly int MinStackSize;
      public readonly int MaxStackSize;

      public GambleItem (string typename, int quantity, int minStackSize, int maxStackSize)
      {
        Typename = typename;
        Quantity = quantity;
        MinStackSize = minStackSize;
        MaxStackSize = maxStackSize;
      }
    }
  }

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
      if (PermissionsManager.CheckAndWarnPermission (causedBy, KingdomsModEntries.MOD_PREFIX + "lootbox")) {
        Lootbox.DoGamble (causedBy);
      }
      return true;
    }
  }
}
