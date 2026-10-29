using UnityEngine;
using UnityEngine.InputSystem;
internal sealed class BattleUI
{
    private readonly GameUI ui;
    private readonly Vector2[] benefitScroll=new Vector2[4];
    private readonly string[] benefitText=new string[4];
    internal BattleUI(GameUI ui) { this.ui=ui; }
    internal void DrawShop(float width, float height)
    {
        if(ui.flow.replacementOffer>=0) { DrawWeaponReplacement(width,height); return; }
        GameUI.Panel(new Rect(0,0,width,height),new Color(0.025f,0.025f,0.02f,0.84f));
        bool runComplete = ui.session.State == WaveSession.Phase.Victory;
        float margin = 24, sideWidth = 224;
        float contentWidth = width - sideWidth - margin * 3;
        float top = Mathf.Max(150, height * 0.32f);
        float cardHeight = 290;
        ui.ShopText(new Rect(margin,22,250,24),$"WAVE {ui.session.Wave} COMPLETE",18,Color.white,true);
        GameUI.Panel(new Rect(margin,57,186,18),new Color(0.21f,0.16f,0.14f));
        GameUI.Panel(new Rect(margin,57,186*ui.view.Health/100f,18),new Color(0.8f,0.13f,0.12f));
        ui.ShopText(new Rect(margin,56,186,20),$"{ui.view.Health} / 100 HP",13,Color.white,true,TextAnchor.MiddleCenter);
        ui.ShopText(new Rect(margin,85,250,38),$"{ui.equipment.Materials}  MATERIALS",24,GameUI.ShopGreen,true);
        ui.ShopText(new Rect(margin,top-86,contentWidth,45),runComplete ? (ui.session.IsBossWave?"FOREST STONE KING DEFEATED":"RUN COMPLETE") : "UPGRADE YOUR LOADOUT",30,Color.white,true,TextAnchor.MiddleCenter);
        string result=GameLanguage.Chinese?$"存活 {ui.survivalTime/60:0.0} 分钟 · 击杀 {ui.kills} · 分数 {ui.Score}":$"{ui.survivalTime/60:0.0} MIN SURVIVED · {ui.kills} KILLS · {ui.Score} SCORE";
        ui.ShopText(new Rect(margin,top-42,contentWidth,26),runComplete ? result : "Spend materials. Build your next wave.",15,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
        int count = Mathf.Max(1,ui.shop.Count);
        float cardWidth = (contentWidth - 12*(count-1))/count;
        for (int i=0;i<ui.shop.Count;i++)
        {
            Rect card=new Rect(margin+i*(cardWidth+12),top,cardWidth,cardHeight);
            int item=ui.shop.Item(i);
            if(item<0)
            {
                GameUI.Panel(card,new Color(0.05f,0.06f,0.04f));
                ui.ShopText(card,"NO MORE UPGRADES",16,GameUI.ShopMuted,true,TextAnchor.MiddleCenter);
                continue;
            }
            int level=ui.equipment.Level(item);
            bool sold = ui.shop.Sold(i), max = level >= ui.equipment.MaxLevel(item);
            bool available = !runComplete && !sold && !max && ui.equipment.Materials >= ui.shop.Price(i);
            int weaponId = WeaponCatalog.WeaponId(item);
            bool isEvo = weaponId >= 0 && ui.equipment.CanEvolve(weaponId);
            Color border = isEvo ? new Color(1f, 0.82f, 0.2f, 0.95f) : ui.shopSelection == i ? GameUI.ShopGreen : new Color(0.25f,0.26f,0.22f);
            GameUI.Panel(new Rect(card.x-2,card.y-2,card.width+4,card.height+4), border);
            GameUI.Panel(card,new Color(0.035f,0.04f,0.033f,0.98f));
            ui.UpgradeIcon(new Rect(card.x+12,card.y+14,64,64),item);
            ui.ShopText(new Rect(card.x+12,card.y+82,card.width-24,40),ui.EquipmentName(item),16,isEvo?new Color(1f,.88f,.3f):Color.white,true);
            string tierText = isEvo ? (GameLanguage.Chinese ? "【超武进化】" : "[EVOLUTION]") : WeaponCatalog.Passive(item) ? $"STACK {level}/{ui.equipment.MaxLevel(item)}" : item>=3 ? (level<0?"NEW WEAPON":$"TIER {level+1} > {Mathf.Min(level+2,ui.equipment.MaxLevel(item)+1)}") : $"MOD {level}/{ui.equipment.MaxLevel(item)}";
            ui.ShopText(new Rect(card.x+85,card.y+22,card.width-94,50), tierText, 12, isEvo ? new Color(1f, 0.85f, 0.15f) : GameUI.ShopGreen, true);
            string benefit=sold ? "Purchased\n\nYour stats have been updated." : max ? "Maximum upgrade reached." : ui.UpgradeBenefit(item);
            if(benefitText[i]!=benefit) { benefitText[i]=benefit; benefitScroll[i]=Vector2.zero; }
            Rect viewport=new Rect(card.x+14,card.y+124,card.width-28,82);
            var style=new GUIStyle(GUI.skin.label) {fontSize=12,wordWrap=true,padding=new RectOffset(0,0,0,0)};
            float textWidth=viewport.width-18;
            float textHeight=style.CalcHeight(new GUIContent(GameLanguage.Text(benefit)),textWidth);
            benefitScroll[i]=GUI.BeginScrollView(viewport,benefitScroll[i],new Rect(0,0,textWidth,Mathf.Max(viewport.height,textHeight)));
            ui.ShopText(new Rect(0,0,textWidth,textHeight),benefit,12,sold || max ? GameUI.ShopMuted : isEvo ? new Color(1f,.92f,.55f) : GameUI.ShopGreen);
            GUI.EndScrollView();
            string label = runComplete ? "RUN COMPLETE" : sold ? "PURCHASED" : max ? "MAX LEVEL" : isEvo ? (available ? (GameLanguage.Chinese ? $"超武进阶  /  {ui.shop.Price(i)}" : $"EVOLVE  /  {ui.shop.Price(i)}") : $"NEED {ui.shop.Price(i)}") : available ? $"BUY  /  {ui.shop.Price(i)}" : $"NEED {ui.shop.Price(i)}";
            bool enabled=GUI.enabled; GUI.enabled=enabled && !runComplete && !sold && !max;
            if(GameUI.UiButton(new Rect(card.x+12,card.yMax-76,card.width-24,22),ui.shop.Locked(i)?"LOCKED / UNLOCK":"LOCK [L / X]")) { ui.shopSelection=i; ui.flow.ToggleLock(i); }
            GUI.enabled=enabled;
            if (ui.ShopButton(new Rect(card.x+12,card.yMax-46,card.width-24,34),label,i,available,isEvo)) { ui.shopSelection=i; ui.flow.BuyEquipment(i); }
        }
        if(ui.shop.Count == 0) ui.ShopText(new Rect(margin,top,contentWidth,cardHeight),"ALL UPGRADES MAXED\nReady for the next wave.",24,GameUI.ShopGreen,true,TextAnchor.MiddleCenter);
        float buttonWidth = Mathf.Min(270,(contentWidth-16)/2);
        float bx = margin+(contentWidth-buttonWidth*2-16)/2;
        if (ui.ShopButton(new Rect(bx,top+cardHeight+28,buttonWidth,44),$"REFRESH  /  {ui.shop.RefreshCost} MATERIALS",ui.shop.Count,!runComplete && ui.shop.CanRefresh)) ui.flow.RefreshShop();
        if (ui.ShopButton(new Rect(bx+buttonWidth+16,top+cardHeight+28,buttonWidth,44),runComplete ? "NEW RUN  >" : $"NEXT WAVE  {ui.session.Wave+1}  >",ui.shop.Count+1,true,true))
        { ui.shopSelection=ui.shop.Count+1; ui.flow.ConfirmPhase(); }
        ui.ShopText(new Rect(margin,top+cardHeight+86,contentWidth,35),"ARROWS / STICK: SELECT   ENTER / A: BUY   L / X: LOCK   TAB / VIEW: CODEX",12,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
        if(GameUI.UiButton(new Rect(width-sideWidth-margin,24,sideWidth,32),"ITEM LIBRARY [TAB / VIEW]")) ui.ToggleCodex();
        float sx = width-margin-sideWidth;
        GameUI.Panel(new Rect(sx,124,sideWidth,height-148),new Color(0.055f,0.06f,0.05f,0.96f));
        ui.ShopText(new Rect(sx+18,142,sideWidth-36,30),"YOUR STATS",22,Color.white,true);
        ui.ShopText(new Rect(sx+18,179,sideWidth-36,22),"LOADOUT   /   3 SLOTS",12,GameUI.ShopGreen,true);
        float row = 218;
        ui.ShopStat(sx,ref row,"Health",$"{ui.view.Health} / 100");
        ui.ShopStat(sx,ref row,"Move speed",$"+{(ui.equipment.SpeedMultiplier-1)*100:0}%");
        foreach(int id in ui.EquippedWeapons())
        {
            string label = ui.equipment.IsEvolved(id) ? (WeaponCatalog.EvolvedName(id) + " / EVO") : (GameLanguage.Text(WeaponCatalog.Names[id]) + " / L" + ui.equipment.Weapon(id).Level);
            ui.ShopStat(sx,ref row,label,"",true);
            ui.ShopStat(sx,ref row,"Damage",ui.view.WeaponDamage(id).ToString());
            ui.ShopStat(sx,ref row,"Range",ui.view.WeaponRange(id).ToString("0.0"));
        }
        ui.ShopStat(sx,ref row,GameLanguage.Chinese?"武器槽":"Equipped",ui.equipment.EquippedCount+" / 3");
    }

    internal void DrawGameUI()
    {
        
        if (ui.titleStyle == null)
        {
            ui.titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = false };
            ui.labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = false };
            ui.centerStyle = new GUIStyle(ui.titleStyle) { alignment = TextAnchor.MiddleCenter };
            ui.stickStyle = new GUIStyle(ui.labelStyle) { fontSize = 13 };
        }
        float scale = PlaygroundCamera.HudScale;
        Matrix4x4 original = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        if(ui.flow.Paused) { DrawPause(width,height); GUI.matrix=original; return; }
        if(ui.flow.frontScreen!=0) { ui.DrawFrontMenu(width,height); GUI.matrix=original; return; }
        if(ui.flow.choosingStarter || ui.codexOpen)
        {
            if(ui.codexOpen) ui.DrawCodex(width,height); else ui.DrawStarter(width,height);
            GUI.matrix=original; return;
        }
        if (ui.flow.ShowingShop)
        {
            DrawShop(width, height);
            GUI.matrix = original;
            return;
        }
        if(ui.session.State==WaveSession.Phase.Combat)
        {
            if(GameUI.UiButton(new Rect(width-150,78,136,28),GameLanguage.Chinese?"暂停 [Esc / Start]":"PAUSE [Esc / Start]")) ui.flow.TogglePause();
            ui.ShopText(new Rect(width/2-150,72,300,22),GameLanguage.Chinese?"鼠标右键拖动 / 右摇杆：旋转视角":"RMB drag / right stick: rotate camera",12,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
        }
        DrawFloatingScores(scale);
        Color hudBackground = new Color(0.025f, 0.04f, 0.06f, 0.72f);
        GameUI.Panel(new Rect(14f, 14f, 200f, 54f), hudBackground);
        GameUI.HudLabel(new Rect(24f, 19f, 180f, 23f), $"HP  {ui.view.Health} / 100", ui.labelStyle);
        GameUI.Panel(new Rect(24f, 48f, 180f, 6f), new Color(0.25f, 0.27f, 0.3f));
        GameUI.Panel(new Rect(24f, 48f, 180f * ui.view.Health / 100f, 6f), new Color(0.95f, 0.18f, 0.24f));

        int seconds = Mathf.CeilToInt((float)ui.session.Remaining);
        if(ui.view.BossActive && ui.session.State==WaveSession.Phase.Combat) DrawForestBossBar(width);
        else
        {
            GameUI.Panel(new Rect(width / 2f - 90f, 14f, 180f, 36f), hudBackground);
            GameUI.HudLabel(new Rect(width / 2f - 78f, 20f, 160f, 24f),
                ui.session.IsBossWave?$"WAVE {ui.session.Wave} / BOSS":$"WAVE {ui.session.Wave}/{(ui.session.Endless?"∞":ui.session.FinalWave.ToString())}   {seconds / 60:00}:{seconds % 60:00}", ui.labelStyle);
        }

        GameUI.Panel(new Rect(width - 190f, 14f, 176f, 58f), hudBackground);
        GameUI.HudLabel(new Rect(width - 178f, 19f, 160f, 24f), $"MATERIALS {ui.equipment.Materials}", ui.labelStyle);
        DrawScoreHUD(width);

        var equipped=ui.EquippedWeapons();
        for(int slot=0;slot<RunEquipment.WeaponSlotCount;slot++)
        {
            Rect frame=new Rect(14+slot*54,height-74,48,60);
            bool occupied=slot<equipped.Count;
            int weapon=occupied?equipped[slot]:-1;
            bool evolved=occupied && ui.equipment.IsEvolved(weapon);
            Color border=evolved?new Color(1f,.85f,.2f,.9f):occupied?new Color(.36f,.46f,.32f,.4f):new Color(.22f,.27f,.29f,.25f);
            GameUI.Panel(frame,new Color(.025f,.04f,.06f,.28f));
            GameUI.Panel(new Rect(frame.x,frame.y,48,1),border);
            GameUI.Panel(new Rect(frame.x,frame.yMax-1,48,1),border);
            GameUI.Panel(new Rect(frame.x,frame.y,1,60),border);
            GameUI.Panel(new Rect(frame.xMax-1,frame.y,1,60),border);
            if(occupied)
            {
                // Scale the existing icon artwork, including its fixed-size details.
                Matrix4x4 iconMatrix=GUI.matrix;
                GUI.matrix=iconMatrix*Matrix4x4.TRS(new Vector3(frame.x+3,frame.y+3,0),Quaternion.identity,new Vector3(.625f,.625f,1));
                ui.UpgradeIcon(new Rect(0,0,68,64),WeaponCatalog.ShopId(weapon),false);
                GUI.matrix=iconMatrix;
                string lvlText=evolved?"EVO":"L"+ui.equipment.Weapon(weapon).Level;
                Color lvlColor=evolved?new Color(1f,.85f,.2f):new Color(.52f,.95f,.30f,.85f);
                ui.ShopText(new Rect(frame.x+3,frame.y+43,42,15),lvlText,11,lvlColor,true,TextAnchor.MiddleCenter);
            }
            else
            {
                Color muted=new Color(.38f,.44f,.45f,.35f);
                GameUI.Panel(new Rect(frame.center.x-6,frame.center.y,12,1),muted);
                GameUI.Panel(new Rect(frame.center.x,frame.center.y-6,1,12),muted);
            }
        }
        DrawStickHUD(new Vector2(width - 48f, height - 48f));
        if(ui.session.State==WaveSession.Phase.Combat) DrawDashHUD(height);
        if(ui.flow.ShowingVictory) { DrawVictoryLoot(width,height); GUI.matrix=original; return; }
        if (ui.session.State != WaveSession.Phase.Combat)
        {
            bool resting = ui.session.State == WaveSession.Phase.Rest;
            bool victory = ui.session.State == WaveSession.Phase.Victory;
            GameUI.Panel(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, 0.65f));
            if (resting) DrawShop(width, height);
            else
            {
            string title = resting ? $"WAVE {ui.session.Wave} COMPLETE / REST" : victory ? $"VICTORY / ALL {ui.session.FinalWave} WAVES CLEARED" : "YOU DIED";
            GameUI.HudLabel(new Rect(width / 2 - 300, height / 2 - 90, 600, 60), title, ui.centerStyle);
            GameUI.HudLabel(new Rect(width / 2 - 300, height / 2 - 30, 600, 40),
                resting ? $"HP {ui.view.Health} / 100   /   {ui.kills} kills" : $"{ui.survivalTime:0.0}s survived   /   {ui.kills} kills", ui.centerStyle);
            GameUI.HudLabel(new Rect(width / 2 - 300, height / 2 + 6, 600, 32),
                resting ? "Take a break. Continue when ready." : "ENTER / R / GAMEPAD A / CROSS", ui.centerStyle);
            string button = resting ? $"START WAVE {ui.session.Wave + 1}  [ENTER / A / CROSS]" : "PLAY AGAIN  [ENTER / R / A / CROSS]";
            if (GameUI.UiButton(new Rect(width / 2 - 220, height / 2 + 60, 440, 45), button)) ui.flow.ConfirmPhase();
            }
        }
        if(GameUI.UiButton(new Rect(width-150,height-115,136,26),"ITEM CODEX [TAB]")) ui.ToggleCodex();
        GUI.matrix = original;
    }

