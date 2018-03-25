using System;
using System.IO;
using System.Collections.Generic;
using Pipliz.JSON;

namespace ScarabolMods
{
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

    public static void Load ()
    {
      try {
        JSONNode json;
        if (JSON.Deserialize (JsonFilePath, out json, false)) {
          JSONNode jsonKingdoms;
          if (!json.TryGetAs ("kingdoms", out jsonKingdoms) || jsonKingdoms.NodeType != NodeType.Array) {
            Pipliz.Log.WriteError ("No 'kingdoms' array found in kingdoms.json");
            return;
          }
          foreach (JSONNode jsonNode in jsonKingdoms.LoopArray ()) {
            string type;
            if (jsonNode.TryGetAs ("type", out type)) {
              NpcKingdom kingdom;
              if ("farm".Equals (type)) {
                kingdom = new NpcKingdomFarm ();
              } else {
                Pipliz.Log.WriteError ($"Unknown npc kingdom type {type}");
                continue;
              }
              kingdom.InitFromJson (jsonNode);
              NextID = Math.Max (NextID, kingdom.NpcID);
            }
          }
        }
      } catch (Exception exception) {
        Pipliz.Log.WriteError (string.Format ("Exception while loading kingdoms; {0}", exception.Message));
      }
    }

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
      } catch (Exception exception) {
        Pipliz.Log.WriteError (string.Format ("Exception while saving kingdoms; {0}", exception.Message));
      }
    }

  }
}
