# 骨针射手

模型按 `Shooter-Concept.png` 制作：灰绿方块身体、骨白发射臂、橙色眼睛与枪口、背部三根骨针。
`BlockShooter.blend` 是可编辑源文件，`create_shooter.py` 可重新生成模型和 `Shooter-preview.png`。
运行 Blender `--background --python ArtSource/Enemies/create_shooter.py`。
导出 `Assets/Resources/Characters/EnemyShooter.fbx`，共 782 三角形。
独立的 `ShootArm`、`Muzzle`、`Breech` 支持抬臂与枪口定位；`FaceAnchor` 沿用当前模型朝向流程。

## 战斗与配置

- `EnemyKind.Shooter` 和 `ShooterOnly` 追加在枚举末尾，保留已有资产类型编号。
- 第 6 波起，每第 8 次成功生成有资格替换普通怪；原有特殊敌人优先级保留。
  第 6–12 波最多 2 只、1 个攻击名额；第 13 波起最多 3 只、2 个攻击名额。
  攻击名额包含蓄力和飞行中的骨针，避免同时射击形成弹幕。
- 基础生命 18，乘当前波次生命倍率；接触伤害、掉落、击杀计分沿用普通敌人规则。
- 超过 8.5 米以基础移速 70% 接近，4.5–8.5 米保持距离。
  距离不足 4.5 米时，以基础移速 55% 后撤最多 0.6 秒，再停 1.8 秒。
  冰冻影响移动；后撤受障碍物和场地边界限制。
- 4.5–9 米、有视线且位于画面安全区域内时蓄力。抬臂和橙色枪口发光，地面细线显示弹道。
  前 0.5 秒追踪，最后 0.3 秒锁定方向。长帧也至少保留完整锁定窗口。
  锤击眩晕、死亡或离开画面会取消尚未释放的攻击。
- 单发直线骨针，速度 7 米/秒、射程 12 米、伤害 8、碰撞半径 0.12 米；带短橙色拖尾。
  发射后冷却 2–2.4 秒，含蓄力的完整周期约 3 秒。发射后短暂停步。
- 骨针使用整段移动的碰撞检测，掩体优先挡住子弹；沿用玩家无敌时间与护盾。
  已经射出的骨针在射手死亡后继续飞行；波次结算、失败、重开和销毁时立即清理。
- 默认配置资产已追加射手阶段，其余已有配置保持原样。自定义配置需要手动添加 Shooter 阶段。
  菜单 `Tools > Survivors > Wave Configuration` 的 `ShooterOnly` 可从第一波单独测试。

## 验证

在非 Play Mode 下执行 `Tools > Survivors > Verify Bone Needle Shooter`，或 Unity batchmode
`-executeMethod ShooterChecks.Run`（不加 `-quit`，验证流程自行退出）。

验证包含模型导入、抬臂、波次规则、接近/后撤/冰冻、锁定与侧移、长帧命中、掩体、屏幕外限制、
护盾/无敌、眩晕/死亡、射程、暂停、数量/攻击名额、失败/结算清理和混合阵容。
成功标记为 `SHOOTER_CHECKS_PASSED`；实机截图为 `Shooter-InGame.png`。
