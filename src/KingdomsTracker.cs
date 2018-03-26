using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsTracker
  {
    static uint NextID = 742000000;
    static List<NpcKingdom> kingdoms = new List<NpcKingdom> ();

    public static void RegisterKingdom (NpcKingdom npcKingdom)
    {
      kingdoms.Add (npcKingdom);
      npcKingdom.StartThread ();
    }

    public static uint GetNextID ()
    {
      NextID++;
      return NextID;
    }

    static string JsonFilePath {
      get {
        return Path.Combine (Path.Combine ("gamedata", "savegames"), Path.Combine (ServerManager.WorldName, "kingdoms.json"));
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterNetworkSetup, "scarabol.kingdoms.afternetworksetup")]
    public static void Load ()
    {
      try {
        JSONNode json;
        if (JSON.Deserialize (JsonFilePath, out json, false)) {
          kingdoms.Clear ();
          JSONNode jsonKingdoms;
          if (!json.TryGetAs ("kingdoms", out jsonKingdoms) || jsonKingdoms.NodeType != NodeType.Array) {
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
        JSONNode jsonKingdoms = new JSONNode (NodeType.Array);
        foreach (NpcKingdom kingdom in kingdoms) {
          jsonKingdoms.AddToArray (kingdom.GetJson ());
        }
        JSONNode jsonFileNode = new JSONNode ();
        jsonFileNode.SetAs ("kingdoms", jsonKingdoms);
        JSON.Serialize (JsonFilePath, jsonFileNode, 2);
        Log.Write ($"Saved {Count} kingdoms to json");
      } catch (Exception exception) {
        Log.WriteError (string.Format ("Exception while saving kingdoms; {0}", exception.Message));
      }
    }

  }
}
