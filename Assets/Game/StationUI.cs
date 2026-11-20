using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    public sealed partial class Stage
    {
        Texture2D castArt,dawnArt,burstCast,nightCast;
        readonly Texture2D[] scenery=new Texture2D[5];
        readonly Texture2D[] pilotArt=new Texture2D[3];
        Material artMat;
        void LoadArt()
        {
            castArt=Resources.Load<Texture2D>("RadioCast");dawnArt=Resources.Load<Texture2D>("FirstTrain");
            burstCast=Resources.Load<Texture2D>("BurstCast");nightCast=Resources.Load<Texture2D>("MidnightCast");
            var places=new[]{"SaltPort","BlueLine","GlassArk","TwinSpire","DeadAir"};
            for(int i=0;i<places.Length;i++)scenery[i]=Resources.Load<Texture2D>(places[i]);
            string[] pilots={"Akari","Mio","Sena"};
            for(int i=0;i<pilots.Length;i++)pilotArt[i]=Resources.Load<Texture2D>("Pilot-"+pilots[i]);
            LoadRivalArt();
            artMat=new Material(Resources.Load<Shader>("RadioArt"));
        }
        void Plate(Rect r,Color c)
        {
            mat.SetPass(0);GL.Begin(GL.TRIANGLES);Box(r.x,r.y,r.width,r.height,c);GL.End();
        }
        void Photo(Texture2D tex,Rect dst,Rect uv,Color tint)
        {
            if(!tex)return;
            artMat.mainTexture=tex;artMat.SetPass(0);GL.Begin(GL.QUADS);GL.Color(tint);
            GlyphPoint(dst.x,dst.y,new Vector2(uv.xMin,uv.yMax));
            GlyphPoint(dst.xMax,dst.y,new Vector2(uv.xMax,uv.yMax));
            GlyphPoint(dst.xMax,dst.yMax,new Vector2(uv.xMax,uv.yMin));
            GlyphPoint(dst.xMin,dst.yMax,new Vector2(uv.xMin,uv.yMin));GL.End();
        }
        void Portrait(int person,Rect dst,float light=1,Texture2D sheet=null)
        {
            if(!sheet)sheet=castArt;
            if(!sheet)return;
            float srcW=sheet.width/3f,srcH=sheet.height,aspect=dst.width/dst.height;
            float w=Mathf.Min(1,aspect*srcH/srcW),h=Mathf.Min(1,srcW/(srcH*aspect));
            float top=Mathf.Clamp(.32f-h*.42f,0,1-h);
            Photo(sheet,dst,new Rect((person+(1-w)*.5f)/3f,1-top-h,w/3,h),new Color(light,light,light,1));
        }
        void JP(string s,float x,float y,float w,float h,int size,Color c,bool bold=false)
            =>Label(s,x,y,w,h,size,c,bold,TextAnchor.MiddleLeft,true);
        void CopyLines(string s,float x,float y,int size,Color c,float step)
        {
            foreach(string part in s.Split('\n')){JP(part,x,y,1500-x,step,size,c,true);y+=step;}
        }
        void CastCover()
        {
            Plate(new Rect(885,100,697,691),Fade(Night,.96f));
            for(int i=0;i<3;i++)
            {
                var r=new Rect(914+i*205,153+i*23,198,511);
                Portrait(i,r);Plate(new Rect(r.x,r.yMax,r.width,7),Broadcast.Colors[i]);
                Label(Broadcast.Calls[i],r.x+3,r.yMax+12,r.width,38,21,Broadcast.Colors[i],true);
            }
        }
        void DeskText()
        {
            Plate(new Rect(0,0,1600,900),Hex(0x101625));
            Label("PARADE   FM    /    PROGRAM GUIDE",70,35,900,44,24,Mint,true);
            JP("放送卓",68,74,640,86,62,Paper,true);
            JP("受信できる周波数を選択",73,151,810,36,21,Soft);
            Plate(AlbumButton,Dark);JP("G　放送アルバム　"+AlbumCount+" / 5",AlbumButton.x+15,AlbumButton.y,352,AlbumButton.height,23,AlbumCount>0?Paper:Soft,true);
            for(int i=0;i<5;i++)
            {
                var r=ChapterRect(i);bool active=i==selected,locked=i>save.unlocked;
                var ep=Broadcast.All[i];var ink=active?Night:locked?Hex(0x8890a6):Paper;
                Plate(r,active?ep.color:Hex(0x20273b));Plate(new Rect(r.x,r.y,7,r.height),locked?Slate:ep.color);
                Label("0"+(i+1),r.x+23,r.y,r.width,78,36,ink,true);
                JP(ep.place,r.x+105,r.y+4,360,43,32,ink,true);
                Label(ep.call,r.x+106,r.y+45,510,26,17,active?Night:Soft,true);
                Label(locked?"LOCKED":save.pending==i?"ON AIR":save.scores[i]>0?save.scores[i].ToString("D7"):"TUNE IN",r.x+547,r.y,244,78,23,ink,true,TextAnchor.MiddleRight);
            }
            JP(Ep.objective,72,659,828,51,25,Ep.color,true);
            var edge=Broadcast.Colors[pilot];
            Portrait(pilot,new Rect(939,52,564,457));Plate(new Rect(939,509,564,6),edge);
            JP(Broadcast.Names[pilot],941,518,530,61,37,Paper,true);
            JP(Broadcast.Calls[pilot]+"　/　"+Broadcast.Guns[pilot],944,578,570,48,23,edge,true);
            for(int i=0;i<3;i++)
            {
                var r=PilotRect(i);Plate(r,i==pilot?edge:Dark);
                JP(i>=CrewCount?"未接続":Broadcast.Short[i],r.x+23,r.y,r.width-25,r.height,26,i==pilot?Night:i>=CrewCount?Slate:Paper,true);
            }
            string[] bio=Broadcast.Bios[pilot].Length>22?new[]{Broadcast.Bios[pilot].Substring(0,22),Broadcast.Bios[pilot].Substring(22)}:new[]{Broadcast.Bios[pilot]};
            for(int i=0;i<bio.Length;i++)JP(bio[i],940,728+i*33,585,32,21,Soft);
            Plate(DispatchRect,Ep.color);JP(save.pending==selected?"ENTER　帰投通信":"ENTER　回線を開く",95,737,426,78,32,Night,true);
            bool tape=(save.crewTapes&(1<<pilot))!=0;
            Plate(TapeRect,Dark);JP(tape?"R　深夜の個人放送":"個人放送　未収録",542,737,331,48,25,tape?edge:Soft,true);
            JP(tape?Broadcast.NightTitles[pilot]:"この操縦士で任意の章をクリア",543,785,330,26,17,Soft);
            JP("↑ ↓　章選択　　 ← →　操縦士　　 Esc　タイトル",71,840,1350,42,21,Soft,true);
        }
        void TalkText()
        {
            Voice voice=CurrentVoice;var accent=Broadcast.Colors[voice.person];
            Plate(new Rect(0,0,1600,900),Hex(0x101523));
            Portrait(voice.person,new Rect(0,0,535,900),1,post?nightCast:castArt);Plate(new Rect(535,0,8,900),accent);
            Label("PARADE   FM     /     "+Ep.call,603,65,900,44,24,Ep.color,true);
            JP("第 "+(selected+1)+" 話　　"+Ep.place,603,135,900,54,28,Soft,true);
            JP(Ep.title,600,193,945,95,62,Paper,true);
            Plate(new Rect(605,321,890,2),Slate);
            JP(choosing?"救援回線を選ぶ":Broadcast.Names[voice.person],603,375,910,69,43,accent,true);
            if(choosing)
            {
                CopyLines("どちらの回線を、地上につなぐ？\n選んだ支援が第4話・第5話の戦闘に届きます。",604,489,29,Paper,57);
                for(int i=0;i<2;i++){var r=ChoiceRect(i);Plate(r,choice==i?accent:Dark);JP(i==0?"整備班　／　修理":"増幅塔　／　充填",r.x+22,r.y,r.width-37,r.height,29,choice==i?Night:Paper,true);}
            }
            else CopyLines(voice.words,604,477,31,Paper,63);
            if(openMic>0 && selected==4 && post && line==1)
            {
                for(int i=0;i<4;i++)Plate(new Rect(605+i*105,664,88,9),4-openMic>=i?accent:Slate);
                JP("回線開放中",604,689,600,45,23,accent,true);
            }
            JP(choosing?"← →  選択　 Enter  決定":"Enter / クリック　次へ　　 Tab　会話送り",604,785,910,55,23,Soft);
            Label(post?"RETURN SIGNAL":"BEFORE SORTIE",604,842,520,31,16,Ep.color,true);
            int count=(post?Ep.after.Length:Ep.before.Length)+(post && selected==4?1:0);
            Label(choosing||branchReceipt?"LINK":(line+1)+"  /  "+count,1260,837,237,39,21,Soft,true,TextAnchor.MiddleRight);
        }
        void TapeText()
        {
            var pages=Broadcast.NightShows[pilot];var voice=pages[line];var c=Broadcast.Colors[voice.person];
            Plate(new Rect(0,0,1600,900),Hex(0x171524));
            Portrait(voice.person,new Rect(0,0,535,900),1,nightCast);Plate(new Rect(535,0,8,900),c);
            Label("AFTER HOURS   /   "+Broadcast.Calls[pilot],602,68,890,47,27,c,true);
            JP("深夜の個人放送",602,166,840,54,28,Soft,true);JP(Broadcast.NightTitles[pilot],600,222,940,84,60,Paper,true);
            Plate(new Rect(603,344,894,2),Slate);JP(Broadcast.Names[voice.person],602,391,910,64,40,c,true);
            CopyLines(voice.words,602,492,30,Paper,58);JP("Enter / クリック　次へ　　 Esc　放送卓へ",603,799,915,51,23,Soft);
            Label((line+1)+"  /  "+pages.Length,1290,838,210,41,21,c,true,TextAnchor.MiddleRight);
        }
        void CreditsText()
        {
            Photo(dawnArt,new Rect(0,0,1600,900),new Rect(0,0,1,1),Color.white);
            Plate(new Rect(0,0,655,900),Fade(Night,.89f));Plate(new Rect(71,169,90,7),Lime);
            Label("PARADE   FM   /   OFF AIR",71,92,570,48,25,Lime,true);
            JP(save.route==0?"始発の声":"街角の四秒",65,204,590,110,70,Paper,true);
            JP("全5話　放送終了",71,341,572,56,32,Mint,true);
            CopyLines(save.route==0?"翌朝、海底線が運転を再開した。\n始発のマイクを握るのはミオだ。":"日曜日、街角に小さな放送卓ができた。\n最初のリクエストは四秒の静寂。",71,438,25,Paper,45);
            int sum=0;foreach(int n in save.scores)sum+=n;
            Label("CHAPTER BEST TOTAL",71,611,520,39,21,Soft,true);Label(sum.ToString("D7"),68,653,537,70,58,Lime,true);
            JP("Enter　放送卓へ",71,802,530,52,26,Paper,true);
        }
        void FlightRadio()
        {
            bool cut=cutTime>0 && !lowFX;
            if(!cut&&radioTime<=0)return;
            int who=cut?pilot:radioVoice.person;var color=Broadcast.Colors[who];
            float y=game.player.y>1.45f && game.player.x<-.8f?706:92;
            float width=cut?440:635, height=cut?102:76;
            Plate(new Rect(20,y,width,height),Fade(Night,.91f));Plate(new Rect(20,y,5,height),color);
            Portrait(who,new Rect(25,y,cut?115:83,height),1,cut?burstCast:castArt);
            float x=cut?155:122;
            Label(cut?Broadcast.Calls[who]+"   /   BURST":Broadcast.Calls[who]+"   /   RADIO",x,y+2,width-112,30,18,color,true);
            JP(cut?Broadcast.Shouts[who]:radioVoice.words,x,y+29,width-110,height-28,cut?31:22,Paper,true);
        }
    }
}
