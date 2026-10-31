# Mixamo clips for Port Boss (pb-anim, 2026-10-30)

Every name below was checked against Mixamo's own search (mixamo.com/api/v1/products, 30 Oct 2026). Mixamo gives several
clips the same name, so **search the name, then pick the card whose subtitle matches the "Subtitle" column**.

## How to download (same for every clip)
1. Sign in at https://www.mixamo.com (any Adobe account, free).
2. Leave the default character (Y Bot) selected. Don't upload ours.
3. Search the name, click the card with the matching subtitle.
4. Right panel: tick **In Place** where the list says so (only shows on clips that move). Leave the other sliders alone.
5. **Download** with these settings:
   - Format: **FBX for Unity (.fbx)**
   - Skin: **Without Skin**
   - Frames per second: **30**
   - Keyframe Reduction: **none**
6. Save to **`C:\Users\asus\Desktop\Port Boss\mixamo-incoming\`**. Keep Mixamo's file name. If two files would get the
   same name, add the number from the list, e.g. `Idle (1).fbx` is fine. Windows does that for you.
7. One extra, once: download **Y Bot** itself (Characters tab → Y Bot → Download → FBX for Unity, **With Skin**, T-pose).
   I use it as the reference rig.

Need: the 25 marked ● (about 10 minutes). ○ are nice to have (same settings).

## Locomotion
| | Search | Subtitle on the card | In Place | For |
|---|---|---|---|---|
| ● | Breathing Idle | Breathing Idle | — | standing idle |
| ● | Standard Walk | Standard Walk | ✔ | walk (~1.2 m/s) |
| ● | Jogging | Jogging | ✔ | jog (~3.5 m/s) |
| ● | Standard Run | Standard Running | ✔ | run (~5 m/s) |
| ● | Sprint | Standard Sprint | ✔ | sprint (~6.5 m/s) |
| ○ | Fast Run | Running Fast | ✔ | alternative sprint if Sprint looks too stiff |
| ○ | Start Walking | Walking From Standing | ✔ | start step |
| ○ | Stop Walking | Walking To Standing Idle | ✔ | stop step |
| ○ | Run To Stop | Fast Stop From Full Run | ✔ | skid stop from sprint |

## Jump and fall
| | Search | Subtitle | In Place | For |
|---|---|---|---|---|
| ● | Jumping Up | Jumping Up From Action Idle | ✔ | take-off |
| ● | Falling Idle | Mid-Air Falling Idle | — | airborne / falling loop |
| ● | Falling To Landing | Landing From Falling Idle | ✔ | landing |
| ○ | Jump | Forward Running Jump | ✔ | jump while running |
| ○ | Hard Landing | Mid-Air Falling To A Hard Landing | ✔ | landing from a big drop (boat deck to quay) |

## Water
| | Search | Subtitle | In Place | For |
|---|---|---|---|---|
| ● | Falling Into Pool | Losing Balance And Falling Forward Into A Pool | ✔ | knocked / thrown into the sea |
| ● | Treading Water | Floating | — | treading water (arms in the water) |
| ● | Swimming | Swimming Underwater | ✔ | swim stroke (I tilt it to the surface in game) |
| ○ | Swimming To Edge | Swimming Breastroke To Edge Of Pool | ✔ | breaststroke alternative |

## Ladder
| | Search | Subtitle | In Place | For |
|---|---|---|---|---|
| ● | Climbing Ladder | Climbing Up A Ladder | ✔ | climb loop (played backwards for climbing down) |
| ● | Climbing To Top | Climbing Up A Ladder To Standing | — (leave off) | climb off the top onto the quay |
| ● | Start Climbing Ladder | Standing To Climb Ladder Quickly | — (leave off) | step onto the ladder |
| ○ | Climbing Down | Climbing Down To Standing | — | step off at the bottom, if it fits |

## Crates and the trolley
| | Search | Subtitle | In Place | For |
|---|---|---|---|---|
| ● | Lifting Object | Lifting A Heavy Object With Both Hands | — | pick a crate up off the ground |
| ● | Picking Up | Picking Up An Object | — | lighter pick-up (one crate onto a stack) |
| ● | Putting Down | Putting Down An Object | — | put a crate down |
| ● | Box Idle | Carrying A Box | — | carry idle (arms under the box) |
| ● | Jogging With Box | Jogging While Carrying A Box | ✔ | carry, moving fast |
| ○ | Box Turn | Turning Left Carrying A Box | — | carry turn in place |
| ● | Pushing | Pushing An Object Forward | ✔ | trolley push walk |
| ○ | Push Start | Start To Push A Heavy Object | ✔ | start pushing |
| ○ | Push Stop | Stop Pushing A Heavy Object | ✔ | stop pushing |

Carry walk: Mixamo has no straight "walk carrying a box" loop (only "Box Walk Arc", a curve). The game puts the
Box Idle arms on top of Standard Walk legs, so nothing more to download for it.

## Things the game already does with these
| | Search | Subtitle | In Place | For |
|---|---|---|---|---|
| ● | Throw | Throwing An Object From A Standard Pose | — | throwing a crate |
| ● | Driving | Male Driving A Car | — | forklift / boat driver seat |
| ● | Laughing | Laughing Standing | — | Marlowe gloating |
| ● | Pointing Gesture | Big Vegas Pointing Gesture | — | Marlowe pointing |
| ● | Getting Thrown | Being Thrown To The Side | — | captain throws a stowaway off the boat |
| ○ | Standing Idle 02 | Standing Idle Looking Around | — | market customers |

## After downloading
Tell Kufu "Mixamo files are in". I import them into `Assets/ThirdParty/Mixamo/` (gitignored, never committed), set them to
Humanoid, and swap them in for the stand-in clips. Nothing to rename.
