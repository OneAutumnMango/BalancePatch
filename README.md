This is my attempt to balance magequit/do what my friends thought of.

I have no experience with modding so apologies if I've done some horrible things

Thank you to the developers for letting people mod this game, check out their discord if you have a chance https://discord.gg/jS5Rsvtp

DO NOT USE THESE MODS FOR ONLINE PLAY WITH RANDOMS. IT CAN CAUSE SOME MAJOR ISSUES.


## How to Install

1. Install BepinEx 5 into your MageQuit directory.
2. Run and then open MageQuit to generate BepinEx files.
3. Build this plugin with `dotnet build` (maybe there will be a prebuild dll in bin).

   If this breaks remove the `CopyToBepInEx` target from the `.csproj` file probably, message `oneautumnmango` on Discord, DO NOT ASK THE MAGEQUIT DISCORD.
4. If you didn't build, you need to copy `bin/Debug/net472/BalancePatch.dll` and `0Harmony.dll` (i think). Perhaps just the former.
5. Launch the game.


## Changes:
MageQuit Gameplay Changes:

- Snowball: increased damage by 16.66%
- Steal Trap: increased distance by 50%,
- Brrage: reduced brrage shots to 3, increased damage by 20%
- Chain Lightning: does 25% less damage
- Tetherball: duration reduced from 7s → 5s
- Sustain: now deals 5 damage
- Rocket: increased range
- Fire Melee: damage reduced by 17%
- Chameleon: cooldown reduced from 13s → 9s (only works locally rn)
- Chainmail: duration reduced from 4.7s → 3.5s
- Flame Leap: reduced hitbox offset
- Flashflood: removed refresh effect
- Geyser: shorter animation
- Sommar Assault: increase radius from 4 to 5
