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

Crystallised husks and the Watcher Knights chandelier don't release lumaflies, so they aren't counted by default.
Set the corresponding flag to `true` in the mod's global settings if you want to count them.

</details>

By default, after releasing lumafiles from an object, it will remain in its "after" state even after a room transition.
This can be disabled by setting `"permanentLumaflyRelease": false` in the mod's global settings.


Loosely based on [GrassyKnight](https://github.com/itsjohncs/GrassyKnight).