    private void DrawDashHUD(float height)
    {
        Rect frame=new Rect(188,height-74,156,60);
        int charges=ui.view.DashCharges;
        int maxCharges=ui.view.MaxDashCharges;
        float remaining=ui.view.DashCooldownRemaining;
        bool ready=charges>0;
        Color accent=charges==maxCharges?new Color(.55f,.92f,1):ready?new Color(.7f,.95f,1):GameUI.ShopMuted;
        float flash=ui.view.DashReadyFlash/.6f;
        GameUI.Panel(frame,new Color(.025f,.04f,.06f,.72f));
        GameUI.Panel(new Rect(frame.x,frame.y,frame.width,2),Color.Lerp(accent,Color.white,flash));
        // Two stepped chevrons represent the two charges.
        for(int arrow=0;arrow<2;arrow++)
        {
            Color arrColor=arrow<charges?accent:new Color(.2f,.25f,.28f);
            for(int row=0;row<5;row++)
                GameUI.Panel(new Rect(frame.x+9+arrow*10+(2-Mathf.Abs(row-2))*3,frame.y+13+row*4,5,4),arrColor);
        }
        string state=ui.view.IsDashing?(GameLanguage.Chinese?"冲刺中":"DASHING"):
            charges==maxCharges?(GameLanguage.Chinese?"短冲刺 · 2次就绪":"DASH · 2/2 READY"):
            charges>0?(GameLanguage.Chinese?$"短冲刺 · {charges}/{maxCharges} [+{remaining:0.0}s]":$"DASH · {charges}/{maxCharges} [+{remaining:0.0}s]"):
            $"{(GameLanguage.Chinese?"短冲刺":"DASH")}  {remaining:0.0}s";
        ui.ShopText(new Rect(frame.x+40,frame.y+7,112,22),state,12,accent,true);
        ui.ShopText(new Rect(frame.x+8,frame.y+31,140,18),GameLanguage.Chinese?"空格 / LB / L1":"SPACE / LB / L1",11,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);

        // Two segmented bars (width 68px each, 4px gap)
        float barWidth=68,barHeight=4,gap=4;
        for(int i=0;i<maxCharges;i++)
        {
            float bx=frame.x+8+i*(barWidth+gap);
            GameUI.Panel(new Rect(bx,frame.y+52,barWidth,barHeight),new Color(.2f,.25f,.28f));
            float fill=0;
            if(i<charges) fill=1f;
            else if(i==charges) fill=1f-Mathf.Clamp01(remaining/PlayerController.DashCooldown);
            if(fill>0)
            {
                Color segColor=i<charges?Color.Lerp(new Color(.55f,.92f,1),Color.white,flash):new Color(.4f,.75f,.9f);
                GameUI.Panel(new Rect(bx,frame.y+52,barWidth*fill,barHeight),segColor);
            }
        }
    }

