using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCurrent
{
    public sealed class Interface : MonoBehaviour
    {
        public Game Game;
        GUIStyle hero,heading,body,small,kicker,button,center,number,slider,thumb;
        GUIStyle keyStyle,rightBody,transcriptStyle,largeNoteStyle,evidenceStyle,dialogueText,hudGoal,hudTiny;
        Texture2D dialogueCard,warningCard,paperPanel;
        readonly List<Rect> paperRegions=new List<Rect>(8);
        readonly Dictionary<GUIStyle,GUIStyle> paperStyles=new Dictionary<GUIStyle,GUIStyle>();
        readonly Color paperInk=new Color(.105f,.074f,.065f),oxblood=new Color(.38f,.12f,.13f);
        string cachedSubtitle,dialogueSpeaker,dialogueBody;
        // Field Settings pages: 0 controls & audio, 1 dialogue display, 2 castle night (castle only).
        int settingsPage;
        bool CastleScreen=>(Game.Started||Game.Loading?Game.Map:Game.SelectedMap)==MapId.Castle;
        readonly string[] glyphs=new string[128];
        readonly float[] glyphWidths=new float[128];
        int shownRecords=-1;string recordLabel;
        Font serif;
        Texture2D backdrop,trackTexture,thumbTexture,hoverTexture;
        Material artworkMaterial;
        int anchor=-1,pattern=-1,closure=-1;
        struct MenuEntry {public string id;public Rect rect;public bool slider;}
        readonly List<MenuEntry> menu=new List<MenuEntry>(24);
        string screenKey,focusedId,pendingConfirm;
        int pendingSlider;
        Vector2 heldDirection;
        float nextNavigation;
        public string ControllerFocus=>focusedId;
        public void ShowDisplayPreferences(){settingsPage=1;Game.Paused=true;Game.ApplyFocus();}
        bool ConsumeControllerAction(string id,bool enabled,bool layout)
        {
            if(!layout||pendingConfirm!=id)return false;
            pendingConfirm=null;return enabled;
        }
        // Words and art for the screen being drawn: the chosen map on the title,
        // the destination while loading, otherwise the open map.
        CaseBook Book=>CaseBook.For(Game.Loading?Game.LoadingTarget:!Game.Started?Game.SelectedMap:Game.Map);
        string ScreenKey()=>Game.Loading?"loading":!Game.Started?(Game.Paused?"settings-title":"title"):Game.Case.Terminal?(Game.Case.Phase==Phase.Dead&&Game.Creature.PresentationRemaining>0?"capture":"ending"):Game.NoteBody!=null?"note":Game.Journal?"journal":Game.Paused?"settings":"play";
        void RefreshMenu()
        {
            string key=ScreenKey();if(screenKey==key)return;
            screenKey=key;menu.Clear();focusedId=pendingConfirm=null;pendingSlider=0;heldDirection=Vector2.zero;nextNavigation=0;
        }
        void Register(string id,Rect rect,bool isSlider=false)
        {
            if(!GUI.enabled||Event.current.type!=EventType.Layout)return;
            menu.Add(new MenuEntry{id=id,rect=rect,slider=isSlider});
            if(focusedId==null)focusedId=id;
        }
        void ControllerNavigation()
        {
            RefreshMenu();var pad=PadInput.Current;
            if(!pad||!pad.Connected||screenKey=="play"||screenKey=="loading"||screenKey=="capture"||menu.Count==0)return;
            int selected=-1;for(int i=0;i<menu.Count;i++)if(menu[i].id==focusedId){selected=i;break;}
            if(selected<0){selected=0;focusedId=menu[0].id;}
            Vector2 raw=pad.Navigate;
            Vector2 direction=Mathf.Max(Mathf.Abs(raw.x),Mathf.Abs(raw.y))<.55f?Vector2.zero:Mathf.Abs(raw.x)>Mathf.Abs(raw.y)?new Vector2(Mathf.Sign(raw.x),0):new Vector2(0,-Mathf.Sign(raw.y));
            if(direction==Vector2.zero)heldDirection=Vector2.zero;
            else if(direction!=heldDirection||Time.unscaledTime>=nextNavigation)
            {
                bool first=direction!=heldDirection;heldDirection=direction;nextNavigation=Time.unscaledTime+(first?.34f:.14f);
                if(menu[selected].slider&&direction.x!=0)pendingSlider=(int)direction.x;
                else
                {
                    int best=-1;float score=float.PositiveInfinity;Vector2 origin=menu[selected].rect.center;
                    for(int i=0;i<menu.Count;i++)
                    {
                        Vector2 delta=menu[i].rect.center-origin;float forward=Vector2.Dot(delta,direction);
                        if(forward<1)continue;
                        float across=Mathf.Abs(delta.x*direction.y-delta.y*direction.x);
                        float candidate=forward+across*2;
                        if(candidate<score){score=candidate;best=i;}
                    }
                    if(best>=0){focusedId=menu[best].id;Game.Sound?.UI();}
                }
            }
            if(pad.Confirm&&!menu[selected].slider)pendingConfirm=focusedId;
        }
        readonly Color ink=new Color(.91f,.89f,.82f),muted=new Color(.64f,.66f,.63f),copper=new Color(.63f,.51f,.35f),line=new Color(.42f,.36f,.29f,.65f);
        void Styles()
        {
            if(hero!=null)return;
            serif=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman","Liberation Serif"},72);
            if(!serif)serif=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hero=new GUIStyle(GUI.skin.label){font=serif,fontSize=76,wordWrap=true,normal={textColor=ink}};
            heading=new GUIStyle(hero){fontSize=36};number=new GUIStyle(hero){fontSize=40};
            body=new GUIStyle(GUI.skin.label){font=serif,fontSize=20,wordWrap=true,normal={textColor=ink}};
            small=new GUIStyle(body){fontSize=15,normal={textColor=muted}};
            kicker=new GUIStyle(small){fontSize=13,normal={textColor=copper}};
            center=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(body){fontSize=18,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(22,70,0,0)};
            keyStyle=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=12};rightBody=new GUIStyle(body){alignment=TextAnchor.MiddleRight};
            transcriptStyle=new GUIStyle(body){fontSize=19};largeNoteStyle=new GUIStyle(body){fontSize=23};evidenceStyle=new GUIStyle(body){fontSize=17};
            for(int c=32;c<128;c++){glyphs[c]=((char)c).ToString();glyphWidths[c]=kicker.CalcSize(new GUIContent(glyphs[c])).x+2.4f;}
            dialogueText=new GUIStyle(body){font=serif,fontSize=24,wordWrap=true,alignment=TextAnchor.UpperLeft};
            hudGoal=new GUIStyle(body){font=serif,fontSize=19,wordWrap=true};hudTiny=new GUIStyle(small){fontSize=12};
            string cards=CaseBook.For(Game.Map).UiArtFolder;
            dialogueCard=Resources.Load<Texture2D>(cards+"/Dialogue");
            warningCard=Resources.Load<Texture2D>(cards+"/Warning");paperPanel=Resources.Load<Texture2D>(cards+"/Panel");
            if(!dialogueCard||!warningCard||!paperPanel)
            {
                if(!MissingArt.Placeholders)throw new InvalidOperationException("Approved Mourning Note artwork is missing");
                // A map without its own card art borrows the Mourning Note frames
                // (never a magenta panel); only if those are gone too is it magenta.
                MissingArt.Report("ui",cards);
                if(!dialogueCard)dialogueCard=Resources.Load<Texture2D>("UI/MourningNote/Dialogue");if(!warningCard)warningCard=Resources.Load<Texture2D>("UI/MourningNote/Warning");if(!paperPanel)paperPanel=Resources.Load<Texture2D>("UI/MourningNote/Panel");
                if(!dialogueCard)dialogueCard=MissingArt.MagentaTexture;if(!warningCard)warningCard=MissingArt.MagentaTexture;if(!paperPanel)paperPanel=MissingArt.MagentaTexture;
            }
            backdrop=BackdropFor(Book);
            artworkMaterial=new Material(Resources.Load<Shader>("UIArtwork"));
            trackTexture=Solid(new Color(.21f,.24f,.24f));thumbTexture=Solid(copper);hoverTexture=Solid(ink);
            slider=new GUIStyle(GUI.skin.horizontalSlider){fixedHeight=4,margin=new RectOffset(0,0,9,0),border=new RectOffset(),normal={background=trackTexture}};
            thumb=new GUIStyle(GUI.skin.horizontalSliderThumb){fixedWidth=12,fixedHeight=22,normal={background=thumbTexture},hover={background=hoverTexture},active={background=hoverTexture},border=new RectOffset()};
            Debug.Log("HC_UI_READY live cinematic interface; font="+serif.name+" artwork="+(backdrop!=null));
        }
        static Texture2D Solid(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c.linear);t.Apply();return t;}
        void OnDestroy(){if(trackTexture)Destroy(trackTexture);if(thumbTexture)Destroy(thumbTexture);if(hoverTexture)Destroy(hoverTexture);if(artworkMaterial)Destroy(artworkMaterial);}
        void OnGUI()
        {
            if(Game==null)return;Styles();paperRegions.Clear();
            RefreshMenu();if(Event.current.type==EventType.Layout)menu.Clear();
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(Game.Loading){Loading();return;}
            if(!Game.Started){if(Game.Paused)Settings(true);else Title();return;}
            if(Game.Case.Terminal)
            {
                if(Game.Case.Phase==Phase.Dead&&Game.Creature.PresentationRemaining>0)
                {float fade=1-Game.Creature.PresentationRemaining/Game.Creature.Profile.CapturePresentationSeconds;Panel(new Rect(0,0,1600,900),new Color(.005f,.008f,.01f,Mathf.Pow(fade,3)));return;}
                Ending();return;
            }
            if(Game.NoteBody!=null){Note();return;}
            if(Game.Journal){Journal();return;}
            if(Game.Paused){Settings(false);return;}
            HUD();
        }
        void Panel(Rect r,Color c){var old=GUI.color;GUI.color=c.linear;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Border(Rect r,Color c){Panel(new Rect(r.x,r.y,r.width,1),c);Panel(new Rect(r.x,r.yMax-1,r.width,1),c);Panel(new Rect(r.x,r.y,1,r.height),c);Panel(new Rect(r.xMax-1,r.y,1,r.height),c);}
        bool OnPaper(Vector2 point){foreach(var r in paperRegions)if(r.Contains(point))return true;return false;}
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null)
        {
            style=style??body;
            if(OnPaper(new Vector2(x+5,y+5)))
            {
                if(!paperStyles.TryGetValue(style,out var dark)){dark=new GUIStyle(style);paperStyles[style]=dark;}
                dark.fontSize=style.fontSize;dark.normal.textColor=style==kicker?oxblood:paperInk;GUI.Label(new Rect(x,y,w,h),text,dark);
            }
            else GUI.Label(new Rect(x,y,w,h),text,style);
        }
        void Rule(float x,float y,float w)=>Panel(new Rect(x,y,w,1),line);
        void Track(float x,float y,string text,Color? color=null)
        {
            var old=kicker.normal.textColor;kicker.normal.textColor=OnPaper(new Vector2(x,y))?oxblood:color??copper;
            foreach(char c in text){string s=c<128?glyphs[c]:c.ToString();GUI.Label(new Rect(x,y,24,24),s,kicker);x+=c<128?glyphWidths[c]:kicker.CalcSize(new GUIContent(s)).x+2.4f;}
            kicker.normal.textColor=old;
        }
        // Menu backdrop per map (title follows the selected map), cached per path.
        readonly Dictionary<string,Texture2D> backdrops=new Dictionary<string,Texture2D>();
        Texture2D BackdropFor(CaseBook book)
        {
            if(!backdrops.TryGetValue(book.Backdrop,out var texture)){texture=Resources.Load<Texture2D>(book.Backdrop);if(!texture)texture=Resources.Load<Texture2D>("UI/MenuBackdrop");backdrops[book.Backdrop]=texture;}
            return texture;
        }
        void Backdrop(bool cool=false,float shade=0)
        {
            backdrop=BackdropFor(Book);
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;var old=GUI.color;
            if(backdrop)Artwork(new Rect(0,0,Screen.width,Screen.height),backdrop,true,cool?new Color(.64f,.75f,.84f):Color.white);
            else Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.02f,.025f));
            GUI.color=old;if(shade>0)Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.008f,.012f,.014f,shade));GUI.matrix=matrix;
        }
        void Artwork(Rect rect,Texture texture,bool crop=false,Color? tint=null)
        {
            if(Event.current.type!=EventType.Repaint)return;
            float factor=crop?Mathf.Max(rect.width/texture.width,rect.height/texture.height):Mathf.Min(rect.width/texture.width,rect.height/texture.height);
            var draw=new Rect(rect.center.x-texture.width*factor*.5f,rect.center.y-texture.height*factor*.5f,texture.width*factor,texture.height*factor);
            var matrix=GUI.matrix;var p=matrix.MultiplyPoint3x4(new Vector3(draw.x,draw.y,0));var size=matrix.MultiplyVector(new Vector3(draw.width,draw.height,0));GUI.matrix=Matrix4x4.identity;
            artworkMaterial.SetColor("_Tint",(tint??Color.white).linear);
            Graphics.DrawTexture(new Rect(p.x,p.y,size.x,size.y),texture,artworkMaterial);GUI.matrix=matrix;
        }
        void Brand(string right=null)
        {right=right??Book.PlaceTag+"  /  "+Book.CaseTag;Track(80,48,"HOLLOW CURRENT",ink);Track(80,78,"FIELD INVESTIGATION OFFICE",muted);Label(1110,51,410,35,right,small);Rule(80,121,50);}
        void Footer(string hint)
        {
            if(PadInput.Active)hint=PadInput.Current.PlayStation?"D-pad / left stick navigate   ·   × select   ·   ○ back   ·   Options pause":"D-pad / left stick navigate   ·   A select   ·   B back   ·   Start pause";
            else if(PadInput.Current&&!PadInput.Current.Connected&&PadInput.Current.DeviceName!="No controller")hint="Controller disconnected. Reconnect, or use keyboard and mouse.";
            Rule(80,843,1440);Label(80,863,1160,25,hint,small);Label(1260,863,260,25,Book.FooterName,kicker);
        }
        void TextureFrame(Rect rect,Texture texture,Color tint)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var matrix=GUI.matrix;var position=matrix.MultiplyPoint3x4(new Vector3(rect.x,rect.y,0));var size=matrix.MultiplyVector(new Vector3(rect.width,rect.height,0));GUI.matrix=Matrix4x4.identity;
            artworkMaterial.SetColor("_Tint",tint.linear);Graphics.DrawTexture(new Rect(position.x,position.y,size.x,size.y),texture,new Rect(0,0,1,1),0,0,0,0,Color.white,artworkMaterial);GUI.matrix=matrix;
        }
        void Card(Rect r){TextureFrame(new Rect(r.x-18,r.y-42,r.width+36,r.height+72),paperPanel,new Color(1,1,1,.97f));paperRegions.Add(r);}
        void DarkCard(Rect r){TextureFrame(r,warningCard,new Color(1,1,1,.96f));}
        bool Action(float x,float y,float w,float h,string text,string key="",bool primary=false,bool plain=false,bool selected=false,string controlId=null)
        {
            var rect=new Rect(x,y,w,h);string id="hc-ui/"+(controlId??text);
            Register(id,rect);
            bool padFocus=PadInput.Active&&focusedId==id;
            bool focus=(!PadInput.Active&&GUI.GetNameOfFocusedControl()==id)||padFocus,hover=!PadInput.Active&&rect.Contains(Event.current.mousePosition)&&GUI.enabled;
            if(!plain||hover||focus)
            {
                TextureFrame(rect,warningCard,!GUI.enabled?new Color(.65f,.65f,.65f,.55f):selected||hover||focus?new Color(1.3f,1.15f,1.05f,1):Color.white);
                if(GUI.enabled&&(primary||selected||hover||focus))Panel(new Rect(x+w*.16f,y+h-7,w*.66f,1),copper);
            }
            if(selected)Panel(new Rect(x,y,3,h),copper);
            // Label padding: right 80 keeps the label >=12 px clear of the key cap (cap spans w-66..w-18).
            GUI.SetNextControlName(id);bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
            if(PadInput.Active)clicked=false;
            if(ConsumeControllerAction(id,GUI.enabled,Event.current.type==EventType.Layout))clicked=true;
            if(PadInput.Active)key=padFocus?PadInput.Prompt("ENTER"):"";
            var old=button.normal.textColor;button.normal.textColor=GUI.enabled?(plain&&!hover&&!focus&&OnPaper(rect.center)?paperInk:ink):new Color(.47f,.43f,.38f);
            button.padding.left=Mathf.Clamp(Mathf.RoundToInt(w*.14f),22,68);button.padding.right=key.Length>0?80:22;GUI.Label(rect,text,button);button.normal.textColor=old;
            if(key.Length>0){var keyRect=new Rect(x+w-66,y+(h-24)/2,48,24);Border(keyRect,GUI.enabled?line:new Color(.16f,.18f,.17f));var oldKey=keyStyle.normal.textColor;keyStyle.normal.textColor=plain&&!hover&&!focus&&OnPaper(rect.center)?paperInk:ink;GUI.Label(keyRect,key,keyStyle);keyStyle.normal.textColor=oldKey;}
            if(clicked)Game.Sound.UI();
            return clicked;
        }
        void Title()
        {
            Backdrop();Brand();Card(new Rect(45,145,705,650));Track(80,176,"AN ORIGINAL INVESTIGATION");
            Label(74,213,650,200,"Hollow\nCurrent",hero);Label(82,424,640,52,Book.TitleName,heading);
            Label(82,476,640,64,Book.Tagline);
            // BEGIN is registered first so a controller lands on it; the picker sits above.
            if(Action(80,617,440,62,"BEGIN INVESTIGATION","ENTER",true))Game.BeginInvestigation();
            for(int i=0;i<MapCatalog.All.Count;i++)
            {
                var map=MapCatalog.All[i];bool chosen=Game.SelectedMap==map.Id;var tab=new Rect(80+i*325,550,310,50);
                if(Game.MapAvailable(map.Id)){if(Action(tab.x,tab.y,tab.width,tab.height,chosen?"•  "+map.DisplayName:map.DisplayName,"",chosen,false,chosen,"map/"+map.Id))Game.SelectMap(map.Id);continue;}
                // A map whose scene is not in this player: a parchment tab with dark ink
                // (readable, not focusable), never an empty disabled bar.
                TextureFrame(tab,paperPanel,new Color(1,1,1,.97f));Border(tab,line);
                var ink=button.normal.textColor;var size=button.fontSize;button.normal.textColor=paperInk;button.fontSize=17;button.padding.left=22;button.padding.right=22;
                GUI.Label(new Rect(tab.x,tab.y-7,tab.width,tab.height),map.DisplayName,button);button.normal.textColor=ink;button.fontSize=size;
                var oldSmall=small.normal.textColor;small.normal.textColor=oxblood;GUI.Label(new Rect(tab.x+22,tab.y+26,tab.width-30,20),"not in this build",small);small.normal.textColor=oldSmall;
            }
            if(Action(80,700,270,46,"FIELD SETTINGS","S",false,true)){Game.Paused=true;Game.ApplyFocus();}
            if(Action(355,700,300,46,"EXIT TO DESKTOP","Q",false,true))Game.Quit();
            Footer("1 / 2 choose map   ·   WASD move   ·   Mouse look   ·   E interact   ·   Headphones suggested");
        }
        void Loading()
        {
            Backdrop(false,.15f);Brand("FIELD OFFICE  /  PREPARING CASE");Card(new Rect(45,210,750,555));Track(80,245,Book.LoadingTrack);
            Label(76,288,680,178,"A new\ninvestigation.",hero);
            Label(82,520,540,50,Game.LoadingProgress<.01f?"Preparing the case files…":"Restoring the house and field equipment…");
            Rule(82,604,510);Panel(new Rect(82,603,510*Mathf.Clamp01(Game.LoadingProgress),3),copper);
            Label(82,631,500,35,Game.LoadingProgress<.01f?"PLEASE WAIT":"LOADING  "+Mathf.RoundToInt(Game.LoadingProgress*100)+"%",kicker);
            Footer("Listen before moving. Footsteps and radio calls can give your position away.");
        }
        void Metric(float x,float y,string value,string label)
        {Label(x,y,180,55,value,number);Track(x,y+65,label,muted);}
        void Ending()
        {
            bool won=Game.Case.Phase==Phase.Won;Backdrop(!won,won?0:.14f);Brand();if(won)Card(new Rect(45,145,850,685));else DarkCard(new Rect(45,145,850,685));Track(80,175,won?Book.CaseTag+" / CLOSED":"LAST TRANSMISSION / LOST");
            bool slain=won&&Game.Case.HunterSlain;
            Label(76,214,635,188,won?(slain?Book.KillHero:Book.WonHero):"Signal\nlost.",hero);
            Label(82,428,530,125,won?(slain?Book.KillBody:Book.WonBody):Book.LostBody);
            Metric(82,588,Game.Case.Evidence.Count+" / 3","EVIDENCE");Metric(285,588,(Game.Case.Survived?Game.Case.Hunts:0).ToString(),"HUNTS SURVIVED");Metric(505,588,TimeSpan.FromSeconds(Game.Case.Elapsed).ToString(@"mm\:ss"),"FIELD TIME");
            Panel(new Rect(261,597,1,76),line);Panel(new Rect(481,597,1,76),line);
            if(!won&&Game.HasCheckpoint&&Action(80,704,440,52,"RETRY CHECKPOINT","",true))Game.RetryCheckpoint();
            if(Action(80,772,440,52,"NEW INVESTIGATION","N",won))Game.Retry();
            if(Action(543,736,310,46,"EXIT TO DESKTOP","Q",false,true))Game.Quit();
            Footer(won?(slain?Book.KillFooter:Book.WonFooter):Book.LostFooter);
        }
        float SliderRow(float x,float y,float w,string label,float value,float min,float max,string formatted,string hint)
        {
            string id="hc-ui/"+label;Register(id,new Rect(x,y,w,90),true);
            bool focused=PadInput.Active&&focusedId==id;
            if(focused){Border(new Rect(x-10,y-6,w+20,100),copper);hint="D-pad left / right to adjust";}
            if(focused&&pendingSlider!=0&&Event.current.type==EventType.Layout){value=Mathf.Clamp(value+pendingSlider*(max-min)/30,min,max);pendingSlider=0;}
            Label(x,y,w-160,34,label);Label(x+w-160,y,160,34,formatted,rightBody);
            GUI.SetNextControlName("hc-ui/"+label);value=GUI.HorizontalSlider(new Rect(x,y+48,w,24),value,min,max,slider,thumb);
            Label(x,y+73,w,24,hint,small);return value;
        }
        bool ToggleRow(float x,float y,float w,string label,bool value,string hint="")
        {
            Label(x,y+7,w-125,35,label);if(hint.Length>0)Label(x+350,y+9,150,28,hint,small);
            if(Action(x+w-96,y,96,37,value?"ON":"OFF","",false,false,value,"toggle/"+label))value=!value;
            return value;
        }
        void Settings(bool fromTitle)
        {
            Backdrop(false,.35f);Brand("FIELD SETTINGS  /  "+Screen.width+" × "+Screen.height);
            Track(80,191,fromTitle?"BEFORE YOU ENTER":"INVESTIGATION PAUSED");Label(76,237,440,180,fromTitle?"Field\nsettings.":"Take a\nbreath.",hero);
            Label(82,439,345,110,fromTitle?"Prepare the field kit and controls.\nYour preferences are kept between cases.":"Your place in the investigation is held.\nResume when you are ready.");
            if(Action(80,607,340,58,fromTitle?"BACK TO TITLE":"RESUME INVESTIGATION","ESC",true))Game.Resume();
            if(!fromTitle&&Action(80,688,340,48,"RESTART CASE","N"))Game.Retry();
            if(Action(80,760,340,44,"EXIT TO DESKTOP","Q",false,true))Game.Quit();
            if(settingsPage==2&&!CastleScreen)settingsPage=0;
            string nextPage=settingsPage==0?"DIALOGUE DISPLAY":settingsPage==1&&CastleScreen?"CASTLE NIGHT":"CONTROLS & AUDIO";
            if(Action(80,544,340,42,nextPage,"",false,false,settingsPage!=0,"settings-page"))settingsPage=settingsPage==0?1:settingsPage==1&&CastleScreen?2:0;
            Card(new Rect(520,173,1000,635));
            if(settingsPage==2){CastleSettings();return;}
            if(settingsPage==1)
            {
                Track(558,202,Book.CardStyle+" / DIALOGUE DISPLAY");
                Game.SubtitleScale=SliderRow(558,256,924,"Subtitle size",Game.SubtitleScale,.85f,1.3f,Mathf.RoundToInt(Game.SubtitleScale*100)+"%","Larger type for living-room play");
                Game.CardOpacity=SliderRow(558,372,924,"Card opacity",Game.CardOpacity,.55f,1,Mathf.RoundToInt(Game.CardOpacity*100)+"%","Message text stays fully readable");
                Game.Subtitles=ToggleRow(558,487,924,"Subtitles",Game.Subtitles);
                Dialogue(new Rect(650,555,740,220),"DISPATCH / CHANNEL 04","The signal is changing. Keep your light steady.",false,1);
                Footer("Preview your dialogue cards. Preferences are saved when you return.");return;
            }
            Track(558,202,"CONTROLS & FIELD EQUIPMENT");
            Game.Player.Sensitivity=SliderRow(558,244,924,"Look sensitivity",Game.Player.Sensitivity,.3f,4,Game.Player.Sensitivity.ToString("F2"),"[1 / 2] Adjust mouse / right-stick sensitivity");
            Game.Player.Brightness=SliderRow(558,356,924,"Flashlight level",Game.Player.Brightness,.7f,1.8f,Mathf.RoundToInt(Game.Player.Brightness*100)+"%","[3 / 4] Adjust light output");
            Game.Volume=SliderRow(558,468,924,"Master volume",Game.Volume,0,1,Mathf.RoundToInt(Game.Volume*100)+"%","[5 / 6] Adjust volume");
            Rule(558,579,924);Game.Player.InvertY=ToggleRow(558,597,924,"Invert look Y",Game.Player.InvertY,"[I]");
            Game.Subtitles=ToggleRow(558,650,924,"Subtitles",Game.Subtitles,"[T]");
            Game.HelpHints=ToggleRow(558,703,924,"Detailed field help",Game.HelpHints,"[H]");
            Footer("ESC return   ·   Shift sprint   ·   Ctrl crouch   ·   E interact   ·   F flashlight   ·   R radio");
        }
        // FEAR_PLAN items 6 and 19: the Long Night dial and the opt-in microphone (castle only).
        void CastleSettings()
        {
            Track(558,202,"THE COUNT'S NIGHT / FIELD RULES");
            bool longNight=ToggleRow(558,244,924,"Long night",Game.LongNight);
            Label(558,288,820,60,"No warning before a hunt, a darker start, longer hunts, a restless perch, and two hiding places nailed shut. "+(Game.Started&&Game.Map==MapId.Castle?"Takes effect on the next castle case.":"Takes effect when the castle case begins."),small);
            if(longNight!=Game.LongNight){Game.LongNight=longNight;Game.SavePreferences();}
            Rule(558,360,924);
            bool mic=ToggleRow(558,380,924,"Microphone",Game.MicEnabled);
            Label(558,424,820,60,"During a hunt he hears your voice through the microphone. macOS asks for microphone access the first time. Off by default.",small);
            if(mic!=Game.MicEnabled){Game.MicEnabled=mic;Game.SavePreferences();}
            GUI.enabled=Game.MicEnabled;
            float threshold=SliderRow(558,500,924,"Microphone threshold",Game.MicThreshold,.005f,.08f,Game.MicThreshold<.015f?"WHISPER":Game.MicThreshold<.035f?"SPEECH":"SHOUT","How loud you must be before he hears you");
            GUI.enabled=true;
            if(!Mathf.Approximately(threshold,Game.MicThreshold))Game.MicThreshold=threshold;
            if(Game.Player.MicLive)
            {
                // Live input level against the threshold (scaled so the threshold range fills the bar).
                Track(558,612,"INPUT",muted);var bar=new Rect(640,620,842,4);Panel(bar,line);
                Panel(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(Game.Player.MicLevel/.1f),4),Game.Player.MicLevel>Game.MicThreshold?oxblood:copper);
                Panel(new Rect(bar.x+bar.width*Mathf.Clamp01(Game.MicThreshold/.1f),bar.y-6,2,16),paperInk);
            }
            Footer("Field rules for the castle. Preferences are kept between cases.");
        }
        // The journal's pulse: a hand-drawn ink trace whose rate and height follow Dread (with +-2 of slow
        // noise). Never a number (FEAR_PLAN item 1).
        void PulseLine(Rect r)
        {
            if(Event.current.type!=EventType.Repaint)return;
            float t=Time.unscaledTime;
            float dread=Mathf.Clamp(FearLink.Dread(Game.Case)+(Mathf.PerlinNoise(t*.35f,.5f)*2-1)*2,0,100);
            float rate=.9f+dread*.022f; // beats per second: 0.9 calm to 3.1 at the edge
            float height=r.height*(.18f+dread*.0042f);
            float baseline=r.center.y;
            var ink=new Color(paperInk.r,paperInk.g,paperInk.b,.85f);
            Vector2 previous=Vector2.zero;
            for(int i=0;i<=110;i++)
            {
                float u=i/110f,x=r.x+u*r.width;
                float phase=Mathf.Repeat(u*r.width/90f*rate-t*rate,1);
                // P wave, QRS spike, T wave; the hand wobbles a little at every point.
                float y=Mathf.Exp(-Mathf.Pow((phase-.18f)/.035f,2))*.18f-Mathf.Exp(-Mathf.Pow((phase-.30f)/.012f,2))*.25f+Mathf.Exp(-Mathf.Pow((phase-.34f)/.014f,2))-Mathf.Exp(-Mathf.Pow((phase-.39f)/.014f,2))*.35f+Mathf.Exp(-Mathf.Pow((phase-.58f)/.06f,2))*.28f;
                y+=(Mathf.PerlinNoise(i*.37f,t*.6f)-.5f)*.07f;
                var point=new Vector2(x,baseline-y*height);
                if(i>0)InkSegment(previous,point,ink);
                previous=point;
            }
        }
        void InkSegment(Vector2 a,Vector2 b,Color c)
        {
            var d=b-a;float length=d.magnitude;if(length<.01f)return;
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,a);
            Panel(new Rect(a.x,a.y-.9f,length+.6f,1.8f),c);GUI.matrix=matrix;
        }
        void Note()
        {
            Backdrop(false,.48f);Brand("RECOVERED DOCUMENT / "+Book.CaseTag);
            Card(new Rect(80,166,1440,633));Track(115,190,Book.ArchiveTrack);Label(112,224,1365,65,Game.NoteTitle,heading);Rule(116,300,1368);
            // Art per map from CaseBook.NoteArt; a missing castle page falls back to text only.
            string asset=Book.NoteArtFor(Game.NoteTitle);
            var texture=asset==null?null:Resources.Load<Texture2D>(asset);
            if(texture)
            {
                var area=new Rect(125,338,580,364);float fit=Mathf.Min(area.width/texture.width,area.height/texture.height);
                var paper=new Rect(area.center.x-texture.width*fit*.5f-8,area.center.y-texture.height*fit*.5f-8,texture.width*fit+16,texture.height*fit+16);
                Panel(paper,new Color(.67f,.62f,.51f));Artwork(area,texture);
                Track(756,332,"FIELD TRANSCRIPT",muted);Label(756,371,714,315,Game.NoteBody,transcriptStyle);
            }
            else Label(116,334,1170,350,Game.NoteBody,largeNoteStyle);
            if(Action(115,734,360,46,Game.Journal?"RETURN TO JOURNAL":"FILE & CLOSE","ESC",true)){Game.NoteBody=null;Game.ApplyFocus();}
            Footer("Recovered documents remain listed in your journal.   /   ESC close");
        }
        void Update()
        {
            if(Game==null)return;ControllerNavigation();
            if(Game==null||!Game.Journal||Game.NoteBody!=null||Game.Paused||Game.Loading)return;
            for(int i=0;i<9;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))
            {if(i<3)anchor=i;else if(i<6)pattern=i-3;else closure=i-6;if(NativeVerification.UIOnly)Debug.Log("HC_UI_KEY_CHOICE "+(i+1));}
            if(Input.GetKeyDown(KeyCode.Return)){if(NativeVerification.UIOnly)Debug.Log("HC_UI_KEY_SUBMIT ready="+CanSubmit());if(CanSubmit())Submit();}
        }
        bool CanSubmit()=>Game.Case.MysteryReady&&anchor>=0&&pattern>=0&&closure>=0;
        void Choice(float y,string label,string[] choices,ref int selected,int key)
        {
            Track(760,y,label,muted);
            for(int i=0;i<3;i++)if(Action(760+i*238,y+35,226,66,choices[i],(key+i).ToString(),false,false,selected==i))selected=i;
        }
        void Journal()
        {
            Backdrop(false,.52f);Brand("FIELD JOURNAL / "+Book.CaseTag);Label(77,147,1100,62,Book.JournalQuestion,heading);
            Card(new Rect(80,229,610,568));Card(new Rect(728,229,792,568));Track(111,252,"INSTRUMENT RECORDS");
            float y=293;
            foreach(Evidence e in Enum.GetValues(typeof(Evidence)))
            {
                bool yes=Game.Case.Evidence.Contains(e);Panel(new Rect(111,y+4,3,67),yes?copper:line);
                Track(130,y,yes?"RECORDED":"NOT RECORDED",yes?copper:muted);Label(130,y+26,526,65,EvidenceText(e,yes),evidenceStyle);y+=90;
            }
            Rule(111,570,548);Track(111,592,Book.ContextTrack);
            if(Game.Map==MapId.Castle){Track(111,734,"PULSE",muted);PulseLine(new Rect(190,728,469,58));}
            string[] names=Book.NoteTitles;
            for(int i=0;i<3;i++)
            {
                bool filed=Game.Case.Notes.Contains(names[i]);GUI.enabled=filed;
                if(Action(110,620+i*34,549,32,names[i],filed?"OPEN":"—",false,true))OpenFiledNote(names[i]);GUI.enabled=true;
            }
            Choice(260,"ANCHOR / WHAT KEEPS RETURNING?",Book.AnchorChoices,ref anchor,1);
            Choice(387,"PATTERN / WHAT IS REPEATED?",Book.PatternChoices,ref pattern,4);
            Choice(514,"CLOSURE / WHAT SHOULD WE DO?",Book.ClosureChoices,ref closure,7);
            Label(760,630,718,48,Game.Case.Diagnosed?"Reconstruction filed. Follow the containment procedure.":Game.Case.MysteryReady?Book.JournalReady:Book.JournalNotReady,small);
            GUI.enabled=CanSubmit();if(Action(760,690,718,46,"FILE RECONSTRUCTION","ENTER",true))Submit();GUI.enabled=true;
            if(Action(1240,149,280,48,"CLOSE JOURNAL","J",false,true)){Game.Journal=false;Game.ApplyFocus();}
            Footer("1–9 choose a conclusion   ·   ENTER file reconstruction   ·   J or ESC close");
        }
        void OpenFiledNote(string title)
        {foreach(var item in Game.World.Root.GetComponentsInChildren<Interactable>())if(item.Kind==InteractKind.Note&&item.Label==title){item.Use(Game);return;}}
        string EvidenceText(Evidence e,bool yes)
        {
            return yes?Book.EvidenceRecorded[(int)e]:Book.EvidenceMissing[(int)e];
        }
        void Submit(){if(!CanSubmit())return;Game.Case.Conclude((AnchorKind)anchor,(HauntingPattern)pattern,(ClosureMethod)closure);Game.Journal=false;Game.ApplyFocus();}
        string Location(){var p=Game.Player;var b=Book;if(p.InVan)return b.LocSafe;if(!p.Inside)return b.LocOutside;float y=p.transform.position.y;return y< -1?b.LocLower:y>b.TowerY?b.LocTower:y>2?b.LocUpper:b.LocGround;}
        void Dialogue(Rect rect,string speaker,string text,bool danger,float fade)
        {
            TextureFrame(rect,danger?warningCard:dialogueCard,new Color(1,1,1,Game.CardOpacity*fade));
            var old=GUI.color;GUI.color=new Color(1,1,1,fade);
            var oldKicker=kicker.normal.textColor;kicker.normal.textColor=danger?new Color(.82f,.39f,.34f):paperInk;
            float left=rect.x+rect.width*.17f;
            GUI.Label(new Rect(left,rect.y+rect.height*.13f,rect.width*.74f,25),speaker,kicker);kicker.normal.textColor=oldKicker;
            Panel(new Rect(left,rect.y+rect.height*.29f,Mathf.Min(155,rect.width*.3f),1),danger?new Color(.7f,.2f,.2f):oxblood);
            dialogueText.fontSize=Mathf.RoundToInt(24*Game.SubtitleScale);dialogueText.normal.textColor=danger?ink:paperInk;
            GUI.Label(new Rect(left,rect.y+rect.height*.35f,rect.width*.72f,rect.height*.53f),text,dialogueText);
            GUI.color=old;
        }
        void SubtitleCard()
        {
            if(cachedSubtitle!=Game.Subtitle)
            {
                cachedSubtitle=Game.Subtitle??"";dialogueBody=cachedSubtitle;dialogueSpeaker="FIELD SIGNAL / LOCAL";
                int divider=cachedSubtitle.IndexOf(" / ");
                if(divider>=0){dialogueSpeaker=cachedSubtitle.Substring(0,divider).ToUpperInvariant();dialogueBody=cachedSubtitle.Substring(divider+3);}
                
                if(dialogueSpeaker=="DISPATCH"||dialogueSpeaker=="RADIO")dialogueSpeaker+=" / CHANNEL 04";
                if(dialogueSpeaker=="EVIDENCE SECURED")dialogueSpeaker=Book.CaseTag+" / EVIDENCE RECORDED";
            }
            dialogueText.fontSize=Mathf.RoundToInt(24*Game.SubtitleScale);
            float height=Mathf.Clamp(dialogueText.CalcHeight(new GUIContent(dialogueBody),460)/.53f,125,245);
            bool danger=Game.Case.Phase==Phase.Warning||Game.Case.Phase==Phase.Hunt||Game.Scares.Interference||Game.Map==MapId.Castle&&Game.Case.HuntActive;
            float fade=Mathf.Clamp01(Game.SubtitleTime/.4f);
            Dialogue(new Rect(495,858-height,610,height),dialogueSpeaker,dialogueBody,danger,fade);
        }
        void KeyPill(float x,float y,string key,string text)
        {
            Border(new Rect(x,y,34,26),new Color(.54f,.47f,.35f,.8f));Label(x,y,34,26,key,keyStyle);Label(x+45,y+2,240,26,text,hudTiny);
        }
        void HUD()
        {
            if(shownRecords!=Game.Case.Evidence.Count){shownRecords=Game.Case.Evidence.Count;recordLabel=Book.CaseTag+" / "+shownRecords+" RECORDS";}
            Track(42,24,"OBJECTIVE",ink);Rule(42,53,320);Track(660,24,"HOLLOW CURRENT",ink);Rule(660,53,285);Label(42,63,690,70,Game.Case.Goal,hudGoal);
            Label(1290,28,268,28,Location(),rightBody);Panel(new Rect(1355,62,200,1),copper);Label(1290,73,268,24,recordLabel,rightBody);
            Panel(new Rect(798,448,4,4),new Color(.8f,.8f,.7f,.7f));var p=Game.Player;
            string use=PadInput.Active?(PadInput.Current.PlayStation?"[×] ":"[A] "):"[E] ",torch=PadInput.Active?(PadInput.Current.PlayStation?"[□] ":"[X] "):"[F] ";
            string interaction=p.Hidden?use+"Leave hiding place · "+(p.TorchOn?torch+"TORCH OFF":"Stay quiet"):p.Focus!=null?use+p.Focus.Label+(p.Focus.Kind==InteractKind.Anchor?" · hold":""):"";
            Label(405,510,790,62,interaction,center);if(p.Capture>0)Panel(new Rect(630,568,340*Mathf.Clamp01(p.Capture),3),copper);
            if(Game.Castle&&Game.Castle.Forge.CraftProgress01>0&&!Game.Castle.Forge.HasSword){Label(545,583,510,30,"FORGING "+Mathf.RoundToInt(Game.Castle.Forge.CraftProgress01*100)+"%",center);Panel(new Rect(615,622,370*Game.Castle.Forge.CraftProgress01,3),copper);}
            if(Game.Castle&&(Game.Castle.Forge.RelicCount>0||Game.Castle.Forge.HasSword))Label(1290,100,268,24,Game.Castle.Forge.HasSword?"MOONSTONE SWORD · "+Game.Castle.Fight.Hits+" / 3":"RELICS "+Game.Castle.Forge.RelicCount+" / 3",rightBody);
            if(Game.Case.SealProgress>0){Label(545,583,510,30,"SEALING "+Mathf.RoundToInt(Game.Case.SealProgress*100)+"%",center);Panel(new Rect(615,622,370*Game.Case.SealProgress,3),copper);}
            if(Game.Subtitles&&Game.SubtitleTime>0&&FieldMessages.ShowCard(Game.Subtitle))SubtitleCard();
            Track(42,796,Book.InstrumentNames[Mathf.Clamp(p.Tool-1,0,2)],ink);
            Label(42,824,290,48,Game.Scares.Interference?"SIGNAL UNSTABLE":p.Capture>0?"RECORDING… HOLD STEADY":p.Tool==1?p.InductionDisplay+" / 5.0":"FIELD KIT CONNECTED",hudTiny);
            bool ps=PadInput.Active&&PadInput.Current.PlayStation;
            KeyPill(1315,800,PadInput.Active?(ps?"R2":"RT"):"LMB",p.Tool==3?"ASK / RECORD":"HOLD TO RECORD");
            KeyPill(1315,840,PadInput.Active?(ps?"□":"X"):"F","FLASHLIGHT");
            if(Game.HelpHints)Label(365,869,855,25,PadInput.Active?(ps?"× INTERACT   △ JOURNAL   L2 RADIO   L3 SPRINT   ○ CROUCH":"A INTERACT   Y JOURNAL   LT RADIO   LS SPRINT   B CROUCH"):"E INTERACT   J JOURNAL   R RADIO   SHIFT SPRINT   CTRL CROUCH",hudTiny);
            if(p.Stamina<.99f){Panel(new Rect(44,878,220,2),line);Panel(new Rect(44,878,220*p.Stamina,2),copper);}
            if(Game.Case.Phase==Phase.Warning)Label(540,106,520,44,"SIGNAL COLLAPSE  "+Mathf.Max(0,Mathf.CeilToInt((Game.Map==MapId.Castle?FearLink.WarningSeconds(Game.Case):9)-Game.Case.PhaseTime)),center);
        }
    }
}
