# Port Boss M2: co-op on the boat (how to test)

## What changed in M3.1 (3 Nov night, from your two-PC test of 20261103-d9565e0)
- **Inviting works without Steam's overlay.** Press **I** any time you're in a crew; you don't need Esc first (Esc,
  then I also works). An invite list opens over the game:
  - **Invite <friend>** for each Steam friend online. This sends the invite straight to their Steam chat, so it works
    even when Steam's overlay doesn't. On Manan's PC the overlay never started inside the game, so the old I did nothing.
  - **Copy the crew code** (like `2Q7-0K3M`). The code is also shown in the pause menu (Esc).
  - **Steam's invite window**, if the overlay is running.
  - A note on the left says why, when the overlay can't open.
  - The friend joins from the invite in their Steam chat, or picks **Join a friend on Steam**, then **Type a crew
    code**.
- **No more stray float hanging in the air** when a crewmate quits or rejoins with their rod out.
- **Patching the leak:**
  - the hole spurts water, with a red ring round it;
  - a pin with an arrow shows where it is, even off screen, and fills up as you patch;
  - the patcher kneels and hammers a plank on, with a knock on every blow. Your friend sees all of it.
- **Catching a fish:** you hold it up with the rod still in your right hand for a second. Then you reach back, put the
  rod on your back, and hold the fish in both hands.
- **You can't walk through each other any more:** on the pier, the jetty and the boat. Walking into a friend stops you,
  or slides you round them if you're a little off line. Someone standing still is never pushed, so nobody goes off
  the boat from being walked into.
- **Shove a friend: G** (pad: **D-pad right**) (within 1.5 m, in front of you). They fall over for a second. At the rail, they can go over
  the side. You can shove once every 3 s.
  - A friend whose line is out waiting gets it reeled in; the bait stays on.
  - A friend fighting a fish loses it, and the bait too.
- **Rejoining:** the view stays covered ("Joining the crew…") until you're yourself and back where you were. Before,
  the camera showed the host first, then switched.

## What changed since M2 (this is build M2.1, 3 Nov)
From your co-op test this morning and two audits of the M2 build:
- **Holding the rod, everyone keeps their chin up** and looks at the horizon, then at the bobber, then at the fish they
  hold up (all six crew). Before, they stared at the deck.
- **On the cast wind-up the rod and mitts stay clear of the hat**, and the cast power arc now sits round the distance
  label instead of floating in the middle of the screen.
- **The rod on your back is broken down into its two halves**, so no more antennae over everyone's heads.
- **A friend who joins starts on their own spot on the pier**, not inside you.
- **The fishing bar sits just above the hotbar**, so you can see your angler during the fight.
- **Quieter start:** the welcome tips come one at a time, the "1 Rod" hint shows only for the first 20 s, an empty
  slot isn't lit, and the title card has no hotbar under it.
- **A bluer sky** near the horizon, and **no dark oval in the water** by the pier end.
- **Co-op fixes:**
  - fish you caught alone before joining a crew are kept (they used to vanish);
  - if you quit and come straight back, you're yourself again (same fish, colour and crew member), not a stranger;
  - the host can drop the anchor while a friend drives;
  - if joining fails, a note on the left says why;
  - a fifth player is told "The crew is full".
- **Steam invite: start `PortBoss.exe` first, then accept** (see section 2). With the game closed, Steam starts
  "Spacewar" instead.

## Also in this build: the day (M3, 3 Nov)
Built from `main` after 3 Nov, the game has a clock, so a 30-minute session sees a whole evening:
- **1 real second = 1 game minute.** You start at 7:00 AM. Golden light around 5:30 PM (~10 minutes in), dusk ~12
  minutes in, **night at 9 PM (~14 minutes in)**: blue, with lamps on the pier, the shack and Biscuit's stern.
- **Sleep:** after 8 PM, **hold E on Walt's cot** (beside the shack). Everyone gets a 5 s warning, then the night skips
  to 7:00 AM for the whole crew (it waits up to 10 s for someone fighting a fish). A card shows the day that ended and
  tomorrow's weather.
- **Stay up and you get tired:** a note at 11 PM, slower walking at 1:30 AM, and **at 2 AM (~19 minutes in) you pass
  out** where you are (on the boat, Biscuit anchors herself). No penalty.
