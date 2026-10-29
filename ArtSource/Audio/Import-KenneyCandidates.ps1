param(
    [string]$SourceRoot = 'C:\Oracle\projects\kenney\Audio',
    [string]$DestinationRoot = (Join-Path $PSScriptRoot '..\..\Assets\Audio\Kenney')
)

$ErrorActionPreference = 'Stop'
$DestinationRoot = [IO.Path]::GetFullPath($DestinationRoot)
$SourceRoot = [IO.Path]::GetFullPath($SourceRoot)
$rules = @(
    @('RPG Audio', '^knifeSlice\d*$', 'Weapons/Melee', 'sfx_melee_knife_slice', '刀刃挥击'),
    @('RPG Audio', '^drawKnife\d+$', 'Weapons/Melee', 'sfx_melee_knife_draw', '出刀、装备'),
    @('RPG Audio', '^chop$', 'Weapons/Melee', 'sfx_melee_chop', '劈砍命中'),
    @('Foley Sounds', '^woosh\d+$', 'Weapons/Melee', 'sfx_melee_whoosh', '挥动、长矛与飞刀投掷'),
    @('Foley Sounds', '^sword\d+$', 'Weapons/Melee', 'sfx_melee_sword', '近战刀剑候选'),
    @('Foley Sounds', '^swordMetal\d+$', 'Weapons/Melee', 'sfx_melee_metal_hit', '刀刃金属碰撞'),
    @('Foley Sounds', '^stoneHit\d+$', 'Weapons/Hammer', 'sfx_hammer_stone_hit', '重锤石质撞击层'),
    @('Foley Sounds', '^rockHit\d+$', 'Weapons/Hammer', 'sfx_hammer_rock_hit', '重锤岩石撞击层'),
    @('Impact Sounds', '^impactMining_\d+$', 'Weapons/Hammer', 'sfx_hammer_mining_hit', '重锤落地候选'),
    @('Impact Sounds', '^impactPunch_heavy_\d+$', 'Combat/Hits', 'sfx_hit_punch_heavy', '重击、受伤，也可用于重锤'),
    @('Impact Sounds', '^impactSoft_medium_\d+$', 'Combat/Hits', 'sfx_hit_soft_medium', '普通命中、受伤'),
    @('Impact Sounds', '^footstep_grass_\d+$', 'Player/Footsteps', 'sfx_footstep_grass', '玩家草地脚步'),
    @('Foley Sounds', '^pickup\d+$', 'Pickups', 'sfx_pickup_material', '材料拾取候选'),
    @('RPG Audio', '^handleCoins\d*$', 'Shop', 'sfx_shop_coins', '购买、材料结算'),
    @('Interface Sounds', '^select_\d+$', 'UI', 'sfx_ui_select', '菜单与卡片选择'),
    @('Interface Sounds', '^click_\d+$', 'UI', 'sfx_ui_click', '点击'),
    @('Interface Sounds', '^confirmation_\d+$', 'UI', 'sfx_ui_confirm', '确认、购买成功'),
    @('Interface Sounds', '^back_\d+$', 'UI', 'sfx_ui_back', '返回、取消'),
    @('Interface Sounds', '^error_\d+$', 'UI', 'sfx_ui_error', '材料不足、无效操作'),
    @('Interface Sounds', '^scroll_\d+$', 'UI', 'sfx_ui_scroll', '列表滚动'),
    @('Interface Sounds', '^toggle_\d+$', 'UI', 'sfx_ui_toggle', '设置开关'),
    @('Casino Audio', '^card-slide-\d+$', 'Shop/Cards', 'sfx_shop_card_slide', '卡片滑入、移动'),
    @('Casino Audio', '^card-place-\d+$', 'Shop/Cards', 'sfx_shop_card_place', '卡片出现、落位'),
    @('Casino Audio', '^card-shuffle$', 'Shop/Cards', 'sfx_shop_card_shuffle', '商店刷新'),
    @('Sci-Fi Sounds', '^slime_\d+$', 'Weapons/Poison', 'sfx_poison_slime', '毒液喷洒与落地候选'),
    @('Foley Sounds', '^drip\d+$', 'Weapons/Poison', 'sfx_poison_drip', '毒液滴落候选'),
    @('Sci-Fi Sounds', '^explosionCrunch_\d+$', 'Weapons/Fireball', 'sfx_fireball_explosion_crunch', '火球爆炸候选'),
    @('Sci-Fi Sounds', '^lowFrequency_explosion_\d+$', 'Weapons/Fireball', 'sfx_fireball_explosion_low', '爆炸低频层候选'),
    @('Sci-Fi Sounds', '^thrusterFire_\d+$', 'Weapons/Fire', 'sfx_fire_thruster', '喷火、火把候选；可能需要加工'),
    @('Impact Sounds', '^impactGlass_light_\d+$', 'Weapons/Frost', 'sfx_frost_glass_light', '冰晶碎裂候选；原素材为玻璃'),
    @('Impact Sounds', '^impactGlass_medium_\d+$', 'Weapons/Frost', 'sfx_frost_glass_medium', '冰晶落地候选；原素材为玻璃'),
    @('Music Jingles', '^jingles-hit_\d+$', 'Jingles', 'jingle_hit', '结算提示乐；试听后决定事件'),
    @('Music Jingles', '^jingles-pizzicato_\d+$', 'Jingles', 'jingle_pizzicato', '升级、胜利提示乐候选')
)

