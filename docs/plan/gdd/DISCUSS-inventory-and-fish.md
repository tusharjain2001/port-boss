# To discuss: inventory and the 15 fish (Manan 31 Oct)

**APPROVED by Manan, 31 Oct (via Kufu): all five picks below are MUST; the 15 fish are final for the build.** Applied across `fishing/`, `economy/`, `systems/`, `world/` (see `CHANGES.md` §E).

One page, 3 minutes. Your words: "Discuss inventory and types of fish", "warehouse from the start, expandable", "15 fish, 3 per rod", "legendary needs the best rod AND best bait", "any fish anywhere". Nothing here is final until you say so.

## A. Inventory

**On you: the 8-slot hotbar (MUST, unchanged).** Rod, fish, guns, tools, collectibles. Fish stack 5 (one species + one grade). The phone is never in it (Tab).

**Bait pouch (on the rod, not a slot).** Holds 3 bait types to start, 99 of each; one upgrade ($250) makes it 5, so every bait fits. Switch bait with the radial (hold R).

**The warehouse (NEW, from Day 1, at our dock).** Walt's run-down warehouse next to the office is our one storage place: fish, spare rods, tools, collectibles, extra bait. It replaces the separate chests and ice chest. Anyone in the crew can open it. Walk in, press E, drag between the warehouse grid and the hotbar ("Store all fish" button).

| Tier | Name | Price | When | Slots | Cold room (fish rot at 25%) | Looks like |
|---|---|---|---|---|---|---|
| 0 | Walt's Old Shed | free | Day 1 | 12 | – | Sagging roof, one working light, gulls inside |
| 1 | Patched Warehouse | $300 | Day 1–2 | 24 | – | New planks, racks on the walls |
| 2 | Cold Room | $2,500 | Act 2 | 24 | +12 | An insulated back room, frosty door |
| 3 | Big Warehouse | $8,000 | Act 3 | 48 | +24 | Extended shed, forklift lane. **Workers' catch lands here** |
| 4 | Ice House | $30,000 | Act 4 | 96 | +48 (rot at 10%) | A proper port store with our sign |

- **Rot:** fish on the open racks rot at normal speed (they still go bad in 2 days, MUST). Only the cold room slows it. That makes the cold room the first "big" storage buy and keeps selling fresh fish worth it.
- **Upgrades install themselves** (your rule): pay, and next morning the new warehouse is built; no placing, no editor.
- **Cut** (to keep it simple): the insulated satchel, wooden chests, the separate ice chest and the boat live well. One place to store, one thing to upgrade.

## B. The 15 fish: 5 rods × 3 fish

**How it works.** Every fish can bite anywhere (no zones). Your **rod** decides which fish *can* bite: each rod adds 3 new fish and still catches the earlier ones. Your **bait** decides which of those *usually* bite (favourite bait ×4, liked bait ×2, other ×0.5). Time and weather only nudge the odds (×1.5 in its favourite time or weather), never lock a fish out. **The legend needs the best rod (Old Faithful) AND the best bait (Walt's Mix)**, both at once, or it never bites.

**Baits (5, one new per rod tier):** Worms $5/10 · Shrimp $12/10 · Sardine $18/10 · Squid $25/10 · **Walt's Mix** (best; the big Trophy fish love it, small fish ignore it, and it is the only bait for the legend).

| Rod (price) | Fish | Class | Baits (★ favourite) | Base $ | Flavour |
|---|---|---|---|---|---|
| **1 Walt's Bamboo** (free) | Harbour Sprat | Everyday | ★Worms (also bites bare hook) | 6 | Tutorial fish; tiny, wriggly, everywhere |
| | Grey Mullet | Everyday | ★Worms, Shrimp | 8 | Cruises our dock pilings; you can see it from the pier |
| | Mackerel | Everyday | ★Worms, Shrimp, Sardine | 10 | Comes in shoals under diving gulls; best at dawn |
| **2 Fibreglass** ($450) | Garfish | Uncommon | ★Shrimp, Sardine | 20 | Green bones; leaps out of the water on the last hit |
| | Black Bream | Uncommon | ★Shrimp, Squid | 24 | Steady daytime biter, a reliable earner |
| | Conger Eel | Uncommon | ★Sardine, Squid | 30 | Lives under our own old pier; night fish; big ones ask "Fight it?" |
| **3 Carbon** ($1,600) | Red Snapper | Scarce | ★Squid, Sardine | 55 | The Pelican's favourite order; bites at dusk |
| | Mahi-mahi | Scarce | ★Sardine, Shrimp | 65 | Gold that fades to grey as it goes stale (rot you can see) |
| | Dusky Grouper | Scarce | ★Squid, Sardine | 70 | Big-lipped cousin of Walt's legend; likes grey days |
| **4 Deep-sea** ($6,000) | Ocean Sunfish | Trophy | ★Squid, Shrimp, Walt's Mix | 140 | Absurd reveal: held two-handed, wobbling; calm clear days |
| | Blue Shark | Trophy | ★Sardine, Squid, Walt's Mix | 160 | Rain fish; circles the boat before it bites |
| | Swordfish | Trophy | ★Squid, Sardine, Walt's Mix | 180 | Leaps twice during the fight; night fish |
| **5 Old Faithful** (Walt's rod restored, $15,000) | Deep Anglerfish | Trophy | ★Squid, Walt's Mix | 200 | Its lure light stays lit in the hotbar; night |
| | Giant Oarfish | Trophy | ★Walt's Mix, Squid | 220 | Storm fish; the reveal pans along its 5 m body |
| | **Old Brass** (Legend) | Legend | **Walt's Mix only** | 1,500 | Walt's lifelong golden grouper, the one in the office photo |

All 15 are from the old 39-fish list, with the same prices; the new list is `fishing/fish-roster.csv` and `fishing/fishing_model.py` checks it (practised player, $/real hour with the best bait: about $1.2k on the bamboo rod, $3k fibreglass, $5.9k carbon, $11k deep-sea, $27k Old Faithful + Walt's Mix). The other 24 fish and 3 legends are cut (kept in a "later" list, not in the game).

## C. Questions for you (my pick first)

1. **Warehouse slot size.** **A slot holds 10 fish** (storage should feel roomier than your pockets), or 5 like the hotbar?
2. **Walt's Mix (best bait).** **Sold at June's bait shop once you own Old Faithful, $40 each**, or made only through Walt's letters (story, a few at a time)?
3. **Old Brass, once or again.** **First catch is the big story moment (mount it in the office or sell it), then it can bite again, rarely, for money**, or once per save only?
4. **Time and weather.** **Keep a light nudge (×1.5 at a fish's favourite time or weather, never a lock)**, or no effect at all?
5. **Fish-to-rod order.** Is the ladder above right (cheap shore fish → reef → big game → deep-sea → legend), or do you want a different fish on a rod?

Two more things to confirm sit in `CHANGES.md` (★ readings): "automatic customisation" (now confirmed) and the map (settled: map B, Crescent Bay).