- **Weather** changes through the day (clear, fair, breezy, fog, rain); no storm on the first two days. Fish slowly go
  off over 48 game hours: the hotbar shows a freshness bar.
- **In a storm** (later days): Biscuit can spring a leak (hold E at the hole to patch it, hold E elsewhere on deck to
  bail), and rogue waves come with a warning: **hold C to brace**, or you get knocked over.
- To try it quickly (the host only): **F10** jumps an hour (Shift+F10: six hours), **F11** holds the next weather.
  A storm held with F11 brings its own trouble, timed from the moment you held it (off the pier, on the boat): the first
  rogue wave within ~1¼ minutes (then two more, ~80 s apart) and a leak within ~2½ minutes.
- **Fish freshness:** each stage lasts 12 game hours (Fresh, Good, Stale, Old, then Rotten at 48 h). A fish caught
  **between 5 and 7 PM** is "Good" after a night's sleep (woken at 7 AM); one caught after 7 PM is still "Fresh".

M2 adds co-op: up to 4 of you on Biscuit, fishing together, handing fish around, talking by voice, over Steam or your
home network. This page is for Tushar and Manan. Play together on two PCs for about 30 minutes, tick the boxes, and note
anything that felt wrong.

## 1. Before you start (both PCs)
1. **Get the same build on both PCs.** Pull `main` and build it: `bash tools/m1build.sh "$PWD/Builds/m1"`. The game is
   `Builds\m1\PortBoss.exe`. Or Tushar copies his `Builds\m1` folder to Manan privately (never a public link).
2. **Start Steam and log in to your own account** on each PC. You need two different accounts, and you need to be
   Steam friends with each other.
3. **Use headphones** (or turn the speakers down), so the voice chat doesn't echo.
4. The first time you host, Windows may ask whether to let the game through the firewall. Click **Allow**.

## 2. Getting into one crew over Steam (the main test)
1. **Tushar:** run `PortBoss.exe`. The title menu shows. Pick **2 Host a crew on Steam**.
2. **Tushar:** in the game press **I** (any time; Esc then I works too). The invite list opens: pick **Invite Manan**.
   No list, or Manan isn't in it? Pick **Copy the crew code** and send it to Manan (the code is also in the pause menu).
3. **Manan:** start `PortBoss.exe` first and wait on the title menu, **then** accept the invite in Steam. Don't accept it
   with the game closed: on Steam's test app (480) Steam would start "Spacewar", not Port Boss. (That works only once Port
   Boss has its own Steam app ID.)
   - Other ways in: run `PortBoss.exe` and pick **3 Join a friend on Steam**, then choose "Tushar's crew", or **Type a
     crew code** and type Tushar's code; or right-click Tushar in your Steam friends list and pick **Join game** (also
     with the game already running).
4. You should both appear on Walt's pier. Each of you sees the other's name over their head and both names in the
   **crew list** (top right).

**If Steam won't connect:** write down what happened, then use the network instead (next section). The rest of the
checklist works the same way.

## 3. Getting into one crew on your home network (the fallback)
1. Both PCs on the same Wi-Fi or router.
2. **Tushar:** pick **4 Host on this network**. The line under it shows his address, like `192.168.1.4`.
3. **Manan:** pick **5 Join on this network**, type that address, then press Enter.
- On **one PC** (quick check alone): run the game twice. One picks 4, the other picks 5 and types `127.0.0.1`. Two
  copies on one Steam account can't join each other over Steam; the menu tells you so.