$entries = [Collections.Generic.List[object]]::new()
foreach ($rule in $rules) {
    $files = @(Get-ChildItem -LiteralPath (Join-Path $SourceRoot $rule[0]) -Recurse -File -Filter '*.ogg' |
        Where-Object { $_.BaseName -match $rule[1] } |
        Sort-Object @{Expression={ [regex]::Replace($_.BaseName, '\d+', { param($m) $m.Value.PadLeft(6, '0') }) }})
    if ($files.Count -eq 0) { throw "No source matches: $($rule[0]) / $($rule[1])" }
    $index = 0
    foreach ($file in $files) {
        $index++
        $entries.Add([pscustomobject]@{
            destination = ('{0}/{1}_{2:D2}.ogg' -f $rule[2], $rule[3], $index)
            source = $file.FullName.Substring($SourceRoot.Length + 1).Replace('\', '/')
            pack = $rule[0]
            usage = $rule[4]
            status = '待试听'
        })
    }
}

# Keep the non-retro loops as an audition pool; names do not imply a gameplay assignment.
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $SourceRoot 'Music Loops\Loops') -File -Filter '*.ogg' | Sort-Object Name) {
    $slug = ($file.BaseName.ToLowerInvariant() -replace '[^a-z0-9]+', '_').Trim('_')
    $entries.Add([pscustomobject]@{
        destination = "Music/Loops/music_loop_$slug.ogg"
        source = $file.FullName.Substring($SourceRoot.Length + 1).Replace('\', '/')
        pack = 'Music Loops'
        usage = '背景音乐候选；试听后决定场景，循环接缝尚未验证'
        status = '待试听'
    })
}

if (@($entries.destination | Sort-Object -Unique).Count -ne $entries.Count) { throw 'Duplicate destination names' }
# Preflight every destination before copying. Existing identical copies are safe to reuse.
foreach ($entry in $entries) {
    $source = Join-Path $SourceRoot $entry.source
    $target = Join-Path $DestinationRoot $entry.destination
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    $entry | Add-Member -NotePropertyName sha256 -NotePropertyValue $hash
    if ((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash) {
        throw "Destination has different content: $target"
    }
}
foreach ($entry in $entries) {
    $target = Join-Path $DestinationRoot $entry.destination
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    if (-not (Test-Path -LiteralPath $target)) { Copy-Item -LiteralPath (Join-Path $SourceRoot $entry.source) -Destination $target }
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Copy verification failed: $target" }
}

$licenseDir = Join-Path $DestinationRoot 'Licenses'
New-Item -ItemType Directory -Path $licenseDir -Force | Out-Null
foreach ($pack in $entries.pack | Sort-Object -Unique) {
    $slug = $pack.ToLowerInvariant() -replace '[^a-z0-9]+', '_'
    Copy-Item -LiteralPath (Join-Path $SourceRoot "$pack\License.txt") -Destination (Join-Path $licenseDir "kenney_${slug}_license.txt")
}
$entries | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $DestinationRoot 'source_manifest.json') -Encoding UTF8
$readme = @(
    '# Kenney 音频候选库', '',
    "共 $($entries.Count) 个音频，来源：$SourceRoot。", '',
    '按文件语义初筛的候选库，原始音频内容、音量和采样率均未修改。游戏事件映射及调音方法见项目根目录 AUDIO.md；其余变体保留供试听替换。', '',
    '## 试听', '',
    '在 Unity Project 窗口打开 Assets/Audio/Kenney，选中音频，在 Inspector 底部预览播放器点击播放。',
    '建议顺序：UI → Pickups / Shop → Combat → Weapons → Jingles → Music。',
    '同一系列从 01 编号，可按喜好选一个或保留多个变体。火焰、寒霜和音乐仍需确认风格适配。', '',
    '## 命名', '',
    '- 音效：sfx_用途_声音类型_两位编号.ogg',
    '- 提示乐：jingle_类型_两位编号.ogg',
    '- 音乐：music_loop_原曲名小写下划线.ogg',
    '- source_manifest.json 保存新旧路径、建议用途和 SHA256；Licenses 保留各包原始许可。', '',
    '## 分组', '',
    '| 目录 | 数量 |', '| --- | ---: |'
)
foreach ($group in $entries | Group-Object { ($_.destination -split '/')[0] } | Sort-Object Name) {
    $readme += "| $($group.Name) | $($group.Count) |"
}
$readme += @('', '## 用途与源文件对照', '', '| 新文件 | 建议用途 | 原文件 |', '| --- | --- | --- |')
foreach ($entry in $entries) { $readme += "| $($entry.destination) | $($entry.usage) | $($entry.source) |" }
$readme | Set-Content -LiteralPath (Join-Path $DestinationRoot 'README.md') -Encoding UTF8
$entries | Group-Object { ($_.destination -split '/')[0] } | Sort-Object Name | Select-Object Name, Count
Write-Output "Copied and SHA256 verified $($entries.Count) audio files to $DestinationRoot"
