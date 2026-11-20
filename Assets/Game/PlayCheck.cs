using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Parade
{
    // Opt-in player smoke test. Uses the standard simulation with movement and burst inputs.
    public sealed class PlayCheck : MonoBehaviour
    {
        internal Stage stage;
        string folder;
        int fired,hits,erased,ready,hurts;
        void Seen(string action,Vector2 position,int value)
        {
            if(action=="fire")fired++; if(action=="hit")hits++; if(action=="ready")ready++;
            if(action=="hurt")hurts++; if(action=="burst")erased+=value;
        }
        readonly System.Collections.Generic.HashSet<Kind> enemies=new System.Collections.Generic.HashSet<Kind>();
        bool good=true;
        void Check(bool passed,string label)
        {if(!passed){good=false;Debug.LogError("PARADE_QA failed "+label);}}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-paradeOut");
            if(i<0||i+1>=args.Length){Application.Quit(2);yield break;}
            folder=args[i+1]; Directory.CreateDirectory(folder);
            var g=stage.game;g.signal+=Seen;
            yield return null;
            Check(stage.ArtReady,"all illustrations included");
            if(stage.qaResume){
                var s=stage.save;
                Check(s.unlocked==4&&s.endings==3&&s.crewTapes==7&&s.route==1&&s.pending==-1&&Array.TrueForAll(s.scores,n=>n>0),"fresh player read saved campaign");
                stage.Advance();stage.OpenAlbum();Check(stage.mode==ScreenMode.Album&&stage.AlbumCount==5,"album after restart");stage.Advance();
                stage.CleanTestSave();Debug.Log("PARADE_QA resume="+(good?"PASS":"FAIL"));Application.Quit(good?0:1);yield break;
            }
            yield return Capture("cover");
            stage.Advance();stage.SelectEpisode(4);stage.SelectPilot(2);
            Check(stage.selected==0 && stage.pilot==1,"initial locks");
            stage.OpenAlbum();Check(stage.mode==ScreenMode.Album&&stage.AlbumCount==0,"new album locks");stage.Advance();
            yield return Capture("station");
            bool record=Array.IndexOf(args,"-paradeClip")>=0;
            for(int route=0;route<2;route++)
            {
                for(int ch=route==0?0:2;ch<5;ch++)
                {
                    stage.SelectEpisode(ch);stage.SelectPilot(ch==0?0:ch==1?1:ch>=3?2:0);
                    Check(stage.selected==ch && stage.mode==ScreenMode.Desk,"chapter navigation "+ch);
                    stage.Advance();Check(!stage.post && stage.mode==ScreenMode.Talk,"opening "+ch);
                    yield return Capture("opening-"+ch);
                    int pages=0;
                    while(stage.mode==ScreenMode.Talk && pages++<12)stage.Advance();
                    Check(pages==stage.Ep.before.Length && stage.mode==ScreenMode.Flying,"opening pages");
                    int tick=0;Vector2 move=Vector2.zero;
                    bool captured=false,bossShot=false,lateBoss=false,duel=false;
                    int startFire=fired,startHits=hits,clip=0;bool sample=record && route==0 && (ch==1||ch==2||ch==4);
                    string sub="clip"+ch;
                    if(sample)Directory.CreateDirectory(Path.Combine(folder,sub));
                    bool flightClip=record && route==0 && (ch==0||ch==1||ch==3);
                    string flightFolder="sortie"+ch;
                    if(flightClip)Directory.CreateDirectory(Path.Combine(folder,flightFolder));
                    float startRecord=-1;
                    while(g.phase==Phase.Run && g.time<164)
                    {
                        for(int k=0;k<120&&g.phase==Phase.Run;k++)
                        {
                            if(stage.mode==ScreenMode.Duel)
                            {
                                Check(!duel && g.bossSpawned && stage.duelLine==0,"boss introduction "+ch);
                                duel=true;float held=g.time;
                                stage.Tick(.05f,Vector2.right,true);Check(held==g.time,"dialogue pauses combat");
                                if(route==0){yield return Capture("duel-"+ch);stage.Advance();
                                    Check(stage.duelLine==1,"crew reply");yield return Capture("duel-reply-"+ch);stage.Advance();}
                                else stage.StorySkip();
                                Check(stage.mode==ScreenMode.Flying && g.phase==Phase.Run,"duel resumes");
                            }
                            if(tick%6==0)move=Pilot(g);
                            bool expose=route==0&&ch==3&&!stage.supportUsed;
                            stage.Tick(1f/120,expose?Vector2.zero:move,!expose && g.charge>=1 && (g.bossSpawned||g.units.Count>12));
                            if(tick%12==0)foreach(var u in g.units)if(u.kind<Kind.Boss)enemies.Add(u.kind);
                            tick++;
                            if(route==0 && tick==1680){
                                if(ch==0){
                                    float held=g.time;
                                    stage.SetPaused(true);stage.Tick(.1f,Vector2.right,true);
                                    Check(g.time==held,"pause holds flight and scrolling");stage.SetPaused(false);
                                }
                                yield return Capture("scroll-before-"+ch);
                            }
                            if(route==0 && tick==1740)yield return Capture("scroll-after-"+ch);
                            if(flightClip && tick>=1680 && tick<2040 && tick%6==0)
                                yield return Capture(flightFolder+"/frame"+((tick-1680)/6).ToString("D3"),800,450);
                            if(sample && g.bossSpawned && startRecord<0 && g.bossRatio<.94f)startRecord=g.time;
                            if(sample && startRecord>=0 && clip<60 && tick%6==0)
                                yield return Capture(sub+"/frame"+(clip++).ToString("D3"),800,450);
                        }
                        if(!captured&&g.time>16){captured=true;yield return Capture("flight-"+ch);}
                        if(!bossShot&&g.bossSpawned&&g.bossRatio<.87f){bossShot=true;yield return Capture("boss-"+ch);}
                        if(!lateBoss&&g.bossSpawned&&g.bossRatio<.48f){lateBoss=true;yield return Capture("break-"+ch);}
                        yield return null;
                    }
                    Debug.Log($"PARADE_QA chapter={ch+1} route={route} pilot={g.pilot} phase={g.phase} time={g.time:F1} bossHP={g.bossRatio:F2} hull={g.hull} bursts={g.bursts} chain={g.maxCombo} support={stage.supportUsed}");
                    Check(g.phase==Phase.Clear && duel && bossShot && lateBoss && g.bursts>0 && g.maxCombo>1 && fired>startFire+40 && hits>startHits+20,"combat "+ch);
                    if(g.phase!=Phase.Clear){Application.Quit(1);yield break;}
                    if(ch==3)Check(stage.supportUsed,"route support "+route);
                    while(!stage.ResultVisible)yield return null;
                    var loaded=StationSave.Read(JsonUtility.ToJson(stage.save));
                    Check(loaded.pending==ch && loaded.scores[ch]>=g.score,"checkpoint "+ch);
                    if(ch==1 && route==0){stage.Desk();stage.Advance();}else stage.Advance();
                    Check(stage.mode==ScreenMode.Event && stage.albumPage==ch,"chapter event "+ch);
                    yield return Capture("event-"+ch);
                    stage.ToggleArtText();Check(stage.hideEventText,"illustration-only mode");
                    if(ch==0&&route==0)yield return Capture("event-unframed");
                    stage.ToggleArtText();stage.Advance();
                    Check(stage.post && stage.mode==ScreenMode.Talk,"return signal");
                    pages=0;int requiredPages=stage.Ep.after.Length+(ch==2?2:ch==4?1:0);
                    while(stage.mode==ScreenMode.Talk && pages++<12)
                    {
                        if(stage.choosing){yield return Capture("choice");stage.choice=route;}
                        if(stage.openMic>0 && route==0){yield return Capture("silence");while(stage.openMic>0)yield return null;}
                        else stage.Advance();
                    }
                    Check(pages==requiredPages,"return pages chapter "+ch);
                    if(ch==2)Check(stage.save.route==route && stage.CrewCount==3,"decision and recruitment");
                }
                Check(stage.mode==ScreenMode.Credits && (stage.save.endings&(1<<route))!=0,"ending "+route);
                yield return Capture("ending-"+route);stage.Advance();
            }
            Check(enemies.Count==10 && stage.save.endings==3 && erased>0,"complete roster and both endings");
            Check(stage.save.crewTapes==7,"personal stories unlocked");
            stage.OpenAlbum();
            var collection=new System.Collections.Generic.HashSet<int>();
            for(int j=0;j<5;j++){collection.Add(stage.albumPage);stage.BrowseAlbum(1);}
            Check(stage.AlbumCount==5&&collection.Count==5,"all five CGs available");
            yield return Capture("album");stage.Advance();
            for(int cast=0;cast<3;cast++){
                stage.SelectPilot(cast);stage.OpenTape();
                Check(stage.mode==ScreenMode.NightShow,"tape opened "+cast);
                yield return Capture("tape-"+cast);
                int pages=0;while(stage.mode==ScreenMode.NightShow&&pages++<8)stage.Advance();
                Check(stage.mode==ScreenMode.Desk&&pages==Broadcast.NightShows[cast].Length,"tape complete "+cast);
            }
            stage.SelectEpisode(0);stage.Advance();stage.StorySkip();
            while(g.phase==Phase.Run && g.time<140)
            {
                for(int k=0;k<160 && g.phase==Phase.Run;k++){
                    if(stage.mode==ScreenMode.Duel)stage.StorySkip();
                    stage.Tick(1f/120,Vector2.zero,false);
                }
                yield return null;
            }
            Check(g.phase==Phase.Defeat,"defeat");
            while(!stage.ResultVisible)yield return null;
            yield return Capture("defeat");stage.Advance();
            Check(g.phase==Phase.Run&&g.hull==3&&g.score==0&&g.bursts==0,"retry");
            Debug.Log("PARADE_QA "+(good?"PASS":"FAIL")+" mobTypes="+enemies.Count+" endingMask="+stage.save.endings+" erased="+erased);
            Application.Quit(good?0:1);
        }
        IEnumerator Capture(string name,int width=1280,int height=720)
        {
            yield return null;
            var camera=stage.GetComponent<Camera>();
            var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(width,height,24);
            camera.targetTexture=rt;
            camera.Render();
            RenderTexture.active=rt;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Destroy(tex);
        }
        Vector2 Pilot(Combat g)
        {
            // Consider a short movement horizon, steering toward a firing lane while dodging.
            Vector2 target=new Vector2(-4.1f,0);float rank=float.NegativeInfinity;
            foreach(var u in g.units)
            {
                if(u.kind==Kind.Bullet||u.kind==Kind.Shot || u.pos.x<g.player.x+.3f)continue;
                float r=5-Mathf.Abs(u.pos.y-g.player.y)*.9f-(u.pos.x-g.player.x)*.23f;
                if(u.kind==Kind.Boss)r+=30;
                if(r<=rank)continue;
                rank=r;target.y=u.pos.y;
                if(u.kind==Kind.Boss){
                    float arrival=u.age+Mathf.Clamp((u.pos.x-g.player.x)/16f,0,.9f);
                    target.y=g.chapter==0?Mathf.Sin(arrival*(g.bossRatio<.5f?1.18f:.9f))*2.15f:
                        g.chapter==1?Mathf.Sin(arrival*.62f)*2.75f:
                        g.chapter==2?Mathf.Sin(arrival*.52f)*1.75f+Mathf.Sin(arrival*1.41f)*.4f:
                        g.chapter==3?Mathf.Sin(arrival*.88f)*2.45f:Mathf.Sin(arrival*.65f)*1.22f;
                }
            }
            Vector2 best=Vector2.zero;float bestCost=float.PositiveInfinity;
            for(int i=-1;i<16;i++)
            {
                Vector2 dir=i<0?Vector2.zero:Ink.Rad(i*Mathf.PI/8);
                float cost=0;
                for(int s=1;s<=5;s++)
                {
                    float t=s*.11f;Vector2 p=g.player+dir*(7.8f*t);
                    p.x=Mathf.Clamp(p.x,-8,5.7f);p.y=Mathf.Clamp(p.y,-3.7f,3.7f);
                    cost+=Mathf.Abs(p.y-target.y)*.38f+Mathf.Abs(p.x-target.x)*.18f;
                    foreach(var u in g.units)
                    {
                        if(u.kind==Kind.Shot)continue;
                        float gap=Vector2.Distance(p,u.pos+u.vel*t)-u.radius-.32f;
                        if(gap<1.0f)cost+=(1-gap)*(1-gap)*(7-s)*3f;
                    }
                }
                if(cost<bestCost){bestCost=cost;best=dir;}
            }
            return best;
        }
    }
}
