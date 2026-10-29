using UnityEngine;
using UnityEngine.InputSystem;
internal sealed class MenuUI
{
    private readonly GameUI ui;
    internal MenuUI(GameUI ui) { this.ui=ui; }
    internal void DrawFrontMenu(float width,float height)
    {
        ui.shopSelection=ui.frontSelection;
        if(ui.flow.frontScreen==1)
        {
            GameUI.Panel(new Rect(0,0,width,height),new Color(0.02f,0.03f,0.02f,0.86f));
            ui.ShopText(new Rect(0,height*0.17f,width,70),GameLanguage.Chinese?"林地幸存者":"FOREST SURVIVORS",40,GameUI.ShopGreen,true,TextAnchor.MiddleCenter);
            string mode=ui.session.Endless?(GameLanguage.Chinese?"无尽模式":"ENDLESS"):(GameLanguage.Chinese?$"{ui.session.FinalWave} 波挑战":$"{ui.session.FinalWave} WAVES");
            ui.ShopText(new Rect(0,height*.17f+70,width,28),mode+(GameLanguage.Chinese?$" · 从第 {ui.session.Wave} 波开始":$" · START AT WAVE {ui.session.Wave}"),16,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
            float x=width/2-160,y=height*0.43f;
            string[] labels=GameLanguage.Chinese?new[]{"新游戏","设置","结束"}:new[]{"NEW GAME","SETTINGS","QUIT"};
            for(int i=0;i<3;i++) if(ui.ShopButton(new Rect(x,y+i*66,320,48),labels[i],i,true))
            { ui.frontSelection=i; ui.ConfirmFrontMenu(); }
            ui.ShopText(new Rect(0,height-55,width,30),GameLanguage.Chinese?"方向键 / 摇杆：选择    Enter / A：确认":"ARROWS / STICK: SELECT    ENTER / A: CONFIRM",14,GameUI.ShopMuted,false,TextAnchor.MiddleCenter);
            return;
        }
        // Keep the world visible on the right so the camera changes can be previewed.
        float px=36,py=height/2-255;
        GameUI.Panel(new Rect(px,py,470,510),new Color(0.025f,0.035f,0.025f,0.96f));
        ui.ShopText(new Rect(px+24,py+20,422,45),GameLanguage.Chinese?"设置":"SETTINGS",30,GameUI.ShopGreen,true);
        ui.ShopText(new Rect(px+24,py+70,422,38),GameLanguage.Chinese?"调整镜头，可在右侧预览效果。":"Adjust the camera. Preview the view on the right.",14,GameUI.ShopMuted);
        if(ui.view.Camera!=null)
        {
            string mode=GameLanguage.Chinese?(ui.view.Camera.ThirdPerson?"视角：第三人称":"视角：俯视"):(ui.view.Camera.ThirdPerson?"VIEW: THIRD PERSON":"VIEW: TOP DOWN");
            if(ui.ShopButton(new Rect(px+24,py+118,422,38),mode,0,true)) { ui.frontSelection=0; ui.ConfirmFrontMenu(); }
            CameraSettingSlider(new Rect(px+24,py+174,422,62),1,GameLanguage.Chinese?"俯角":"TILT",true);
            CameraSettingSlider(new Rect(px+24,py+247,422,62),2,GameLanguage.Chinese?"距离":"DISTANCE",false);
        }
        if(ui.ShopButton(new Rect(px+24,py+320,422,34),GameLanguage.Chinese?"语言：中文 / English":"Language: English / 中文",3,true)) { ui.frontSelection=3; ui.ConfirmFrontMenu(); }
        if(ui.ShopButton(new Rect(px+24,py+369,422,34),GameLanguage.Chinese?"重置当前镜头":"RESET CURRENT VIEW",4,ui.view.Camera!=null)) { ui.frontSelection=4; ui.ConfirmFrontMenu(); }
        if(ui.ShopButton(new Rect(px+24,py+425,422,40),GameLanguage.Chinese?"保存并返回":"SAVE AND BACK",5,true)) ui.flow.CloseSettings();
        ui.ShopText(new Rect(px+24,py+475,422,25),GameLanguage.Chinese?"左右：调整数值    Esc / B：保存并返回":"LEFT / RIGHT: ADJUST    ESC / B: SAVE AND BACK",12,GameUI.ShopMuted);
        if(GameAudio.Current!=null)
        {
            float ax=px+488,aw=Mathf.Min(340,width-ax-24);
            GameUI.Panel(new Rect(ax,py+118,aw,306),new Color(.025f,.035f,.025f,.96f));
            ui.ShopText(new Rect(ax+20,py+136,aw-40,30),GameLanguage.Chinese?"声音":"AUDIO",23,GameUI.ShopGreen,true);
            for(int channel=0;channel<3;channel++)
                AudioSettingSlider(new Rect(ax+20,py+181+channel*73,aw-40,62),channel);
        }
    }

    private void AudioSettingSlider(Rect rect,int channel)
    {
        var audio=GameAudio.Current;
        int index=6+channel;
        if(rect.Contains(Event.current.mousePosition) && Event.current.type==EventType.MouseDown) ui.frontSelection=index;
        float value=channel==0?audio.MasterVolume:channel==1?audio.EffectsVolume:audio.MusicVolume;
        string title=GameLanguage.Chinese?(channel==0?"总音量":channel==1?"音效":"音乐"):(channel==0?"MASTER":channel==1?"EFFECTS":"MUSIC");
        ui.ShopText(new Rect(rect.x,rect.y,rect.width,25),$"{title}  {value:P0}",17,ui.frontSelection==index?GameUI.ShopGreen:Color.white,true);
        float next=GUI.HorizontalSlider(new Rect(rect.x,rect.y+36,rect.width,20),value,0,1);
        if(!Mathf.Approximately(next,value)) { audio.SetVolume(channel,next); if(channel!=2) GameAudio.Play(AudioCue.Select); }
    }

    internal void CameraSettingSlider(Rect rect,int index,string title,bool angle)
    {
        if(rect.Contains(Event.current.mousePosition) && Event.current.type==EventType.MouseDown) ui.frontSelection=index;
        float value=angle?ui.view.Camera.Tilt:ui.view.Camera.Distance;
        ui.ShopText(new Rect(rect.x,rect.y,rect.width,25),$"{title}   {value:0.0}"+(angle?"°":""),17,ui.frontSelection==index?GameUI.ShopGreen:Color.white,true);
        value=GUI.HorizontalSlider(new Rect(rect.x,rect.y+36,rect.width,20),value,angle?ui.view.Camera.MinTilt:ui.view.Camera.MinDistance,angle?ui.view.Camera.MaxTilt:ui.view.Camera.MaxDistance);
        if(angle) ui.view.Camera.Tilt=value; else ui.view.Camera.Distance=value;
    }

    internal void DrawStarter(float width,float height)
    {
        GameUI.Panel(new Rect(0,0,width,height),new Color(.02f,.025f,.015f,.95f));
        if(GameUI.UiButton(new Rect(width-184,20,160,28),GameLanguage.Chinese?"返回主菜单 [Esc / B]":"MAIN MENU [Esc / B]")) ui.flow.OpenMainMenu();
        ui.ShopText(new Rect(24,45,width-48,42),"CHOOSE YOUR STARTING WEAPON",28,Color.white,true,TextAnchor.MiddleCenter);
        ui.ShopText(new Rect(24,96,width-48,30),GameLanguage.Chinese?"最多装备 3 种武器 · 自带短冲刺：空格 / LB / L1":"Equip up to 3 weapons · Free dash: SPACE / LB / L1",16,GameUI.ShopGreen,false,TextAnchor.MiddleCenter);
        int columns=GameUI.StarterColumns,rows=(WeaponCatalog.StarterCount+columns-1)/columns;
        float cardWidth=(width-44-(columns-1)*12)/columns,cardHeight=(height-276-(rows-1)*12)/rows;
        for(int i=0;i<WeaponCatalog.StarterCount;i++)
        {
            Rect r=new Rect(22+(i%columns)*(cardWidth+12),140+(i/columns)*(cardHeight+12),cardWidth,cardHeight);
            if(r.Contains(Event.current.mousePosition) && Event.current.type==EventType.MouseMove) ui.starterSelection=i;
            GameUI.Panel(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),ui.starterSelection==i?GameUI.ShopGreen:GameUI.ShopMuted);
            GameUI.Panel(r,new Color(.055f,.065f,.045f));
            ui.UpgradeIcon(new Rect(r.x+10,r.y+10,46,46),WeaponCatalog.ShopId(i));
            ui.ShopText(new Rect(r.x+64,r.y+12,r.width-70,38),WeaponCatalog.Names[i],16,Color.white,true);

            if(ui.ShopButton(new Rect(r.x+10,r.yMax-36,r.width-20,28),"CHOOSE / START",i,true)) ui.flow.BeginRun(i);
        }
        ui.ShopText(new Rect(24,height-126,width-48,70),GameLanguage.Chinese?WeaponCatalog.Description[ui.starterSelection]:WeaponCatalog.EnglishDescription[ui.starterSelection],15,GameUI.ShopMuted);
        if(GameUI.UiButton(new Rect(width/2-140,height-48,280,28),GameLanguage.Chinese?$"浏览 {ItemLibrary.Count} 个道具 [Tab / View]":$"BROWSE {ItemLibrary.Count} ITEMS [TAB / VIEW]")) ui.ToggleCodex();
    }

