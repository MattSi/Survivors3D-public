# Block Hero

角色已加入免费短冲刺：空格 / LB / L1，2.5 米、0.15 秒、6 秒冷却，带短暂无敌与蓝白拖尾。
操作、碰撞规则和验证入口见 [DASH.md](DASH.md)。

Blender 5.2.1 制作的原创方块小战士，参考大头、短手脚、简洁五官的低多边形比例，红衣搭配深色短发、金色装饰和腰扣。

- `BlockHero.blend`：可编辑 Blender 源文件，包含模型和预览摄影棚。
- `BlockHero-preview.png`：实际 Blender 渲染预览。
- `create_block_hero.py`：生成模型、导出 FBX 和渲染的脚本。
- `../../Assets/Resources/Characters/BlockHero.fbx`：Unity 游戏模型（不含灯光、地面或相机）。

模型包含 35 个网格部件、1540 个三角形、平色材质及 `MouthAnchor` 嘴部锚点，无骨骼或动画。部件可在 Blender 中分别调整。

俯视适配：头部整体上仰 15°，眼睛与眉毛放大约 25%，缩短刘海。嘴部锚点与头部一起变换。新版预览采用接近游戏的俯视机位，默认游戏镜头为 55°。

Unity 的 `PlayerAvatar` 自动加载 FBX、转换为 URP 材质、统一高度为 1.4 米、脚底对齐地面并隐藏旧红色方块。原 Player 的移动碰撞体和武器根节点保留。只有角色外观随移动转向；旋转双刀不受转向影响。受伤闪色作用于全身，吐火使用模型嘴部锚点。

场景中已挂载 `PlayerAvatar`，编辑器和运行时都能显示模型；首次导入 FBX 完成后自动出现。镜头、灯光、武器和场地不包含在模型资源内。

重新生成：

```powershell
& 'C:/Oracle/softs/blender-5.2.1-windows-x64/blender.exe' --background --threads 4 --python 'C:/Oracle/projects/Survivors3D/ArtSource/Player/create_block_hero.py'
```

已完成 Blender 导出、实际渲染检查及 Unity 程序集编译检查。未完成 Unity Play Mode 实机验证。
