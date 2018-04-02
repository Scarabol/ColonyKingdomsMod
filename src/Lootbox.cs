using System.Collections.Generic;
using System.Linq;
using Pipliz;
using Pipliz.JSON;
using Pipliz.Chatting;
using Server.Science;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class Lootbox
  {
    public static string ITEMKEY = KingdomsModEntries.MOD_PREFIX + "lootbox";
    public static readonly int DefaultLootboxMinRespawnDelay = 30;
    public static readonly int DefaultLootboxMaxRespawnDelay = 90;

    static readonly string SPECIAL_SCIENCE = "special:completeactivescience";
    static readonly string SPECIAL_DEATH = "special:death";

    static readonly Dictionary<string, LootboxProperties> LootboxPropertiesByKingdomType = new Dictionary<string, LootboxProperties> {
      { "farm", new LootboxProperties (1, 2, DefaultLootboxMinRespawnDelay, DefaultLootboxMaxRespawnDelay) }
    };

    static List<LootOption> LootOptions = new List<LootOption> {
      new LootOption (SPECIAL_SCIENCE, 1, 1, 1),
      new LootOption (SPECIAL_DEATH, 0, 1, 1),
      new LootOption ("sciencebagcolony", 15, 1, 1),
      new LootOption ("ironwrought", 25, 1, 3),
      new LootOption ("steelingot", 25, 1, 5),
      new LootOption ("sciencebagadvanced", 50, 1, 1),
      new LootOption ("lanternred", 8, 1, 2),
      new LootOption ("lanternblue", 8, 1, 2),
      new LootOption ("lanterncyan", 8, 1, 2),
      new LootOption ("lanternpink", 8, 1, 2),
      new LootOption ("lanterngreen", 8, 1, 2),
      new LootOption ("lanternwhite", 8, 1, 2),
      new LootOption ("lanternorange", 8, 1, 2),
      new LootOption ("lanternyellow", 8, 1, 2),
      new LootOption ("copper", 75, 10, 20),
      new LootOption ("silveringot", 75, 5, 10),
      new LootOption ("bread", 125, 10, 20),
      new LootOption ("air", 545, 1, 1)
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
        var priceItem = PickLoot ();
        ushort priceType;
        if (SPECIAL_SCIENCE.Equals (priceItem.Typename)) {
          Chat.Send (player, "Jackpot! Just found the solution to your current research");
          ScienceManager.GetPlayerManager (player).AddActiveResearchProgress (1000000);
        } else if (SPECIAL_DEATH.Equals (priceItem.Typename)) {
          Chat.Send (player, "Fatal! Something inside killed you");
          Players.TakeHit (player, 1000000);
        } else if (!ItemTypes.IndexLookup.TryGetIndex (priceItem.Typename, out priceType)) {
          Log.WriteError ($"Unknown gambling price {priceItem.Typename} won by {player}");
          Chat.Send (player, "I have bad feelings about this");
        } else if (priceType == BuiltinBlocks.Air) {
          Chat.Send (player, "You found an empty box :-(");
        } else {
          Chat.Send (player, $"You found {priceItem.Amount} x {priceItem.Typename} as loot!");
          if (!Inventory.GetInventory (player).TryAdd (priceType, priceItem.Amount)) {
            Stockpile.GetStockPile (player).Add (priceType, priceItem.Amount);
          }
        }
      }
    }

    public static JSONNode GetJson ()
    {
      var jsonLootOptions = new JSONNode ();
      foreach (LootOption item in LootOptions) {
        jsonLootOptions.SetAs (item.Typename, item.GetJson ());
      }
      var result = new JSONNode ();
      result.SetAs ("LootOptions", jsonLootOptions);
      var jsonLootboxProperties = new JSONNode ();
      foreach (var entry in LootboxPropertiesByKingdomType) {
        jsonLootboxProperties.SetAs (entry.Key, entry.Value.GetJson ());
      }
      result.SetAs ("LootboxProperties", jsonLootboxProperties);
      return result;
    }

    public static void SetFromJson (JSONNode jsonNode)
    {
      JSONNode jsonLootOptions;
      if (jsonNode.TryGetAs ("LootOptions", out jsonLootOptions)) {
        LootOptions.Clear ();
        foreach (var jsonLootOption in jsonLootOptions.LoopObject ()) {
          LootOptions.Add (new LootOption (jsonLootOption.Key, jsonLootOption.Value));
        }
      }
      JSONNode jsonLootboxProperties;
      if (jsonNode.TryGetAs ("LootboxProperties", out jsonLootboxProperties)) {
        LootboxPropertiesByKingdomType.Clear ();
        foreach (var jsonKingdomLootbox in jsonLootboxProperties.LoopObject ()) {
          LootboxPropertiesByKingdomType.Add (jsonKingdomLootbox.Key, new LootboxProperties (jsonKingdomLootbox.Value));
        }
      }
    }

    public static LootboxProperties GetLootboxProperties (string kingdomType)
    {
      LootboxProperties properties;
      if (LootboxPropertiesByKingdomType.TryGetValue (kingdomType, out properties)) {
        return properties;
      }
      Log.Write ($"Could not get lootbox properties for {kingdomType} returning default values");
      return new LootboxProperties (0, 0, DefaultLootboxMinRespawnDelay, DefaultLootboxMaxRespawnDelay);
    }

    static Loot PickLoot ()
    {
      var index = Random.Next (LootOptions.Sum (item => item.WeightedProbability));
      foreach (LootOption item in LootOptions) {
        index -= item.WeightedProbability;
        if (index <= 0) {
          var amount = item.MinStackSize + Random.Next (item.MaxStackSize - item.MinStackSize);
          return new Loot (item.Typename, amount);
        }
      }
      return new Loot ("air", 0);
    }

    class Loot
    {
      public readonly string Typename;
      public readonly int Amount;

      public Loot (string typename, int amount)
      {
        Typename = typename;
        Amount = amount;
      }
    }

    class LootOption
    {
      public readonly string Typename;
      public readonly int WeightedProbability;
      public readonly int MinStackSize;
      public readonly int MaxStackSize;

      public LootOption (string typename, int weightedProbability, int minStackSize, int maxStackSize)
      {
        Typename = typename;
        WeightedProbability = weightedProbability;
        MinStackSize = minStackSize;
        MaxStackSize = maxStackSize;
      }

      public LootOption (string typename, JSONNode jsonNode)
      {
        Typename = typename;
        WeightedProbability = jsonNode.GetAsOrDefault ("WeightedProbability", 0);
        MinStackSize = jsonNode.GetAsOrDefault ("MinStackSize", 1);
        MaxStackSize = jsonNode.GetAsOrDefault ("MaxStackSize", 1);
      }

      public JSONNode GetJson ()
      {
        var result = new JSONNode ();
        result.SetAs ("WeightedProbability", WeightedProbability);
        result.SetAs ("MinStackSize", MinStackSize);
        result.SetAs ("MaxStackSize", MaxStackSize);
        return result;
      }
    }
  }

  public class LootboxProperties
  {
    public readonly int MinCount;
    public readonly int MaxCount;
    public int MinRespawnDelayMinutes;
    public int MaxRespawnDelayMinutes;

    public LootboxProperties (int minCount, int maxCount, int minRespawnDelayMinutes, int maxRespawnDelayMinutes)
    {
      MinCount = minCount;
      MaxCount = maxCount;
      SetRespawnDelayMinutes (minRespawnDelayMinutes, maxRespawnDelayMinutes);
    }

    public LootboxProperties (JSONNode jsonNode)
    {
      MinCount = jsonNode.GetAsOrDefault ("MinCount", 0);
      MaxCount = jsonNode.GetAsOrDefault ("MaxCount", 0);
      SetRespawnDelayMinutes (jsonNode.GetAsOrDefault ("MinRespawnDelayMinutes", Lootbox.DefaultLootboxMinRespawnDelay), jsonNode.GetAsOrDefault ("MaxRespawnDelayMinutes", Lootbox.DefaultLootboxMaxRespawnDelay));
    }

    public JSONNode GetJson ()
    {
      var result = new JSONNode ();
      result.SetAs ("MinCount", MinCount);
      result.SetAs ("MaxCount", MaxCount);
      result.SetAs ("MinRespawnDelayMinutes", MinRespawnDelayMinutes);
      result.SetAs ("MaxRespawnDelayMinutes", MaxRespawnDelayMinutes);
      return result;
    }

    void SetRespawnDelayMinutes (int minMinutes, int maxMinutes)
    {
      if (minMinutes < 1) {
        Log.WriteError ($"Loot minimal respawn delay value forced to 1. Input value '{minMinutes}' is too low");
        minMinutes = 1;
      }
      MinRespawnDelayMinutes = minMinutes;
      if (maxMinutes < MinRespawnDelayMinutes) {
        Log.WriteError ($"Loot maximum respawn delay value forced to {MinRespawnDelayMinutes}. Input value '{maxMinutes}' is to low");
        maxMinutes = MinRespawnDelayMinutes;
      }
      MaxRespawnDelayMinutes = maxMinutes;
    }
  }
}
