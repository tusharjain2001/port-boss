# Port Boss: the music slots (M7)

The game has a music player but no music. Each track below is an empty slot: put a file in and the game plays it. A missing file is silent and harmless.

| Slot | When it plays | File |
|---|---|---|
| morning | 5 AM to 10 AM | `Assets/Game/Resources/Music/morning.ogg` |
| day | 10 AM to 6 PM | `Assets/Game/Resources/Music/day.ogg` |
| evening | 6 PM to 9 PM | `Assets/Game/Resources/Music/evening.ogg` |
| night | 9 PM to 5 AM | `Assets/Game/Resources/Music/night.ogg` |
| storm | During a storm (it takes over from the time of day) | `Assets/Game/Resources/Music/storm.ogg` |
| mari | Mari's radio: from her taco boat, only near her, while she's moored and open (9 AM to 10 PM) | `Assets/Game/Resources/Music/mari.ogg` |

- Tracks loop and crossfade over 3 seconds when the mood changes.
- Any audio format Unity reads will do; `.ogg` is best. Name the file exactly as in the table.
- Players set the level in the phone: Settings, Sound, Music. Default 70%.
- Rule 4 in HANDOVER: only Manan downloads Suno tracks, and any AI music must be disclosed on the Steam page.
- `Assets/StreamingAssets/Music/night.mp3` is from the old prototype, and it's unclear where it came from. The player doesn't use it.
- The code is `Assets/Game/Scripts/Music.cs`. The test is `-pb-musictest`, run by `tools/m7test.sh`.
