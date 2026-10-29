using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

internal sealed class GameUI : System.IDisposable
{
    private readonly MenuUI menuui;
    private readonly BattleUI battleui;
    internal readonly GameFlowController flow;
    internal readonly RunState run;
    internal readonly ICombatView view;
    internal RunEquipment equipment=>run.Equipment;
    internal WaveShop shop=>run.Shop;
    internal WaveSession session=>run.Session;
    internal int kills=>run.Kills;
    internal int Score=>run.Score;
    internal float survivalTime=>run.SurvivalTime;
    internal float victoryAge=>view.VictoryAge;
    internal bool victoryContinueRequested=>view.VictoryContinueRequested;
    internal IReadOnlyList<CombatController.FloatingScore> floatingScores=>view.FloatingScores;
    internal IReadOnlyList<CombatController.FloatingScore> floatingDamage=>view.FloatingDamage;
    internal IReadOnlyList<CombatController.LootFlight> lootFlights=>view.LootFlights;
    internal const float ScoreLifetime=.9f;
    internal List<int> EquippedWeapons() { var list=new List<int>(); for(int i=0;i<WeaponCatalog.Count;i++) if(equipment.HasWeapon(i)) list.Add(i); return list; }
    internal GameUI(GameFlowController flow,RunState run,ICombatView view)
    {
        this.flow=flow; this.run=run; this.view=view;
        menuui=new MenuUI(this); battleui=new BattleUI(this);
        restartAction=InputSystem.actions.FindAction("Player/Restart",true).Clone();
        shopNavigate=InputSystem.actions.FindAction("UI/Navigate",true).Clone();
        CreateMenuActions();
    }
    internal void Enable() { restartAction.Enable(); shopNavigate.Enable(); lockCardAction.Enable(); codexAction.Enable(); backMenuAction.Enable(); languageAction.Enable(); victoryConfirmAction.Enable(); }
    internal void Disable() { restartAction.Disable(); shopNavigate.Disable(); lockCardAction.Disable(); codexAction.Disable(); backMenuAction.Disable(); languageAction.Disable(); victoryConfirmAction.Disable(); if(codexOpen) ToggleCodex(); flow.ResumeGame(); }
    public void Dispose() { DisposeMenuActions(); restartAction.Dispose(); shopNavigate.Dispose(); if(stickCircle!=null) UnityEngine.Object.Destroy(stickCircle); if(localizedSkin!=null) UnityEngine.Object.Destroy(localizedSkin); }

    internal int pauseSelection;
    internal const int StarterColumns=4;
    internal int shopSelection=3,starterSelection,codexSelection,frontSelection,replacementSelection;
    internal float navigateAt;
    internal bool codexOpen;
    internal float codexPreviousScale=1;
    internal InputAction restartAction,shopNavigate,victoryConfirmAction;
    internal InputAction lockCardAction,codexAction,backMenuAction,languageAction;
    internal GUIStyle titleStyle,labelStyle,centerStyle,stickStyle,scoreStyle;
    internal Texture2D stickCircle;
    internal GUISkin localizedSkin;
    internal static readonly Color ShopGreen=new Color(.52f,.95f,.30f),ShopMuted=new Color(.64f,.65f,.61f);
    internal static readonly string[] UpgradeNames={"Rapid Magazine","Blade Reach Mod","Running Boots","Gun","Orbiting Blades","Fire Breath"};
    internal void CreateMenuActions()
    {
        lockCardAction=new InputAction("Lock card",InputActionType.Button);
        lockCardAction.AddBinding("<Keyboard>/l"); lockCardAction.AddBinding("<Gamepad>/buttonWest");
        codexAction=new InputAction("Item codex",InputActionType.Button);
        codexAction.AddBinding("<Keyboard>/tab"); codexAction.AddBinding("<Gamepad>/select");
        backMenuAction=new InputAction("Close codex",InputActionType.Button);
        backMenuAction.AddBinding("<Keyboard>/escape"); backMenuAction.AddBinding("<Gamepad>/buttonEast");
        languageAction=new InputAction("Switch language",InputActionType.Button);
        languageAction.AddBinding("<Keyboard>/f2");
        victoryConfirmAction=new InputAction("Victory continue",InputActionType.Button);
        victoryConfirmAction.AddBinding("<Keyboard>/space"); victoryConfirmAction.AddBinding("<Gamepad>/buttonSouth");
    }

