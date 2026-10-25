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
        // Castle journal / end card (fd-journal): all text on the paper is held to >= 7:1 against the darkest 5 % of the paper, so near-black ink and oxblood replace the lighter ones there.
        // owner 2026-10-20 (approved variant A, smoked glass with a gold outline): castle cards are translucent black glass, so their ink is
        // bone white with antique-gold kickers (was oxblood 32,4,4 / ink 20,13,12 on the old vellum).
        readonly Color castleKicker=new Color32(200,162,98,255),castleInk=new Color32(234,224,206,255);bool castleCards;
        // fe-ui: label / rule colour of a disabled control on the castle card (dark umber; contrast measured by tools/docui_journal_mock.py).
        readonly Color castleDisabled=new Color32(126,108,92,255);
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
        public void ShowSettingsPage(int page){settingsPage=page;Game.Paused=true;Game.ApplyFocus();}   // fe-integ: castle driver mode "settings"
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
        static bool RealKey(KeyCode k)=>!Game.ScriptedRun&&Input.GetKeyDown(k);
        void OnGUI()
        {
            if(Game==null)return;
            // Owner rule: a real click or key press never reaches the menus of a scripted run.
            if(Game.ScriptedRun&&(Event.current.isMouse||Event.current.isKey||Event.current.type==EventType.ScrollWheel))Event.current.Use();Styles();paperRegions.Clear();
            RefreshMenu();if(Event.current.type==EventType.Layout)menu.Clear();
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(Game.Loading){Loading();return;}
            if(!Game.Started){if(Game.Paused)Settings(true);else Title();return;}
            // fd-grab: the grab's HUD record (HC_GRAB_HUD) covers the kill presentation too.
            bool grabNow=Game.Map==MapId.Castle&&Game.Creature&&(Game.Creature.GrabCamActive||Game.Creature.GrabInProgress);
            if(Game.Case.Terminal)
            {
                if(Game.Case.Phase==Phase.Dead&&Game.Creature.PresentationRemaining>0)
                {
                    float fade=1-Game.Creature.PresentationRemaining/Game.Creature.Profile.CapturePresentationSeconds;
                    if(grabNow)GrabHudFrame(true,Mathf.Pow(fade,3)>.004f);
                    // The presentation's own fade to black (not HUD): nothing else is drawn over the kill.
                    Panel(new Rect(0,0,1600,900),new Color(.005f,.008f,.01f,Mathf.Pow(fade,3)));return;
                }
                GrabHudEnd("presentation over");
                Ending();return;
            }
            if(Game.NoteBody!=null){Note();return;}
            if(Game.Journal){Journal();return;}
            if(Game.Paused){Settings(false);return;}
            // Castle only (r12 critic): during a grab or kill presentation no prompt, hint, objective or instrument line is drawn;
            // only captions stay. Bellwether never takes this path.
            // fd-grab (r13 natural2: the dispatch card "He is out. He is over the courtyard..." stayed on every Return grab frame): from the
            // commit frame nothing is drawn at all, captions and dispatch cards included; they resume (if still timed) after the grab.
            bool grabHidden=grabNow;
            if(grabHidden!=hudGrabHidden){hudGrabHidden=grabHidden;Debug.Log("HC_UI grab_hidden="+(grabHidden?"true":"false"));}
            if(grabHidden){GrabHudFrame(false,false);return;}
            GrabHudEnd("grab over");
            HUD();
        }
        bool hudGrabHidden;
        // HC_GRAB_HUD per grab: repaint frames under the grab, HUD draws in them (Panel/Label/Track/Artwork/TextureFrame count while
        // grabHud is set; must stay 0), captions held back, and frames with the kill's fade to black (presentation, not HUD).
        bool grabHud;int grabHudFrames,grabHudDraws,grabHudCaptions,grabHudFade;
        void GrabHudFrame(bool presenting,bool fading)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(!grabHud){grabHud=true;grabHudFrames=grabHudDraws=grabHudCaptions=grabHudFade=0;}
            grabHudFrames++;if(fading)grabHudFade++;
            if(Game.Subtitles&&Game.SubtitleTime>0&&FieldMessages.ShowCard(Game.Subtitle))grabHudCaptions++;
        }
        void GrabHudEnd(string why)
        {
            if(!grabHud)return;grabHud=false;
            Debug.Log("HC_GRAB_HUD pixels="+(grabHudDraws==0?"0":"unknown")+" draws="+grabHudDraws+" frames="+grabHudFrames+" captionsHeld="+grabHudCaptions+" fadeFrames="+grabHudFade+" end="+why);
        }
        void CountHudDraw(){if(grabHud)grabHudDraws++;}
        void Panel(Rect r,Color c){if(grabHud&&!(Game.Case!=null&&Game.Case.Terminal))CountHudDraw();var old=GUI.color;GUI.color=c.linear;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Border(Rect r,Color c){Panel(new Rect(r.x,r.y,r.width,1),c);Panel(new Rect(r.x,r.yMax-1,r.width,1),c);Panel(new Rect(r.x,r.y,1,r.height),c);Panel(new Rect(r.xMax-1,r.y,1,r.height),c);}
        bool OnPaper(Vector2 point){foreach(var r in paperRegions)if(r.Contains(point))return true;return false;}
        void Label(float x,float y,float w,float h,string text,GUIStyle style=null)
        {
            CountHudDraw();
            style=style??body;
            if(OnPaper(new Vector2(x+5,y+5)))
            {
                if(!paperStyles.TryGetValue(style,out var dark)){dark=new GUIStyle(style);paperStyles[style]=dark;}
                dark.fontSize=style.fontSize;dark.normal.textColor=style==kicker?(castleCards?castleKicker:oxblood):castleCards?castleInk:paperInk;GUI.Label(new Rect(x,y,w,h),text,dark);
            }
            else GUI.Label(new Rect(x,y,w,h),text,style);
        }
        void Rule(float x,float y,float w)=>Panel(new Rect(x,y,w,1),line);
        void Track(float x,float y,string text,Color? color=null)
        {
            CountHudDraw();
            var old=kicker.normal.textColor;kicker.normal.textColor=OnPaper(new Vector2(x,y))?(castleCards?castleKicker:oxblood):color??copper;
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
            CountHudDraw();
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
            CountHudDraw();
            if(Event.current.type!=EventType.Repaint)return;
            var matrix=GUI.matrix;var position=matrix.MultiplyPoint3x4(new Vector3(rect.x,rect.y,0));var size=matrix.MultiplyVector(new Vector3(rect.width,rect.height,0));GUI.matrix=Matrix4x4.identity;
            artworkMaterial.SetColor("_Tint",tint.linear);Graphics.DrawTexture(new Rect(position.x,position.y,size.x,size.y),texture,new Rect(0,0,1,1),0,0,0,0,Color.white,artworkMaterial);GUI.matrix=matrix;
        }
        void Card(Rect r){TextureFrame(new Rect(r.x-18,r.y-42,r.width+36,r.height+72),paperPanel,new Color(1,1,1,.97f));paperRegions.Add(r);}
        void DarkCard(Rect r){TextureFrame(r,warningCard,new Color(1,1,1,.96f));}
        // Piece fd-journal (r13 critic section 4): the castle's paper card is drawn as a nine-slice at one fixed scale so its corner
        // ornaments (filigree, feather, wax seal, blots) always sit in the four corners; text is laid out by Core/DocLayout.cs, which keeps
        // DocPanel.Pad clear of them. Bellwether and the settings screens keep the stretched Card().
        Texture2D castlePanel;bool castlePanelTried;
        bool CastlePanel
        {
            get
            {
                if(Game.Map!=MapId.Castle)return false;
                if(!castlePanelTried){castlePanelTried=true;castlePanel=Resources.Load<Texture2D>("UI/BloodLedger/PanelCard");Debug.Log("HC_DOC_PANEL card="+(castlePanel?castlePanel.width+"x"+castlePanel.height:"missing"));}
                return castlePanel!=null&&castlePanel.width==Mathf.RoundToInt(DocPanel.TexW)&&castlePanel.height==Mathf.RoundToInt(DocPanel.TexH);
            }
        }
        void CastleCard(LRect frame,Rect content)
        {
            paperRegions.Add(content);
            if(Event.current.type!=EventType.Repaint)return;
            float[] cx=DocPanel.CellX(frame),cy=DocPanel.CellY(frame);float[] tx={0,DocPanel.ColL,DocPanel.ColR,DocPanel.TexW},ty={0,DocPanel.RowT,DocPanel.RowB,DocPanel.TexH};
            var matrix=GUI.matrix;artworkMaterial.SetColor("_Tint",new Color(1,1,1,.97f).linear);
            for(int j=0;j<3;j++)for(int i=0;i<3;i++)
            {
                var a=matrix.MultiplyPoint3x4(new Vector3(cx[i],cy[j],0));var b=matrix.MultiplyPoint3x4(new Vector3(cx[i+1],cy[j+1],0));
                // fd-integ: whole-pixel cell edges. With fractional edges the draw rounded each cell's position and size separately, so a
                // neighbour could start one pixel after this cell ended: a 1 px dark seam showed at ColR and RowB on both journal cards.
                a.x=Mathf.Round(a.x);a.y=Mathf.Round(a.y);b.x=Mathf.Round(b.x);b.y=Mathf.Round(b.y);
                var src=new Rect(tx[i]/DocPanel.TexW,1-ty[j+1]/DocPanel.TexH,(tx[i+1]-tx[i])/DocPanel.TexW,(ty[j+1]-ty[j])/DocPanel.TexH);
                GUI.matrix=Matrix4x4.identity;Graphics.DrawTexture(new Rect(a.x,a.y,b.x-a.x,b.y-a.y),castlePanel,src,0,0,0,0,Color.white,artworkMaterial);GUI.matrix=matrix;
            }
        }
        // `HC_DOC_LAYOUT overlaps=N`: the geometric assertion that no text or control of this screen touches an ornament or leaves the paper.
        readonly HashSet<string> layoutLogged=new HashSet<string>();
        void LogLayout(DocLayout layout,string extra)
        {
            if(!layoutLogged.Add(layout.Screen))return;
            var bad=layout.Verify();
            Debug.Log("HC_DOC_LAYOUT screen="+layout.Screen+" overlaps="+bad.Count+" frames="+layout.Frames.Count+" texts="+layout.Texts.Count+" scale="+DocPanel.Scale+" pad="+DocPanel.Pad+" "+extra);
            foreach(var line in bad)Debug.LogError("HC_DOC_LAYOUT violation "+line);
        }
        bool Action(float x,float y,float w,float h,string text,string key="",bool primary=false,bool plain=false,bool selected=false,string controlId=null)
        {
            var rect=new Rect(x,y,w,h);string id="hc-ui/"+(controlId??text);
            Register(id,rect);
            bool padFocus=PadInput.Active&&focusedId==id;
            bool focus=(!PadInput.Active&&GUI.GetNameOfFocusedControl()==id)||padFocus,hover=!PadInput.Active&&rect.Contains(Event.current.mousePosition)&&GUI.enabled;
            // fe-ui: a disabled button on the castle's vellum card is not the dimmed dark art (grey on beige, unreadable): it is an outlined
            // slip in umber (castleDisabled, >= 4.5:1 on the paper), so the label stays legible and the control still reads as unavailable.
            bool slip=!GUI.enabled&&castleCards&&!plain;
            if(slip){Panel(rect,new Color(castleDisabled.r,castleDisabled.g,castleDisabled.b,.07f));Border(rect,castleDisabled);}
            else if(!plain||hover||focus)
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
            var old=button.normal.textColor;button.normal.textColor=GUI.enabled?(plain&&!hover&&!focus&&OnPaper(rect.center)?(castleCards?castleInk:paperInk):ink):slip?castleDisabled:new Color(.47f,.43f,.38f);
            button.padding.left=Mathf.Clamp(Mathf.RoundToInt(w*.14f),22,68);button.padding.right=key.Length>0?80:22;GUI.Label(rect,text,button);button.normal.textColor=old;
            if(key.Length>0){var keyRect=new Rect(x+w-66,y+(h-24)/2,48,24);Border(keyRect,slip?castleDisabled:GUI.enabled?line:new Color(.16f,.18f,.17f));var oldKey=keyStyle.normal.textColor;keyStyle.normal.textColor=slip?castleDisabled:plain&&!hover&&!focus&&OnPaper(rect.center)?(castleCards?castleInk:paperInk):ink;GUI.Label(keyRect,key,keyStyle);keyStyle.normal.textColor=oldKey;}
            if(clicked)Game.Sound.UI();
            return clicked;
        }
        // Title (piece T1): full-bleed key art, left-aligned type, two map cards and three actions.
        // Drawing lives in TitleScreen; layout, input and controller focus live here.
        void Title()
        {
            var screen=GetComponent<TitleScreen>();if(!screen)screen=gameObject.AddComponent<TitleScreen>();
            screen.Prepare(serif);var book=Book;
            screen.Backdrop(artworkMaterial,BackdropFor(book),(int)Game.SelectedMap);
            // Brand: two tracked lines only.
            screen.Tracked("HOLLOW CURRENT",80,44,12,3,new Color(ink.r,ink.g,ink.b,.6f),false);
            screen.Tracked("FIELD INVESTIGATION OFFICE",80,62,12,3,new Color(muted.r,muted.g,muted.b,.6f),false);
            // Type block.
            screen.Tracked("HOLLOW",80,78,96,10,ink);
            float titleWidth=screen.Tracked("CURRENT",80,168,96,10,ink);
            Panel(new Rect(80,280,titleWidth,1),copper);
            screen.Tracked(book.TitleName,80,296,34,2.5f,ink);
            if(titleTagline==null)titleTagline=new GUIStyle(body){fontStyle=FontStyle.Italic};
            titleTagline.normal.textColor=new Color(ink.r,ink.g,ink.b,.7f);
            // Fixed 23.5 design-px line pitch, as in the approved mock (critic r10 #41), instead of the font's line height.
            var taglineLines=book.Tagline.Split('\n');
            for(int i=0;i<taglineLines.Length;i++)Label(80,350+i*23.5f,640,30,taglineLines[i],titleTagline);
            // BEGIN is registered first so a controller lands on it; the map cards sit above it on screen.
            if(TitleBegin(screen,new Rect(80,668,680,60)))Game.BeginInvestigation();
            for(int i=0;i<MapCatalog.All.Count;i++)TitleCard(screen,MapCatalog.All[i],new Rect(80+i*350,440,330,185));
            if(TitleLink(screen,80,766,"FIELD SETTINGS","FIELD SETTINGS")){Game.Paused=true;Game.ApplyFocus();}
            if(TitleLink(screen,310,766,"EXIT TO DESKTOP","EXIT TO DESKTOP"))Game.Quit();
            Footer("1 / 2 choose map   ·   WASD move   ·   Mouse look   ·   E interact   ·   Headphones suggested");
        }
        void Loading()
        {
            Backdrop(false,.15f);Brand("FIELD OFFICE  /  PREPARING CASE");Card(new Rect(45,210,750,555));Track(80,245,Book.LoadingTrack);
            Label(76,288,680,178,"A new\ninvestigation.",hero);
            Label(82,520,540,50,Game.LoadingProgress<.01f?"Preparing the case files…":"Restoring the house and field equipment…");
            Rule(82,604,510);Panel(new Rect(82,603,510*Mathf.Clamp01(Game.LoadingProgress),3),copper);
            Label(82,631,500,35,Game.LoadingProgress<.01f?"PLEASE WAIT":"LOADING  "+Mathf.RoundToInt(Game.LoadingProgress*100)+"%",kicker);
            // fg-integ (r15 critic: "Flashlight" and "radio" leak into the castle UI): castle wording here, Bellwether unchanged.
            Footer(CastleScreen?"Listen before moving. Footsteps and the resonance box can give your position away.":"Listen before moving. Footsteps and radio calls can give your position away.");
        }
        void Metric(float x,float y,string value,string label)
        {Label(x,y,180,55,value,number);Track(x,y+65,label,muted);}
        // Castle won / killed / lost card on the fixed-scale panel (layout from DocLayout.Ending(lost)). fe-ui: the lost card moved here from
        // the stretched dark card, so its text column, buttons and ornaments follow the same safe-rect rule.
        DocLayout.EndingSpec endingSpec,lostSpec;
        void EndingCastle(bool won)
        {
            var e=won?(endingSpec??(endingSpec=DocLayout.Ending())):(lostSpec??(lostSpec=DocLayout.Ending(true)));LogLayout(e.Layout,"x="+e.X);castleCards=true;
            Backdrop(!won,won?0:.14f);Brand();CastleCard(e.Frame,new Rect(e.Content.X,e.Content.Y,e.Content.W,e.Content.H));Track(e.Kicker.X,e.Kicker.Y,won?Book.CaseTag+" / CLOSED":"LAST TRANSMISSION / LOST");
            bool slain=won&&Game.Case.HunterSlain;
            Label(e.Hero.X,e.Hero.Y,e.Hero.W,e.Hero.H,won?(slain?Book.KillHero:Book.WonHero):"Signal\nlost.",hero);
            Label(e.Body.X,e.Body.Y,e.Body.W,e.Body.H,won?(slain?Book.KillBody:Book.WonBody):Book.LostBody);
            Metric(e.MetricX[0],e.MetricY,Game.Case.Evidence.Count+" / 3","EVIDENCE");Metric(e.MetricX[1],e.MetricY,(Game.Case.Survived?Game.Case.Hunts:0).ToString(),"HUNTS SURVIVED");Metric(e.MetricX[2],e.MetricY,TimeSpan.FromSeconds(Game.Case.Elapsed).ToString(@"mm\:ss"),"FIELD TIME");
            Panel(new Rect(e.Separator[0],e.MetricY+9,1,76),line);Panel(new Rect(e.Separator[1],e.MetricY+9,1,76),line);
            if(!won&&Game.HasCheckpoint&&Action(e.Retry.X,e.Retry.Y,e.Retry.W,e.Retry.H,"RETRY CHECKPOINT","",true))Game.RetryCheckpoint();
            var nb=!won&&!Game.HasCheckpoint?e.NewSolo:e.New;
            if(Action(nb.X,nb.Y,nb.W,nb.H,"NEW INVESTIGATION","N",won))Game.Retry();
            if(Action(e.Exit.X,e.Exit.Y,e.Exit.W,e.Exit.H,"EXIT TO DESKTOP","Q",false,true))Game.Quit();
            castleCards=false;Footer(won?(slain?Book.KillFooter:Book.WonFooter):Book.LostFooter);
        }
        void Ending()
        {
            bool won=Game.Case.Phase==Phase.Won;
            if(CastlePanel){EndingCastle(won);return;}Backdrop(!won,won?0:.14f);Brand();if(won)Card(new Rect(45,145,850,685));else DarkCard(new Rect(45,145,850,685));Track(80,175,won?Book.CaseTag+" / CLOSED":"LAST TRANSMISSION / LOST");
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
            // Bellwether keeps its label-derived controller ids (critic r10 #24); only the castle's three-page cycle uses a fixed id.
            if(Action(80,544,340,42,nextPage,"",false,false,settingsPage!=0,CastleScreen?"settings-page":null))settingsPage=settingsPage==0?1:settingsPage==1&&CastleScreen?2:0;
            // fe-ui: on the castle the card is the fixed-scale nine-slice and every control sits where DocLayout.Settings() puts it
            // (logged as HC_DOC_LAYOUT screen=settings); Bellwether keeps the stretched Card and its old positions.
            bool cc=CastlePanel;DocLayout.SettingsSpec sp=null;
            if(cc){sp=settingsSpec??(settingsSpec=DocLayout.Settings());LogLayout(sp.Layout,"x0="+sp.X0+" lowY="+sp.LowY+" lowW="+sp.LowW);CastleCard(sp.Frame,new Rect(sp.Content.X,sp.Content.Y,sp.Content.W,sp.Content.H));castleCards=true;}
            else Card(new Rect(520,173,1000,635));
            float sx=cc?sp.X0:558,sw=924;
            if(settingsPage==2){CastleSettings(sx,sw,sp);castleCards=false;return;}
            if(settingsPage==1)
            {
                Track(sx,202,Book.CardStyle+" / DIALOGUE DISPLAY");
                Game.SubtitleScale=SliderRow(sx,256,sw,"Subtitle size",Game.SubtitleScale,.85f,1.3f,Mathf.RoundToInt(Game.SubtitleScale*100)+"%","Larger type for living-room play");
                Game.CardOpacity=SliderRow(sx,372,sw,"Card opacity",Game.CardOpacity,.55f,1,Mathf.RoundToInt(Game.CardOpacity*100)+"%","Message text stays fully readable");
                Game.Subtitles=ToggleRow(sx,487,cc?sp.ToggleW(487):sw,"Subtitles",Game.Subtitles);
                Dialogue(new Rect(650,555,740,220),"DISPATCH / CHANNEL 04","The signal is changing. Keep your light steady.",false,1);
                castleCards=false;Footer("Preview your dialogue cards. Preferences are saved when you return.");return;
            }
            Track(sx,202,"CONTROLS & FIELD EQUIPMENT");
            Game.Player.Sensitivity=SliderRow(sx,244,sw,"Look sensitivity",Game.Player.Sensitivity,.3f,4,Game.Player.Sensitivity.ToString("F2"),"[1 / 2] Adjust mouse / right-stick sensitivity");
            Game.Player.Brightness=SliderRow(sx,356,sw,CastleScreen?"Lantern level":"Flashlight level",Game.Player.Brightness,.7f,1.8f,Mathf.RoundToInt(Game.Player.Brightness*100)+"%","[3 / 4] Adjust light output");
            Game.Volume=SliderRow(sx,468,sw,"Master volume",Game.Volume,0,1,Mathf.RoundToInt(Game.Volume*100)+"%","[5 / 6] Adjust volume");
            Rule(sx,579,sw);Game.Player.InvertY=ToggleRow(sx,597,cc?sp.ToggleW(597):sw,"Invert look Y",Game.Player.InvertY,"[I]");
            Game.Subtitles=ToggleRow(sx,650,cc?sp.ToggleW(650):sw,"Subtitles",Game.Subtitles,"[T]");
            Game.HelpHints=ToggleRow(sx,703,cc?sp.ToggleW(703):sw,"Detailed field help",Game.HelpHints,"[H]");
            castleCards=false;Footer(CastleScreen?"ESC return   ·   Shift sprint   ·   Ctrl crouch   ·   E interact   ·   F lantern   ·   R resonance box":"ESC return   ·   Shift sprint   ·   Ctrl crouch   ·   E interact   ·   F flashlight   ·   R radio");
        }
        DocLayout.SettingsSpec settingsSpec;
        // FEAR_PLAN items 6 and 19: the Long Night dial and the opt-in microphone (castle only).
        void CastleSettings(float sx,float sw,DocLayout.SettingsSpec sp)
        {
            Track(sx,202,"THE COUNT'S NIGHT / FIELD RULES");
            bool longNight=ToggleRow(sx,244,sp!=null?sp.ToggleW(244):sw,"Long night",Game.LongNight);
            Label(sx,288,820,60,"No warning before a hunt, a darker start, longer hunts, a restless perch, and two hiding places nailed shut. "+(Game.Started&&Game.Map==MapId.Castle?"Takes effect on the next castle case.":"Takes effect when the castle case begins."),small);
            if(longNight!=Game.LongNight){Game.LongNight=longNight;Game.SavePreferences();}
            Rule(sx,360,sw);
            bool mic=ToggleRow(sx,380,sp!=null?sp.ToggleW(380):sw,"Microphone",Game.MicEnabled);
            Label(sx,424,820,60,"During a hunt he hears your voice through the microphone. macOS asks for microphone access the first time. Off by default.",small);
            if(mic!=Game.MicEnabled){Game.MicEnabled=mic;Game.SavePreferences();}
            GUI.enabled=Game.MicEnabled;
            float threshold=SliderRow(sx,500,sw,"Microphone threshold",Game.MicThreshold,.005f,.08f,Game.MicThreshold<.015f?"WHISPER":Game.MicThreshold<.035f?"SPEECH":"SHOUT","How loud you must be before he hears you");
            GUI.enabled=true;
            if(!Mathf.Approximately(threshold,Game.MicThreshold))Game.MicThreshold=threshold;
            if(Game.Player.MicLive)
            {
                // Live input level against the threshold (scaled so the threshold range fills the bar).
                Track(sx,612,"INPUT",muted);var bar=new Rect(sx+82,620,sw-82,4);Panel(bar,line);
                Panel(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(Game.Player.MicLevel/.1f),4),Game.Player.MicLevel>Game.MicThreshold?oxblood:copper);
                Panel(new Rect(bar.x+bar.width*Mathf.Clamp01(Game.MicThreshold/.1f),bar.y-6,2,16),paperInk);
            }
            Footer("Field rules for the castle. Preferences are kept between cases.");
        }
        // Castle (critic r10 #4): the HUD goal does not flip to the hunt goal. Through Hunt and Recovery it keeps the calm goal,
        // with the hunt-survived state frozen at the hunt's start.
        bool goalSurvived;
        // fb-onboard (owner feedback 2026-10-15, item 12): the castle objective is CastleObjective's chain (letter, ledger pages with
        // the rooms still to search, relics, forge, bell, winch), so a new player always has a next step. Bellwether: Case.Goal.
        public string HudGoal()
        {
            var c=Game.Case;
            if(Game.Map!=MapId.Castle)return c.Goal;
            return Objective().Text;
        }
        readonly bool[] relicsFound=new bool[3];
        int objectiveFrame=-1;CastleObjective.State objective;
        public CastleObjective.State Objective()
        {
            // Once per frame (OnGUI runs several events a frame; the strings are rebuilt only when asked again in a later frame).
            if(objectiveFrame==Time.frameCount)return objective;objectiveFrame=Time.frameCount;
            var c=Game.Case;
            if(c.Phase!=Phase.Hunt&&c.Phase!=Phase.Recovery)goalSurvived=c.Survived;
            var forge=Game.Castle?Game.Castle.Forge:null;
            for(int i=0;i<3;i++)relicsFound[i]=forge!=null&&forge.HasRelic((Relic)i);
            objective=CastleObjective.For(c,relicsFound,forge!=null&&forge.HasSword,goalSurvived);return objective;
        }
        public bool[] RelicsFound{get{Objective();return relicsFound;}}
        // The objective step and when it last changed (unscaled time), for the HUD's "objective updated" mark; CastleOnboarding logs it.
        string shownStep;float stepChangedAt=-99;
        GUIStyle noticeStyle;
        // The journal's pulse: a hand-drawn ink trace whose rate and height follow FeltDread (critic r10 #3: +-12 slow noise,
        // no hunt floor; hc/rules2's FeltDread lags and lies). Never a number (FEAR_PLAN item 1).
        void PulseLine(Rect r)
        {
            if(Event.current.type!=EventType.Repaint)return;
            float t=Time.unscaledTime;
            float dread=Mathf.Clamp(Soundscape.FeltDread(Game.Case)+(Mathf.PerlinNoise(t*.35f,.5f)*2-1)*12,0,100);
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
            // fd-integ: RotateAroundPivot takes its pivot in screen space (it does not apply the UI scale in GUI.matrix), so at any scale
            // other than 1 every segment rotated about the wrong point and the trace scattered into loose dashes over the ledger rows.
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,matrix.MultiplyPoint3x4(a));
            Panel(new Rect(a.x,a.y-.9f,length+.6f,1.8f),c);GUI.matrix=matrix;
        }
        readonly DocumentUi docs=new DocumentUi();
        // Found-document reader (piece fb-docui): full-bleed paper, one heading, quiet close line. Falls back to the old card
        // (NoteLegacy) only if the paper sheets are missing from Resources/UI/Documents.
        void Note()
        {
            docs.Load(serif);
            string asset=Book.NoteArtFor(Game.NoteTitle);
            var print=asset==null?null:Resources.Load<Texture2D>(asset);
            var pad=PadInput.Active?PadInput.Current:null;
            if(!docs.Draw(artworkMaterial,Game.Map==MapId.Castle,Game.NoteTitle,Game.NoteBody,print,pad!=null,pad!=null?pad.Navigate:Vector2.zero,pad!=null&&pad.PlayStation)){NoteLegacy();return;}
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
            bool clicked=TitleHit(docs.CloseRect,"NOTE CLOSE",out bool hover,out bool focus);
            GUI.matrix=matrix;
            docs.DrawClose(hover||focus);
            if(clicked){Game.NoteBody=null;Game.ApplyFocus();}
        }
        void NoteLegacy()
        {
            Backdrop(false,.48f);Brand("RECOVERED DOCUMENT / "+Book.CaseTag);
            Card(new Rect(80,166,1440,633));
            // Castle (r12 critic S8): the torn card's corner ornament covers x < 200, y < 275, so the kicker, title and rule start
            // at x 215 with an 8 px gap between kicker and title; Bellwether keeps its layout.
            bool castle=Game.Map==MapId.Castle;float nx=castle?215:115;
            if(castle){Track(nx,206,Book.ArchiveTrack);Label(nx-3,246,1250,62,Game.NoteTitle,heading);Rule(nx,314,1250);}
            else{Track(115,190,Book.ArchiveTrack);Label(112,224,1365,65,Game.NoteTitle,heading);Rule(116,300,1368);}
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
            else if(castle)Label(150,340,1250,350,Game.NoteBody,largeNoteStyle);
            else Label(116,334,1170,350,Game.NoteBody,largeNoteStyle);
            if(Action(115,734,360,46,Game.Journal?"RETURN TO JOURNAL":"FILE & CLOSE","ESC",true)){Game.NoteBody=null;Game.ApplyFocus();}
            Footer("Recovered documents remain listed in your journal.   /   ESC close");
        }
        void Update()
        {
            if(Game==null)return;ControllerNavigation();
            if(Game==null||!Game.Journal||Game.NoteBody!=null||Game.Paused||Game.Loading)return;
            for(int i=0;i<9;i++)if(RealKey((KeyCode)((int)KeyCode.Alpha1+i)))
            {if(i<3)anchor=i;else if(i<6)pattern=i-3;else closure=i-6;if(NativeVerification.UIOnly)Debug.Log("HC_UI_KEY_CHOICE "+(i+1));}
            if(RealKey(KeyCode.Return)){if(NativeVerification.UIOnly)Debug.Log("HC_UI_KEY_SUBMIT ready="+CanSubmit());if(CanSubmit())Submit();}
        }
        bool CanSubmit()=>Game.Case.MysteryReady&&anchor>=0&&pattern>=0&&closure>=0;
        void Choice(float y,string label,string[] choices,ref int selected,int key,float x0=760,float step=238,float width=226)
        {
            Track(x0,y,label,muted);
            for(int i=0;i<3;i++)if(Action(x0+i*step,y+35,width,66,choices[i],(key+i).ToString(),false,false,selected==i))selected=i;
        }
        // Castle journal on the fixed-scale card: every position comes from DocLayout.Journal(), which is verified clear of the ornaments.
        DocLayout.JournalSpec journalSpec;
        void JournalCastle()
        {
            var j=journalSpec??(journalSpec=DocLayout.Journal());
            LogLayout(j.Layout,"colL="+j.ColL+" lowerR="+j.LowerR+" x0="+j.X0);castleCards=true;
            Backdrop(false,.52f);Brand("FIELD JOURNAL / "+Book.CaseTag);Label(77,147,1100,62,Book.JournalQuestion,heading);
            CastleCard(j.LeftFrame,new Rect(j.LeftContent.X,j.LeftContent.Y,j.LeftContent.W,j.LeftContent.H));CastleCard(j.RightFrame,new Rect(j.RightContent.X,j.RightContent.Y,j.RightContent.W,j.RightContent.H));
            Track(j.Kicker.X,j.Kicker.Y,"INSTRUMENT RECORDS");
            int slot=0;
            foreach(Evidence e in Enum.GetValues(typeof(Evidence)))
            {
                bool yes=Game.Case.Evidence.Contains(e);var bar=j.Bar[slot];Panel(new Rect(bar.X,bar.Y,bar.W,bar.H),yes?copper:line);
                Track(j.Tag[slot].X,j.Tag[slot].Y,yes?"RECORDED":"NOT RECORDED",yes?copper:muted);var ev=j.Evidence[slot];Label(ev.X,ev.Y,ev.W,ev.H,EvidenceText(e,yes),evidenceStyle);slot++;
            }
            Rule(j.Rule.X,j.Rule.Y,j.Rule.W);docs.Load(serif);if(docs.Ready)docs.JournalKicker(j.LedgerKicker.X,j.LedgerKicker.Y,Book.ContextTrack,true,true);else Track(j.LedgerKicker.X,j.LedgerKicker.Y,Book.ContextTrack);
            Track(j.PulseLabel.X,j.PulseLabel.Y,"PULSE",muted);PulseLine(new Rect(j.Pulse.X,j.Pulse.Y,j.Pulse.W,j.Pulse.H));
            string[] names=Book.NoteTitles;
            for(int i=0;i<3;i++)
            {
                bool filed=Game.Case.Notes.Contains(names[i]);GUI.enabled=filed;var row=j.Rows[i];
                if(docs.Ready){if(JournalDocRow(new Rect(row.X,row.Y,row.W,row.H),names[i],filed))OpenFiledNote(names[i]);}
                else if(Action(row.X,row.Y,row.W,row.H,names[i],filed?"OPEN":"—",false,true))OpenFiledNote(names[i]);GUI.enabled=true;
            }
            Choice(j.ChoiceY[0],"ANCHOR / WHAT KEEPS RETURNING?",Book.AnchorChoices,ref anchor,1,j.X0,j.ChoiceStep,j.ChoiceW);
            Choice(j.ChoiceY[1],"PATTERN / WHAT IS REPEATED?",Book.PatternChoices,ref pattern,4,j.X0,j.ChoiceStep,j.ChoiceW);
            Choice(j.ChoiceY[2],"CLOSURE / WHAT SHOULD WE DO?",Book.ClosureChoices,ref closure,7,j.X0,j.ChoiceStep,j.ChoiceW);
            Label(j.Compare.X,j.Compare.Y,j.Compare.W,j.Compare.H,Game.Case.Diagnosed?"Reconstruction filed. Follow the containment procedure.":Game.Case.MysteryReady?Book.JournalReady:Book.JournalNotReady,small);
            GUI.enabled=CanSubmit();if(Action(j.Submit.X,j.Submit.Y,j.Submit.W,j.Submit.H,"FILE RECONSTRUCTION","ENTER",true))Submit();GUI.enabled=true;
            if(Action(1240,149,280,48,"CLOSE JOURNAL","J",false,true)){Game.Journal=false;Game.ApplyFocus();}
            castleCards=false;Footer("1–9 choose a conclusion   ·   ENTER file reconstruction   ·   J or ESC close");
        }
        void Journal()
        {
            if(CastlePanel){JournalCastle();return;}
            Backdrop(false,.52f);Brand("FIELD JOURNAL / "+Book.CaseTag);Label(77,147,1100,62,Book.JournalQuestion,heading);
            Card(new Rect(80,229,610,568));Card(new Rect(728,229,792,568));Track(111,252,"INSTRUMENT RECORDS");
            float y=293;
            foreach(Evidence e in Enum.GetValues(typeof(Evidence)))
            {
                bool yes=Game.Case.Evidence.Contains(e);Panel(new Rect(111,y+4,3,67),yes?copper:line);
                Track(130,y,yes?"RECORDED":"NOT RECORDED",yes?copper:muted);Label(130,y+26,526,65,EvidenceText(e,yes),evidenceStyle);y+=90;
            }
            Rule(111,570,548);docs.Load(serif);if(docs.Ready)docs.JournalKicker(111,592,Book.ContextTrack,Game.Map==MapId.Castle);else Track(111,592,Book.ContextTrack);
            if(Game.Map==MapId.Castle){Track(111,734,"PULSE",muted);PulseLine(new Rect(190,728,469,58));}
            string[] names=Book.NoteTitles;
            for(int i=0;i<3;i++)
            {
                bool filed=Game.Case.Notes.Contains(names[i]);GUI.enabled=filed;
                if(docs.Ready){if(JournalDocRow(new Rect(110,620+i*34,549,32),names[i],filed))OpenFiledNote(names[i]);}
                else if(Action(110,620+i*34,549,32,names[i],filed?"OPEN":"—",false,true))OpenFiledNote(names[i]);GUI.enabled=true;
            }
            Choice(260,"ANCHOR / WHAT KEEPS RETURNING?",Book.AnchorChoices,ref anchor,1);
            Choice(387,"PATTERN / WHAT IS REPEATED?",Book.PatternChoices,ref pattern,4);
            Choice(514,"CLOSURE / WHAT SHOULD WE DO?",Book.ClosureChoices,ref closure,7);
            Label(760,630,718,48,Game.Case.Diagnosed?"Reconstruction filed. Follow the containment procedure.":Game.Case.MysteryReady?Book.JournalReady:Book.JournalNotReady,small);
            GUI.enabled=CanSubmit();if(Action(760,690,718,46,"FILE RECONSTRUCTION","ENTER",true))Submit();GUI.enabled=true;
            if(Action(1240,149,280,48,"CLOSE JOURNAL","J",false,true)){Game.Journal=false;Game.ApplyFocus();}
            Footer("1–9 choose a conclusion   ·   ENTER file reconstruction   ·   J or ESC close");
        }
        // A recovered-document row in the journal, drawn in the same faces as the reader; hit testing and controller focus stay here.
        bool JournalDocRow(Rect r,string title,bool filed)
        {
            bool clicked=TitleHit(r,title,out bool hover,out bool focus);
            docs.JournalRow(r,title,filed,hover||focus,Game.Map==MapId.Castle,castleCards);
            return clicked&&filed;
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
            bool glass=Game.Map==MapId.Castle;
            var oldKicker=kicker.normal.textColor;kicker.normal.textColor=danger?new Color(.86f,.36f,.31f):glass?castleKicker:paperInk;
            float left=rect.x+rect.width*(glass?.08f:.17f),textW=rect.width*(glass?.84f:.72f);
            GUI.Label(new Rect(left,rect.y+rect.height*.13f,rect.width*.74f,25),speaker,kicker);kicker.normal.textColor=oldKicker;
            Panel(new Rect(left,rect.y+rect.height*.29f,Mathf.Min(155,rect.width*.3f),1),danger?new Color(.7f,.2f,.2f):glass?new Color(.78f,.63f,.38f,.7f):oxblood);
            dialogueText.fontSize=glass&&dialogueFont>0?dialogueFont:Mathf.RoundToInt(24*Game.SubtitleScale);dialogueText.normal.textColor=danger?ink:glass?castleInk:paperInk;
            GUI.Label(new Rect(left,rect.y+rect.height*.35f,textW,rect.height*(glass?.52f:.53f)),text,dialogueText);
            GUI.color=old;
        }
        int dialogueFont; // castle subtitle-card font after fitting (0 = default)
        const float CastleCardW=520;
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
            if(Game.Map==MapId.Castle)
            {
                // Castle (r12 critic: a 3-line dispatch ran over the torn edge): measure at the real text width (72 % of the card)
                // and keep the text in the top 45 % band below the kicker, clear of the torn bottom edge; above 285 px the font steps
                // down (to 18 px) instead of the card growing further.
                // owner 2026-10-20: smaller glass card (520 px, text across 84 %), font 18-22, height 110-230.
                int size=Mathf.Min(dialogueText.fontSize,22);dialogueText.fontSize=size;float textHeight=dialogueText.CalcHeight(new GUIContent(dialogueBody),CastleCardW*.84f);
                while(textHeight/.5f>230&&size>17){size--;dialogueText.fontSize=size;textHeight=dialogueText.CalcHeight(new GUIContent(dialogueBody),CastleCardW*.84f);}
                dialogueFont=size;height=Mathf.Clamp(textHeight/.5f,110,230);
            }
            // Castle (critic r10 #14): cards do not turn to danger styling for a hunt; only a Warning or real interference does.
            bool danger=Game.Map==MapId.Castle?Game.Case.Phase==Phase.Warning||Game.Scares.Interference:Game.Case.Phase==Phase.Warning||Game.Case.Phase==Phase.Hunt||Game.Scares.Interference;
            float fade=Mathf.Clamp01(Game.SubtitleTime/.4f);
            float cw=Game.Map==MapId.Castle?CastleCardW:610;
            Dialogue(new Rect(800-cw*.5f,858-height,cw,height),dialogueSpeaker,dialogueBody,danger,fade);dialogueFont=0;
        }
        void KeyPill(float x,float y,string key,string text)
        {
            Border(new Rect(x,y,34,26),new Color(.54f,.47f,.35f,.8f));Label(x,y,34,26,key,keyStyle);Label(x+45,y+2,240,26,text,hudTiny);
        }
        void HUD()
        {
            if(shownRecords!=Game.Case.Evidence.Count){shownRecords=Game.Case.Evidence.Count;recordLabel=Book.CaseTag+" / "+shownRecords+" RECORDS";}
            if(Game.Map==MapId.Castle)CastleObjectiveHud();
            else{Track(42,24,"OBJECTIVE",ink);Rule(42,53,320);Label(42,63,690,70,HudGoal(),hudGoal);}
            Track(660,24,"HOLLOW CURRENT",ink);Rule(660,53,285);
            Label(1290,28,268,28,Location(),rightBody);Panel(new Rect(1355,62,200,1),copper);Label(1290,73,268,24,recordLabel,rightBody);
            Panel(new Rect(798,448,4,4),new Color(.8f,.8f,.7f,.7f));var p=Game.Player;
            string use=PadInput.Active?(PadInput.Current.PlayStation?"[×] ":"[A] "):"[E] ",torch=PadInput.Active?(PadInput.Current.PlayStation?"[□] ":"[X] "):"[F] ";
            string interaction=p.Hidden?use+"Leave hiding place · "+(p.TorchOn?torch+(Game.Map==MapId.Castle?"LANTERN OFF":"TORCH OFF"):"Stay quiet"):p.Focus!=null?use+p.Focus.Prompt+(p.Focus.Kind==InteractKind.Anchor?" · hold":""):"";
            Label(405,510,790,62,interaction,center);if(p.Capture>0)Panel(new Rect(630,568,340*Mathf.Clamp01(p.Capture),3),copper);
            if(Game.Castle&&Game.Castle.Forge.CraftProgress01>0&&!Game.Castle.Forge.HasSword){Label(545,583,510,30,"FORGING "+Mathf.RoundToInt(Game.Castle.Forge.CraftProgress01*100)+"%",center);Panel(new Rect(615,622,370*Game.Castle.Forge.CraftProgress01,3),copper);}
            if(Game.Castle&&(Game.Castle.Forge.RelicCount>0||Game.Castle.Forge.HasSword))Label(1290,100,268,24,Game.Castle.Forge.HasSword?"MOONSTONE SWORD · "+Game.Castle.Fight.Hits+" / 3":"RELICS "+Game.Castle.Forge.RelicCount+" / 3",rightBody);
            if(Game.Case.SealProgress>0){Label(545,583,510,30,"SEALING "+Mathf.RoundToInt(Game.Case.SealProgress*100)+"%",center);Panel(new Rect(615,622,370*Game.Case.SealProgress,3),copper);}
            if(Game.Subtitles&&Game.SubtitleTime>0&&FieldMessages.ShowCard(Game.Subtitle))SubtitleCard();
            // fb-onboard (item 7, "never silent"): a short action notice (the door is held, nailed shut, the rack is empty, the idle
            // objective nudge). Game.Notice set it; FieldMessages keeps it off the dialogue card, so it has its own line above the prompt.
            if(Game.NoticeTime>0&&!string.IsNullOrEmpty(Game.NoticeText))
            {
                // Just under the interaction prompt (never across the screen centre), fading out over its last half second.
                if(noticeStyle==null)noticeStyle=new GUIStyle(center){fontSize=18};
                float a=Mathf.Clamp01(Game.NoticeTime/.5f);noticeStyle.normal.textColor=new Color(ink.r,ink.g,ink.b,a);
                float w=Mathf.Min(790,noticeStyle.CalcSize(new GUIContent(Game.NoticeText)).x+36),h=noticeStyle.CalcHeight(new GUIContent(Game.NoticeText),w-20)+14;
                Panel(new Rect(800-w*.5f,552,w,h),new Color(.01f,.012f,.016f,.55f*a));GUI.Label(new Rect(810-w*.5f,552,w-20,h),Game.NoticeText,noticeStyle);
            }
            Track(42,796,Book.InstrumentNames[Mathf.Clamp(p.Tool-1,0,2)],ink);
            Label(42,824,290,48,Game.Scares.Interference?"SIGNAL UNSTABLE":p.Capture>0?"RECORDING… HOLD STEADY":p.Tool==1?p.InductionDisplay+" / 5.0":"FIELD KIT CONNECTED",hudTiny);
            bool ps=PadInput.Active&&PadInput.Current.PlayStation;
            KeyPill(1315,800,PadInput.Active?(ps?"R2":"RT"):"LMB",p.Tool==3?"ASK / RECORD":"HOLD TO RECORD");
            bool castleHud=Game.Map==MapId.Castle;string radioWord=castleHud?"RESONANCE BOX":"RADIO";
            KeyPill(1315,840,PadInput.Active?(ps?"□":"X"):"F",castleHud?"LANTERN":"FLASHLIGHT");
            if(Game.HelpHints)Label(365,869,855,25,PadInput.Active?(ps?"× INTERACT   △ JOURNAL   L2 "+radioWord+"   L3 SPRINT   ○ CROUCH":"A INTERACT   Y JOURNAL   LT "+radioWord+"   LS SPRINT   B CROUCH"):"E INTERACT   J JOURNAL   R "+radioWord+"   SHIFT SPRINT   CTRL CROUCH",hudTiny);
            if(p.Stamina<.99f){Panel(new Rect(44,878,220,2),line);Panel(new Rect(44,878,220*p.Stamina,2),copper);}
            // Castle: a non-numeric cue only (critic r10 #14); Bellwether keeps its countdown.
            if(Game.Case.Phase==Phase.Warning)Label(540,106,520,44,Game.Map==MapId.Castle?"SIGNAL COLLAPSING":"SIGNAL COLLAPSE  "+Mathf.Max(0,Mathf.CeilToInt(9-Game.Case.PhaseTime)),center);
        }
        // Castle objective block: the kicker reads OBJECTIVE UPDATED for 5 s after the step changes, the goal sentence, and a one-line
        // tracker (records, ledger pages, relics, hunt survived) so both routes to the end stay visible.
        void CastleObjectiveHud()
        {
            var o=Objective();
            if(o.Step!=shownStep){if(shownStep!=null)stepChangedAt=Time.unscaledTime;shownStep=o.Step;}
            bool fresh=Time.unscaledTime-stepChangedAt<5;
            Track(42,24,fresh?"OBJECTIVE UPDATED":"OBJECTIVE",fresh?copper:ink);Rule(42,53,320);Label(42,63,690,70,o.Text,hudGoal);
            if(o.Step!=CastleObjective.Letter&&o.Step!=CastleObjective.Briefing&&o.Step!=CastleObjective.Won&&o.Step!=CastleObjective.Dead)Label(42,128,690,22,o.Tracker,hudTiny);
        }
        // ---- Title helpers (piece T1) ----
        GUIStyle titleTagline;
        // Hit test, focus and controller activation for a title control; the same ids and rules as Action().
        bool TitleHit(Rect rect,string controlId,out bool hover,out bool focus)
        {
            string id="hc-ui/"+controlId;Register(id,rect);
            bool padFocus=PadInput.Active&&focusedId==id;
            focus=(!PadInput.Active&&GUI.GetNameOfFocusedControl()==id)||padFocus;
            hover=!PadInput.Active&&rect.Contains(Event.current.mousePosition)&&GUI.enabled;
            GUI.SetNextControlName(id);bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);
            if(PadInput.Active)clicked=false;
            if(ConsumeControllerAction(id,GUI.enabled,Event.current.type==EventType.Layout))clicked=true;
            if(clicked)Game.Sound.UI();
            return clicked;
        }
        void TitleFrame(Rect r,Color c,float t)
        {Panel(new Rect(r.x,r.y,r.width,t),c);Panel(new Rect(r.x,r.yMax-t,r.width,t),c);Panel(new Rect(r.x,r.y,t,r.height),c);Panel(new Rect(r.xMax-t,r.y,t,r.height),c);}
        bool TitleBegin(TitleScreen screen,Rect r)
        {
            bool clicked=TitleHit(r,"BEGIN INVESTIGATION",out bool hover,out bool focus);
            Panel(r,new Color(copper.r,copper.g,copper.b,hover||focus?.3f:.18f));Border(r,copper);
            screen.Tracked("BEGIN INVESTIGATION",r.x+26,r.center.y-screen.LineBox(20)/2,20,2.6f,Color.white);
            string key=PadInput.Active?(focus?PadInput.Prompt("ENTER"):""):"ENTER";
            if(key.Length>0)
            {
                var cap=new Rect(r.xMax-66,r.y+(r.height-24)/2,48,24);Border(cap,line);
                var old=keyStyle.normal.textColor;keyStyle.normal.textColor=ink;GUI.Label(cap,key,keyStyle);keyStyle.normal.textColor=old;
            }
            return clicked;
        }
        // Plain text action: tracked caps, a thin copper underline on hover or focus.
        bool TitleLink(TitleScreen screen,float x,float centerY,string text,string controlId)
        {
            float width=screen.Measure(text,16,2.2f);
            bool clicked=TitleHit(new Rect(x-10,centerY-20,width+20,40),controlId,out bool hover,out bool focus);
            bool lit=hover||focus;
            screen.Tracked(text,x,centerY-screen.LineBox(16)/2,16,2.2f,lit?ink:new Color(ink.r,ink.g,ink.b,.78f));
            if(lit)Panel(new Rect(x,centerY+11,width,1),copper);
            return clicked;
        }
        // A map card: cropped art, shade, the map name; copper frame and full brightness when chosen.
        void TitleCard(TitleScreen screen,MapInfo map,Rect r)
        {
            bool available=Game.MapAvailable(map.Id),chosen=Game.SelectedMap==map.Id,hover=false,focus=false;
            if(available&&TitleHit(r,"map/"+map.Id,out hover,out focus))Game.SelectMap(map.Id);
            float brightness=!available?.55f:chosen?1f:hover||focus?.8f:.6f;
            var art=BackdropFor(CaseBook.For(map.Id));var crop=TitleScreen.CropFor(map.Id);
            // r13 critic: the Bellwether thumbnail (a crop of its very dark key art) was almost black; a lifted crop centred on the house replaces it.
            if(map.Id==MapId.Bellwether){var thumb=Resources.Load<Texture2D>("UI/BellwetherCard");if(thumb){art=thumb;crop=new Rect(0,0,1,1);}}
            if(art)screen.CardArt(artworkMaterial,r,art,crop,brightness);
            else Panel(r,new Color(.05f,.06f,.07f));
            // A map not in this build: drained of colour (a neutral veil) and labelled.
            if(!available)Panel(r,new Color(.2f,.2f,.2f,.6f));
            int size=18;string name=map.DisplayName;
            while(size>13&&screen.Measure(name,size,1.6f)>r.width-32)size--;
            float nameY=r.yMax-(available?16:36)-screen.LineBox(size);
            screen.Tracked(name,r.x+16,nameY,size,1.6f,new Color(ink.r,ink.g,ink.b,available?1f:.8f));
            if(!available)screen.Tracked("not in this build",r.x+16,r.yMax-14-screen.LineBox(12,false),12,1.2f,copper,false);
            if(chosen)TitleFrame(r,copper,2);
            else if(focus)TitleFrame(r,new Color(copper.r,copper.g,copper.b,.7f),1);
            else Border(r,new Color(line.r,line.g,line.b,.45f));
        }
    }
}
