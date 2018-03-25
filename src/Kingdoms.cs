using System.IO;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsModEntries
  {
    public static string MOD_PREFIX = "mods.scarabol.kingdoms.";
    public static string ModDirectory;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.kingdoms.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      ModDirectory = Path.GetDirectoryName (path);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.kingdoms.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded Kingdoms Mod 6.0.2 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterNetworkSetup, "scarabol.kingdoms.afternetworksetup")]
    public static void AfterNetworkSetup ()
    {
      KingdomsTracker.Load ();
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAutoSaveWorld, "scarabol.kingdoms.onautosaveworld")]
    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnQuitEarly, "scarabol.kingdoms.onquitearly")]
    public static void OnAutoSaveWorld ()
    {
      KingdomsTracker.Save ();
    }
  }
}