    internal void DisposeMenuActions()
    {
        victoryConfirmAction?.Dispose();
        lockCardAction?.Dispose(); codexAction?.Dispose(); backMenuAction?.Dispose(); languageAction?.Dispose();
        if(codexOpen) Time.timeScale=codexPreviousScale;
    }

    internal void ToggleCodex()
    {
        GameAudio.Play(codexOpen?AudioCue.Back:AudioCue.Confirm);
        if(codexOpen) { codexOpen=false; Time.timeScale=codexPreviousScale; }
        else { codexOpen=true; codexPreviousScale=Time.timeScale; Time.timeScale=0; }
        flow.MarkInputHandled(); navigateAt=Time.unscaledTime+0.2f;
    }

    internal static int NextStarter(int selection,Vector2 move)
    {
        if(Mathf.Abs(move.x)>Mathf.Abs(move.y)) return (selection+(move.x>0?1:-1)+WeaponCatalog.StarterCount)%WeaponCatalog.StarterCount;
        int column=selection%StarterColumns;
        int rows=(WeaponCatalog.StarterCount+StarterColumns-1)/StarterColumns;
        int row=selection/StarterColumns;
        // Skip empty cells in the final row while retaining the selected column.
        do { row=(row+(move.y<0?1:-1)+rows)%rows; } while(row*StarterColumns+column>=WeaponCatalog.StarterCount);
        return row*StarterColumns+column;
    }

    internal bool UpdateMenus()
    {
        if(flow.Paused)
        {
            if(backMenuAction.WasPressedThisFrame() || (Gamepad.current!=null && Gamepad.current.startButton.wasPressedThisFrame)) { flow.ResumeGame(); return true; }
            float move=shopNavigate.ReadValue<Vector2>().y;
            if(Mathf.Abs(move)<.5f) navigateAt=0;
            else if(Time.unscaledTime>=navigateAt) { pauseSelection=(pauseSelection+(move<0?1:2))%3; GameAudio.Play(AudioCue.Select); navigateAt=Time.unscaledTime+.2f; }
            if(Time.frameCount>flow.phaseChangedFrame && restartAction.WasPressedThisFrame()) ConfirmPause();
            return true;
        }
        if(!codexOpen && !flow.choosingStarter && flow.frontScreen==0 && session.State==WaveSession.Phase.Combat &&
            (backMenuAction.WasPressedThisFrame() || (Gamepad.current!=null && Gamepad.current.startButton.wasPressedThisFrame)))
        { flow.TogglePause(); return true; }
        if(languageAction!=null && languageAction.WasPressedThisFrame()) GameLanguage.Toggle();
        if(UpdateFrontMenu()) return true;
        if(flow.replacementOffer>=0) { UpdateWeaponReplacement(); return true; }
        if(codexAction!=null && codexAction.WasPressedThisFrame()) { ToggleCodex(); return true; }
        if(codexOpen)
        {
            if(backMenuAction.WasPressedThisFrame()) { ToggleCodex(); return true; }
            float vertical=shopNavigate.ReadValue<Vector2>().y;
            if(Mathf.Abs(vertical)<0.5f) navigateAt=0;
            else if(Time.unscaledTime>=navigateAt) { codexSelection=(codexSelection+(vertical<0?1:ItemLibrary.Count-1))%ItemLibrary.Count; GameAudio.Play(AudioCue.Select); navigateAt=Time.unscaledTime+0.17f; }
            return true;
        }
        if(flow.choosingStarter)
        {
            if(backMenuAction.WasPressedThisFrame()) { flow.OpenMainMenu(); return true; }
            Vector2 move=shopNavigate.ReadValue<Vector2>(); float horizontal=Mathf.Abs(move.x)>Mathf.Abs(move.y)?move.x:-move.y*4;
            if(Mathf.Abs(horizontal)<0.5f) navigateAt=0;
            else if(Time.unscaledTime>=navigateAt) { starterSelection=NextStarter(starterSelection,move); GameAudio.Play(AudioCue.Select); navigateAt=Time.unscaledTime+0.2f; }
            shopSelection=starterSelection;
            if(Time.frameCount>flow.phaseChangedFrame && restartAction.WasPressedThisFrame()) flow.BeginRun(starterSelection);
            return true;
        }
        if(flow.ShowingShop && session.State==WaveSession.Phase.Rest && lockCardAction.WasPressedThisFrame() && shopSelection<shop.Count)
            flow.ToggleLock(shopSelection);
        return false;
    }

