﻿using System;
using System.Threading;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;
using Pipliz.Chatting;
using Server.TerrainGeneration;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomSpawner
  {
    static readonly int DefaultMaxNumberOfKingdoms = 10;
    static readonly int DefaultMaxRangeFromSpawn = 10000;
    static readonly int DefaultDelayBetweenPlacingAttempts = 10000;
    static readonly int DefaultNumOfSpotsToCheckPerAttempt = 50;
    static readonly int DefaultDelayBetweenSpotChecks = 1000;
    static readonly string DefaultNotifyPlacementPermission = "";

    static int MaxNumberOfKingdoms = DefaultMaxNumberOfKingdoms;
    static int MaxRangeFromSpawn = DefaultMaxRangeFromSpawn;
    static int DelayBetweenPlacingAttempts = DefaultDelayBetweenPlacingAttempts;
    static int NumOfSpotsToCheckPerAttempt = DefaultNumOfSpotsToCheckPerAttempt;
    static int DelayBetweenSpotChecks = DefaultDelayBetweenSpotChecks;
    static string NotifyPlacementPermission = DefaultNotifyPlacementPermission;

    static HashSet<Vector3Int> currentlyUsedChunks = new HashSet<Vector3Int> ();
    static readonly ReaderWriterLockSlim currentlyUsedChunksLock = new ReaderWriterLockSlim ();

    public static JSONNode GetJson ()
    {
      JSONNode result = new JSONNode ();
      result.SetAs ("MaxNumberOfKingdoms", MaxNumberOfKingdoms);
      result.SetAs ("MaxRangeFromSpawn", MaxRangeFromSpawn);
      result.SetAs ("DelayBetweenPlacingAttempts", DelayBetweenPlacingAttempts);
      result.SetAs ("NumOfSpotsToCheckPerAttempt", NumOfSpotsToCheckPerAttempt);
      result.SetAs ("DelayBetweenSpotChecks", DelayBetweenSpotChecks);
      result.SetAs ("NotifyPlacementPermission", NotifyPlacementPermission);
      return result;
    }

    public static void SetFromJson (JSONNode jsonNode)
    {
      jsonNode.TryGetAsOrDefault ("MaxNumberOfKingdoms", out MaxNumberOfKingdoms, DefaultMaxNumberOfKingdoms);
      jsonNode.TryGetAsOrDefault ("MaxRangeFromSpawn", out MaxRangeFromSpawn, DefaultMaxRangeFromSpawn);
      jsonNode.TryGetAsOrDefault ("DelayBetweenPlacingAttempts", out DelayBetweenPlacingAttempts, DefaultDelayBetweenPlacingAttempts);
      jsonNode.TryGetAsOrDefault ("NumOfSpotsToCheckPerAttempt", out NumOfSpotsToCheckPerAttempt, DefaultNumOfSpotsToCheckPerAttempt);
      jsonNode.TryGetAsOrDefault ("DelayBetweenSpotChecks", out DelayBetweenSpotChecks, DefaultDelayBetweenSpotChecks);
      jsonNode.TryGetAsOrDefault ("NotifyPlacementPermission", out NotifyPlacementPermission, DefaultNotifyPlacementPermission);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterNetworkSetup, "scarabol.kingdoms.kingdomspawner.afternetworksetup")]
    public static void AfterNetworkSetup ()
    {
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        Log.Write ("Started kingdom spawner thread");
        while (true) {
          Thread.Sleep (DelayBetweenPlacingAttempts);
          try {
            if (KingdomsTracker.Count < MaxNumberOfKingdoms) {
              for (int c = 0; c < NumOfSpotsToCheckPerAttempt; c++) {
                var farmPosition = GetRandomSpot (MaxRangeFromSpawn);
                var farmSize = 1 + Pipliz.Random.Next (NpcFarmBuilder.MAX_SIZE);
                var npcFarm = NpcKingdomFarm.Create (farmPosition, farmSize);
                LoadChunksBlocking (npcFarm.GetPrimaryChunkPositions ());
                if (npcFarm.IsAreaClear ()) {
                  LoadChunksBlocking (npcFarm.GetTotalChunkPositions ());
                  npcFarm.InitNew ();
                  string notification = $"Placed a farm of size {farmSize} at {farmPosition}";
                  Log.Write (notification);
                  notifyPlacement ("farm", farmPosition);
                  if (KingdomsTracker.Count >= MaxNumberOfKingdoms) {
                    Log.Write ($"Reached maximum number ({MaxNumberOfKingdoms}) of kingdoms");
                  }
                  break;
                }
                try {
                  currentlyUsedChunksLock.EnterWriteLock ();
                  currentlyUsedChunks.Clear ();
                } finally {
                  if (currentlyUsedChunksLock.IsWriteLockHeld) {
                    currentlyUsedChunksLock.ExitWriteLock ();
                  }
                }
                Thread.Sleep (DelayBetweenSpotChecks);
              }
            }
          } catch (Exception exception) {
            Log.WriteError ($"Exception in kingdom update thread; {exception.Message}");
          }
        }
      }).Start ();
    }

    static Vector3Int GetRandomSpot (int maxRange)
    {
      var spawnLocation = new Vector3Int (TerrainGenerator.UsedGenerator.GetSpawnLocation (null));
      var rx = -maxRange + 2 * Pipliz.Random.Next (maxRange);
      var rz = -maxRange + 2 * Pipliz.Random.Next (maxRange);
      var result = spawnLocation.Add (rx, 0, rz);
      result.y = ((int)TerrainGenerator.UsedGenerator.GetHeight (result.x, result.z)) + 1;
      return result;
    }

    static void LoadChunksBlocking (HashSet<Vector3Int> chunksToLoad)
    {
      try {
        currentlyUsedChunksLock.EnterWriteLock ();
        currentlyUsedChunks = new HashSet<Vector3Int> (chunksToLoad);
      } finally {
        if (currentlyUsedChunksLock.IsWriteLockHeld) {
          currentlyUsedChunksLock.ExitWriteLock ();
        }
      }
      foreach (Vector3Int chunkPosition in chunksToLoad) {
        ChunkQueue.QueuePlayerSurrounding (chunkPosition);
      }
      ChunkQueue.PokeThread ();
      while (chunksToLoad.Count > 0) {
        chunksToLoad.RemoveWhere (chunkPosition => {
          Chunk chunk = World.GetChunk (chunkPosition);
          return chunk != null && chunk.DataState == Chunk.ChunkDataState.DataFull;
        });
        Thread.Sleep (10);
      }
    }

    static void notifyPlacement (string kingdomType, Vector3Int origin)
    {
      for (int c = 0; c < Players.CountConnected; c++) {
        var player = Players.GetConnectedByIndex (c);
        if (Permissions.PermissionsManager.HasPermission (player, NotifyPlacementPermission)) {
          Chat.Send (player, $"New {kingdomType} spawned at {origin}");
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnShouldKeepChunkLoaded, "scarabol.kingdoms.kingdomspawner.onshouldkeepchunkloaded")]
    public static void OnShouldKeepChunkLoaded (ChunkUpdating.KeepChunkLoadedData data)
    {
      try {
        currentlyUsedChunksLock.EnterReadLock ();
        if (currentlyUsedChunks.Contains (data.CheckedChunk.Position)) {
          data.Result = true;
        }
      } finally {
        if (currentlyUsedChunksLock.IsReadLockHeld) {
          currentlyUsedChunksLock.ExitReadLock ();
        }
      }
    }
  }
}