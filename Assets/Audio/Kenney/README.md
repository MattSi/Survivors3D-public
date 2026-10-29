# Kenney 音频候选库

共 197 个音频，来源：C:\Oracle\projects\kenney\Audio。

按文件语义初筛的候选库，原始音频内容、音量和采样率均未修改。已从中选择一批绑定游戏事件，详细映射及调音方法见项目根目录 AUDIO.md；其余变体保留供试听替换。

## 试听

在 Unity Project 窗口打开 Assets/Audio/Kenney，选中音频，在 Inspector 底部预览播放器点击播放。
建议顺序：UI → Pickups / Shop → Combat → Weapons → Jingles → Music。
同一系列从 01 编号，可按喜好选一个或保留多个变体。火焰、寒霜和音乐仍需确认风格适配。

## 命名

- 音效：sfx_用途_声音类型_两位编号.ogg
- 提示乐：jingle_类型_两位编号.ogg
- 音乐：music_loop_原曲名小写下划线.ogg
- source_manifest.json 保存新旧路径、建议用途和 SHA256；Licenses 保留各包原始许可。

## 分组

| 目录 | 数量 |
| --- | ---: |
| Combat | 10 |
| Jingles | 34 |
| Music | 19 |
| Pickups | 4 |
| Player | 5 |
| Shop | 15 |
| UI | 38 |
| Weapons | 72 |

## 用途与源文件对照

