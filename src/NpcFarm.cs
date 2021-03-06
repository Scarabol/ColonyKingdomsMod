﻿using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;
using BlockTypes.Builtin;
using NPC;

namespace ScarabolMods
{
  public class NpcFarm : NpcKingdom
  {
    int Size;
    bool Build;

    public static NpcFarm Create (Vector3Int farmPosition, int size)
    {
      var result = new NpcFarm ();
      result.Origin = farmPosition;
      result.Size = size;
      return result;
    }

    public NpcFarm () : base ("farm")
    {
      RangeInChunks = 2;
      HeightInChunks = 1;
      PrimaryRange = 8;
      PrimaryMinY = 0;
      PrimaryMaxY = 6;
    }

    public override JSONNode GetJson ()
    {
      var kingdomNode = base.GetJson ();
      kingdomNode.SetAs ("Size", Size);
      return kingdomNode;
    }

    public override void InitFromJson (JSONNode jsonNode)
    {
      base.InitFromJson (jsonNode);
      jsonNode.TryGetAs ("Size", out Size);
    }

    public override void InitNew ()
    {
      base.InitNew ();
      Name = "NPC-Farmer";
      Build = true;
      KingdomsTracker.SendKingdomNotification ($"Placed a {KingdomType} of size {Size} at {Origin}");
    }

    protected override void Update (Players.Player player)
    {
      if (Build) {
        Build = false;
        var builder = new NpcFarmBuilder (player, Origin, Size);
        builder.Build ();
        LootSpawner.SetPossibleLootSpots (builder.LootSpots);
      } else if (BedBlockTracker.GetCount (player) < 1) {
        KingdomsTracker.SendKingdomNotification ($"Farm at {Origin} is dead! Lost all beds");
        Kill ();
      } else {
        var stockpile = Stockpile.GetStockPile (player);
        var colony = Colony.Get (player);
        CheckItemAmount (stockpile, BuiltinBlocks.Bread, 5000);
        CheckFollower (player, colony);
        CheckItemAmount (stockpile, BuiltinBlocks.WheatStage1, 100);
      }
    }

    void CheckItemAmount (Stockpile stockpile, ushort itemType, int minAmount)
    {
      var missing = minAmount - stockpile.AmountContained (itemType);
      stockpile.Add (itemType, missing);
    }

    void CheckFollower (Players.Player player, Colony colony)
    {
      int maxFollower = Math.Min (Size, BedBlockTracker.GetCount (player));
      for (int c = colony.FollowerCount; c < maxFollower; c++) {
        SpawnNpc (colony);
      }
    }

    void SpawnNpc (Colony colony)
    {
      NPCBase npc = new NPCBase (Server.NPCs.NPCType.GetByKeyNameOrDefault ("pipliz.laborer"), Origin.Vector, colony);
      ModLoader.TriggerCallbacks (ModLoader.EModCallbackType.OnNPCRecruited, npc);
      colony.SendUpdate ();
    }
  }
}
