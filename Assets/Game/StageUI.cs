using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    public sealed partial class Stage
    {
        Material type;
        Font latin,japanese;
        static readonly Color Soft=Hex(0xacb5ca);
        void Label(string value,float x,float y,float w,float h,int size,Color color,
            bool bold=false,TextAnchor align=TextAnchor.MiddleLeft,bool jp=false)
        {
            var font=jp?japanese:latin;var style=bold?FontStyle.Bold:FontStyle.Normal;
            font.RequestCharactersInTexture(value,size,style);
            float length=0;
            foreach(char ch in value)if(font.GetCharacterInfo(ch,out var info,size,style))length+=info.advance;
            if(align==TextAnchor.MiddleCenter)x+=(w-length)*.5f;
            if(align==TextAnchor.MiddleRight)x+=w-length;
            float baseline=y+h*.5f+size*.34f;
            type.mainTexture=font.material.mainTexture;
            type.SetPass(0);GL.Begin(GL.QUADS); GL.Color(color);
            foreach(char ch in value)
            {
                if(!font.GetCharacterInfo(ch,out var info,size,style))continue;
                GlyphPoint(x+info.minX,baseline-info.maxY,info.uvTopLeft);
                GlyphPoint(x+info.maxX,baseline-info.maxY,info.uvTopRight);
                GlyphPoint(x+info.maxX,baseline-info.minY,info.uvBottomRight);
                GlyphPoint(x+info.minX,baseline-info.minY,info.uvBottomLeft);
                x+=info.advance;
            }
            GL.End();
        }
        static void GlyphPoint(float x,float y,Vector2 uv){GL.TexCoord2(uv.x,uv.y);GL.Vertex3(x,y,0);}
        void DrawText()
        {
            if(type==null)
            {
                type=new Material(Resources.Load<Shader>("ParadeType"));
                latin=Font.CreateDynamicFontFromOSFont(new[]{"Bahnschrift","Arial"},32);
                japanese=Font.CreateDynamicFontFromOSFont(new[]{"Yu Gothic UI","Meiryo","Arial"},30);
            }
            GL.PushMatrix(); GL.LoadPixelMatrix(0,1600,900,0);
            if(mode==ScreenMode.Cover){CastCover();TitleText();}
            else if(mode==ScreenMode.Desk)DeskText();
            else if(mode==ScreenMode.Talk)TalkText();
            else if(mode==ScreenMode.Credits)CreditsText();
            else if(mode==ScreenMode.NightShow)TapeText();
            else if(mode==ScreenMode.Duel)DuelText();
            else if(mode==ScreenMode.Event||mode==ScreenMode.Album)EventText();
            else if(ResultVisible||paused)ResultText();
            else HudText();
            GL.PopMatrix();
        }
        void TitleText()
        {
            Label("BP   /   PARADE ARCADE",102,48,750,42,26,Paper,true);
            Label("BEST   "+best.ToString("D7"),1050,48,448,42,26,Lime,true,TextAnchor.MiddleRight);
            Label("LAST FREQUENCY",177,113,790,45,24,Hot,true);
            JP("真夜中の五局ネット。街の声を、取り戻せ。",104,565,865,65,28,Paper,true);
            ButtonText("放送卓へ");
            Footer();
        }
        void ButtonText(string action)
        {
            Label("ENTER",129,ActionButton.y,98,80,24,Night,true);
            Label(action,239,ActionButton.y-2,217,80,34,Night,true,TextAnchor.MiddleLeft,true);
        }
        void Footer()
        {
            Label("WASD / 矢印　移動　　 SPACE　バースト　　 射撃はオート",104,815,930,55,22,Paper,true,TextAnchor.MiddleLeft,true);
            Label("M  音 "+(audioRig.muted?"OFF":"ON")+"　　F  演出 "+(lowFX?"弱":"強")+"　　ESC  終了",975,815,523,55,21,Soft,false,TextAnchor.MiddleRight,true);
        }
        void HudText()
        {
            Label("SCORE",41,6,230,25,19,Soft,true);
            Label(game.score.ToString("D7"),41,26,240,45,38,Paper,true);
            string sector=game.bossSpawned?Ep.boss+"　／　"+Ep.rival.name:"0"+(selected+1)+"   /   "+Ep.call;
            Label(sector,460,4,680,55,26,game.bossSpawned?Hot:Paper,true,TextAnchor.MiddleCenter,game.bossSpawned);
            Label("HULL",1040,15,95,50,19,Soft,true);
            Label(((int)game.time/60).ToString("D2")+":"+((int)game.time%60).ToString("D2"),1345,9,208,55,36,Paper,true,TextAnchor.MiddleRight);
            Label("BURST",41,842,120,57,22,Lime,true);
            Label(game.charge>=1?"SPACE  発動":"充填  "+Mathf.FloorToInt(game.charge*100)+"%",402,842,260,57,24,game.charge>=1?Lime:Soft,true,TextAnchor.MiddleLeft,true);
            Label("ESC  PAUSE",672,842,256,57,17,Soft,false,TextAnchor.MiddleCenter);
            int n=game.combo;
            Label(n>1?n+"  CHAIN   /   ×"+Mathf.Min(n,8):"CHAIN   /   ×1",1096,842,457,57,n>1?28:21,n>1?Hot:Soft,true,TextAnchor.MiddleRight);
            if(alert>0)Label("WARNING   /   "+Ep.boss,410,94,780,65,38,Flame,true,TextAnchor.MiddleCenter);
            else if(phaseTag>0)PhaseTransmission();
            else if(game.time<3)Label("WASD / 矢印で移動　・　射撃はオート",450,95,700,54,23,Paper,true,TextAnchor.MiddleCenter,true);
            if(burstWave>=0 && game.phase==Phase.Run)
            {
                float pop=Mathf.Clamp01(burstWave/.09f)*Mathf.Clamp01((.65f-burstWave)/.20f);
                Label(Broadcast.Bursts[pilot],80,744,590,68,44,Fade(Broadcast.Colors[pilot],pop),true);
            }
            foreach(var m in marks)if(m.chain>=3)
            {
                float x=(m.p.x+Side)*90, y=(5-m.p.y)*90-48-(1-m.life/m.full)*29;
                Label("×"+m.chain,x-65,y,130,45,25,Fade(Lime,m.life/m.full),true,TextAnchor.MiddleCenter);
            }
            if(game.phase==Phase.Run)FlightRadio();
        }
        void ResultText()
        {
            bool clear=game.phase==Phase.Clear;
            Label("BP   /   PARADE ARCADE",104,59,650,49,25,Paper,true);
            Label("BEST   "+best.ToString("D7"),1040,59,458,49,25,Lime,true,TextAnchor.MiddleRight);
            Label(paused?"INTERMISSION":clear?"STAGE   /   COMPLETE":"SIGNAL   /   LOST",105,215,800,60,28,paused?Mint:clear?Lime:Hot,true);
            Label(paused?"PAUSED":clear?"STAGE CLEAR":"GAME OVER",96,266,925,140,96,Paper,true);
            JP(paused?"Enter / Esc で再開　　Tab で放送卓へ":clear?Ep.rival.name+"が武装を解除。回線がつながった。":"機体ロスト。この章から再出撃できます。",105,399,850,59,30,Paper,true);
            Label("SCORE",106,490,245,39,22,Soft,true);
            Label("MAX CHAIN",423,490,240,39,22,Soft,true);
            Label("BURSTS",676,490,160,39,22,Soft,true);
            Label(game.score.ToString("D7"),104,521,318,80,55,Lime,true);
            Label("×"+game.maxCombo,422,521,240,80,55,Paper,true);
            Label(game.bursts.ToString(),675,521,190,80,55,Paper,true);
            ButtonText(paused?"再　開":clear?"帰投通信":"再 出 撃");
            if(paused)Label("M  音 "+(audioRig.muted?"OFF":"ON")+"　　F  演出 "+(lowFX?"弱":"強"),105,799,900,50,21,Soft,false,TextAnchor.MiddleLeft,true);
            else JP("Tab / Esc　放送卓へ",105,799,900,50,21,Soft);
        }
    }
}
