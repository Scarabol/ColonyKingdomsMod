using System;
using System.Threading;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;
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
    static readonly int DefaultMinDistanceToBanners = 200;

    static int MaxNumberOfKingdoms = DefaultMaxNumberOfKingdoms;
    static int MaxRangeFromSpawn = DefaultMaxRangeFromSpawn;
    static int DelayBetweenPlacingAttempts = DefaultDelayBetweenPlacingAttempts;
    static int NumOfSpotsToCheckPerAttempt = DefaultNumOfSpotsToCheckPerAttempt;
    static int DelayBetweenSpotChecks = DefaultDelayBetweenSpotChecks;
    static int MinDistanceToBanners = DefaultMinDistanceToBanners;

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
      result.SetAs ("MinDistanceToBanners", MinDistanceToBanners);
      return result;
    }

    public static void SetFromJson (JSONNode jsonNode)
    {
      jsonNode.TryGetAsOrDefault ("MaxNumberOfKingdoms", out MaxNumberOfKingdoms, DefaultMaxNumberOfKingdoms);
      jsonNode.TryGetAsOrDefault ("MaxRangeFromSpawn", out MaxRangeFromSpawn, DefaultMaxRangeFromSpawn);
      jsonNode.TryGetAsOrDefault ("DelayBetweenPlacingAttempts", out DelayBetweenPlacingAttempts, DefaultDelayBetweenPlacingAttempts);
      jsonNode.TryGetAsOrDefault ("NumOfSpotsToCheckPerAttempt", out NumOfSpotsToCheckPerAttempt, DefaultNumOfSpotsToCheckPerAttempt);
      jsonNode.TryGetAsOrDefault ("DelayBetweenSpotChecks", out DelayBetweenSpotChecks, DefaultDelayBetweenSpotChecks);
      jsonNode.TryGetAsOrDefault ("MinDistanceToBanners", out MinDistanceToBanners, DefaultMinDistanceToBanners);
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
                var kingdomPosition = GetRandomSpot (MaxRangeFromSpawn);
                var farmSize = 1 + Pipliz.Random.Next (NpcFarmBuilder.MAX_SIZE);
                var npcKingdom = NpcFarm.Create (kingdomPosition, farmSize);
                var closestBanner = BannerTracker.GetClosest (kingdomPosition, MinDistanceToBanners);
                if (closestBanner == null) {
                  LoadChunksBlocking (npcKingdom.GetPrimaryChunkPositions ());
                  if (npcKingdom.IsAreaClear ()) {
                    LoadChunksBlocking (npcKingdom.GetTotalChunkPositions ());
                    npcKingdom.InitNew ();
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
