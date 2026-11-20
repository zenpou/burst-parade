using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    public sealed partial class Stage
    {
        static void Sky(Color top,Color bottom)
        {
            GL.Color(top);GL.Vertex3(-9,5,0);GL.Vertex3(9,5,0);GL.Color(bottom);GL.Vertex3(-9,-5,0);
            GL.Vertex3(-9,-5,0);GL.Vertex3(9,-5,0);GL.Color(top);GL.Vertex3(9,5,0);
        }
        void OtherSky(float t)
        {
            var c=Ep.color;int ch=selected;
            Sky(ch==1?Hex(0x082532):ch==2?Hex(0x19333c):ch==3?Hex(0x130f2d):Hex(0x251222),ch==1?Hex(0x123d50):ch==2?Hex(0x384455):ch==3?Hex(0x352544):Hex(0x4b2833));
            for(int i=0;i<95;i++){
                float x=Mathf.Repeat(i*7.37f-t*(.12f+i%4*.04f),18)-9;
                float y=Mathf.Repeat(i*i*.137f+i*.827f+(ch==1?t*.20f:0),10)-5;
                if(ch==1 && i%5==0)Ring(V(x,y),.033f+i%3*.03f,.012f,Fade(Mint,.22f),0,6.283f,10);
                else Box(x,y,.02f+i%3*.014f,.018f,Fade(Paper,.18f+i%4*.08f));
            }
            switch(ch)
            {
                case 1:
                    for(int i=0;i<8;i++){
                        float x=Mathf.Repeat(i*3.25f-t*.32f,26)-13;
                        Quad(V(x,5),V(x+.55f,5),V(x-2.85f,-2.4f),V(x-4.2f,-2.4f),Fade(Mint,.048f));
                    }
                    for(int i=0;i<11;i++){
                        float x=Mathf.Repeat(i*2.1f-t*.8f,23.1f)-11.5f;
                        Box(x,-3.43f,1.98f,.78f,Hex(0x1c5362));Box(x,-3.5f,2.0f,.073f,Hex(0x5b9394));
                        for(int j=0;j<5;j++)Box(x+.08f+j*.38f,-3.05f,.25f,.20f,Fade(Mint,.28f));
                        Box(x+.24f,-4.3f,.15f,.76f,Hex(0x183549));
                    }
                    for(int i=0;i<7;i++){float x=Mathf.Repeat(i*3.2f-t*.68f,22.4f)-11;Ring(V(x,-2.42f),1.65f,.085f,Hex(0x275060),0,Mathf.PI,36);}
                    break;
                case 2:
                    Aura(V(4,2),3,2.4f,Fade(Flame,.25f),36);
                    for(int i=0;i<9;i++){
                        float x=Mathf.Repeat(i*3.1f-t*.36f,27.9f)-13.5f,y=-2.5f+Mathf.Sin(i*2.4f)*.52f;
                        Tri(V(x-1.2f,y),V(x+1.3f,y),V(x-.13f,y-1.1f),Hex(0x243744));
                        Ring(V(x,y),1.10f,.031f,Fade(Mint,.55f),0,Mathf.PI,40);
                        for(int j=0;j<4;j++){float r=.42f+j*.18f;Ring(V(x,y),r,.019f,Fade(Mint,.23f),0,Mathf.PI,20);}
                        Line(V(x-1.22f,y),V(x+1.23f,y),.07f,Hex(0x5d766b));
                        for(int j=0;j<5;j++){float xx=x-.7f+j*.35f;var bud=V(xx,y+.3f+.2f*Mathf.Sin(j+i));Line(V(xx,y),bud,.036f,Hex(0x55776e));Diamond(bud,.10f,.14f,Hex(0x967680));}
                    }break;
                case 3:
                    var planet=V(-2.2f,-12.2f);Ring(planet,11.7f,3.5f,Hex(0x344963),0,Mathf.PI,112);
                    Ring(planet,11.78f,.065f,Fade(Mint,.53f),0,Mathf.PI,112);
                    for(int i=0;i<6;i++){
                        float x=Mathf.Repeat(i*5.5f-t*.46f,33)-16.5f;
                        for(int j=0;j<9;j++){var a=V(x+j*.24f,-5+j*1.24f);var b=a+V(.24f,1.24f);Line(a,b,.065f,Slate);Line(a+V(.43f,0),b+V(.43f,0),.065f,Slate);Line(a,b+V(.43f,0),.035f,Slate);}
                    }break;
                default:
                    for(int i=0;i<23;i++){
                        float x=Mathf.Repeat(i*.98f-t*.37f,22.54f)-11.2f;
                        float h=1.0f+Mathf.Abs(Mathf.Sin(i*.58f)) *2.35f;
                        Box(x,-4.6f,.46f,h,Hex(0x563447));Box(x+.07f,-4.6f,.05f,h,Hex(0x895565));
                        Box(x,4.8f-h*.64f,.41f,h*.64f,Hex(0x392233));
                        Diamond(V(x+.22f,-4.6f+h),.23f,.16f,Hex(0x88614f));
                    }
                    for(int j=0;j<3;j++)Ring(V(4.7f,.3f),2.1f+j*.37f,.025f,Fade(Flame,.29f),t*.03f+j,5.68f,84);
                    for(int i=0;i<74;i++){float x=i*.25f-9.1f;float h=.04f+.29f*Mathf.Abs(Mathf.Sin(t*3+i*.52f)*Mathf.Cos(i*.13f));Box(x,-3.96f,.12f,h,Fade(Hot,.36f));}
                    break;
            }
        }
        void DrawFoe(Unit u,Vector2 p,float s)
        {
            float t=u.age;
            if(u.kind==Kind.Boss){RivalTell(u,p);WorldRival(p,3.48f,t);return;}
            switch(u.kind)
            {
                case Kind.Diver:
                    Quad(p+V(-.61f,0),p+V(.12f,.26f),p+V(.40f,0),p+V(.06f,-.22f),Mint);
                    for(int k=-1;k<=1;k+=2)Tri(p+V(-.09f,k*.12f),p+V(.38f,k*.49f),p+V(.26f,k*.08f),Purple);
                    Diamond(p+V(-.30f,0),.12f,.08f,Paper);break;
                case Kind.Mine:
                    for(int i=0;i<8;i++){var d=Rad(t*.6f+i*.7854f);Tri(p+d*.42f,p+d*.16f+Vector2.Perpendicular(d)*.1f,p+d*.16f-Vector2.Perpendicular(d)*.1f,Flame);}
                    Ring(p,.25f,.19f,Hot,0,6.283f,8);Diamond(p,.13f,.13f,Paper);break;
                case Kind.Turret:
                    Ring(p,.45f,.31f,Slate,0,6.283f,6);Ring(p,.41f,.06f,Flame,0,6.283f,6);
                    var dir=(game.player-p).normalized;Line(p,p+dir*.61f,.12f,Flame);Diamond(p,.13f,.13f,Paper);break;
                case Kind.Winder:
                    for(int i=3;i>=0;i--){var a=p+V(i*.16f-.24f,Mathf.Sin(t*5+i*.74f)*.14f);Diamond(a,.22f,.18f,i==0?Paper:i%2==0?Purple:Hot);}
                    break;
                case Kind.Splitter:
                    for(int k=-1;k<=1;k+=2){Diamond(p+V(0,k*.15f),.48f,.20f,k==1?Lime:Mint);Diamond(p+V(-.12f,k*.15f),.12f,.055f,Night);}break;
                case Kind.Shield:
                    Diamond(p,.49f,.37f,Slate);Diamond(p,.30f,.21f,Flame);
                    Ring(p,.61f,u.guardOpen?.025f:.105f,Fade(Mint,u.guardOpen?.25f:.93f),1.75f,u.guardOpen?1.0f:2.77f,24);break;
                case Kind.Lancer:
                    Tri(p+V(-.68f,0),p+V(.38f,.24f),p+V(.24f,-.24f),Purple);
                    Line(p+V(-.60f,0),p+V(.28f,0),.053f,Paper);
                    if(u.timer>0 && u.telegraph>.40f){var aim=(game.player-p).normalized;for(int i=1;i<=6;i++)Line(p+aim*(i*.45f),p+aim*(i*.45f+.16f),.022f,Fade(Flame,u.telegraph));}break;
                default:Enemy(p,u.kind,s,anim);break;
            }
        }
    }
}
