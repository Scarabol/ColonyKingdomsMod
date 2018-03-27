
# Colony Kingdoms Mod by Scarabol

This mod adds npc kingdoms to your world! This means after a while some farms, ~~camps, castles~~ and more appear in random places.

The maximum number of kingdoms is configurable to not flood the world.


## Configuration ##

All configuration for the mod is done by the **kingdoms.json** file in the worlds savegame folder.
If the file doesn't exists, it is created with default values.
On closing the world the file is overwritten! You'll loose all changes made on an active/loaded world.


## Notifications ##

Each time a kingdom is added or removed a notification is sent out to all players with a certain permission.
The permission is configurable via the json file.
By default the super permission ("") is required, but can be set to any other permission.


## Chat Commands ##

<dl>
<dt>/farm [maxSize]</dt>
<dd>Requires permission: <b>mods.scarabol.kingdoms.farm</b><br>Creates a farm with given 'maxSize' amount of fields, right where you are. Fields are only placed on solid flat ground.</dd>
</dl>

More to come...


## Technical Details ##

Read this if you experience performance issues using the mod.

A background process crawls the world in random positions and tries to find suitable spots to place a kingdom.
The timings and limits for this process can be configured via the json file.


## Installation

**This mod must be installed on server side!**

* download a (compatible) [release](https://github.com/Scarabol/ColonyKingdomsMod/releases) or build from source code (see below)
* place the unzipped *Scarabol* folder inside your *ColonySurvival/gamedata/mods/* directory, like *ColonySurvival/gamedata/mods/Scarabol/*


## Build

* install Linux
* download source code
```Shell
git clone https://github.com/Scarabol/ColonyKingdomsMod
```
* use make
```Shell
cd ColonyKingdomsMod
make
```

**Pull requests welcome!**