    private void DrawForestBossBar(float width)
    {
        float w=Mathf.Min(420,width-448),x=(width-w)/2;
        GameUI.Panel(new Rect(x,14,w,54),new Color(.03f,.04f,.025f,.72f));
        string title=GameLanguage.Chinese?"林地石王":"FOREST STONE KING";
        if(ui.view.BossEnraged) title+=GameLanguage.Chinese?" · 狂怒":" · ENRAGED";
        ui.ShopText(new Rect(x+10,17,w-20,22),title,14,new Color(1,.65f,.22f),true,TextAnchor.MiddleCenter);
        GameUI.Panel(new Rect(x+10,42,w-20,6),new Color(.20f,.17f,.12f));
        float fraction=Mathf.Clamp01((float)ui.view.BossHealth/Mathf.Max(1,ui.view.BossMaxHealth));
        GameUI.Panel(new Rect(x+10,42,(w-20)*fraction,6),ui.view.BossEnraged?new Color(1,.30f,.06f):new Color(.95f,.61f,.15f));
        string wave=GameLanguage.Chinese?$"第 {ui.session.Wave} 波":$"WAVE {ui.session.Wave}";
        ui.ShopText(new Rect(x+10,49,w-20,17),wave+" · "+GameLanguage.Text(ui.view.BossMove)+$"   {ui.view.BossHealth} / {ui.view.BossMaxHealth}",11,Color.white,false,TextAnchor.MiddleCenter);
    }

