This is my attempt to balance magequit/do what my friends thought of.
It now also contains a randomiser for spell attributes if you want that.

I have no experience with modding so apologies if I've done some horrible things

Thank you to the developers for letting people mod this game, check out their discord if you have a chance https://discord.gg/jS5Rsvtp

DO NOT USE THESE MODS FOR ONLINE PLAY WITH RANDOMS. IT CAN CAUSE SOME MAJOR ISSUES.


## How to Install

1. Install BepinEx 5 into your MageQuit directory.
2. Run and then open MageQuit to generate BepinEx files.
3. (optional) Build this plugin with `dotnet build` (there will be a prebuilt dll in bin).

   If this breaks remove the `CopyToBepInEx` target from the `.csproj` file probably, message `oneautumnmango` on Discord, DO NOT ASK THE MAGEQUIT DISCORD.
4. If you didn't build, you need to copy `bin/Debug/net472/BalancePatch.dll` to `MageQuit/BepInEx/plugins`.
5. Launch the game.


## How to Use the Randomiser

1. Launch the game (after install)
2. Enter in a seed (letters + numbers)
3. Click `Randomise`
4. Enter into a game.


#### IMPORTANT!! Make sure to reach this before playing:
- You must randomise BEFORE entering a game.
- Ensure EVERYONE you're playing with uses the same seed.
- Once randomised you have to quit MageQuit to randomise again.


## Changes:
**MageQuit Gameplay Changes**:

- Snowball: increased damage by 16.66%
- Steal Trap: increased distance by 50%,
- Brrage: reduced brrage shots to 3, increased damage by 20%
- Chain Lightning: does 25% less damage
- Tetherball: duration reduced from 7s → 5s
- Sustain: now deals 5 damage
- Rocket: increased range by 50%
- Ignite: damage reduced by 17%
- Chameleon: cooldown reduced from 13s → 9s
- Chainmail: duration reduced from 4.7s → 3.5s
- Flame Leap: reduced hitbox offset, cooldown increased from 11s → 12s
- Flashflood: removed refresh effect
- Geyser: shorter animation
- Sommar Assault: increase radius from 4 → 5
- Bombshell: turtle speed increased by 25%
- Tsunami: speed increased by 15%
- Icepack: dogs 20% slower
- Towvine: cooldown reduced from 12s → 9s
- Bubble Breaker: prevents lava damage
- BullRush: cooldown increased from 12s → 13s
- Hinder: slow reduced from 50% → 40%
- Echo: cooldown reduced from 5.5s → 5s

**Randomiser**:

Added a randomiser to randomise the following:

- Cooldowns (not of recasts)
- Animation WindUp and WindDown
- Spell Velocity
- Projectile Size
- Damage
- Hitbox Radius
- X and Y Knockback

**Debugging**:

- Damage and Healing logged
- Hitboxes for player shown (spells used by others online don't show for you)