    internal void ConfirmPause()
    {
        if(pauseSelection==0) flow.ResumeGame();
        else flow.StopRun(pauseSelection==2);
    }

    internal void ConfirmFrontMenu()
    {
        GameAudio.Play(AudioCue.Confirm);
        if(flow.frontScreen==1)
        {
            if(frontSelection==0) flow.NewGame();
            else if(frontSelection==1) flow.OpenSettings();
            else flow.QuitGame();
        }
        else if(flow.frontScreen==2)
        {
            if(frontSelection==0 && view.Camera!=null) view.Camera.ThirdPerson=!view.Camera.ThirdPerson;
            else if(frontSelection==3) GameLanguage.Toggle();
            else if(frontSelection==4 && view.Camera!=null) view.Camera.ResetCurrentView();
            else if(frontSelection==5) flow.CloseSettings();
        }
    }

    internal bool UpdateFrontMenu()
    {
        if(flow.frontScreen==0) return false;
        if(backMenuAction.WasPressedThisFrame() && flow.frontScreen==2) { flow.CloseSettings(); return true; }
        Vector2 move=shopNavigate.ReadValue<Vector2>();
        if(Mathf.Abs(move.y)<0.5f) navigateAt=0;
        else if(Time.unscaledTime>=navigateAt)
        {
            int count=flow.frontScreen==1?3:9;
            frontSelection=(frontSelection+(move.y<0?1:count-1))%count; GameAudio.Play(AudioCue.Select);
            navigateAt=Time.unscaledTime+0.2f;
        }
        if(flow.frontScreen==2 && view.Camera!=null && Mathf.Abs(move.x)>0.4f)
        {
            if(frontSelection==1) view.Camera.Tilt+=move.x*20f*Time.unscaledDeltaTime;
            if(frontSelection==2) view.Camera.Distance+=move.x*5f*Time.unscaledDeltaTime;
        }
        if(flow.frontScreen==2 && frontSelection>=6 && GameAudio.Current!=null && Mathf.Abs(move.x)>.4f)
        {
            var audio=GameAudio.Current; int channel=frontSelection-6;
            float value=channel==0?audio.MasterVolume:channel==1?audio.EffectsVolume:audio.MusicVolume;
            audio.SetVolume(channel,value+move.x*.5f*Time.unscaledDeltaTime);
            if(channel!=2) GameAudio.Play(AudioCue.Select);
        }
        if(Time.frameCount>flow.phaseChangedFrame && restartAction.WasPressedThisFrame()) ConfirmFrontMenu();
        return true;
    }

    internal string EquipmentName(int item)
    {
        if(WeaponCatalog.Passive(item)) return ItemLibrary.At(item).Name;
        int id=WeaponCatalog.WeaponId(item);
        if(id>=0) return (equipment.IsEvolved(id) || equipment.CanEvolve(id))?WeaponCatalog.EvolvedName(id):WeaponCatalog.Names[id];
        return UpgradeNames[item];
    }