    internal void DrawStickHUD(Vector2 center)
    {
        if (ui.stickCircle == null)
        {
            ui.stickCircle = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "HUD Stick Circle", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    pixels[y * 64 + x] = new Color(1f, 1f, 1f,
                        Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))));
            ui.stickCircle.SetPixels(pixels);
            ui.stickCircle.Apply(false, true);
        }

        // Share the player's processed action so keyboard and analog movement match the HUD.
        Vector2 stick = ui.view.MoveInput;
        Color accent = stick.sqrMagnitude > .001f ? new Color(0.3f, 0.85f, 1f) : new Color(0.4f, 0.45f, 0.5f);
        DrawStickCircle(center, 31f, new Color(0.3f, 0.4f, 0.48f));
        DrawStickCircle(center, 29f, new Color(0.075f, 0.11f, 0.15f,.85f));
        GameUI.Panel(new Rect(center.x - 19f, center.y, 38f, 1f), new Color(0.2f, 0.28f, 0.34f));
        GameUI.Panel(new Rect(center.x, center.y - 19f, 1f, 38f), new Color(0.2f, 0.28f, 0.34f));
        Vector2 knob = center + new Vector2(stick.x, -stick.y) * 20f;
        DrawStickCircle(knob, 6f, accent);
    }

    private void DrawPause(float width,float height)
    {
        GameUI.Panel(new Rect(0,0,width,height),new Color(.025f,.035f,.025f,.88f));
        float x=width/2-190,y=height/2-160;
        GameUI.Panel(new Rect(x,y,380,320),new Color(.055f,.075f,.045f,.98f));
        ui.ShopText(new Rect(x+20,y+20,340,42),GameLanguage.Chinese?"游戏已暂停":"PAUSED",28,Color.white,true,TextAnchor.MiddleCenter);
        string[] labels=GameLanguage.Chinese?new[] {"返回游戏 [Esc]","停止本局 / 重新选武器","返回主菜单"}:new[] {"RESUME [Esc]","STOP RUN / CHOOSE WEAPON","RETURN TO MAIN MENU"};
        for(int i=0;i<3;i++)
        {
            Rect button=new Rect(x+30,y+85+i*57,320,43);
            if(ui.pauseSelection==i) GameUI.Panel(new Rect(button.x-2,button.y-2,button.width+4,button.height+4),GameUI.ShopGreen);
            if(GameUI.UiButton(button,labels[i])) { ui.pauseSelection=i; ui.ConfirmPause(); }
        }
        ui.ShopText(new Rect(x+25,y+265,330,44),GameLanguage.Chinese?"停止或返回主菜单将结束当前这一局。":"Stopping or returning to the menu ends this run.",13,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
    }

    internal void DrawStickCircle(Vector2 center, float radius, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), ui.stickCircle);
        GUI.color = previous;
    }

    internal void DrawWeaponReplacement(float width,float height)
    {
        GameUI.Panel(new Rect(0,0,width,height),new Color(.02f,.025f,.02f,.98f));
        string incoming=GameLanguage.Text(ui.EquipmentName(ui.shop.Item(ui.flow.replacementOffer)));
        ui.ShopText(new Rect(30,70,width-60,50),GameLanguage.Chinese?"选择替换武器 → "+incoming:"Replace a weapon with "+incoming,26,GameUI.ShopGreen,true,TextAnchor.MiddleCenter);
        ui.ShopText(new Rect(30,130,width-60,65),GameLanguage.Chinese?"最多装备 3 种。旧武器及等级将被移除，不返还材料。\n确认后扣除新武器价格；取消不扣费。":"Equip up to 3. The replaced weapon and its level are lost without a refund.\nPay only on confirmation. Cancel is free.",16,Color.white,false,TextAnchor.MiddleCenter);
        var weapons=ui.EquippedWeapons(); float w=Mathf.Min(250,(width-80)/3);
        for(int i=0;i<weapons.Count;i++)
        {
            int id=weapons[i]; Rect rect=new Rect(width/2-(w*3+24)/2+i*(w+12),height/2-55,w,135);
            GameUI.Panel(rect,ui.replacementSelection==i?new Color(.3f,.45f,.2f):new Color(.12f,.15f,.1f));
            ui.ShopText(new Rect(rect.x+10,rect.y+16,w-20,50),GameLanguage.Text(WeaponCatalog.Names[id])+"  L"+ui.equipment.Weapon(id).Level,21,Color.white,true,TextAnchor.MiddleCenter);
            if(GameUI.UiButton(new Rect(rect.x+12,rect.yMax-42,w-24,30),GameLanguage.Chinese?"替换 / "+ui.shop.Price(ui.flow.replacementOffer):"REPLACE / "+ui.shop.Price(ui.flow.replacementOffer))) ui.flow.FinishReplacement(id);
            if(ui.flow.replacementOffer<0) return;
        }
        Rect cancel=new Rect(width/2-130,height/2+112,260,38);
        if(ui.replacementSelection==3) GameUI.Panel(new Rect(cancel.x-2,cancel.y-2,cancel.width+4,cancel.height+4),GameUI.ShopGreen);
        if(GameUI.UiButton(cancel,GameLanguage.Chinese?"取消 [Esc / B]":"CANCEL [Esc / B]")) ui.flow.CancelReplacement();
    }

    private void EnsureScoreStyle()
    {
        if(ui.scoreStyle == null) ui.scoreStyle = new GUIStyle(GUI.skin.label) {
            alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,
            padding=new RectOffset(0,0,0,0),clipping=TextClipping.Overflow };
    }

    private void DrawScoreHUD(float width)
    {
        Rect scoreRect = ScoreRect(width);
        float freshestAge = 999f;
        int recentKills = 0;
        for(int i = 0; i < ui.floatingScores.Count; i++)
        {
            var s = ui.floatingScores[i];
            if(s.Age < freshestAge) freshestAge = s.Age;
            if(s.Age < 0.65f) recentKills++;
        }

        if(freshestAge < 0.65f)
        {
            float pop = Mathf.Clamp01(1f - freshestAge / 0.45f);
            float yOffset = -2f * Mathf.Sin(pop * Mathf.PI);
            Color original = ui.stickStyle.normal.textColor;
            Color flashColor = new Color(.55f, 1f, .4f);
            ui.stickStyle.normal.textColor = Color.Lerp(original, flashColor, pop);
            GameUI.HudLabel(new Rect(scoreRect.x, scoreRect.y + yOffset, scoreRect.width, scoreRect.height),
                $"KILLS {ui.kills}   SCORE {ui.Score}", ui.stickStyle);
            ui.stickStyle.normal.textColor = original;

            float incT = freshestAge / 0.65f;
            float incAlpha = 1f - Mathf.SmoothStep(0f, 1f, incT);
            float incRise = 12f * (1f - (1f - incT) * (1f - incT));
            string incText = recentKills > 1 ? $"+{recentKills * 10}" : "+10";
            Rect incRect = new Rect(scoreRect.x + 126f, scoreRect.y - 1f - incRise, 38f, 18f);

            EnsureScoreStyle();
            int oldSize = ui.scoreStyle.fontSize;
            ui.scoreStyle.fontSize = 11;
            ui.scoreStyle.normal.textColor = new Color(0.02f, 0.02f, 0.02f, incAlpha * 0.85f);
            GUI.Label(new Rect(incRect.x + 1, incRect.y + 1, incRect.width, incRect.height), incText, ui.scoreStyle);
            ui.scoreStyle.normal.textColor = new Color(.55f, 1f, .35f, incAlpha);
            GUI.Label(incRect, incText, ui.scoreStyle);
            ui.scoreStyle.fontSize = oldSize;
        }
        else
        {
            GameUI.HudLabel(scoreRect, $"KILLS {ui.kills}   SCORE {ui.Score}", ui.stickStyle);
        }
    }

    internal void DrawFloatingScores(float scale)
    {
        Camera camera = ui.view.Camera != null ? ui.view.Camera.GetComponent<Camera>() : Camera.main;
        if(camera == null) return;
        EnsureScoreStyle();
        foreach(CombatController.FloatingScore dmg in ui.floatingDamage)
        {
            Vector3 screen=camera.WorldToScreenPoint(dmg.Position);
            if(screen.z<=0) continue;
            float t=dmg.Age/CombatController.DamageNumberLifetime;
            float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.55f,1,t));
            float rise=dmg.Critical?36*(1-(1-t)*(1-t)):28*(1-(1-t)*(1-t));
            float pop=dmg.Critical?Mathf.Clamp01(1-t*4f)*6f:Mathf.Clamp01(1-t*5f)*3f;
            int baseSize=dmg.Critical?22:dmg.Burn || dmg.Poison?14:16;
            ui.scoreStyle.fontSize=Mathf.RoundToInt(baseSize+pop);
            Color textColor=dmg.Critical?new Color(1f,.85f,.12f):
                dmg.Burn?new Color(1f,.45f,.1f):
                dmg.Poison?new Color(.35f,.95f,.25f):
                dmg.Ice?new Color(.45f,.85f,1f):Color.white;
            Rect rect=new Rect(screen.x/scale-40+dmg.Drift*t,(Screen.height-screen.y)/scale-16-rise,80,28);
            ui.scoreStyle.normal.textColor=new Color(0.02f,0.02f,0.02f,alpha*0.85f);
            GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),dmg.Text,ui.scoreStyle);
            textColor.a=alpha; ui.scoreStyle.normal.textColor=textColor;
            GUI.Label(rect,dmg.Text,ui.scoreStyle);
        }
    }

    internal static Rect ScoreRect(float width) => new Rect(width - 178f, 43f, 160f, 22f);

    internal void DrawVictoryLoot(float width,float height)
    {
        DrawVictoryCelebration(width,height);
        string prompt=GameLanguage.Chinese?(ui.victoryContinueRequested?"庆祝后进入商店……":"按空格 / 手柄 A 进入商店"):
            (ui.victoryContinueRequested?"Opening shop after celebration and loot...":"SPACE / GAMEPAD A: OPEN SHOP");
        ui.ShopText(new Rect(width/2-250,143,500,28),prompt,16,Color.white,true,TextAnchor.MiddleCenter);
        ui.ShopText(new Rect(width/2-300,height-84,600,26),
            GameLanguage.Chinese?$"通关奖励：{CombatController.WaveCompletionMaterials} 材料 · 清场敌人只加分":
                $"Wave reward: {CombatController.WaveCompletionMaterials} materials · Remaining enemies give score only",
            15,new Color(1f,.85f,.4f),true,TextAnchor.MiddleCenter);
        if(GameUI.UiButton(new Rect(width/2-150,height-52,300,32),GameLanguage.Chinese?"进入商店 [空格 / A]":"OPEN SHOP [SPACE / A]")) ui.flow.RequestVictoryContinue();
        Vector2 target=ScoreRect(width).center;
        foreach(CombatController.LootFlight flight in ui.lootFlights)
        {
            Vector2 start=Vector2.Scale(flight.Start,new Vector2(width,height));
            float t=Mathf.Clamp01((flight.Age-flight.Delay)/flight.Duration); t=t*t*(3-2*t);
            Vector2 control=(start+target)*.5f+Vector2.down*80;
            Vector2 at=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*target;
            GameUI.RotatedPanel(at,new Vector2(10,10),45,flight.ScoreOnly?new Color(1f,.8f,.25f):new Color(.15f,.95f,.55f));
        }
    }

    internal void DrawVictoryCelebration(float width,float height)
    {
        Camera camera=ui.view.Camera!=null?ui.view.Camera.GetComponent<Camera>():Camera.main;
        Vector2 anchor=new Vector2(width/2,height/2);
        if(camera!=null)
        {
            Vector3 point=camera.WorldToViewportPoint(ui.view.PlayerPosition+Vector3.up*2.3f);
            if(point.z>0) anchor=new Vector2(point.x*width,(1-point.y)*height);
        }
        float pop=Mathf.Clamp01((ui.victoryAge-.3f)/.3f);
        if(pop>0)
        {
            float settle=Mathf.Clamp01((ui.victoryAge-1.5f)/.4f);
            Vector2 title=Vector2.Lerp(anchor,new Vector2(width/2,110),Mathf.SmoothStep(0,1,settle));
            title.x=Mathf.Clamp(title.x,130,width-130);
            title.y=Mathf.Clamp(title.y,45,height-100);
            int size=Mathf.RoundToInt(36*(pop+Mathf.Sin(pop*Mathf.PI)*.35f));
            ui.ShopText(new Rect(title.x-160,title.y-30,320,60),GameLanguage.Chinese?"胜利！":"VICTORY!",
                size,new Color(1f,.8f,.18f,pop),true,TextAnchor.MiddleCenter);
        }
        float age=ui.victoryAge-1f;
        if(age<0 || age>1.4f) return;
        for(int i=0;i<28;i++)
        {
            float angle=i*2.39996f;
            float speed=65+(i%5)*18;
            Vector2 at=anchor+new Vector2(Mathf.Cos(angle)*speed*age,
                -Mathf.Abs(Mathf.Sin(angle))*speed*age+95*age*age);
            GameUI.RotatedPanel(at,new Vector2(6,10),i*37+age*(i%2==0?220:-180),
                new Color(1f,i%3==0?.95f:.68f,.15f,1-age/1.4f));
        }
    }
}
