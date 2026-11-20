using System.Collections.Generic;
using UnityEngine;

namespace Parade
{
    internal static class Ink
    {
        public static readonly Color Night = Hex(0x101526), Dark = Hex(0x222842), Slate = Hex(0x424767),
            Paper = Hex(0xfff1d0), Mint = Hex(0x49f2dd), Lime = Hex(0xdfff46), Hot = Hex(0xff507d),
            Flame = Hex(0xffad54), Purple = Hex(0xa28afb);
        public static Color Hex(int n) => new Color((n >> 16 & 255) / 255f, (n >> 8 & 255) / 255f, (n & 255) / 255f);
        public static Color Fade(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }
        public static Vector2 V(float x, float y) => new Vector2(x,y);
        static void Point(Vector2 p) => GL.Vertex3(p.x,p.y,0);
        public static void Tri(Vector2 a, Vector2 b, Vector2 c, Color ink)
        {
            GL.Color(ink); Point(a); Point(b); Point(c);
        }
        public static void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink)
        { Tri(a,b,c,ink); Tri(c,d,a,ink); }
        public static void Box(float x,float y,float w,float h,Color c)
        { Quad(V(x,y),V(x+w,y),V(x+w,y+h),V(x,y+h),c); }
        public static void Line(Vector2 a,Vector2 b,float width,Color c)
        {
            var n = Vector2.Perpendicular(b-a).normalized*(width*.5f);
            Quad(a+n,b+n,b-n,a-n,c);
        }
        public static Vector2 Rad(float angle) => V(Mathf.Cos(angle),Mathf.Sin(angle));
        public static void Ring(Vector2 p,float r,float thickness,Color c,float start=0,float sweep=6.283185f,int count=48)
        {
            var prev=Rad(start);
            for(int i=1;i<=count;i++)
            {
                var next=Rad(start+sweep*i/count);
                Quad(p+prev*r,p+next*r,p+next*(r-thickness),p+prev*(r-thickness),c);
                prev=next;
            }
        }
        public static void Diamond(Vector2 p,float w,float h,Color c)
        { Quad(p+V(-w,0),p+V(0,h),p+V(w,0),p+V(0,-h),c); }
        public static void Aura(Vector2 p,float w,float h,Color c,int n=16)
        {
            var last=p+V(w,0); var edge=Fade(c,0);
            for(int i=1;i<=n;i++)
            {
                var next=p+Vector2.Scale(Rad(i*6.283185f/n),V(w,h));
                GL.Color(c);Point(p);GL.Color(edge);Point(last);Point(next);last=next;
            }
        }
        // Stencil lettering, with sheared square cells and tight tracking.
        static readonly Dictionary<char,string> Letters = new Dictionary<char,string>{
            {'B',"11110100101001011110100111001111110"},
            {'U',"11001110011100111001110011100101110"},
            {'R',"11110100111001111110111001101011001"},
            {'S',"01111110001100001110000110001111110"},
            {'T',"11111001100011000110001100011000110"},
            {'P',"11110110111101111110110001100011000"},
            {'A',"01110110111101111111110111101111011"},
            {'D',"11110110111101111011110111101111110"},
            {'E',"11111110001100011110110001100011111"} };
        public static void Word(string text,float left,float top,float cell,Color c)
        {
            foreach(char ch in text)
            {
                string mask=Letters[ch];
                for(int row=0;row<7;row++)
                for(int col=0;col<5;col++)
                {
                    if(mask[row*5+col]!='1') continue;
                    float x=left+(col+(6-row)*.15f)*cell, y=top-row*cell;
                    Quad(V(x,y),V(x+cell*1.005f,y),V(x+cell*.855f,y-cell*1.005f),V(x-cell*.15f,y-cell*1.005f),c);
                }
                left+=5.65f*cell;
            }
        }
        public static void Enemy(Vector2 p,Kind kind,float s,float t)
        {
            bool boss=kind==Kind.Boss;
            Color armor=kind==Kind.Gunner?Purple:kind==Kind.Heavy?Flame:Hot;
            if(boss) armor=Hot;
            if(kind==Kind.Scout)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Tri(p+V(-.94f,0)*s,p+V(.64f,side*.67f)*s,p+V(.28f,side*.10f)*s,armor);
                    Tri(p+V(-.94f,0)*s,p+V(.30f,side*.26f)*s,p+V(-.06f,0)*s,Paper);
                }
                Diamond(p+V(.19f,0)*s,.20f*s,.09f*s,Flame);return;
            }
            if(kind==Kind.Heavy)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Quad(p+V(-.68f,.54f*side)*s,p+V(.31f,.79f*side)*s,p+V(.72f,.48f*side)*s,p+V(-.15f,.32f*side)*s,Flame);
                    Line(p+V(-.79f,.46f*side)*s,p+V(.32f,.53f*side)*s,.105f*s,Paper);
                }
            }
            // Mirrored wing plates, exposed joints and a luminous forward optic.
            for(int side=-1;side<=1;side+=2)
            {
                float wing=boss?1.1f:kind==Kind.Scout?.65f:.85f;
                Vector2 tip=p+V(.78f,side*wing)*s;
                Quad(p+V(-.60f,.14f*side)*s,p+V(.22f,.52f*side)*s,tip,p+V(.22f,.12f*side)*s,armor);
                Tri(p+V(-.6f,.14f*side)*s,p+V(.22f,.52f*side)*s,p+V(.19f,.37f*side)*s,Paper);
                Quad(p+V(.12f,.20f*side)*s,p+V(.65f,.56f*side)*s,p+V(.48f,.22f*side)*s,p+V(.21f,.09f*side)*s,Slate);
                if(boss || kind==Kind.Heavy)
                {
                    Quad(p+V(-.25f,.38f*side)*s,p+V(.18f,.66f*side)*s,p+V(.42f,.74f*side)*s,p+V(.08f,.42f*side)*s,Dark);
                    Line(p+V(-.21f,.4f*side)*s,p+V(.20f,.65f*side)*s,.026f*s,Lime);
                }
            }
            Diamond(p,.80f*s,.29f*s,Slate);
            Tri(p+V(-.83f,0)*s,p+V(.08f,.29f)*s,p+V(.34f,.06f)*s,Paper);
            Tri(p+V(-.83f,0)*s,p+V(.42f,-.12f)*s,p+V(.08f,-.29f)*s,armor);
            Diamond(p+V(-.21f,0)*s,.32f*s,.11f*s,Night);
            Diamond(p+V(-.23f,0)*s,.16f*s,(.055f+.013f*Mathf.Sin(t*6))*s,Lime);
            if(boss) for(int i=0;i<3;i++) Box(p.x+(.03f+i*.09f)*s,p.y-.025f*s,.045f*s,.05f*s,Flame);
        }
    }
}
