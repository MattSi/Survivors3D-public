# 方块敌人模型

`BlockEnemies.blend` 包含三种同风格敌人和预览摄影棚；`Enemies-preview.png` 为俯视角渲染。源生成脚本是 `create_enemies.py`。

- `EnemyImp`：带角、宽耳的小怪，572 三角形。
- `EnemyHood`：兜帽小怪，484 三角形。
- `EnemyRock`：宽肩石头小怪，484 三角形。

游戏 FBX 位于 `Assets/Resources/Characters`。每个模型合并为一个网格，保留材质槽和 `FaceAnchor`，不导出灯光或地面。Unity 使用共享 URP 材质，模型朝向追踪目标，脚底对齐地面，高度约 1.02 米。

三种普通外形随机生成，共用当前敌人的血量、速度和掉落规则。喷火焦化会覆盖模型全部材质槽，死亡后沿用焦化残骸效果。

新增 `EnemyCharger` 冲锋小鬼：砖红色方块造型，748 三角形；源文件为 `BlockCharger.blend`，生成脚本为 `create_charger.py`。第 3 波起替换少量普通怪，具备锁定方向、蓄力预警、直线冲锋和恢复停顿。参数和验证入口见 [CHARGER.md](CHARGER.md)，实机场景预览为 `Charger-InGame.png`。

新增 `EnemyStoneArmor` 石甲怪：宽肩灰石甲、苔绿色块，1,056 三角形；源文件为 `BlockStoneArmor.blend`，生成脚本为 `create_stone_armor.py`。第 5 波起少量出现，独立基础血量 120、55% 移速，使用更大的实体碰撞体。参数与验证入口见 [STONE_ARMOR.md](STONE_ARMOR.md)。

新增 `EnemyBomber` 投弹兜帽怪：深紫兜帽、腰包、独立投掷手臂和炸弹，共 768 三角形。源文件为 `BlockBomber.blend`，生成脚本为 `create_bomber.py`。第 8 波起少量出现，保持距离，向预警落点抛投延迟爆炸的炸弹。参数和验证入口见 [BOMBER.md](BOMBER.md)。

普通模型已完成 Blender 渲染和 Unity 程序集编译检查；特殊敌人的 Play Mode 检查见各自说明。
# 分裂怪

新增 `EnemySplitter.fbx` 母体和 `EnemySplitling.fbx` 小怪，源文件、行为与测试入口见 [SPLITTER.md](SPLITTER.md)。

# 骨针射手

新增 `EnemyShooter.fbx`：灰绿方块身体、骨白发射臂、背部三根骨针，共 782 三角形。
源文件 `BlockShooter.blend`，生成脚本 `create_shooter.py`；模型预览 `Shooter-preview.png`。
第 6 波开始少量出现，蓄力后发射可躲避、可被掩体挡住的直线骨针。参数与测试入口见 [SHOOTER.md](SHOOTER.md)。
