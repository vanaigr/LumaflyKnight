# LumaflyKnight

Hollow Knight mod for tracking how many lumaflies you've freed. The current room progress and overall progress are displayed next to the geo counter.

![image](https://github.com/user-attachments/assets/0066f7fc-a0e9-40d9-a3bf-371471221768)

What counts as containing lumaflies:

<details>
  
- Poles, lamps, etc.
- Husk miners (including Myla).
- Crystallised husks (if `countZombieBeamMiners` is set).
- Empty Junk Pit chest.
- Watcher Knights chandelier (if `countChandelier` is set).
- The lamp on a breakable wall before Watcher Knights chandelier (if `countChandelier` is set).
- Ascending the Seer (if `countSeerAssension` is set).

Crystallised husks, the Watcher Knights chandelier and wall contain but don't release lumaflies.
By default, the mod counts them and adds lumafly release animations. If you want to disable animations
set `spawnLumaflies` to false. If you don't want to count husks etc., set their flag to false.

</details>

By default, after releasing lumafiles from an object, it will remain in its "after" state even after a room transition.
This can be disabled by setting `"permanentLumaflyRelease": false` in the mod's global settings.

All config options mentioned above are also available in the in-game menu at Options > Mods > LumaflyKnight Options. 

Loosely based on [GrassyKnight](https://github.com/itsjohncs/GrassyKnight).