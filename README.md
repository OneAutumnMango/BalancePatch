This is my attempt to balance magequit/do what my friends thought of.
It now also contains a randomiser for spell attributes if you want that.

I have no experience with modding so apologies if I've done some horrible things (especially the randomiser 💀)

Thank you to the developers for letting people mod this game, check out their discord if you have a chance https://discord.gg/jS5Rsvtp

DO NOT USE THESE MODS FOR ONLINE PLAY WITH RANDOMS. IT CAN CAUSE SOME MAJOR ISSUES.


## How to Install

1. Install [BepinEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.4) into your MageQuit directory.
1. Run and then open MageQuit to generate BepInEx files.
1. Download the latest release here.
1. Extract the contents of the downloaded zip file into your MageQuit directory. (should be like `MageQuit/BepInEx/plugins/MageKit.dll`)
1. Download https://github.com/OneAutumnMango/MageQuitModFramework and put next to the dll.
1. Launch the game.
1. Press F5 to open the MageKit menu and enable the mods you want to use.

BepInEx mirror: https://www.nexusmods.com/magequit/mods/1 <br>
MageKit mirror: https://www.nexusmods.com/magequit/mods/2


## Juggernaut (New Gamemode!)

At the start of each battle, one player will be randomly selected as the Juggernaut. The Juggernaut gets:
- Greatly increased HP (3x)
- Increased damage, radius, knockback, and healing of all spells
- Faster spells
- Longer animations
- Increased size and cool glowing colour

This mode can be combined with Balance and other mods.


## How to Use the Randomiser

1. Launch the game (after install)
2. Enter in a seed (letters + numbers)
3. Click `Randomise`
4. Enter into a game.

#### IMPORTANT!! Make sure to reach this before playing:
- You must randomise BEFORE entering a game.
- Ensure EVERYONE you're playing with uses the same seed.
- Once randomised you have to quit MageQuit to randomise again.

## How to Use Boosted (New Gamemode!)

1. Launch the game (after install)
2. Enter into a game.
3. Click `Load Boosted`

To play a new game after having played one already, click `Unload Boosted` then once youre in the picking screen of the next game `Load Boosted`.

This mode is NOT compatible with the Randomiser.

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
- Somar Assault: increase radius from 4 → 5
- Bombshell: turtle speed increased by 25%
- Tsunami: speed increased by 15%
- Icepack: dogs 20% slower
- Towvine: cooldown reduced from 12s → 9s
- Bubble Breaker: prevents lava damage
- BullRush: cooldown increased from 12s → 13s
- Hinder: slow reduced from 50% → 35%
- Echo: cooldown reduced from 5.5s → 5s
- Wormhole: Gives 1.8x movespeed for 5s instead of previous, 9s cooldown

- All clones HP reduced by 50%
- Max rounds increased to 30

**Randomiser**:

Added a randomiser to randomise the following:

- Cooldowns (not of recasts)
- Animation WindUp and WindDown
- Spell Velocity
- Damage
- Hitbox Radius
- X and Y Knockback

**Boosted**:

This is a new gamemode where every round you get offered 10 choices to upgrade your spells.
You may select 3 of them.

You can choose to increase or decrease attributes. (Green increase, Red decrease)
You may want to choose decrease for attributes like cooldown or animation windup.

You may choose to ban an upgrade instead to never get it again.
You get one free ban a round.

This mode can be used with the Balance patch but some things may not work. e.g. Geyser winddown might not update according to this patch.


**Debugging**:

- Damage and Healing logged
- Hitboxes for player shown (spells used by others online don't show for you)
