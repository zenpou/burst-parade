using System;
using System.Collections.Generic;
using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    [RequireComponent(typeof(Camera))]
    public sealed partial class Stage : MonoBehaviour
    {
        internal readonly Combat game=new Combat();
        internal bool paused, qa;
        bool lowFX;
        float ending;
        internal bool ResultVisible => game.phase!=Phase.Run && game.phase!=Phase.Title && ending<=0;
        readonly Vector2[] wake=new Vector2[12];
        float wakeTick;
        float flightBank,muzzleFlash;
        Camera cam,back;
        Material mat;
        Chiptune audioRig;
        float anim,shake,burstWave=-1,alert,freeze,scale,ox,oy,accum;
        int best;
        struct Fleck {public Vector2 p,v; public Color c; public float size,life,full;}
        struct Mark {public Vector2 p; public float life,full,size; public Color c; public int chain;}
        readonly List<Fleck> sparks=new List<Fleck>(512);
        readonly List<Mark> marks=new List<Mark>(64);
        readonly System.Random fx=new System.Random(5432);
        static readonly Rect ActionButton=new Rect(103,643,402,80);
        void Awake()
        {
            qa=Array.IndexOf(Environment.GetCommandLineArgs(),"-paradeQA")>=0;
            cam=GetComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=5;
            cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Night; cam.cullingMask=0;
            cam.allowHDR=false; cam.allowMSAA=false; cam.useOcclusionCulling=false;
            var letterbox=new GameObject("Window margins");letterbox.transform.SetParent(transform,false);
            back=letterbox.AddComponent<Camera>();back.depth=cam.depth-1;back.cullingMask=0;
            back.clearFlags=CameraClearFlags.SolidColor;back.backgroundColor=Color.black;
            mat=new Material(Resources.Load<Shader>("PrintInk"));
            QualitySettings.vSyncCount=0; Application.targetFrameRate=60;
            audioRig=new Chiptune(gameObject);
            LoadStation();
            best=qa?0:PlayerPrefs.GetInt("best",0); lowFX=!qa && PlayerPrefs.GetInt("lowFX",0)==1;
            game.signal=Signal;
            if(qa){Application.runInBackground=true;gameObject.AddComponent<PlayCheck>().stage=this;}
            Resize();
        }
        void Resize()
        {
            scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            ox=(Screen.width-1600*scale)*.5f; oy=(Screen.height-900*scale)*.5f;
            cam.rect=new Rect(ox/Screen.width,oy/Screen.height,1600*scale/Screen.width,900*scale/Screen.height);
            back.enabled=ox>.5f||oy>.5f;
        }
        internal void Launch()
        {
            paused=false; burstPending=false; accum=0; shake=freeze=alert=0; burstWave=-1;
            PrepareSortie();sparks.Clear(); marks.Clear(); game.StartRun(selected,pilot); audioRig.PlayRun(selected);
            ending=0;wakeTick=flightBank=muzzleFlash=0;for(int i=0;i<wake.Length;i++)wake[i]=game.player;
        }
        void Update()
        {
            Resize(); anim+=Mathf.Min(Time.unscaledDeltaTime,.08f);
            StationClock(Time.unscaledDeltaTime);
            if(qa){if(mode==ScreenMode.Flying && game.phase!=Phase.Run)TickVfx(Mathf.Min(Time.unscaledDeltaTime,.06f));return;}
            if(Input.GetKeyDown(KeyCode.M)) audioRig.Toggle();
            if(Input.GetKeyDown(KeyCode.F)){lowFX=!lowFX; PlayerPrefs.SetInt("lowFX",lowFX?1:0);}
            if(mode!=ScreenMode.Flying){StationInput();return;}
            if((paused||ResultVisible)&&Input.GetKeyDown(KeyCode.Tab)){Desk();return;}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(game.phase==Phase.Run){SetPaused(!paused); return;}
                if(ending<=0)Desk();return;
            }
            bool accept=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
            var mouse=new Vector2((Input.mousePosition.x-ox)/scale,(Screen.height-Input.mousePosition.y-oy)/scale);
            bool click=Input.GetMouseButtonDown(0)&&ActionButton.Contains(mouse);
            if(game.phase!=Phase.Run || paused)
            {
                if((accept||click)&&ending<=0)Advance();
                else if(!paused) TickVfx(Mathf.Min(Time.unscaledDeltaTime,.05f));
                return;
            }
            Vector2 move=V((Held(KeyCode.D,KeyCode.RightArrow)?1:0)-(Held(KeyCode.A,KeyCode.LeftArrow)?1:0),
                (Held(KeyCode.W,KeyCode.UpArrow)?1:0)-(Held(KeyCode.S,KeyCode.DownArrow)?1:0));
            // Input edges survive a render frame shorter than one simulation tick.
            burstPending |= Input.GetKeyDown(KeyCode.Space);
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(freeze>0){freeze-=dt;TickVfx(dt*.3f);return;}
            accum+=dt;
            while(accum>=1f/120 && game.phase==Phase.Run && mode==ScreenMode.Flying)
            {
                Tick(1f/120,move,burstPending);burstPending=false;accum-=1f/120;
            }
        }
        bool burstPending;
        static bool Held(KeyCode a,KeyCode b)=>Input.GetKey(a)||Input.GetKey(b);
        internal void SetPaused(bool value){paused=value;burstPending=false;audioRig.Pause(value);}
        void OnApplicationFocus(bool focused){if(!focused && !qa && mode==ScreenMode.Flying && game.phase==Phase.Run&&!paused) SetPaused(true);}
        internal void Tick(float dt,Vector2 move,bool burst)
        {
            if(mode!=ScreenMode.Flying || paused)return;
            float previousY=game.player.y;
            game.Step(dt,move,burst);
            if(game.phase==Phase.Run){
                float climbSpeed=(game.player.y-previousY)/dt;
                flightBank=Mathf.Lerp(flightBank,Mathf.Clamp(climbSpeed,-8,8),1-Mathf.Exp(-11*dt));
                BroadcastTick(dt);
            }
            TickVfx(dt);
        }
        void TickVfx(float dt)
        {
            shake=Mathf.Max(0,shake-dt*1.2f); alert=Mathf.Max(0,alert-dt);
            muzzleFlash=Mathf.Max(0,muzzleFlash-dt);
            ending=Mathf.Max(0,ending-dt);wakeTick-=dt;
            if(wakeTick<=0 && game.phase==Phase.Run)
            {
                wakeTick=.031f; for(int k=wake.Length-1;k>0;k--)wake[k]=wake[k-1];
                wake[0]=game.player;
            }
            if(burstWave>=0){burstWave+=dt;if(burstWave>.68f)burstWave=-1;}
            for(int i=sparks.Count-1;i>=0;i--){var f=sparks[i];f.life-=dt;if(f.life<=0){sparks.RemoveAt(i);continue;}f.p+=f.v*dt;f.v*=1/(1+dt*2.4f);sparks[i]=f;}
            for(int i=marks.Count-1;i>=0;i--){var m=marks[i];m.life-=dt;if(m.life<=0)marks.RemoveAt(i);else marks[i]=m;}
        }
        float Rand()=> (float)fx.NextDouble();
        void Spray(Vector2 p,Color c,int n,float speed)
        {
            if(lowFX)n=(n+1)/2;
            for(int i=0;i<n && sparks.Count<540;i++)
            {
                float life=.16f+Rand()*.42f;
                sparks.Add(new Fleck{p=p,v=Rad(Rand()*6.2832f)*(.25f+Rand())*speed,c=i%4==0?Paper:c,
                    size=.019f+Rand()*.041f,life=life,full=life});
            }
        }
        void MarkAt(Vector2 p,float life,float size,Color c,int chain=0)
        {marks.Add(new Mark{p=p,life=life,full=life,size=size,c=c,chain=chain});}
        Vector2 burstOrigin;
        void Signal(string name,Vector2 pos,int value)
        {
            audioRig.Event(name,value);
            switch(name)
            {
                case "fire": muzzleFlash=.075f;Spray(pos+Vector2.right*.67f,Broadcast.Colors[pilot],2,1.3f);break;
                case "hit": Spray(pos,Flame,5,3);break;
                case "block": Spray(pos,Mint,5,2);MarkAt(pos,.13f,.48f,Mint);break;
                case "kill":
                    Color c=value>=5?Lime:Hot;
                    Spray(pos,c,17+Mathf.Min(12,value),5.5f);Spray(pos,Mint,7,3.5f);
                    MarkAt(pos,.45f,.72f,c,value);shake=Mathf.Max(shake,.028f);break;
                case "hurt": Spray(pos,Flame,36,6);MarkAt(pos,.5f,1.15f,Flame);shake=.14f;break;
                case "burst": burstOrigin=pos; burstWave=0;cutTime=.72f;Spray(pos,Broadcast.Colors[pilot],92,14);shake=.20f;freeze=.05f;break;
                case "boss": BeginDuel();alert=2.2f;Radio(Ep.air[2]);break;
                case "ready": MarkAt(game.player,.55f,.85f,Lime); break;
                case "clear": case "defeat":
                    ending=name=="clear"?.92f:.52f;
                    var color=name=="clear"?Lime:Hot;
                    Spray(pos,color,90,13);Spray(pos,Flame,62,8);MarkAt(pos,.86f,3.2f,color);shake=.22f;
                    if(name=="clear"){burstOrigin=pos;burstWave=0;}
                    if(name=="clear")StampClear();
                    if(game.score>best){best=game.score;if(!qa){PlayerPrefs.SetInt("best",best);PlayerPrefs.Save();}}
                    break;
            }
        }
        void OnDestroy(){if(mat)Destroy(mat);if(type)Destroy(type);if(artMat)Destroy(artMat);}
    }
}