    internal void DrawCodex(float width,float height)
    {
        GameUI.Panel(new Rect(0,0,width,height),new Color(0.025f,0.03f,0.025f,0.99f));
        ui.ShopText(new Rect(28,24,width-390,45),GameLanguage.Chinese?$"道具图鉴 / {ItemLibrary.Count} 个道具":$"ITEM LIBRARY / {ItemLibrary.Count} ITEMS",24,Color.white,true);
        if(GameUI.UiButton(new Rect(width-180,28,150,32),"BACK [TAB / B]")) ui.ToggleCodex();
        float row=Mathf.Min(30,(height-128)/ItemLibrary.Count);
        for(int i=0;i<ItemLibrary.Count;i++)
        {
            Rect r=new Rect(28,92+i*row,280,row-2);
            GameUI.Panel(r,ui.codexSelection==i?new Color(0.3f,0.44f,0.2f):new Color(0.09f,0.11f,0.08f));
            ui.UpgradeIcon(new Rect(r.x+3,r.y+1,r.height-2,r.height-2),ItemLibrary.ShopId(i));
            ui.ShopText(new Rect(r.x+r.height+5,r.y+3,r.width-r.height-10,r.height),ItemLibrary.All[i].Name,13,Color.white,ui.codexSelection==i);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) ui.codexSelection=i;
        }
        ItemDefinition item=ItemLibrary.All[ui.codexSelection]; float x=340,w=width-x-32;
        GameUI.Panel(new Rect(x,92,w,height-124),new Color(0.06f,0.075f,0.05f));
        ui.UpgradeIcon(new Rect(x+28,118,64,64),ItemLibrary.ShopId(ui.codexSelection));
        ui.ShopText(new Rect(x+28,207,w-56,70),item.Name,30,GameUI.ShopGreen,true);
        ui.ShopText(new Rect(x+28,282,w-56,115),item.Description,20,Color.white);
        ui.ShopText(new Rect(x+28,418,w-56,32),$"SHOP FROM WAVE {item.UnlockWave}   /   MAX STACKS {item.MaxStacks}",15,GameUI.ShopMuted,true);
        ui.ShopText(new Rect(x+28,460,w-56,62),"PAIRS WITH\n"+item.Synergy,17,GameUI.ShopGreen);
        ui.ShopText(new Rect(x+28,height-80,w-56,32),$"BASE PRICE {item.Price}   /   OWNED {ui.equipment.Stacks((RunItem)ui.codexSelection)}",15,Color.white);
    }
}
