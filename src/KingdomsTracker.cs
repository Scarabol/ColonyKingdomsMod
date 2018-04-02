using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Pipliz;
using Pipliz.JSON;
using Pipliz.Chatting;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsTracker
  {
    static readonly List<NpcKingdom> Kingdoms = new List<NpcKingdom> ();
    static readonly ReaderWriterLockSlim KingdomsLock = new ReaderWriterLockSlim ();
    static readonly uint DefaultNextNpcID = 742000000;
    static readonly string DefaultNotifyPermission = "";
    static uint NextNpcID = DefaultNextNpcID;
    static string NotifyKingdomPermission = DefaultNotifyPermission;
    static string NotifyLootboxPermission = DefaultNotifyPermission;

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

    public static void UnregisterKingdom (NpcKingdom npcKingdom)
    {
      try {
        KingdomsLock.EnterWriteLock ();
        Kingdoms.Remove (npcKingdom);
      } finally {
        if (KingdomsLock.IsWriteLockHeld) {
          KingdomsLock.ExitWriteLock ();
        }
      }
    }

    public static List<NpcKingdom> GetAll ()
    {
      try {
        KingdomsLock.EnterReadLock ();
        return new List<NpcKingdom> (Kingdoms);
      } finally {
        if (KingdomsLock.IsReadLockHeld) {
          KingdomsLock.ExitReadLock ();
        }
      }
    }

    public static List<NpcKingdom> GetAllByType (string kingdomType)
    {
      try {
        KingdomsLock.EnterReadLock ();
        return Kingdoms.FindAll (kingdom => kingdom.KingdomType.Equals (kingdomType));
      } finally {
        if (KingdomsLock.IsReadLockHeld) {
          KingdomsLock.ExitReadLock ();
        }
      }
    }

    public static bool TryGetClosest (string kingdomType, Vector3Int position, out NpcKingdom kingdom)
    {
      if (Count < 1) {
        kingdom = null;
        return false;
      }
      kingdom = GetAllByType (kingdomType).OrderBy (npcKingdom => Pipliz.Math.ManhattanDistance (position, npcKingdom.Origin)).First ();
      return true;
    }

    public static void SendKingdomNotification (string notification)
    {
      SendNotification (notification, NotifyKingdomPermission);
    }

    public static void SendLootboxNotification (string notification)
    {
      SendNotification (notification, NotifyLootboxPermission);
    }

    static void SendNotification (string notification, string permission)
    {
      Log.Write (notification);
      for (int c = 0; c < Players.CountConnected; c++) {
        var player = Players.GetConnectedByIndex (c);
        if (Permissions.PermissionsManager.HasPermission (player, permission)) {
          Chat.Send (player, notification);
        }
      }
    }

    public static uint GetNextNpcID ()
    {
      NextNpcID++;
      return NextNpcID;
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
          jsonFileNode.TryGetAsOrDefault ("NextNpcID", out NextNpcID, DefaultNextNpcID);
          jsonFileNode.TryGetAsOrDefault ("NotifyKingdomPermission", out NotifyKingdomPermission, DefaultNotifyPermission);
          jsonFileNode.TryGetAsOrDefault ("NotifyLootboxPermission", out NotifyLootboxPermission, DefaultNotifyPermission);
          JSONNode jsonSpawner;
          if (jsonFileNode.TryGetAs ("spawner", out jsonSpawner) && jsonSpawner.NodeType == NodeType.Object) {
            KingdomSpawner.SetFromJson (jsonSpawner);
          } else {
            Log.Write ($"kingdom spawner not configured in {JsonFilePath}, loading defaults");
          }
          JSONNode jsonLoot;
          if (jsonFileNode.TryGetAs ("loot", out jsonLoot)) {
            Lootbox.SetFromJson (jsonLoot);
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
                kingdom = new NpcFarm ();
              } else {
                Log.WriteError ($"Unknown npc kingdom type {type}");
                continue;
              }
              kingdom.InitFromJson (jsonNode);
              NextNpcID = System.Math.Max (NextNpcID, kingdom.NpcID);
            }
          }
          Log.Write ($"Loaded {Count} kingdoms from json");
        }
      } catch (Exception exception) {
        Log.WriteError ($"Exception while loading kingdoms; {exception.Message}");
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAutoSaveWorld, "scarabol.kingdoms.onautosaveworld")]
    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnQuitEarly, "scarabol.kingdoms.onquitearly")]
    public static void Save ()
    {
      try {
        var jsonFileNode = new JSONNode ();
        jsonFileNode.SetAs ("NextNpcID", NextNpcID);
        jsonFileNode.SetAs ("NotifyKingdomPermission", NotifyKingdomPermission);
        jsonFileNode.SetAs ("NotifyLootboxPermission", NotifyLootboxPermission);
        jsonFileNode.SetAs ("spawner", KingdomSpawner.GetJson ());
        jsonFileNode.SetAs ("loot", Lootbox.GetJson ());
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
        JSON.Serialize (JsonFilePath, jsonFileNode, 3);
        Log.Write ($"Saved {Count} kingdoms to json");
      } catch (Exception exception) {
        Log.WriteError ($"Exception while saving kingdoms; {exception.Message}");
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnShouldKeepChunkLoaded, "scarabol.kingdoms.npckingdomtracker.onshouldkeepchunkloaded")]
    public static void OnShouldKeepChunkLoaded (ChunkUpdating.KeepChunkLoadedData data)
    {
      try {
        KingdomsLock.EnterReadLock ();
        foreach (NpcKingdom kingdom in Kingdoms) {
          if (kingdom.IsInRange (data.CheckedChunk.Position, KingdomsModEntries.CHUNK_SIZE)) {
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
  }
}
