# Kenney Audio Candidate Library

197 audio files in total, from the Kenney audio packs.

This is a candidate library pre-sorted by what each file sounds like; the original audio content, volume, and sample rate are unchanged. A selection has been bound to game events; see AUDIO.md in the project root for the detailed mapping and tuning. The other variants are kept for listening and swapping.

## Listening

In the Unity Project window, open Assets/Audio/Kenney, select an audio file, and click play in the preview player at the bottom of the Inspector.
Suggested order: UI → Pickups / Shop → Combat → Weapons → Jingles → Music.
Each series is numbered from 01; pick one you like or keep several variants. The fire, frost, and music choices still need a style check.

## Naming

- Sound effects: sfx_purpose_soundtype_NN.ogg
- Jingles: jingle_type_NN.ogg
- Music: music_loop_original_title_in_lowercase_with_underscores.ogg
- source_manifest.json stores the new and old paths, suggested use, and SHA256; Licenses keeps each pack's original license.

## Groups

| Folder | Count |
| --- | ---: |
| Combat | 10 |
| Jingles | 34 |
| Music | 19 |
| Pickups | 4 |
| Player | 5 |
| Shop | 15 |
| UI | 38 |
| Weapons | 72 |

## Use and source file mapping

| New file | Suggested use | Original file |
| --- | --- | --- |
| Weapons/Melee/sfx_melee_knife_slice_01.ogg | Blade swing | RPG Audio/Audio/knifeSlice.ogg |
| Weapons/Melee/sfx_melee_knife_slice_02.ogg | Blade swing | RPG Audio/Audio/knifeSlice2.ogg |
| Weapons/Melee/sfx_melee_knife_draw_01.ogg | Blade draw, equip | RPG Audio/Audio/drawKnife1.ogg |
| Weapons/Melee/sfx_melee_knife_draw_02.ogg | Blade draw, equip | RPG Audio/Audio/drawKnife2.ogg |
| Weapons/Melee/sfx_melee_knife_draw_03.ogg | Blade draw, equip | RPG Audio/Audio/drawKnife3.ogg |
| Weapons/Melee/sfx_melee_chop_01.ogg | Chop hit | RPG Audio/Audio/chop.ogg |
| Weapons/Melee/sfx_melee_whoosh_01.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh1.ogg |
| Weapons/Melee/sfx_melee_whoosh_02.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh2.ogg |
| Weapons/Melee/sfx_melee_whoosh_03.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh3.ogg |
| Weapons/Melee/sfx_melee_whoosh_04.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh4.ogg |
| Weapons/Melee/sfx_melee_whoosh_05.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh5.ogg |
| Weapons/Melee/sfx_melee_whoosh_06.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh6.ogg |
| Weapons/Melee/sfx_melee_whoosh_07.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh7.ogg |
| Weapons/Melee/sfx_melee_whoosh_08.ogg | Swing, spear and boomerang throw | Foley Sounds/Audio/Woosh/woosh8.ogg |
| Weapons/Melee/sfx_melee_sword_01.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword1.ogg |
| Weapons/Melee/sfx_melee_sword_02.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword2.ogg |
| Weapons/Melee/sfx_melee_sword_03.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword3.ogg |
| Weapons/Melee/sfx_melee_sword_04.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword4.ogg |
| Weapons/Melee/sfx_melee_sword_05.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword5.ogg |
| Weapons/Melee/sfx_melee_sword_06.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword6.ogg |
| Weapons/Melee/sfx_melee_sword_07.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword7.ogg |
| Weapons/Melee/sfx_melee_sword_08.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword8.ogg |
| Weapons/Melee/sfx_melee_sword_09.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword9.ogg |
| Weapons/Melee/sfx_melee_sword_10.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword10.ogg |
| Weapons/Melee/sfx_melee_sword_11.ogg | Melee sword candidate | Foley Sounds/Audio/Swords/sword11.ogg |
| Weapons/Melee/sfx_melee_metal_hit_01.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal1.ogg |
| Weapons/Melee/sfx_melee_metal_hit_02.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal2.ogg |
| Weapons/Melee/sfx_melee_metal_hit_03.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal3.ogg |
| Weapons/Melee/sfx_melee_metal_hit_04.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal4.ogg |
| Weapons/Melee/sfx_melee_metal_hit_05.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal5.ogg |
| Weapons/Melee/sfx_melee_metal_hit_06.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal6.ogg |
| Weapons/Melee/sfx_melee_metal_hit_07.ogg | Blade metal clash | Foley Sounds/Audio/Swords/swordMetal7.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_01.ogg | Hammer stone impact layer | Foley Sounds/Audio/Rocks/stoneHit1.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_02.ogg | Hammer stone impact layer | Foley Sounds/Audio/Rocks/stoneHit2.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_03.ogg | Hammer stone impact layer | Foley Sounds/Audio/Rocks/stoneHit3.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_04.ogg | Hammer stone impact layer | Foley Sounds/Audio/Rocks/stoneHit4.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_05.ogg | Hammer stone impact layer | Foley Sounds/Audio/Rocks/stoneHit5.ogg |
| Weapons/Hammer/sfx_hammer_rock_hit_01.ogg | Hammer rock impact layer | Foley Sounds/Audio/Rocks/rockHit1.ogg |
| Weapons/Hammer/sfx_hammer_rock_hit_02.ogg | Hammer rock impact layer | Foley Sounds/Audio/Rocks/rockHit2.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_01.ogg | Hammer landing candidate | Impact Sounds/Audio/impactMining_000.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_02.ogg | Hammer landing candidate | Impact Sounds/Audio/impactMining_001.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_03.ogg | Hammer landing candidate | Impact Sounds/Audio/impactMining_002.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_04.ogg | Hammer landing candidate | Impact Sounds/Audio/impactMining_003.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_05.ogg | Hammer landing candidate | Impact Sounds/Audio/impactMining_004.ogg |
| Combat/Hits/sfx_hit_punch_heavy_01.ogg | Heavy hit, hurt; also usable for the hammer | Impact Sounds/Audio/impactPunch_heavy_000.ogg |
| Combat/Hits/sfx_hit_punch_heavy_02.ogg | Heavy hit, hurt; also usable for the hammer | Impact Sounds/Audio/impactPunch_heavy_001.ogg |
| Combat/Hits/sfx_hit_punch_heavy_03.ogg | Heavy hit, hurt; also usable for the hammer | Impact Sounds/Audio/impactPunch_heavy_002.ogg |
| Combat/Hits/sfx_hit_punch_heavy_04.ogg | Heavy hit, hurt; also usable for the hammer | Impact Sounds/Audio/impactPunch_heavy_003.ogg |
| Combat/Hits/sfx_hit_punch_heavy_05.ogg | Heavy hit, hurt; also usable for the hammer | Impact Sounds/Audio/impactPunch_heavy_004.ogg |
| Combat/Hits/sfx_hit_soft_medium_01.ogg | Regular hit, hurt | Impact Sounds/Audio/impactSoft_medium_000.ogg |
| Combat/Hits/sfx_hit_soft_medium_02.ogg | Regular hit, hurt | Impact Sounds/Audio/impactSoft_medium_001.ogg |
| Combat/Hits/sfx_hit_soft_medium_03.ogg | Regular hit, hurt | Impact Sounds/Audio/impactSoft_medium_002.ogg |
| Combat/Hits/sfx_hit_soft_medium_04.ogg | Regular hit, hurt | Impact Sounds/Audio/impactSoft_medium_003.ogg |
| Combat/Hits/sfx_hit_soft_medium_05.ogg | Regular hit, hurt | Impact Sounds/Audio/impactSoft_medium_004.ogg |
| Player/Footsteps/sfx_footstep_grass_01.ogg | Player footsteps on grass | Impact Sounds/Audio/footstep_grass_000.ogg |
| Player/Footsteps/sfx_footstep_grass_02.ogg | Player footsteps on grass | Impact Sounds/Audio/footstep_grass_001.ogg |
| Player/Footsteps/sfx_footstep_grass_03.ogg | Player footsteps on grass | Impact Sounds/Audio/footstep_grass_002.ogg |
| Player/Footsteps/sfx_footstep_grass_04.ogg | Player footsteps on grass | Impact Sounds/Audio/footstep_grass_003.ogg |
| Player/Footsteps/sfx_footstep_grass_05.ogg | Player footsteps on grass | Impact Sounds/Audio/footstep_grass_004.ogg |
| Pickups/sfx_pickup_material_01.ogg | Material pickup candidate | Foley Sounds/Audio/Helmet/pickup1.ogg |
| Pickups/sfx_pickup_material_02.ogg | Material pickup candidate | Foley Sounds/Audio/Helmet/pickup2.ogg |
| Pickups/sfx_pickup_material_03.ogg | Material pickup candidate | Foley Sounds/Audio/Helmet/pickup3.ogg |
| Pickups/sfx_pickup_material_04.ogg | Material pickup candidate | Foley Sounds/Audio/Helmet/pickup4.ogg |
| Shop/sfx_shop_coins_01.ogg | Purchase, material settlement | RPG Audio/Audio/handleCoins.ogg |
| Shop/sfx_shop_coins_02.ogg | Purchase, material settlement | RPG Audio/Audio/handleCoins2.ogg |
| UI/sfx_ui_select_01.ogg | Menu and card selection | Interface Sounds/Audio/select_001.ogg |
| UI/sfx_ui_select_02.ogg | Menu and card selection | Interface Sounds/Audio/select_002.ogg |
| UI/sfx_ui_select_03.ogg | Menu and card selection | Interface Sounds/Audio/select_003.ogg |
| UI/sfx_ui_select_04.ogg | Menu and card selection | Interface Sounds/Audio/select_004.ogg |
| UI/sfx_ui_select_05.ogg | Menu and card selection | Interface Sounds/Audio/select_005.ogg |
| UI/sfx_ui_select_06.ogg | Menu and card selection | Interface Sounds/Audio/select_006.ogg |
| UI/sfx_ui_select_07.ogg | Menu and card selection | Interface Sounds/Audio/select_007.ogg |
| UI/sfx_ui_select_08.ogg | Menu and card selection | Interface Sounds/Audio/select_008.ogg |
| UI/sfx_ui_click_01.ogg | Click | Interface Sounds/Audio/click_001.ogg |
| UI/sfx_ui_click_02.ogg | Click | Interface Sounds/Audio/click_002.ogg |
| UI/sfx_ui_click_03.ogg | Click | Interface Sounds/Audio/click_003.ogg |
| UI/sfx_ui_click_04.ogg | Click | Interface Sounds/Audio/click_004.ogg |
| UI/sfx_ui_click_05.ogg | Click | Interface Sounds/Audio/click_005.ogg |
| UI/sfx_ui_confirm_01.ogg | Confirm, purchase succeeded | Interface Sounds/Audio/confirmation_001.ogg |
| UI/sfx_ui_confirm_02.ogg | Confirm, purchase succeeded | Interface Sounds/Audio/confirmation_002.ogg |
| UI/sfx_ui_confirm_03.ogg | Confirm, purchase succeeded | Interface Sounds/Audio/confirmation_003.ogg |
| UI/sfx_ui_confirm_04.ogg | Confirm, purchase succeeded | Interface Sounds/Audio/confirmation_004.ogg |
| UI/sfx_ui_back_01.ogg | Back, cancel | Interface Sounds/Audio/back_001.ogg |
| UI/sfx_ui_back_02.ogg | Back, cancel | Interface Sounds/Audio/back_002.ogg |
| UI/sfx_ui_back_03.ogg | Back, cancel | Interface Sounds/Audio/back_003.ogg |
| UI/sfx_ui_back_04.ogg | Back, cancel | Interface Sounds/Audio/back_004.ogg |
| UI/sfx_ui_error_01.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_001.ogg |
| UI/sfx_ui_error_02.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_002.ogg |
| UI/sfx_ui_error_03.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_003.ogg |
| UI/sfx_ui_error_04.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_004.ogg |
| UI/sfx_ui_error_05.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_005.ogg |
| UI/sfx_ui_error_06.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_006.ogg |
| UI/sfx_ui_error_07.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_007.ogg |
| UI/sfx_ui_error_08.ogg | Not enough materials, invalid action | Interface Sounds/Audio/error_008.ogg |
| UI/sfx_ui_scroll_01.ogg | List scroll | Interface Sounds/Audio/scroll_001.ogg |
| UI/sfx_ui_scroll_02.ogg | List scroll | Interface Sounds/Audio/scroll_002.ogg |
| UI/sfx_ui_scroll_03.ogg | List scroll | Interface Sounds/Audio/scroll_003.ogg |
| UI/sfx_ui_scroll_04.ogg | List scroll | Interface Sounds/Audio/scroll_004.ogg |
| UI/sfx_ui_scroll_05.ogg | List scroll | Interface Sounds/Audio/scroll_005.ogg |
| UI/sfx_ui_toggle_01.ogg | Settings toggle | Interface Sounds/Audio/toggle_001.ogg |
| UI/sfx_ui_toggle_02.ogg | Settings toggle | Interface Sounds/Audio/toggle_002.ogg |
| UI/sfx_ui_toggle_03.ogg | Settings toggle | Interface Sounds/Audio/toggle_003.ogg |
| UI/sfx_ui_toggle_04.ogg | Settings toggle | Interface Sounds/Audio/toggle_004.ogg |
| Shop/Cards/sfx_shop_card_slide_01.ogg | Card slide in, move | Casino Audio/Audio/card-slide-1.ogg |
| Shop/Cards/sfx_shop_card_slide_02.ogg | Card slide in, move | Casino Audio/Audio/card-slide-2.ogg |
| Shop/Cards/sfx_shop_card_slide_03.ogg | Card slide in, move | Casino Audio/Audio/card-slide-3.ogg |
| Shop/Cards/sfx_shop_card_slide_04.ogg | Card slide in, move | Casino Audio/Audio/card-slide-4.ogg |
| Shop/Cards/sfx_shop_card_slide_05.ogg | Card slide in, move | Casino Audio/Audio/card-slide-5.ogg |
| Shop/Cards/sfx_shop_card_slide_06.ogg | Card slide in, move | Casino Audio/Audio/card-slide-6.ogg |
| Shop/Cards/sfx_shop_card_slide_07.ogg | Card slide in, move | Casino Audio/Audio/card-slide-7.ogg |
| Shop/Cards/sfx_shop_card_slide_08.ogg | Card slide in, move | Casino Audio/Audio/card-slide-8.ogg |
| Shop/Cards/sfx_shop_card_place_01.ogg | Card appear, place | Casino Audio/Audio/card-place-1.ogg |
| Shop/Cards/sfx_shop_card_place_02.ogg | Card appear, place | Casino Audio/Audio/card-place-2.ogg |
| Shop/Cards/sfx_shop_card_place_03.ogg | Card appear, place | Casino Audio/Audio/card-place-3.ogg |
| Shop/Cards/sfx_shop_card_place_04.ogg | Card appear, place | Casino Audio/Audio/card-place-4.ogg |
| Shop/Cards/sfx_shop_card_shuffle_01.ogg | Shop refresh | Casino Audio/Audio/card-shuffle.ogg |
| Weapons/Poison/sfx_poison_slime_01.ogg | Poison spray and landing candidate | Sci-Fi Sounds/Audio/slime_000.ogg |
| Weapons/Poison/sfx_poison_slime_02.ogg | Poison spray and landing candidate | Sci-Fi Sounds/Audio/slime_001.ogg |
| Weapons/Poison/sfx_poison_drip_01.ogg | Poison drip candidate | Foley Sounds/Audio/Water/drip1.ogg |
| Weapons/Poison/sfx_poison_drip_02.ogg | Poison drip candidate | Foley Sounds/Audio/Water/drip2.ogg |
| Weapons/Poison/sfx_poison_drip_03.ogg | Poison drip candidate | Foley Sounds/Audio/Water/drip3.ogg |
| Weapons/Poison/sfx_poison_drip_04.ogg | Poison drip candidate | Foley Sounds/Audio/Water/drip4.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_01.ogg | Fireball explosion candidate | Sci-Fi Sounds/Audio/explosionCrunch_000.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_02.ogg | Fireball explosion candidate | Sci-Fi Sounds/Audio/explosionCrunch_001.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_03.ogg | Fireball explosion candidate | Sci-Fi Sounds/Audio/explosionCrunch_002.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_04.ogg | Fireball explosion candidate | Sci-Fi Sounds/Audio/explosionCrunch_003.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_05.ogg | Fireball explosion candidate | Sci-Fi Sounds/Audio/explosionCrunch_004.ogg |
| Weapons/Fireball/sfx_fireball_explosion_low_01.ogg | Explosion low-frequency layer candidate | Sci-Fi Sounds/Audio/lowFrequency_explosion_000.ogg |
| Weapons/Fireball/sfx_fireball_explosion_low_02.ogg | Explosion low-frequency layer candidate | Sci-Fi Sounds/Audio/lowFrequency_explosion_001.ogg |
| Weapons/Fire/sfx_fire_thruster_01.ogg | Flamethrower, torch candidate; may need processing | Sci-Fi Sounds/Audio/thrusterFire_000.ogg |
| Weapons/Fire/sfx_fire_thruster_02.ogg | Flamethrower, torch candidate; may need processing | Sci-Fi Sounds/Audio/thrusterFire_001.ogg |
| Weapons/Fire/sfx_fire_thruster_03.ogg | Flamethrower, torch candidate; may need processing | Sci-Fi Sounds/Audio/thrusterFire_002.ogg |
| Weapons/Fire/sfx_fire_thruster_04.ogg | Flamethrower, torch candidate; may need processing | Sci-Fi Sounds/Audio/thrusterFire_003.ogg |
| Weapons/Fire/sfx_fire_thruster_05.ogg | Flamethrower, torch candidate; may need processing | Sci-Fi Sounds/Audio/thrusterFire_004.ogg |
| Weapons/Frost/sfx_frost_glass_light_01.ogg | Ice crystal shatter candidate; source is glass | Impact Sounds/Audio/impactGlass_light_000.ogg |
| Weapons/Frost/sfx_frost_glass_light_02.ogg | Ice crystal shatter candidate; source is glass | Impact Sounds/Audio/impactGlass_light_001.ogg |
| Weapons/Frost/sfx_frost_glass_light_03.ogg | Ice crystal shatter candidate; source is glass | Impact Sounds/Audio/impactGlass_light_002.ogg |
| Weapons/Frost/sfx_frost_glass_light_04.ogg | Ice crystal shatter candidate; source is glass | Impact Sounds/Audio/impactGlass_light_003.ogg |
| Weapons/Frost/sfx_frost_glass_light_05.ogg | Ice crystal shatter candidate; source is glass | Impact Sounds/Audio/impactGlass_light_004.ogg |
| Weapons/Frost/sfx_frost_glass_medium_01.ogg | Ice crystal landing candidate; source is glass | Impact Sounds/Audio/impactGlass_medium_000.ogg |
| Weapons/Frost/sfx_frost_glass_medium_02.ogg | Ice crystal landing candidate; source is glass | Impact Sounds/Audio/impactGlass_medium_001.ogg |
| Weapons/Frost/sfx_frost_glass_medium_03.ogg | Ice crystal landing candidate; source is glass | Impact Sounds/Audio/impactGlass_medium_002.ogg |
| Weapons/Frost/sfx_frost_glass_medium_04.ogg | Ice crystal landing candidate; source is glass | Impact Sounds/Audio/impactGlass_medium_003.ogg |
| Weapons/Frost/sfx_frost_glass_medium_05.ogg | Ice crystal landing candidate; source is glass | Impact Sounds/Audio/impactGlass_medium_004.ogg |
| Jingles/jingle_hit_01.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_00.ogg |
| Jingles/jingle_hit_02.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_01.ogg |
| Jingles/jingle_hit_03.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_02.ogg |
| Jingles/jingle_hit_04.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_03.ogg |
| Jingles/jingle_hit_05.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_04.ogg |
| Jingles/jingle_hit_06.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_05.ogg |
| Jingles/jingle_hit_07.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_06.ogg |
| Jingles/jingle_hit_08.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_07.ogg |
| Jingles/jingle_hit_09.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_08.ogg |
| Jingles/jingle_hit_10.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_09.ogg |
| Jingles/jingle_hit_11.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_10.ogg |
| Jingles/jingle_hit_12.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_11.ogg |
| Jingles/jingle_hit_13.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_12.ogg |
| Jingles/jingle_hit_14.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_13.ogg |
| Jingles/jingle_hit_15.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_14.ogg |
| Jingles/jingle_hit_16.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_15.ogg |
| Jingles/jingle_hit_17.ogg | Settlement jingle; pick the event after listening | Music Jingles/Audio (Hit)/jingles-hit_16.ogg |
| Jingles/jingle_pizzicato_01.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_00.ogg |
| Jingles/jingle_pizzicato_02.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_01.ogg |
| Jingles/jingle_pizzicato_03.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_02.ogg |
| Jingles/jingle_pizzicato_04.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_03.ogg |
| Jingles/jingle_pizzicato_05.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_04.ogg |
| Jingles/jingle_pizzicato_06.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_05.ogg |
| Jingles/jingle_pizzicato_07.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_06.ogg |
| Jingles/jingle_pizzicato_08.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_07.ogg |
| Jingles/jingle_pizzicato_09.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_08.ogg |
| Jingles/jingle_pizzicato_10.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_09.ogg |
| Jingles/jingle_pizzicato_11.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_10.ogg |
| Jingles/jingle_pizzicato_12.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_11.ogg |
| Jingles/jingle_pizzicato_13.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_12.ogg |
| Jingles/jingle_pizzicato_14.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_13.ogg |
| Jingles/jingle_pizzicato_15.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_14.ogg |
| Jingles/jingle_pizzicato_16.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_15.ogg |
| Jingles/jingle_pizzicato_17.ogg | Upgrade, victory jingle candidate | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_16.ogg |
| Music/Loops/music_loop_alpha_dance.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Alpha Dance.ogg |
| Music/Loops/music_loop_cheerful_annoyance.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Cheerful Annoyance.ogg |
| Music/Loops/music_loop_drumming_sticks.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Drumming Sticks.ogg |
| Music/Loops/music_loop_farm_frolics.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Farm Frolics.ogg |
| Music/Loops/music_loop_flowing_rocks.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Flowing Rocks.ogg |
| Music/Loops/music_loop_game_over.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Game Over.ogg |
| Music/Loops/music_loop_german_virtue.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/German Virtue.ogg |
| Music/Loops/music_loop_infinite_descent.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Infinite Descent.ogg |
| Music/Loops/music_loop_italian_mom.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Italian Mom.ogg |
| Music/Loops/music_loop_mishief_stroll.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Mishief Stroll.ogg |
| Music/Loops/music_loop_mission_plausible.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Mission Plausible.ogg |
| Music/Loops/music_loop_night_at_the_beach.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Night at the Beach.ogg |
| Music/Loops/music_loop_polka_train.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Polka Train.ogg |
| Music/Loops/music_loop_sad_descent.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Sad Descent.ogg |
| Music/Loops/music_loop_sad_town.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Sad Town.ogg |
| Music/Loops/music_loop_space_cadet.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Space Cadet.ogg |
| Music/Loops/music_loop_swinging_pants.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Swinging Pants.ogg |
| Music/Loops/music_loop_time_driving.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Time Driving.ogg |
| Music/Loops/music_loop_wacky_waiting.ogg | Background music candidate; pick the scene after listening; loop seam not verified yet | Music Loops/Loops/Wacky Waiting.ogg |
