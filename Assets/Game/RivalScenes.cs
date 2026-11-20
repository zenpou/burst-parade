using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    public sealed partial class Stage
    {
        static readonly string[] RivalKeys={"Nagi","Suzu","Lila","Towa","Io"};
        readonly Texture2D[] rivalArt=new Texture2D[5],eventArt=new Texture2D[5];
        static readonly Rect AlbumButton=new Rect(526,84,361,58),ArtToggle=new Rect(1322,752,218,50),
            PrevArt=new Rect(1080,805,115,60),NextArt=new Rect(1210,805,115,60);
        internal int duelLine,albumPage;
        internal bool hideEventText;
        internal int AlbumCount {get{int n=0;foreach(var score in save.scores)if(score>0)n++;return n;}}

        void LoadRivalArt()
        {
            for(int i=0;i<RivalKeys.Length;i++){
                rivalArt[i]=Resources.Load<Texture2D>("Rival-"+RivalKeys[i]);
                eventArt[i]=Resources.Load<Texture2D>("Event-"+RivalKeys[i]);
            }
        }
        void BeginDuel(){mode=ScreenMode.Duel;duelLine=0;burstPending=false;audioRig.Pause(true);audioRig.StationId(selected,false);}
        void ResumeDuel(){mode=ScreenMode.Flying;accum=0;burstPending=false;audioRig.Pause(false);}
        void ShowEvent(){mode=ScreenMode.Event;hideEventText=false;albumPage=selected;audioRig.StationId(selected,true);}
        internal void ToggleArtText()
        {
            if(mode==ScreenMode.Event || mode==ScreenMode.Album&&save.scores[albumPage]>0)hideEventText=!hideEventText;
        }
        internal void OpenAlbum()
        {
            if(mode!=ScreenMode.Desk)return;
            albumPage=selected;hideEventText=false;mode=ScreenMode.Album;
            if(AlbumCount>0 && save.scores[albumPage]==0)BrowseAlbum(-1);
        }
        internal void BrowseAlbum(int step)
        {
            if(AlbumCount==0)return;
            for(int i=0;i<5;i++){
                albumPage=(albumPage+step+5)%5;
                if(save.scores[albumPage]>0)break;
            }
            audioRig.Tune(albumPage);
        }
        static Rect Fit(Texture2D tex,Rect area)
        {
            float factor=Mathf.Min(area.width/tex.width,area.height/tex.height);
            var size=new Vector2(tex.width*factor,tex.height*factor);
            return new Rect(area.center-size*.5f,size);
        }
        void DuelText()
        {
            var rival=Ep.rival;bool answer=duelLine==1;Color accent=Ep.color;
            Photo(scenery[selected],new Rect(0,0,1600,900),new Rect(0,0,1,1),Color.white);
            Plate(new Rect(0,0,1600,900),Fade(Night,.73f));
            Plate(new Rect(0,0,815,900),Fade(Night,.80f));
            float drift=lowFX?0:Mathf.Sin(anim*1.55f)*5;
            Photo(rivalArt[selected],Fit(rivalArt[selected],new Rect(750,58+drift,840,785)),new Rect(0,0,1,1),Color.white);
            Label("PARADE FM     /     INTERCEPT",71,58,700,52,25,accent,true);
            Label("0"+(selected+1)+"   /   "+Ep.call,72,146,700,51,24,Soft,true);
            JP(rival.role,70,220,760,57,37,accent,true);
            JP(rival.name,63,262,770,145,102,Paper,true);
            Label(Ep.boss,70,413,720,62,41,accent,true);
            Plate(new Rect(72,506,649,3),accent);
            if(answer)Portrait(rival.replyPilot,new Rect(72,518,105,98));
            JP(answer?Broadcast.Names[rival.replyPilot]:rival.name,answer?194:73,543,660,51,27,accent,true);
            CopyLines(answer?rival.reply:rival.entry,73,622,28,Paper,51);
            JP(answer?"Enter / クリック　交戦開始":"Enter / クリック　応答",72,799,710,58,26,Paper,true);
            JP("Tab　交信をスキップ",73,859,580,30,17,Soft);
        }
        void EventText()
        {
            bool album=mode==ScreenMode.Album;
            int ch=album?albumPage:selected;var ep=Broadcast.All[ch];var rival=ep.rival;
            bool available=!album||save.scores[ch]>0;
            if(available){
                var tex=eventArt[ch];
                Plate(new Rect(0,0,1600,900),Night);
                var frame=new Rect(0,0,1600,hideEventText?900:742);
                Photo(tex,Fit(tex,frame),new Rect(0,0,1,1),Color.white);
                if(hideEventText)return;
            }else{
                Plate(new Rect(0,0,1600,900),Night);
                JP("放送アルバム",72,154,1030,100,62,Paper,true);
                JP("各章をクリアすると、あの日の一枚がここに届きます。",76,294,1390,68,33,Soft);
            }
            Plate(new Rect(0,742,1600,158),Night);Plate(new Rect(0,742,1600,3),ep.color);
            Label((album?"ALBUM":"AFTER THE DUEL")+"     /     0"+(ch+1)+"     "+ep.call,65,759,450,40,19,ep.color,true);
            JP(available?rival.eventTitle:"未受信",63,797,442,74,42,Paper,true);
            if(available){
                JP(rival.name,504,750,240,44,24,ep.color,true);
                CopyLines(rival.eventLine,504,793,24,Paper,38);
                Plate(ArtToggle,Dark);JP("H　絵だけ見る",1340,752,196,50,22,Paper,true);
            }
            if(album){
                Label(AlbumCount+" / 5",1090,752,210,50,24,ep.color,true,TextAnchor.MiddleRight);
                if(AlbumCount>1){Plate(PrevArt,Dark);Plate(NextArt,Dark);JP("← 前",1097,805,100,60,23,Paper,true);JP("次 →",1226,805,100,60,23,Paper,true);}
                JP("Enter  戻る",1350,805,210,60,22,Paper,true);
            }else JP("Enter  帰投通信へ",1284,809,300,60,24,Paper,true);
        }
        void PhaseTransmission()
        {
            var rival=Ep.rival;
            Label("PHASE II   /   "+Broadcast.PhaseTwo[selected],412,85,780,45,25,Flame,true,TextAnchor.MiddleCenter);
            Label(rival.name+"「"+rival.phaseTwo+"」",350,131,900,50,24,Paper,true,TextAnchor.MiddleCenter,true);
        }
        void RivalTell(Unit unit,Vector2 p)
        {
            float t=unit.age, charge=unit.telegraph;
            Color c=Fade(Ep.color,.38f+.55f*charge);
            float r=1.55f+.16f*charge;
            switch(selected)
            {
                case 0:
                    // Sweeping twin rescue lights follow the wingtips.
                    for(int k=-1;k<=1;k+=2){
                        var lamp=p+V(k*1.25f,.30f);var sweep=Rad(3.14f+k*.43f+Mathf.Sin(t*2)*.32f);
                        Tri(lamp,lamp+sweep*(1.1f+charge)+Vector2.Perpendicular(sweep)*.18f,
                            lamp+sweep*(1.1f+charge)-Vector2.Perpendicular(sweep)*.18f,Fade(Flame,.08f+.15f*charge));
                        Aura(lamp,.22f,.18f,c);
                    }break;
                case 1:
                    for(int i=0;i<12;i++){var d=Rad(i*Mathf.PI/6);Line(p+d*(r-.09f),p+d*r,.030f,c);}
                    var hand=Rad(-t*2.1f);Line(p+hand*1.1f,p+hand*r,.055f,c);break;
                case 2:
                    for(int i=0;i<7;i++){
                        var d=Rad(i*6.283f/7+t*.55f);var q=p+d*(r+charge*.19f);var n=Vector2.Perpendicular(d);
                        Quad(q-d*.12f,q+n*.057f,q+d*(.16f+charge*.12f),q-n*.057f,c);
                    }break;
                case 3:
                    for(int k=-1;k<=1;k+=2){
                        var d=Rad(t*1.6f+k*1.57f);
                        var q=p+V(d.x*1.48f,d.y*.88f);Ring(q,.20f+.09f*charge,.035f,k==1?Mint:Purple,t,5f,18);
                    }break;
                case 4:
                    for(int i=0;i<4;i++){
                        var d=Rad(1.12f+i*.30f);var q=p+d*r;
                        Diamond(q,.06f,i==unit.pattern%4?.20f:.11f,i==unit.pattern%4?Flame:c);
                    }break;
            }
            if(charge>.01f){
                var emitter=p+Vector2.left;
                Aura(emitter,.22f+charge*.27f,.15f+charge*.18f,Fade(Ep.color,charge*.77f));
            }
        }
        void WorldRival(Vector2 p,float height,float age)
        {
            var tex=rivalArt[selected];float width=height*tex.width/tex.height;
            float tilt=lowFX?0:Mathf.Sin(age*1.7f)*2.2f;
            WorldIllustration(tex,p,new Rect(-width*.5f,height*.5f,width,-height),tilt);
        }
        void WorldIllustration(Texture2D tex,Vector2 pivot,Rect frame,float tilt)
        {
            GL.End();
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,tilt),Vector3.one));
            Photo(tex,frame,new Rect(0,0,1,1),Color.white);
            GL.PopMatrix();mat.SetPass(0);GL.Begin(GL.TRIANGLES);
        }
    }
}
