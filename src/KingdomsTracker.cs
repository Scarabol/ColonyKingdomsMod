using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsTracker
  {
    static readonly List<NpcKingdom> Kingdoms = new List<NpcKingdom> ();
    static readonly ReaderWriterLockSlim KingdomsLock = new ReaderWriterLockSlim ();
    static uint NextID = 742000000;

    static string JsonFilePath {
      get {
        return Path.Combine (Path.Combine ("gamedata", "savegames"), Path.Combine (ServerManager.WorldName, "kingdoms.json"));
      }
    }

    public static void RegisterKingdom (NpcKingdom npcKingdom)
    {
      try {
        KingdomsLock.EnterWriteLock ();
        Kingdoms.Add (npcKingdom);
      } finally {
        if (KingdomsLock.IsWriteLockHeld) {
          KingdomsLock.ExitWriteLock ();
        }
      }
      npcKingdom.StartThread ();
    }

    public static uint GetNextID ()
    {
      NextID++;
      return NextID;
    }

    public static int Count {
      get {
        try {
          KingdomsLock.EnterReadLock ();
          return Kingdoms.Count;
        } finally {
          if (KingdomsLock.IsReadLockHeld) {
            KingdomsLock.ExitReadLock ();
          }
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterNetworkSetup, "scarabol.kingdoms.afternetworksetup")]
    public static void Load ()
    {
      try {
        JSONNode jsonFileNode;
        if (JSON.Deserialize (JsonFilePath, out jsonFileNode, false)) {
          try {
            KingdomsLock.EnterWriteLock ();
            Kingdoms.Clear ();
          } finally {
            if (KingdomsLock.IsWriteLockHeld) {
              KingdomsLock.ExitWriteLock ();
            }
          }
          JSONNode jsonSpawner;
          if (jsonFileNode.TryGetAs ("spawner", out jsonSpawner) && jsonSpawner.NodeType == NodeType.Object) {
            KingdomSpawner.SetFromJson (jsonSpawner);
          } else {
            Log.Write ($"kingdom spawner not configured in {JsonFilePath}, loading defaults");
          }
          JSONNode jsonKingdoms;
          if (!jsonFileNode.TryGetAs ("kingdoms", out jsonKingdoms) || jsonKingdoms.NodeType != NodeType.Array) {
            Log.WriteError ($"No 'kingdoms' array found in '{JsonFilePath}'");
            return;
          }
          foreach (JSONNode jsonNode in jsonKingdoms.LoopArray ()) {
            string type;
            if (jsonNode.TryGetAs ("KingdomType", out type)) {
              NpcKingdom kingdom;
              if ("farm".Equals (type)) {
                kingdom = new NpcKingdomFarm ();
              } else {
                Log.WriteError ($"Unknown npc kingdom type {type}");
                continue;
              }
              kingdom.InitFromJson (jsonNode);
              NextID = System.Math.Max (NextID, kingdom.NpcID);
            }
          }
          Log.Write ($"Loaded {Count} kingdoms from json");
        }
      } catch (Exception exception) {
        Log.WriteError (string.Format ("Exception while loading kingdoms; {0}", exception.Message));
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAutoSaveWorld, "scarabol.kingdoms.onautosaveworld")]
    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnQuitEarly, "scarabol.kingdoms.onquitearly")]
    public static void Save ()
    {
      try {
        JSONNode jsonFileNode = new JSONNode ();
        JSONNode jsonSpawner = KingdomSpawner.GetJson ();
        jsonFileNode.SetAs ("spawner", jsonSpawner);
        JSONNode jsonKingdoms = new JSONNode (NodeType.Array);
        try {
          KingdomsLock.EnterReadLock ();
          foreach (NpcKingdom kingdom in Kingdoms) {
            jsonKingdoms.AddToArray (kingdom.GetJson ());
          }
        } finally {
          if (KingdomsLock.IsReadLockHeld) {
            KingdomsLock.ExitReadLock ();
          }
        }
        jsonFileNode.SetAs ("kingdoms", jsonKingdoms);
        JSON.Serialize (JsonFilePath, jsonFileNode, 2);
        Log.Write ($"Saved {Count} kingdoms to json");
      } catch (Exception exception) {
        Log.WriteError (string.Format ("Exception while saving kingdoms; {0}", exception.Message));
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnShouldKeepChunkLoaded, "scarabol.kingdoms.npckingdomtracker.onshouldkeepchunkloaded")]
    public static void OnShouldKeepChunkLoaded (ChunkUpdating.KeepChunkLoadedData data)
    {
      try {
        KingdomsLock.EnterReadLock ();
        foreach (NpcKingdom kingdom in Kingdoms) {
          if (IsInRange (data.CheckedChunk.Position, KingdomsModEntries.CHUNK_SIZE, kingdom.Origin, kingdom.Range)) {
            data.Result = true;
            return;
          }
        }
      } finally {
        if (KingdomsLock.IsReadLockHeld) {
          KingdomsLock.ExitReadLock ();
        }
      }
    }

    static bool IsInRange (Vector3Int position1, int range1, Vector3Int position2, int range2)
    {
      var dx = position1.x - position2.x;
      int dy = position1.y - position2.y;
      int dz = position1.z - position2.z;
      var distanceSquare = Pipliz.Math.Pow2 (dx) + Pipliz.Math.Pow2 (dy) + Pipliz.Math.Pow2 (dz);
      var maxDist = Pipliz.Math.Pow2 (range1 + range2);
      return distanceSquare < maxDist;
    }
  }
}