| 新文件 | 建议用途 | 原文件 |
| --- | --- | --- |
| Weapons/Melee/sfx_melee_knife_slice_01.ogg | 刀刃挥击 | RPG Audio/Audio/knifeSlice.ogg |
| Weapons/Melee/sfx_melee_knife_slice_02.ogg | 刀刃挥击 | RPG Audio/Audio/knifeSlice2.ogg |
| Weapons/Melee/sfx_melee_knife_draw_01.ogg | 出刀、装备 | RPG Audio/Audio/drawKnife1.ogg |
| Weapons/Melee/sfx_melee_knife_draw_02.ogg | 出刀、装备 | RPG Audio/Audio/drawKnife2.ogg |
| Weapons/Melee/sfx_melee_knife_draw_03.ogg | 出刀、装备 | RPG Audio/Audio/drawKnife3.ogg |
| Weapons/Melee/sfx_melee_chop_01.ogg | 劈砍命中 | RPG Audio/Audio/chop.ogg |
| Weapons/Melee/sfx_melee_whoosh_01.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh1.ogg |
| Weapons/Melee/sfx_melee_whoosh_02.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh2.ogg |
| Weapons/Melee/sfx_melee_whoosh_03.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh3.ogg |
| Weapons/Melee/sfx_melee_whoosh_04.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh4.ogg |
| Weapons/Melee/sfx_melee_whoosh_05.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh5.ogg |
| Weapons/Melee/sfx_melee_whoosh_06.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh6.ogg |
| Weapons/Melee/sfx_melee_whoosh_07.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh7.ogg |
| Weapons/Melee/sfx_melee_whoosh_08.ogg | 挥动、长矛与飞刀投掷 | Foley Sounds/Audio/Woosh/woosh8.ogg |
| Weapons/Melee/sfx_melee_sword_01.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword1.ogg |
| Weapons/Melee/sfx_melee_sword_02.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword2.ogg |
| Weapons/Melee/sfx_melee_sword_03.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword3.ogg |
| Weapons/Melee/sfx_melee_sword_04.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword4.ogg |
| Weapons/Melee/sfx_melee_sword_05.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword5.ogg |
| Weapons/Melee/sfx_melee_sword_06.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword6.ogg |
| Weapons/Melee/sfx_melee_sword_07.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword7.ogg |
| Weapons/Melee/sfx_melee_sword_08.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword8.ogg |
| Weapons/Melee/sfx_melee_sword_09.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword9.ogg |
| Weapons/Melee/sfx_melee_sword_10.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword10.ogg |
| Weapons/Melee/sfx_melee_sword_11.ogg | 近战刀剑候选 | Foley Sounds/Audio/Swords/sword11.ogg |
| Weapons/Melee/sfx_melee_metal_hit_01.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal1.ogg |
| Weapons/Melee/sfx_melee_metal_hit_02.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal2.ogg |
| Weapons/Melee/sfx_melee_metal_hit_03.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal3.ogg |
| Weapons/Melee/sfx_melee_metal_hit_04.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal4.ogg |
| Weapons/Melee/sfx_melee_metal_hit_05.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal5.ogg |
| Weapons/Melee/sfx_melee_metal_hit_06.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal6.ogg |
| Weapons/Melee/sfx_melee_metal_hit_07.ogg | 刀刃金属碰撞 | Foley Sounds/Audio/Swords/swordMetal7.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_01.ogg | 重锤石质撞击层 | Foley Sounds/Audio/Rocks/stoneHit1.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_02.ogg | 重锤石质撞击层 | Foley Sounds/Audio/Rocks/stoneHit2.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_03.ogg | 重锤石质撞击层 | Foley Sounds/Audio/Rocks/stoneHit3.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_04.ogg | 重锤石质撞击层 | Foley Sounds/Audio/Rocks/stoneHit4.ogg |
| Weapons/Hammer/sfx_hammer_stone_hit_05.ogg | 重锤石质撞击层 | Foley Sounds/Audio/Rocks/stoneHit5.ogg |
| Weapons/Hammer/sfx_hammer_rock_hit_01.ogg | 重锤岩石撞击层 | Foley Sounds/Audio/Rocks/rockHit1.ogg |
| Weapons/Hammer/sfx_hammer_rock_hit_02.ogg | 重锤岩石撞击层 | Foley Sounds/Audio/Rocks/rockHit2.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_01.ogg | 重锤落地候选 | Impact Sounds/Audio/impactMining_000.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_02.ogg | 重锤落地候选 | Impact Sounds/Audio/impactMining_001.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_03.ogg | 重锤落地候选 | Impact Sounds/Audio/impactMining_002.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_04.ogg | 重锤落地候选 | Impact Sounds/Audio/impactMining_003.ogg |
| Weapons/Hammer/sfx_hammer_mining_hit_05.ogg | 重锤落地候选 | Impact Sounds/Audio/impactMining_004.ogg |
| Combat/Hits/sfx_hit_punch_heavy_01.ogg | 重击、受伤，也可用于重锤 | Impact Sounds/Audio/impactPunch_heavy_000.ogg |
| Combat/Hits/sfx_hit_punch_heavy_02.ogg | 重击、受伤，也可用于重锤 | Impact Sounds/Audio/impactPunch_heavy_001.ogg |
| Combat/Hits/sfx_hit_punch_heavy_03.ogg | 重击、受伤，也可用于重锤 | Impact Sounds/Audio/impactPunch_heavy_002.ogg |
| Combat/Hits/sfx_hit_punch_heavy_04.ogg | 重击、受伤，也可用于重锤 | Impact Sounds/Audio/impactPunch_heavy_003.ogg |
| Combat/Hits/sfx_hit_punch_heavy_05.ogg | 重击、受伤，也可用于重锤 | Impact Sounds/Audio/impactPunch_heavy_004.ogg |
| Combat/Hits/sfx_hit_soft_medium_01.ogg | 普通命中、受伤 | Impact Sounds/Audio/impactSoft_medium_000.ogg |
| Combat/Hits/sfx_hit_soft_medium_02.ogg | 普通命中、受伤 | Impact Sounds/Audio/impactSoft_medium_001.ogg |
| Combat/Hits/sfx_hit_soft_medium_03.ogg | 普通命中、受伤 | Impact Sounds/Audio/impactSoft_medium_002.ogg |
| Combat/Hits/sfx_hit_soft_medium_04.ogg | 普通命中、受伤 | Impact Sounds/Audio/impactSoft_medium_003.ogg |
| Combat/Hits/sfx_hit_soft_medium_05.ogg | 普通命中、受伤 | Impact Sounds/Audio/impactSoft_medium_004.ogg |
| Player/Footsteps/sfx_footstep_grass_01.ogg | 玩家草地脚步 | Impact Sounds/Audio/footstep_grass_000.ogg |
| Player/Footsteps/sfx_footstep_grass_02.ogg | 玩家草地脚步 | Impact Sounds/Audio/footstep_grass_001.ogg |
| Player/Footsteps/sfx_footstep_grass_03.ogg | 玩家草地脚步 | Impact Sounds/Audio/footstep_grass_002.ogg |
| Player/Footsteps/sfx_footstep_grass_04.ogg | 玩家草地脚步 | Impact Sounds/Audio/footstep_grass_003.ogg |
| Player/Footsteps/sfx_footstep_grass_05.ogg | 玩家草地脚步 | Impact Sounds/Audio/footstep_grass_004.ogg |
| Pickups/sfx_pickup_material_01.ogg | 材料拾取候选 | Foley Sounds/Audio/Helmet/pickup1.ogg |
| Pickups/sfx_pickup_material_02.ogg | 材料拾取候选 | Foley Sounds/Audio/Helmet/pickup2.ogg |
| Pickups/sfx_pickup_material_03.ogg | 材料拾取候选 | Foley Sounds/Audio/Helmet/pickup3.ogg |
| Pickups/sfx_pickup_material_04.ogg | 材料拾取候选 | Foley Sounds/Audio/Helmet/pickup4.ogg |
| Shop/sfx_shop_coins_01.ogg | 购买、材料结算 | RPG Audio/Audio/handleCoins.ogg |
| Shop/sfx_shop_coins_02.ogg | 购买、材料结算 | RPG Audio/Audio/handleCoins2.ogg |
| UI/sfx_ui_select_01.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_001.ogg |
| UI/sfx_ui_select_02.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_002.ogg |
| UI/sfx_ui_select_03.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_003.ogg |
| UI/sfx_ui_select_04.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_004.ogg |
| UI/sfx_ui_select_05.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_005.ogg |
| UI/sfx_ui_select_06.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_006.ogg |
| UI/sfx_ui_select_07.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_007.ogg |
| UI/sfx_ui_select_08.ogg | 菜单与卡片选择 | Interface Sounds/Audio/select_008.ogg |
| UI/sfx_ui_click_01.ogg | 点击 | Interface Sounds/Audio/click_001.ogg |
| UI/sfx_ui_click_02.ogg | 点击 | Interface Sounds/Audio/click_002.ogg |
| UI/sfx_ui_click_03.ogg | 点击 | Interface Sounds/Audio/click_003.ogg |
| UI/sfx_ui_click_04.ogg | 点击 | Interface Sounds/Audio/click_004.ogg |
| UI/sfx_ui_click_05.ogg | 点击 | Interface Sounds/Audio/click_005.ogg |
| UI/sfx_ui_confirm_01.ogg | 确认、购买成功 | Interface Sounds/Audio/confirmation_001.ogg |
| UI/sfx_ui_confirm_02.ogg | 确认、购买成功 | Interface Sounds/Audio/confirmation_002.ogg |
| UI/sfx_ui_confirm_03.ogg | 确认、购买成功 | Interface Sounds/Audio/confirmation_003.ogg |
| UI/sfx_ui_confirm_04.ogg | 确认、购买成功 | Interface Sounds/Audio/confirmation_004.ogg |
| UI/sfx_ui_back_01.ogg | 返回、取消 | Interface Sounds/Audio/back_001.ogg |
| UI/sfx_ui_back_02.ogg | 返回、取消 | Interface Sounds/Audio/back_002.ogg |
| UI/sfx_ui_back_03.ogg | 返回、取消 | Interface Sounds/Audio/back_003.ogg |
| UI/sfx_ui_back_04.ogg | 返回、取消 | Interface Sounds/Audio/back_004.ogg |
| UI/sfx_ui_error_01.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_001.ogg |
| UI/sfx_ui_error_02.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_002.ogg |
| UI/sfx_ui_error_03.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_003.ogg |
| UI/sfx_ui_error_04.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_004.ogg |
| UI/sfx_ui_error_05.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_005.ogg |
| UI/sfx_ui_error_06.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_006.ogg |
| UI/sfx_ui_error_07.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_007.ogg |
| UI/sfx_ui_error_08.ogg | 材料不足、无效操作 | Interface Sounds/Audio/error_008.ogg |
| UI/sfx_ui_scroll_01.ogg | 列表滚动 | Interface Sounds/Audio/scroll_001.ogg |
| UI/sfx_ui_scroll_02.ogg | 列表滚动 | Interface Sounds/Audio/scroll_002.ogg |
| UI/sfx_ui_scroll_03.ogg | 列表滚动 | Interface Sounds/Audio/scroll_003.ogg |
| UI/sfx_ui_scroll_04.ogg | 列表滚动 | Interface Sounds/Audio/scroll_004.ogg |
| UI/sfx_ui_scroll_05.ogg | 列表滚动 | Interface Sounds/Audio/scroll_005.ogg |
| UI/sfx_ui_toggle_01.ogg | 设置开关 | Interface Sounds/Audio/toggle_001.ogg |
| UI/sfx_ui_toggle_02.ogg | 设置开关 | Interface Sounds/Audio/toggle_002.ogg |
| UI/sfx_ui_toggle_03.ogg | 设置开关 | Interface Sounds/Audio/toggle_003.ogg |
| UI/sfx_ui_toggle_04.ogg | 设置开关 | Interface Sounds/Audio/toggle_004.ogg |
| Shop/Cards/sfx_shop_card_slide_01.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-1.ogg |
| Shop/Cards/sfx_shop_card_slide_02.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-2.ogg |
| Shop/Cards/sfx_shop_card_slide_03.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-3.ogg |
| Shop/Cards/sfx_shop_card_slide_04.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-4.ogg |
| Shop/Cards/sfx_shop_card_slide_05.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-5.ogg |
| Shop/Cards/sfx_shop_card_slide_06.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-6.ogg |
| Shop/Cards/sfx_shop_card_slide_07.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-7.ogg |
| Shop/Cards/sfx_shop_card_slide_08.ogg | 卡片滑入、移动 | Casino Audio/Audio/card-slide-8.ogg |
| Shop/Cards/sfx_shop_card_place_01.ogg | 卡片出现、落位 | Casino Audio/Audio/card-place-1.ogg |
| Shop/Cards/sfx_shop_card_place_02.ogg | 卡片出现、落位 | Casino Audio/Audio/card-place-2.ogg |
| Shop/Cards/sfx_shop_card_place_03.ogg | 卡片出现、落位 | Casino Audio/Audio/card-place-3.ogg |
| Shop/Cards/sfx_shop_card_place_04.ogg | 卡片出现、落位 | Casino Audio/Audio/card-place-4.ogg |
| Shop/Cards/sfx_shop_card_shuffle_01.ogg | 商店刷新 | Casino Audio/Audio/card-shuffle.ogg |
| Weapons/Poison/sfx_poison_slime_01.ogg | 毒液喷洒与落地候选 | Sci-Fi Sounds/Audio/slime_000.ogg |
| Weapons/Poison/sfx_poison_slime_02.ogg | 毒液喷洒与落地候选 | Sci-Fi Sounds/Audio/slime_001.ogg |
| Weapons/Poison/sfx_poison_drip_01.ogg | 毒液滴落候选 | Foley Sounds/Audio/Water/drip1.ogg |
| Weapons/Poison/sfx_poison_drip_02.ogg | 毒液滴落候选 | Foley Sounds/Audio/Water/drip2.ogg |
| Weapons/Poison/sfx_poison_drip_03.ogg | 毒液滴落候选 | Foley Sounds/Audio/Water/drip3.ogg |
| Weapons/Poison/sfx_poison_drip_04.ogg | 毒液滴落候选 | Foley Sounds/Audio/Water/drip4.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_01.ogg | 火球爆炸候选 | Sci-Fi Sounds/Audio/explosionCrunch_000.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_02.ogg | 火球爆炸候选 | Sci-Fi Sounds/Audio/explosionCrunch_001.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_03.ogg | 火球爆炸候选 | Sci-Fi Sounds/Audio/explosionCrunch_002.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_04.ogg | 火球爆炸候选 | Sci-Fi Sounds/Audio/explosionCrunch_003.ogg |
| Weapons/Fireball/sfx_fireball_explosion_crunch_05.ogg | 火球爆炸候选 | Sci-Fi Sounds/Audio/explosionCrunch_004.ogg |
| Weapons/Fireball/sfx_fireball_explosion_low_01.ogg | 爆炸低频层候选 | Sci-Fi Sounds/Audio/lowFrequency_explosion_000.ogg |
| Weapons/Fireball/sfx_fireball_explosion_low_02.ogg | 爆炸低频层候选 | Sci-Fi Sounds/Audio/lowFrequency_explosion_001.ogg |
| Weapons/Fire/sfx_fire_thruster_01.ogg | 喷火、火把候选；可能需要加工 | Sci-Fi Sounds/Audio/thrusterFire_000.ogg |
| Weapons/Fire/sfx_fire_thruster_02.ogg | 喷火、火把候选；可能需要加工 | Sci-Fi Sounds/Audio/thrusterFire_001.ogg |
| Weapons/Fire/sfx_fire_thruster_03.ogg | 喷火、火把候选；可能需要加工 | Sci-Fi Sounds/Audio/thrusterFire_002.ogg |
| Weapons/Fire/sfx_fire_thruster_04.ogg | 喷火、火把候选；可能需要加工 | Sci-Fi Sounds/Audio/thrusterFire_003.ogg |
| Weapons/Fire/sfx_fire_thruster_05.ogg | 喷火、火把候选；可能需要加工 | Sci-Fi Sounds/Audio/thrusterFire_004.ogg |
| Weapons/Frost/sfx_frost_glass_light_01.ogg | 冰晶碎裂候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_light_000.ogg |
| Weapons/Frost/sfx_frost_glass_light_02.ogg | 冰晶碎裂候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_light_001.ogg |
| Weapons/Frost/sfx_frost_glass_light_03.ogg | 冰晶碎裂候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_light_002.ogg |
| Weapons/Frost/sfx_frost_glass_light_04.ogg | 冰晶碎裂候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_light_003.ogg |
| Weapons/Frost/sfx_frost_glass_light_05.ogg | 冰晶碎裂候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_light_004.ogg |
| Weapons/Frost/sfx_frost_glass_medium_01.ogg | 冰晶落地候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_medium_000.ogg |
| Weapons/Frost/sfx_frost_glass_medium_02.ogg | 冰晶落地候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_medium_001.ogg |
| Weapons/Frost/sfx_frost_glass_medium_03.ogg | 冰晶落地候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_medium_002.ogg |
| Weapons/Frost/sfx_frost_glass_medium_04.ogg | 冰晶落地候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_medium_003.ogg |
| Weapons/Frost/sfx_frost_glass_medium_05.ogg | 冰晶落地候选；原素材为玻璃 | Impact Sounds/Audio/impactGlass_medium_004.ogg |
| Jingles/jingle_hit_01.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_00.ogg |
| Jingles/jingle_hit_02.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_01.ogg |
| Jingles/jingle_hit_03.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_02.ogg |
| Jingles/jingle_hit_04.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_03.ogg |
| Jingles/jingle_hit_05.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_04.ogg |
| Jingles/jingle_hit_06.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_05.ogg |
| Jingles/jingle_hit_07.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_06.ogg |
| Jingles/jingle_hit_08.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_07.ogg |
| Jingles/jingle_hit_09.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_08.ogg |
| Jingles/jingle_hit_10.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_09.ogg |
| Jingles/jingle_hit_11.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_10.ogg |
| Jingles/jingle_hit_12.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_11.ogg |
| Jingles/jingle_hit_13.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_12.ogg |
| Jingles/jingle_hit_14.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_13.ogg |
| Jingles/jingle_hit_15.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_14.ogg |
| Jingles/jingle_hit_16.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_15.ogg |
| Jingles/jingle_hit_17.ogg | 结算提示乐；试听后决定事件 | Music Jingles/Audio (Hit)/jingles-hit_16.ogg |
| Jingles/jingle_pizzicato_01.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_00.ogg |
| Jingles/jingle_pizzicato_02.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_01.ogg |
| Jingles/jingle_pizzicato_03.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_02.ogg |
| Jingles/jingle_pizzicato_04.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_03.ogg |
| Jingles/jingle_pizzicato_05.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_04.ogg |
| Jingles/jingle_pizzicato_06.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_05.ogg |
| Jingles/jingle_pizzicato_07.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_06.ogg |
| Jingles/jingle_pizzicato_08.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_07.ogg |
| Jingles/jingle_pizzicato_09.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_08.ogg |
| Jingles/jingle_pizzicato_10.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_09.ogg |
| Jingles/jingle_pizzicato_11.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_10.ogg |
| Jingles/jingle_pizzicato_12.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_11.ogg |
| Jingles/jingle_pizzicato_13.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_12.ogg |
| Jingles/jingle_pizzicato_14.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_13.ogg |
| Jingles/jingle_pizzicato_15.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_14.ogg |
| Jingles/jingle_pizzicato_16.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_15.ogg |
| Jingles/jingle_pizzicato_17.ogg | 升级、胜利提示乐候选 | Music Jingles/Audio (Pizzicato)/jingles-pizzicato_16.ogg |
| Music/Loops/music_loop_alpha_dance.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Alpha Dance.ogg |
| Music/Loops/music_loop_cheerful_annoyance.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Cheerful Annoyance.ogg |
| Music/Loops/music_loop_drumming_sticks.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Drumming Sticks.ogg |
| Music/Loops/music_loop_farm_frolics.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Farm Frolics.ogg |
| Music/Loops/music_loop_flowing_rocks.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Flowing Rocks.ogg |
| Music/Loops/music_loop_game_over.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Game Over.ogg |
| Music/Loops/music_loop_german_virtue.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/German Virtue.ogg |
| Music/Loops/music_loop_infinite_descent.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Infinite Descent.ogg |
| Music/Loops/music_loop_italian_mom.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Italian Mom.ogg |
| Music/Loops/music_loop_mishief_stroll.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Mishief Stroll.ogg |
| Music/Loops/music_loop_mission_plausible.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Mission Plausible.ogg |
| Music/Loops/music_loop_night_at_the_beach.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Night at the Beach.ogg |
| Music/Loops/music_loop_polka_train.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Polka Train.ogg |
| Music/Loops/music_loop_sad_descent.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Sad Descent.ogg |
| Music/Loops/music_loop_sad_town.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Sad Town.ogg |
| Music/Loops/music_loop_space_cadet.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Space Cadet.ogg |
| Music/Loops/music_loop_swinging_pants.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Swinging Pants.ogg |
| Music/Loops/music_loop_time_driving.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Time Driving.ogg |
| Music/Loops/music_loop_wacky_waiting.ogg | 背景音乐候选；试听后决定场景，循环接缝尚未验证 | Music Loops/Loops/Wacky Waiting.ogg |
