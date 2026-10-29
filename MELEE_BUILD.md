# Close-Quarters Shredding: First Playable Experiment

Target loop: keep the twin blades hitting along the edge of the crowd, use the hammer's stun to close in briefly, and pull back to the side and rear before the stun ends. Life steal makes up for occasional mistakes, and the dash is saved for pincer attacks. These components can already be bought in a normal game; this round adds a reproducible experiment entry point, combat stats, and feedback, with no dedicated weapon or forced set bonus.

## Playtest entry point

Unity menu **Tools → Survivors → Melee Build Lab**. Choose a loadout, an enemy scenario, and a seed, then click to start the experiment. In Play Mode you can restart with another loadout; the window can sit beside the Game view. Stopping Play Mode returns to the original scene configuration.

| Loadout | Initial purchases | Actual cost | Remaining materials |
|---|---|---:|---:|
| Twin blades + range | One range upgrade | 15 | 85 |
| Twin blades + range + life steal | The above + one stack of the life-steal pendant | 43 | 57 |
| Full build | The above + a level 1 hammer | 63 | 37 |
| Free loadout | Start with twin blades, buy at real prices during the prep phase | Your choice | 100 minus spending |

The first three loadouts add one component at a time; their results at different costs can't be read directly as an equal-budget advantage. The free loadout can buy **Range 15 + Whetstone 18 + Twin Blades Upgrade 20 + Speed 10 = 63**, to compare against the full build at the same actual cost. The prep phase doesn't advance combat; it uses wave 8 item eligibility, stops offering new weapons once all three weapon slots are full, and a restart lets you re-equip. Shield and First Aid are ready when the free-loadout fight starts. Later regular shops and waves still follow the normal game rules.

Regular chasers and stone-armored enemies use the wave 8 configuration; the mixed scenario uses the existing wave 8 enemy mix, including chargers, stone-armored enemies, shooters, and bombers. The boss scenario goes to the final wave. Each run deep-copies the default configuration and overrides the test mode, start wave, and starting materials, without writing back to the default asset. A fixed seed only guarantees the initial conditions; random calls for attacks, drops, and so on diverge depending on movement.

## Feedback and stats

- A green ground pulse appears when health is actually restored; it doesn't show at full health, on death, or when life steal hasn't triggered.
- Stun stars shrink during the last 0.3 seconds, signaling the end of the engagement window. The existing stun duration and no-contact-damage rule are unchanged.
- The window shows effective damage, hits, and kills for the twin blades, the hammer, and other sources, plus times hurt, actual health lost, life steal, First Aid, and twin-blade damage during stuns.
- Effective damage excludes overkill; actual healing excludes overflow; clearing the field at settlement doesn't count as weapon kills. Other sources include other weapons, burning, and secondary item damage, without a further breakdown.
- Stats reset at the start of each wave and on restart, and are kept for the current wave after settlement or death. The CSV is a manual export of the current snapshot, not a cumulative total for the run; the cost field records the experiment's initial loadout and doesn't track later shop purchases.
- Click **Append record** to write to `Logs/MeleeBuildLab.csv`. For recommended movement notes, use "edge", "wide circle", or "stand still", together with the actual purchases, to keep comparisons fair.

## First-round verification (2026-09-12, local date)

`Tools → Survivors → Verify Melee Build` / batchmode `-executeMethod MeleeBuildChecks.Run`.
It covers actual prices and configuration isolation, twin-blade edge hits/contact/occlusion, twin-blade hits after hammer control, stun feedback, damage attribution, overkill, actual life steal and shared cooldowns, and death/settlement/wave-change resets, and runs the existing regressions for all 16 items. The success markers are `MELEE_BUILD_CHECKS_PASSED` and `ITEM_SYSTEM_PASSED`. Screenshot: `Logs/MeleeBuild-Feedback.png`.

`Tools → Survivors → Probe Melee Build Movement` / `-executeMethod MeleeBuildProbe.Run`.
36 short probes: 4 loadouts × 3 enemy configurations × 3 movement strategies. Seed 31415, 20-second limit, 0.05-second step. All start at 90 HP; the preset 10 points of health loss don't count toward damage-taken stats. The fourth loadout uses the 63-material comparison above. Outputs `Logs/MeleeBuildProbe.csv`; success marker `MELEE_BUILD_PROBE_PASSED`.

| Wave 8 mixed scenario | Survival time | Remaining HP | Twin-blade effective damage | Hammer effective damage | Life steal | Twin-blade damage during stun |
|---|---:|---:|---:|---:|---:|---:|
| Full build, edge | 20.05 s | 77 | 372 | 278 | 5 | 114 |
| Full build, wide circle | 20.05 s | 46 | 171 | 282 | 1 | 11 |
| Full build, stand still | 13.00 s, died | 0 | 220 | 259 | 4 | 40 |
| Same-cost crit/upgrade/speed comparison, edge | 20.05 s | 25 | 677 | 0 | 0 | 0 |

These results support "hammer control buys a twin-blade damage window", so weapon damage, range, life-steal chance, and item prices are unchanged for now. In the stone-armor edge scenario, 90 points of the full build's twin-blade damage happened during stuns. In the regular-chaser scenario, standing still can still clear enemies; movement pressure depends on mixed enemies.

Limitations: the automated strategies don't use the dash or plan paths around obstacles. Random calls diverge between strategies, so short single-seed results can't prove statistical significance or how it feels to a human player. The boss entry point is playable, but this round's short probes don't include a full boss kill. The probability of the build coming together from random shops in waves 4–6, the overall clear rate, and late-game strength are not proven yet; the next round of human playtesting should focus on recording these. Don't read the probe's success marker as balance sign-off.