    internal void ShopText(Rect rect, string text, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = alignment, wordWrap = true, padding = new RectOffset(0,0,0,0) };
        style.normal.textColor = color;
        GUI.Label(rect, GameLanguage.Text(text), style);
    }

    internal bool ShopButton(Rect rect, string text, int index, bool available, bool primary = false)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        bool selected = shopSelection == index;
        Color background = primary ? new Color(0.34f,0.58f,0.18f) : new Color(0.19f,0.20f,0.18f);
        if (selected || hover) background = available ? new Color(0.80f,0.84f,0.75f) : new Color(0.27f,0.28f,0.25f);
        Panel(rect, background);
        ShopText(rect, text, 16, !available ? ShopMuted : selected || hover ? Color.black : Color.white, true, TextAnchor.MiddleCenter);
        if (selected) Panel(new Rect(rect.x,rect.yMax-3,rect.width,3), ShopGreen);
        bool old = GUI.enabled; GUI.enabled = old && available;
        bool pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        GUI.enabled = old;
        if(pressed) GameAudio.Play(AudioCue.Click);
        return pressed;
    }

    internal void UpgradeIcon(Rect area, int item, bool background=true)
    {
        if(background) Panel(area, new Color(0.15f,0.17f,0.14f));
        float x = area.x + 10, y = area.y + 10;
        Color steel = new Color(0.86f,0.90f,0.82f);
        if(item>=22 && WeaponCatalog.WeaponId(item)>=0)
        {
            int id=WeaponCatalog.WeaponId(item);
            Color color=id==5?new Color(.6f,.63f,.66f):id==6?Color.cyan:id==7?new Color(1,.45f,.1f):steel;
            if(id==11)
            {
                Color wood=new Color(.48f,.27f,.12f);
                foreach(float side in new[]{.2f,.7f}) Panel(new Rect(area.x+area.width*side,area.y+area.height*.18f,area.width*.1f,area.height*.65f),wood);
                Panel(new Rect(area.x+area.width*.13f,area.y+area.height*.16f,area.width*.74f,area.height*.12f),wood);
                Panel(new Rect(area.x+area.width*.13f,area.y+area.height*.8f,area.width*.74f,area.height*.09f),wood);
                Panel(new Rect(area.x+area.width*.47f,area.y+area.height*.28f,area.width*.06f,area.height*.15f),steel);
                Panel(new Rect(area.x+area.width*.36f,area.y+area.height*.43f,area.width*.28f,area.height*.28f),new Color(.65f,.93f,.16f));
                return;
            }
            if(id==10) { GUI.DrawTexture(area,PoisonBackpackIcon.Get(),ScaleMode.ScaleToFit,true); return; }
            if(id==9)
            {
                for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector2 center=area.center+new Vector2(sign*area.width*.24f,-area.height*.04f);
                    RotatedPanel(center,new Vector2(area.width*.38f,area.height*.23f),sign*-25,steel);
                    RotatedPanel(center+new Vector2(0,area.height*.055f),new Vector2(area.width*.34f,area.height*.035f),sign*-25,Color.white);
                    RotatedPanel(area.center+new Vector2(sign*area.width*.12f,area.height*.04f),new Vector2(area.width*.07f,area.height*.3f),sign*-25,new Color(.2f,.22f,.23f));
                }
                Panel(new Rect(area.center.x-area.width*.1f,area.center.y,area.width*.2f,area.height*.2f),new Color(.53f,.29f,.13f));
                for(int sign=-1;sign<=1;sign+=2) Panel(new Rect(area.center.x+sign*area.width*.045f-area.width*.02f,area.center.y,area.width*.04f,area.height*.2f),new Color(.8f,.64f,.38f));
                return;
            }
            if(id==8)
            {
                RotatedPanel(area.center,new Vector2(area.width*.5f,area.height*.5f),30,new Color(1,.35f,.03f));
                RotatedPanel(area.center,new Vector2(area.width*.3f,area.height*.3f),45,new Color(1,.9f,.2f));
                for(int n=1;n<=3;n++) RotatedPanel(area.center+new Vector2(-n*6,n*6),Vector2.one*(8-n),35,new Color(1,.5f,.05f));
                return;
            }
            if(id==6)
            {
                Panel(new Rect(area.x+5,area.yMax-15,area.width-10,9),new Color(.12f,.32f,.48f));
                for(int i=0;i<3;i++)
                {
                    float h=area.height*(i==1?.55f:.38f),crystalX=area.center.x+(i-1)*area.width*.23f;
                    RotatedPanel(new Vector2(crystalX,area.yMax-13-h*.5f),new Vector2(area.width*.16f,h),(i-1)*-15,new Color(.45f,.85f,1));
                    Panel(new Rect(crystalX-1,area.yMax-13-h*.8f,2,h*.6f),new Color(.85f,.98f,1));
                }
                return;
            }
            Panel(new Rect(area.center.x-3,area.y+8,6,area.height-16),color);
            if(id==4)
            {
                Panel(new Rect(area.center.x-5,area.y+10,10,area.height-16),new Color(.5f,.26f,.1f));
                for(int sign=-1;sign<=1;sign+=2)
                    RotatedPanel(new Vector2(area.center.x+sign*area.width*.23f,area.y+area.height*.4f),new Vector2(area.width*.46f,7),sign*-15,new Color(.42f,.45f,.47f));
                for(int n=-1;n<=1;n+=2) Panel(new Rect(area.center.x+n*5-1,area.y+6,3,area.height*.55f),color);
            }
            else if(id==5)
            {
                Panel(new Rect(area.center.x-3,area.y+23,6,area.height-30),new Color(.55f,.29f,.1f));
                Panel(new Rect(area.center.x-20,area.y+8,40,20),color);
                for(int sign=-1;sign<=1;sign+=2) Panel(new Rect(area.center.x+sign*11-2,area.y+8,4,20),new Color(.18f,.17f,.16f));
            }
            else if(id==7)
            {
                Panel(new Rect(area.center.x-4,area.y+25,8,area.height-31),new Color(.5f,.25f,.09f));
                Panel(new Rect(area.center.x-10,area.y+23,20,9),new Color(.22f,.23f,.23f));
                Panel(new Rect(area.center.x-9,area.y+15,18,10),new Color(.72f,.55f,.32f));
                RotatedPanel(new Vector2(area.center.x,area.y+13),new Vector2(13,20),15,color);
                RotatedPanel(new Vector2(area.center.x+2,area.y+16),new Vector2(7,12),-10,new Color(1,.87f,.2f));
            }
            else RotatedPanel(new Vector2(area.center.x,area.y+14),new Vector2(12,12),45,color);
            return;
        }
        if(WeaponCatalog.Passive(item))
        {
            GUI.DrawTexture(area,ItemIcons.Get(ItemLibrary.Index(item)),ScaleMode.ScaleToFit,true);
            return;
        }
        if (item == 0 || item == 3)
        {
            if(item==3)
            {
                Color wood=new Color(.65f,.32f,.09f),iron=new Color(.28f,.30f,.32f);
                Panel(new Rect(x,y+17,18,15),wood);
                Panel(new Rect(x+14,y+14,27,14),iron);
                Panel(new Rect(x+35,y+16,16,7),steel);
                Panel(new Rect(x+22,y+28,9,13),iron);
                Panel(new Rect(x+30,y+23,15,6),wood);
                return;
            }
            Panel(new Rect(x,y+8,38,12),steel); Panel(new Rect(x+7,y+20,12,20),steel);
            Panel(new Rect(x+29,y+10,17,7),ShopGreen);
            if (item == 0) for(int i=0;i<3;i++) Panel(new Rect(x+i*13,y+33,7,10),ShopGreen);
        }
        else if (item == 1 || item == 4)
        {
            Panel(new Rect(area.center.x-5,area.y+7,10,35),steel);
            Panel(new Rect(area.center.x-13,area.y+39,26,5),ShopGreen);
            Panel(new Rect(area.center.x-4,area.y+44,8,13),new Color(0.57f,0.34f,0.15f));
        }
        else if (item == 2)
        {
            Panel(new Rect(x+6,y+5,19,25),steel); Panel(new Rect(x+6,y+28,37,13),ShopGreen);
            Panel(new Rect(x+6,y+42,39,4),steel);
        }
        else
        {
            Panel(new Rect(x+16,y+2,9,17),new Color(1,0.25f,0.08f));
            Panel(new Rect(x+7,y+18,30,24),new Color(0.95f,0.12f,0.07f));
            Panel(new Rect(x+16,y+27,12,16),new Color(1,0.65f,0.12f));
        }
    }

    internal string UpgradeBenefit(int item)
    {
        if(WeaponCatalog.Passive(item)) return ItemLibrary.At(item).Description;
        int id=WeaponCatalog.WeaponId(item);
        if(id>=0)
        {
            if(equipment.CanEvolve(id)) return WeaponCatalog.EvolvedDescription(id);
            return equipment.HasWeapon(id)?WeaponUpgradePreview.Describe(id,equipment,GameLanguage.Chinese)
                :(GameLanguage.Chinese?WeaponCatalog.Description[id]:WeaponCatalog.EnglishDescription[id]);
        }
        if (item == 0) return GameLanguage.Chinese?$"枪械与十字弓射速\n基础射速 +20%\n\n加成：{equipment.Level(0)*20}% → {(equipment.Level(0)+1)*20}%":$"Gun and crossbow fire rate\n+20% base rate\n\nBonus: {equipment.Level(0)*20}% > {(equipment.Level(0)+1)*20}%";
        if (item == 1) return GameLanguage.Chinese?$"所有已装备近战武器\n基础攻击范围 +15%\n\n加成：{equipment.Level(1)*15}% → {(equipment.Level(1)+1)*15}%":$"All equipped melee weapons\n+15% base range\n\nBonus: {equipment.Level(1)*15}% > {(equipment.Level(1)+1)*15}%";
        if (item == 2) return $"Movement speed\n+10% of base speed\n\nBonus: {equipment.Level(2)*10}%  >  {(equipment.Level(2)+1)*10}%";
        return "";
    }

    internal void ShopStat(float x, ref float y, string name, string value, bool heading=false)
    {
        if(heading) { y+=8; Panel(new Rect(x+18,y-4,188,1),new Color(0.22f,0.24f,0.19f)); }
        ShopText(new Rect(x+18,y,188,23),name,heading?12:14,heading?ShopMuted:Color.white,heading);
        ShopText(new Rect(x+120,y,86,23),value,14,ShopGreen,true,TextAnchor.UpperRight);
        y+=25;
    }

    // Compose in HUD coordinates before applying the enclosing UI scale.
    internal static Matrix4x4 ParticleMatrix(Matrix4x4 parent, Vector2 center, float angle)
        => parent * Matrix4x4.TRS(new Vector3(center.x, center.y, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

    internal static void RotatedPanel(Vector2 center, Vector2 size, float angle, Color color)
    {
        Matrix4x4 parent = GUI.matrix;
        try
        {
            GUI.matrix = ParticleMatrix(parent, center, angle);
            Panel(new Rect(-size.x / 2, -size.y / 2, size.x, size.y), color);
        }
        finally { GUI.matrix = parent; }
    }

    internal static void Panel(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    internal void Render()
    {
        GUISkin previous=GUI.skin;
        if(localizedSkin==null)
        {
            localizedSkin=UnityEngine.Object.Instantiate(previous);
            localizedSkin.hideFlags=HideFlags.HideAndDontSave;
            localizedSkin.font=GameLanguage.Font;
            foreach(GUIStyle style in localizedSkin) style.font=GameLanguage.Font;
            localizedSkin.button.fontSize=13;
        }
        GUI.skin=localizedSkin;
        try
        {
            DrawGameUI();
            if(flow.Paused || flow.frontScreen!=0) return;
            Matrix4x4 matrix=GUI.matrix;
            float scale=PlaygroundCamera.HudScale;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale;
            Rect languageRect=codexOpen?new Rect(width-352,28,160,32):
                flow.choosingStarter?new Rect(24,20,160,28):flow.ShowingShop?new Rect(24,126,160,25):new Rect(14,78,160,25);
            if(GUI.Button(languageRect,new GUIContent(GameLanguage.Chinese?"中文 / English [F2]":"English / 中文 [F2]","F2 / Gamepad Menu"))) GameLanguage.Toggle();
            GUI.matrix=matrix;
        }
        finally { GUI.skin=previous; }
    }

    internal static bool UiButton(Rect rect,string text)
    { bool pressed=GUI.Button(rect,GameLanguage.Text(text)); if(pressed) GameAudio.Play(AudioCue.Click); return pressed; }

    internal static void HudLabel(Rect rect,string text,GUIStyle style)=>GUI.Label(rect,GameLanguage.Text(text),style);

    internal void NavigateShop()
    {
        Vector2 input = shopNavigate.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.25f) { navigateAt = 0f; return; }
        if (Time.unscaledTime < navigateAt) return;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            if (shopSelection < shop.Count && shop.Count > 0)
                shopSelection = (shopSelection + (input.x > 0 ? 1 : shop.Count - 1)) % shop.Count;
            else shopSelection = shopSelection == shop.Count ? shop.Count + 1 : shop.Count;
        }
        else if (input.y < 0) shopSelection = shopSelection < shop.Count ? shop.Count : shop.Count + 1;
        else shopSelection = shopSelection >= shop.Count ? Mathf.Max(0, shop.Count - 1) : shop.Count + 1;
        GameAudio.Play(AudioCue.Select);
        navigateAt = Time.unscaledTime + 0.22f;
    }

    internal void UpdateWeaponReplacement()
    {
        if(backMenuAction.WasPressedThisFrame()) { flow.CancelReplacement(); return; }
        Vector2 input=shopNavigate.ReadValue<Vector2>();
        float move=Mathf.Abs(input.x)>Mathf.Abs(input.y)?input.x:-input.y;
        if(Mathf.Abs(move)<.5f) navigateAt=0;
        else if(Time.unscaledTime>=navigateAt) { replacementSelection=(replacementSelection+(move>0?1:3))%4; GameAudio.Play(AudioCue.Select); navigateAt=Time.unscaledTime+.2f; }
        if(Time.frameCount>flow.phaseChangedFrame && restartAction.WasPressedThisFrame())
        { if(replacementSelection==3) { flow.CancelReplacement(); } else flow.FinishReplacement(EquippedWeapons()[replacementSelection]); }
    }

    internal void DrawFrontMenu(float width,float height)=>menuui.DrawFrontMenu(width,height);
    internal void CameraSettingSlider(Rect rect,int index,string title,bool angle)=>menuui.CameraSettingSlider(rect,index,title,angle);
    internal void DrawStarter(float width,float height)=>menuui.DrawStarter(width,height);
    internal void DrawCodex(float width,float height)=>menuui.DrawCodex(width,height);
    internal void DrawShop(float width, float height)=>battleui.DrawShop(width,height);
    internal void DrawGameUI()=>battleui.DrawGameUI();
    internal void DrawStickHUD(Vector2 center)=>battleui.DrawStickHUD(center);
    internal void DrawStickCircle(Vector2 center, float radius, Color color)=>battleui.DrawStickCircle(center,radius,color);
    internal void DrawWeaponReplacement(float width,float height)=>battleui.DrawWeaponReplacement(width,height);
    internal void DrawFloatingScores(float scale)=>battleui.DrawFloatingScores(scale);
    internal void DrawVictoryLoot(float width,float height)=>battleui.DrawVictoryLoot(width,height);
    internal void DrawVictoryCelebration(float width,float height)=>battleui.DrawVictoryCelebration(width,height);
}
