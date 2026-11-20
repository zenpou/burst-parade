using System;
using UnityEngine;

namespace Parade
{
    internal enum ScreenMode { Cover, Desk, Talk, Flying, Credits, NightShow, Duel, Event, Album }
    [Serializable] internal sealed class StationSave
    {
        public int unlocked,route=-1,pilot,pending=-1,endings,crewTapes;
        public int[] scores=new int[5];
        public static StationSave Read(string json)
        {
            var s=new StationSave();
            if(!string.IsNullOrEmpty(json))
            {
                try{JsonUtility.FromJsonOverwrite(json,s);}
                catch(ArgumentException){s=new StationSave();}
            }
            s.unlocked=Mathf.Clamp(s.unlocked,0,4);s.route=Mathf.Clamp(s.route,-1,1);
            s.pilot=Mathf.Clamp(s.pilot,0,s.unlocked>=3?2:1);s.pending=Mathf.Clamp(s.pending,-1,s.unlocked);
            if(s.scores==null||s.scores.Length!=5)s.scores=new int[5];
            return s;
        }
    }
    public sealed partial class Stage
    {
        internal ScreenMode mode=ScreenMode.Cover;
        internal StationSave save;
        internal int selected,pilot,line,choice;
        internal bool post,choosing,supportUsed;
        bool branchReceipt;
        float radioTime,cutTime,phaseTag;
        bool secondForm;
        internal float openMic;
        bool waitForCity;
        int radioIndex;
        Voice radioVoice;
        internal Episode Ep=>Broadcast.All[selected];
        internal int CrewCount=>save.unlocked>=3?3:2;
        internal Voice CurrentVoice=>branchReceipt?Broadcast.BranchLine(save.route):post && selected==4 && line==Ep.after.Length?Broadcast.LastLine(save.route):(post?Ep.after:Ep.before)[line];
        internal bool ArtReady=>castArt&&dawnArt&&burstCast&&nightCast&&Array.TrueForAll(scenery,t=>t!=null)&&Array.TrueForAll(rivalArt,t=>t!=null)&&Array.TrueForAll(eventArt,t=>t!=null)&&Array.TrueForAll(pilotArt,t=>t!=null);
        const string StationKey="lastFrequency.station.v1";
        string SaveKey=>qa?StationKey+".playcheck":StationKey;
        internal bool qaResume;
        void LoadStation()
        {
            qaResume=qa && Array.IndexOf(Environment.GetCommandLineArgs(),"-paradeResume")>=0;
            save=StationSave.Read(qa&&!qaResume?"":PlayerPrefs.GetString(SaveKey,""));selected=save.pending>=0?save.pending:save.unlocked;pilot=save.pilot;
            LoadArt();
        }
        void StoreStation(){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(save));PlayerPrefs.Save();}
        internal void CleanTestSave(){if(qa){PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();}}
        internal void Desk()
        {
            mode=ScreenMode.Desk;paused=false;choosing=false;branchReceipt=false;waitForCity=false;openMic=ending=0;audioRig.Stop();
        }
        internal void SelectEpisode(int index){selected=Mathf.Clamp(index,0,save.unlocked);audioRig.Tune(selected);}
        internal void SelectPilot(int index){pilot=Mathf.Clamp(index,0,CrewCount-1);save.pilot=pilot;audioRig.Tune(pilot+3);StoreStation();}
        void Talk(bool aftermath)
        {
            mode=ScreenMode.Talk;post=aftermath;line=0;choice=0;choosing=branchReceipt=waitForCity=false;
        }
        internal void Advance()
        {
            if(!waitForCity)audioRig.Tune(Mathf.Max(0,line)%4);
            switch(mode)
            {
                case ScreenMode.Cover: Desk();return;
                case ScreenMode.Desk: if(save.pending==selected)ShowEvent();else Talk(false);return;
                case ScreenMode.Duel:
                    if(++duelLine>=2)ResumeDuel();
                    return;
                case ScreenMode.Event: Talk(true);return;
                case ScreenMode.Album: Desk();return;
                case ScreenMode.Talk:
                    if(waitForCity){waitForCity=false;openMic=0;}
                    if(choosing){save.route=choice;StoreStation();choosing=false;branchReceipt=true;return;}
                    if(branchReceipt){FinishEpisode();return;}
                    line++;
                    if(post && selected==4 && line==1){waitForCity=true;openMic=4;audioRig.Quiet();return;}
                    if(line<(post?Ep.after.Length:Ep.before.Length))return;
                    if(!post){Launch();return;}
                    if(selected==2){choosing=true;line=Ep.after.Length-1;return;}
                    if(selected==4 && line==Ep.after.Length)return;
                    FinishEpisode();return;
                case ScreenMode.Flying:
                    if(paused){SetPaused(false);return;}
                    if(ending>0)return;
                    if(game.phase==Phase.Clear)ShowEvent();
                    else if(game.phase==Phase.Defeat)Launch();
                    return;
                case ScreenMode.Credits: Desk();return;
                case ScreenMode.NightShow:
                    if(++line>=Broadcast.NightShows[pilot].Length)Desk();return;
            }
        }
        void FinishEpisode()
        {
            save.pending=-1;branchReceipt=false;
            if(selected==4){save.endings|=1<<Mathf.Max(0,save.route);mode=ScreenMode.Credits;audioRig.PlayRun(5);}
            else{selected++;save.unlocked=Mathf.Max(selected,save.unlocked);Desk();}
            StoreStation();
        }
        internal void StorySkip()
        {
            if(mode==ScreenMode.Duel){ResumeDuel();return;}
            if(mode!=ScreenMode.Talk||choosing||branchReceipt)return;
            if(!post){Launch();return;}
            if(selected==4&&line>=Ep.after.Length){FinishEpisode();return;}
            line=Ep.after.Length-1;Advance();
        }
        void StationClock(float dt)
        {
            if(!waitForCity||mode!=ScreenMode.Talk)return;
            openMic=Mathf.Max(0,openMic-dt);if(openMic==0)Advance();
        }
        void StampClear()
        {
            save.scores[selected]=Mathf.Max(save.scores[selected],game.score);
            save.crewTapes|=1<<pilot;
            save.pending=selected;StoreStation();
        }
        internal void OpenTape()
        {
            if(mode!=ScreenMode.Desk || (save.crewTapes&(1<<pilot))==0)return;
            mode=ScreenMode.NightShow;line=0;audioRig.Tune(pilot);
        }
        void PrepareSortie()
        {
            mode=ScreenMode.Flying;radioTime=cutTime=phaseTag=0;radioIndex=0;supportUsed=secondForm=false;
        }
        void BroadcastTick(float dt)
        {
            radioTime=Mathf.Max(0,radioTime-dt);cutTime=Mathf.Max(0,cutTime-dt);
            phaseTag=Mathf.Max(0,phaseTag-dt);
            if(!secondForm&&game.bossSpawned&&game.bossRatio<=.5f){secondForm=true;phaseTag=2.6f;audioRig.PhaseTwo();}
            if(radioIndex==0 && game.time>=3 || radioIndex==1 && game.time>=25)
                Radio(Ep.air[radioIndex++]);
            if(selected<3||supportUsed)return;
            if(save.route==0 && game.hull==1)
            {supportUsed=true;game.hull++;Radio(new Voice(1,"整備班より応急修理。耐久を1回復！"));MarkAt(game.player,.65f,1.8f,Ink.Mint);}
            else if(save.route==1 && game.time>=18)
            {supportUsed=true;game.charge=1;Radio(new Voice(0,"全市の増幅塔、同期！　バースト満充填！"));Signal("ready",game.player,0);}
        }
        void Radio(Voice v){radioVoice=v;radioTime=3.3f;}
        void StationInput()
        {
            bool enter=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
            bool clk=Input.GetMouseButtonDown(0);
            Vector2 m=new Vector2((Input.mousePosition.x-ox)/scale,(Screen.height-Input.mousePosition.y-oy)/scale);
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(mode==ScreenMode.Cover)Application.Quit();
                else if(mode==ScreenMode.Desk)mode=ScreenMode.Cover;
                else if(mode==ScreenMode.Duel){ResumeDuel();SetPaused(true);}
                else Desk();
                return;
            }
            if(mode==ScreenMode.Desk)
            {
                if(Input.GetKeyDown(KeyCode.G)||clk&&AlbumButton.Contains(m)){OpenAlbum();return;}
                if(Down(KeyCode.UpArrow,KeyCode.W))SelectEpisode(selected-1);
                if(Down(KeyCode.DownArrow,KeyCode.S))SelectEpisode(selected+1);
                if(Down(KeyCode.LeftArrow,KeyCode.A))SelectPilot((pilot+CrewCount-1)%CrewCount);
                if(Down(KeyCode.RightArrow,KeyCode.D))SelectPilot((pilot+1)%CrewCount);
                if(clk)
                {
                    for(int i=0;i<5;i++)if(ChapterRect(i).Contains(m)&&i<=save.unlocked)SelectEpisode(i);
                    for(int i=0;i<CrewCount;i++)if(PilotRect(i).Contains(m))SelectPilot(i);
                }
                if(Input.GetKeyDown(KeyCode.R)||clk&&TapeRect.Contains(m)){OpenTape();return;}
                if(enter||clk&&DispatchRect.Contains(m))Advance();
            }
            else if(mode==ScreenMode.Duel){
                if(Input.GetKeyDown(KeyCode.Tab))StorySkip();
                else if(enter||Input.GetKeyDown(KeyCode.Space)||clk)Advance();
            }
            else if(mode==ScreenMode.Event||mode==ScreenMode.Album)
            {
                if(Input.GetKeyDown(KeyCode.H)||clk&&(hideEventText||ArtToggle.Contains(m))){ToggleArtText();return;}
                if(mode==ScreenMode.Album)
                {
                    if(Down(KeyCode.RightArrow,KeyCode.D)||clk&&NextArt.Contains(m)){BrowseAlbum(1);return;}
                    if(Down(KeyCode.LeftArrow,KeyCode.A)||clk&&PrevArt.Contains(m)){BrowseAlbum(-1);return;}
                }
                if(enter||Input.GetKeyDown(KeyCode.Space)||clk&&!hideEventText&&m.y>742)Advance();
            }
            else if(mode==ScreenMode.NightShow){if(enter||Input.GetKeyDown(KeyCode.Space)||clk)Advance();}
            else if(mode==ScreenMode.Talk)
            {
                if(choosing)
                {
                    if(Down(KeyCode.LeftArrow,KeyCode.A))choice=0;
                    if(Down(KeyCode.RightArrow,KeyCode.D))choice=1;
                    if(clk)for(int i=0;i<2;i++)if(ChoiceRect(i).Contains(m)){choice=i;Advance();return;}
                }
                if(enter||Input.GetKeyDown(KeyCode.Space)||clk&&!choosing && m.y>445)Advance();
                else if(Input.GetKeyDown(KeyCode.Tab))StorySkip();
            }
            else if(enter||clk&&(mode==ScreenMode.Credits||ActionButton.Contains(m)))Advance();
        }
        static bool Down(KeyCode a,KeyCode b)=>Input.GetKeyDown(a)||Input.GetKeyDown(b);
        static Rect ChapterRect(int i)=>new Rect(70,192+i*93,817,78);
        static Rect PilotRect(int i)=>new Rect(938+i*192,655,179,53);
        static Rect ChoiceRect(int i)=>new Rect(602+i*446,634,424,83);
        static readonly Rect DispatchRect=new Rect(70,737,434,78);
        static readonly Rect TapeRect=new Rect(520,737,367,78);
    }
}