## 4. What's new to press
| Do | Keys |
|---|---|
| Give the fish in your hand to a crewmate (look at them, within 3 m) | hold **E** |
| Give the whole stack | **Shift** + hold **E** |
| Toss a fish (a ring shows where it lands; hold longer to throw further) | hold **Q**, let go |
| Drop one fish at your feet (over the side, it swims away) | tap **Q** |
| Invite friends (any time in a crew; Esc first isn't needed) | **I** |
| Shove a crewmate (in front of you, within 1.5 m; once every 3 s) | **G** (pad: **D-pad right**) |
| Patch the leak (in a storm, at the hole) | hold **E** |
| Leave the crew and play on alone | **Esc**, then **L** |
| Switch to the next free crew member | **F9** |
| Fall over (to test the ragdoll) | **F8** |

Voice is an **open mic**: just talk. The chip at the bottom left shows your mic. It rings while the others can hear
you. If it says "Mic (no Steam)", Steam isn't running on that PC.

## 5. Manan's checklist (M2 ★)
Play about 30 minutes in one session, over Steam if you can.

**Joining**
- [ ] Tushar hosts on Steam and presses **I**: the invite list opens. **Invite Manan** from it; Manan gets in from the
  invite. (Also try the crew code: Join a friend on Steam, then Type a crew code.)
- [ ] You're two different crew members, each with your own colour on the armband, the back patch, the name tag and the crew list.

**On the boat**
- [ ] Both board Biscuit. One drives (E at the tiller), the other stands on the deck: no sliding, no jitter, for either of you.
- [ ] The tiller swaps: the driver stands up (E), the other takes it and drives. It works smoothly both ways.
- [ ] While one drives, the other fishes from the deck.

**Fishing together**
- [ ] Both fish at once: you see each other cast, the float land, the bite, the fight and the fish held up.
- [ ] When the other is fighting a fish, a small bar shows over their name (the ghost bar).
- [ ] Cast over each other's line: both get **"Tangled!"** with a *boing*, and both lines reel in (bait kept).

**Fish between you**
- [ ] Hand a fish over (hold E on your friend). Both of you get a message, and it shows in their hotbar.
- [ ] Toss a fish while your friend faces you: they **catch** it ("Caught!").
- [ ] Toss one when nobody's there: it lands on the deck (anyone can pick it up with E) or swims away ("It's free!").

**Voice**
- [ ] Talk while standing close: you hear each other clearly, and a ring pulses on the talker's name.
- [ ] One walks to the far end of the pier (or swims off): the voice gets quieter and is gone past ~35 m.

**Falling about**
- [ ] Jump overboard while the other drives away. The swimmer floats on the waves on both screens, then climbs back up the ladder (E).
- [ ] F8 (fall over): the other sees you go limp and get up again.
- [ ] Walk into each other on the pier and on the boat: you stop or slide round, no shaking, and the one standing
  still doesn't move (nobody gets pushed in).
- [ ] **G** (pad: **D-pad right**) next to your friend: they go over and get up. At the rail they can go in the water. Shove them while
  their line is out (it reels in) and while they fight a fish (it gets away).

**Catching and patching (M3.1)**
- [ ] Catch a fish: the rod stays in your hand for a second, then goes on your back with a reach over the shoulder.
  Your friend sees the same.
- [ ] A leak in a storm (the host presses F11 until "Weather held: Storm", then waits on the boat, off the pier, up to ~2½ minutes; a rogue wave comes first, hold C): the spurting hole, the red ring and the pin are easy to find. Hold E at the
  hole: you kneel and hammer, the pin fills up, and a plank is nailed on. Your friend sees it all.

**Leaving and coming back**
- [ ] Manan quits the game (Esc, Q) and starts it again, then rejoins. **His fish are still there**, and he's back where he was, as the same character.
  The view stays covered until then: the camera never shows Tushar first.
- [ ] Esc, L: you leave the crew and keep playing alone with your boat and fish.

**The day (M3)**
- [ ] Night falls on both screens at the same time, and the lamps come on.
- [ ] One of you sleeps in the cot after 8 PM while the other is out fishing: both wake at 7:00 AM; where does the
  fisher wake?
- [ ] Stay up past 2 AM once: you pass out, and wake in the morning where you fell.

**Lag**
- [ ] Over the internet (Steam), does anything feel laggy? The fishing bar should feel exact, because it runs on your own PC.

## 6. What to tell Claude afterwards
- What felt wrong, ugly or slow, and roughly when. Every timing and look is tunable.
- **If something broke, send the log.** On each PC it's at
  `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Player.log`. It's overwritten every launch, so copy it right
  after the problem.
- Whether Steam worked: the invite, the join, the voice.

## 7. Not in M2 yet (by design)
- The phone (with the forecast), money and saving: M4. The clock is only on the F3 debug overlay (no HUD clock, by
  design).
- A settings screen: voice is open mic for now; push-to-talk (hold V) and mic off come with settings later.
- Saving the game between sessions: a rejoin works while the host keeps playing.
- Shops, money, the phone, the "Double!" alert, the landing net, Bag mode, dropping a whole stack (Shift+Q).
