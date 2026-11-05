# Port Boss M4: the phone, the first income, selling and save (how to test)

M4 is the first money loop: you sell what you catch, buy bait, Mari rents your mooring for $40 a day, and the game
saves. This page is for Manan and Tushar. Play about 30 minutes solo, then 10 minutes in co-op, tick the boxes, and
note anything that felt wrong (especially anything on the phone that got in your way).

**Where things are (until map B is in):** the three counters stand on temporary spots on Walt's Landing.
- **Hook, Line & Sinker** (June Pike, teal awning): bait, tackle and rods.
- **Pratt's Fish Market** (Gideon Pratt, grey awning): buys any fish at 60%.
- **Mari's taco boat** at the pier end: there only once you've accepted her deal.

When map B lands, they move to its `anchor_tackle`, `anchor_pratts`, `anchor_mari_mooring` and `anchor_mari_boat` on
their own; no code change is needed.

## 1. Get the build
Pull the M4 branch (or `main` once Kufu merges it) and run `bash tools/m1build.sh "$PWD/Builds/m1"`. The game is
`Builds\m1\PortBoss.exe`. Your saves live in `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Saves\`.

## 2. The keys
| | Keyboard | Pad |
|---|---|---|
| Phone: open or close | **Tab** (tap) | View / Back |
| Phone: open the newest unread message | **hold Tab** | hold View |
| Phone: back / close | Backspace / Esc | B |
| Phone: buttons | mouse, or arrows + Enter | d-pad + A |
| A shop counter | **E** at the counter | X |
| Buy / sell in a shop | click, or hold E on the button | hold A |
| The bait pouch | **hold R**, point, let go (tap R: the next bait) | hold Y, point with a stick (Y is only the pouch) |
| Shove a crewmate (co-op) | **G** | **D-pad right** |
| Save & quit | Esc, then **Q** (closing the window saves too) | Start, then Q |
| Test only, host only | **F10** = +1 hour, **Shift+F10** = +6 hours | |

## 3. Manan's checklist (solo)
**New game.** Start the game and pick **New game**. You start on Day 1 at 7:00 AM with $40 and June's 10 worms.

- [ ] **The phone is small, useful and never blocks.** Tap Tab:
  - it slides in on the right, and you can still walk;
  - the home screen shows the time, Dot's forecast, what's next, and your bank.
  - Try every app: Messages, Contracts, Crew (locked until Act 2), Map, Bank, Forecast, Journal, Settings.
  - While a fish is biting or on the line, Tab does nothing and the phone puts itself away; it does the same in the water.
  - Esc closes the phone and does **not** open the pause menu.
- [ ] **Sell at Pratt's (60%).** Catch a few fish, walk to Pratt's, press E.
  - Each fish shows its price.
  - Sell them one by one, or with "Sell all".
  - A green "Sold" note appears on the LEFT, and the bank pill (top right) counts up.
  - Sell 10 of one kind: the price slides a little with each one (2% each, never below 60% of it).
  - Pratt's is open 5 AM to 4 PM; outside those hours his counter says he's shut.
- [ ] **Buy bait at June's, and new fish.** At Hook, Line & Sinker, buy shrimp ($12 for 10):
  - pick ×1, ×2, ×5 or Max, then press Buy;
  - the price comes out of the bank.
  - **Hold R**: the pouch radial opens. Point at Shrimp and let go. The bait chip by the hotbar now says "Shrimp × 10".
  - **What "new fish" means on Walt's bamboo rod:** shrimp swaps the sprats for mullet and mackerel (sprats don't take
    shrimp).
  - **Garfish and bream** need the **Fibreglass rod** (June's Rods tab, $450), because the rod is a hard gate (fishing R11).
    On the Fibreglass rod with shrimp they are about two bites in three. To try it without earning $450, press **F5** (the
    debug rod key) once.
  - The squid and Walt's Mix pockets are locked until you buy June's **tackle pouch** ($250).
- [ ] **Mari's text → accept → $40 the next morning.** Mari texts once your company has earned $250 in total, or on
  Day 3 at 7 AM, whichever comes first. To get there fast, press Shift+F10 eight times, or sleep two nights in Walt's cot.
  - Her text pops up on the LEFT ("Tab · Open phone").
  - Hold Tab: her thread opens with the mooring offer. Press **Accept**.
    - "Can you pay more?" only gets a laugh (and a free lunch).
    - Decline and she comes back with $45, then $50; after the third no she gives up.
  - Within the in-game hour her taco boat ties up at the pier end.
  - Sleep (after 8 PM, hold E on Walt's cot).
    - The night card shows the day's sales, the company bank, and "Mari's rent at 7 AM +$40".
    - In the morning a **"Rent's in, landlord. +$40"** note appears on the LEFT, and the bank shows +$40.
- [ ] **Sell to Mari (100%).** From 9 AM to 10 PM press E at her boat.
  - She pays the full price for Everyday fish (sprat, mullet, mackerel), 10 fish a day.
  - At Pratt's, a grey line reminds you that Mari pays more.
- [ ] **Bank and Ledger.** Phone → Bank:
  - today's in and out;
  - what's due at 7 AM;
  - every line of the Ledger (who, what, when, and the balance after it).
  - The bank never goes below $0: June won't sell you what you can't pay for ("Need $X more").
- [ ] **Quit and Continue restores everything.**
  - Before you quit, note:
    - the bank;
    - your fish (and how fresh they are);
    - your bait;
    - Mari's deal;
    - the time;
    - where Biscuit is, and any water in her.
  - Esc → Q. Start the game again: the title shows **Continue** with your world, its day, its bank and when it was saved.
  - Press Continue. Everything above should be exactly as you left it, and the clock carries on.
  - Bonus: the night card ends with "Saved ✓". The game also saves every morning.

## 4. Co-op (10 minutes, two PCs, same as M2)
- [ ] **One company bank for the crew.**
  - The joiner sells at Pratt's: the money goes into the host's bank too, and the fish leave the joiner's hotbar.
  - The joiner buys shrimp: it comes out of the shared bank, into the joiner's own pouch.
- [ ] Mari's text reaches **both** phones. Either of you can accept. The rent toast shows on both.
- [ ] The host sleeps: the joiner's game keeps **a copy of the world** (the joiner's title shows it under "Your worlds",
  marked as a copy). If the host leaves, the host's game saves first.

## 5. What is a stand-in, and what is not in M4
- **Doyle's engine rebuild, Ingrid's planks, the hull patch, fuel and Amara's order** are M5 and later. The first-day
  bot below fakes them as plain bank lines marked "(stand-in until M5)".
- **The economy.** The bot plays the table's three days (§2.3) through the real shops, rent and nights. Under the
  table's own assumptions (a fish averages $16, no saturation: `-pb-sat off`) the bank lands within ±10% of every row.
  With the game's real rules it comes out lower, for two reasons:
  - **Saturation.** §2.1 says each same-species fish sold to one buyer in one day costs 2% (floor 60%), and M4 does
    that. But the §2.3 table prices every Pratt sale at a flat 60%. On Day 2 the table sells ~29 fish to Pratt, so his
    prices slide to $5–7 and the bank runs ~$40 under the table until Mari arrives on Day 3.
  - **Grades.** With Walt's bamboo rod and worms, a fish is worth $8.25 Basic. A practised player lands about 1 in 5
    Rare (×2), so that's about $10 a fish. Getting $16 would need ~94% Rare.
  So either the table or the prices need a decision (economy, not M4 code). `firstday.txt` (saturation on) and
  `firstday-game.txt` (the game's grade odds) in the proof folder have the numbers. The check is
  `firstday-table-mean.txt`: the mean of 4 runs with different catches under the table's assumptions (one run alone
  is ~15 random fish a sale, about ±$12 on a row).
- **The map.** The counters and Mari's boat are on temporary spots on Walt's Landing, as planned, until map B is in the
  scene.

## 6. The automated checks (what a bot ran before you got this)
`bash tools/m4test.sh Builds/m1/PortBoss.exe "C:/Users/asus/Desktop/Port Boss/proof/M4"`:
- EditMode suites: Mari's contract rules; money never below $0 and the Ledger sums (3 seeds × 20,000 random
  operations); the fish ledger; Pratt 60% / Mari 100% / saturation / freshness; the v1 → v2 save migration; a newer
  save refused; gzip + CRC.
- `-pb-firstday`: the practised player's first three days through the real shops, phone, pouch and nights. The bank is
  compared with the economy table at 15 rows (the check: `-pb-sat off`, the table's assumptions, the mean of 4 runs), and every phone
  screen is captured (`proof/M4/phone/`). The same run is filmed as `firstday.mp4`.
- `-pb-savetest`:
  - a lived-in world is saved, then reloaded in a second process: the world's hash and JSON must be identical;
  - three backups are kept;
  - a damaged file falls back to the newest backup;
  - an old v1 save loads.
- `-pb-banktest`: a host and a joiner over LAN. The joiner sells, buys and accepts Mari; the host's morning pays the rent
  and sends the joiner its copy.
- Then the M1–M3 regressions, `bash tools/m4reg.sh Builds/m1/PortBoss.exe` (fishing, menus, co-op, sleep, the day,
  tossing, the boat, swimming), with 0 exceptions.
