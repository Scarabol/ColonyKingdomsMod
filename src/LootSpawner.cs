using System;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Threading;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public class LootSpawner
  {
    readonly List<Vector3Int> PossibleLootSpots = new List<Vector3Int> ();
    readonly List<Vector3Int> PlacedLoot = new List<Vector3Int> ();
    readonly LootboxProperties Properties;
    NetworkID NetworkID;
    bool Dead;

    public LootSpawner (string kingdomType)
    {
      Properties = Lootbox.GetLootboxProperties (kingdomType);
    }

    public void SetPossibleLootSpots (List<Vector3Int> possibleSpots)
    {
      PossibleLootSpots.Clear ();
      PossibleLootSpots.AddRange (possibleSpots);
    }

    public void Start (NetworkID networkID)
    {
      NetworkID = networkID;
      UpdateLoot ();
    }

    public void Kill ()
    {
      Dead = true;
    }

    void UpdateLoot ()
    {
      var invocationDelay = Properties.MinRespawnDelayMinutes + Pipliz.Random.Next (Properties.MaxRespawnDelayMinutes - Properties.MinRespawnDelayMinutes);
      ThreadManager.InvokeOnMainThread (delegate {
        try {
          if (Dead) {
            return;
          }
          var lootType = ItemTypes.IndexLookup.GetIndex (Lootbox.ITEMKEY);
          foreach (var oldPlacedLoot in new List<Vector3Int> (PlacedLoot)) {
            ushort actualType;
            if (World.TryGetTypeAt (oldPlacedLoot, out actualType) && actualType != lootType) {
              PlacedLoot.Remove (oldPlacedLoot);
            }
          }
          int spawnCount = Properties.MinCount + Pipliz.Random.Next (Properties.MaxCount - Properties.MinCount - PlacedLoot.Count);
          var player = Players.GetPlayer (NetworkID);
          var currentSpotList = new List<Vector3Int> (PossibleLootSpots);
          for (int c = 0; c < spawnCount && currentSpotList.Count > 0;) {
            var spot = Pipliz.Random.Next (currentSpotList.Count);
            var position = currentSpotList [spot];
            currentSpotList.RemoveAt (spot);
            ushort actualType;
            if (World.TryGetTypeAt (position, out actualType) && actualType == BuiltinBlocks.Air) {
              PlacedLoot.Add (position);
              BlockPlacementHelper.PlaceBlock (position, lootType, player);
              c++;
              KingdomsTracker.SendLootboxNotification ($"Unclaimed lootbox at {position}");
            }
          }
        } catch (Exception exception) {
          Log.WriteError ($"Exception while spawning loot; {exception.Message}");
        }
        UpdateLoot ();
      }, invocationDelay * 60);
    }
  }

}
