using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    public sealed partial class Stage
    {
        const float Side=80f/9;
        void OnPostRender()
        {
            if(!mat || !mat.SetPass(0))return;
            GL.PushMatrix(); GL.LoadPixelMatrix(-Side,Side,-5,5); GL.Begin(GL.TRIANGLES);
            if(mode==ScreenMode.Flying && scenery[selected]){
                GL.End(); GL.PopMatrix();
                GL.PushMatrix();GL.LoadPixelMatrix(0,1600,900,0);
                ScrollBackdropPhoto(scenery[selected],game.time);
                GL.PopMatrix();GL.PushMatrix();GL.LoadPixelMatrix(-Side,Side,-5,5);
                mat.SetPass(0);GL.Begin(GL.TRIANGLES);
                ScrollLandmarks(game.time);
                Atmosphere();
            }else Backdrop();
            if(mode==ScreenMode.Cover) Cover();
            else if(mode==ScreenMode.Flying)
            {
                Field(); HudPlates();
                if(ResultVisible||paused) ResultPlate();
            }
            GL.End(); GL.PopMatrix(); DrawText();
        }
        const float BackdropWidth=1800f, BackdropBlend=172f, BackdropSpeed=84f;
        void ScrollBackdropPhoto(Texture2D tex,float t)
        {
            // Feather successive strips in their original orientation. Mirroring a
            // harbor or skyline would place a second sun beside the first.
            const float stride=BackdropWidth-BackdropBlend;
            float offset=Mathf.Repeat(t*BackdropSpeed,stride);
            var tint=new Color(.78f,.81f,.88f,1f);
            artMat.mainTexture=tex;artMat.SetPass(0);GL.Begin(GL.QUADS);
            for(int slot=-1;slot<=1;slot++)
            {
                float x=slot*stride-offset;
                if(x>=1600f||x+BackdropWidth<=0)continue;
                const float split=BackdropBlend/BackdropWidth;
                GL.Color(Fade(tint,0));GlyphPoint(x,0,new Vector2(0,1));
                GL.Color(tint);GlyphPoint(x+BackdropBlend,0,new Vector2(split,1));
                GlyphPoint(x+BackdropBlend,900,new Vector2(split,0));
                GL.Color(Fade(tint,0));GlyphPoint(x,900,new Vector2(0,0));
                GL.Color(tint);GlyphPoint(x+BackdropBlend,0,new Vector2(split,1));
                GlyphPoint(x+BackdropWidth,0,new Vector2(1,1));
                GlyphPoint(x+BackdropWidth,900,new Vector2(1,0));
                GlyphPoint(x+BackdropBlend,900,new Vector2(split,0));
            }
            GL.End();
        }
        static float ScrollCoord(float phase,float t,float speed,float span)
        {
            // Add half the span before Repeat so negative time travel does not
            // jump from the left edge to the right edge on the first frame.
            return Mathf.Repeat(phase-t*speed+span*.5f,span)-span*.5f;
        }
        void Atmosphere()
        {
            float t=game.time;
            for(int i=0;i<27;i++)
            {
                float x=ScrollCoord((i+.5f)*3.728f,t,.8f+i%3*.52f,18f);
                float y=Mathf.Repeat(i*1.944f+.14f*Mathf.Sin(t+i),8.2f)-4.1f;
                if(selected==1&&i%3==0)Ring(V(x,y),.054f,.015f,Fade(Mint,.35f),0,6.2832f,12);
                else if(selected==2&&i%3==0)Diamond(V(x,y),.052f,.023f,Fade(Paper,.39f));
                else Line(V(x,y),V(x+.045f+i%4*.055f,y+.012f),.015f,Fade(Ep.color,.33f));
            }
        }
        // Two edge-only parallax planes make the single illustration read as a
        // side-scrolling stage while leaving the central bullet lane open.
        void ScrollLandmarks(float t)
        {
            if(mode!=ScreenMode.Flying || game.phase!=Phase.Run)return;
            const float span=22f;
            float farSpacing=span/8f, nearSpacing=span/6f;
            Color farInk=Fade(Ep.color,.22f);

            // Distant, thin silhouettes travel slowly along the upper and lower
            // edges.  The repeated 22-unit span always starts off-screen.
            for(int i=0;i<8;i++)
            {
                float x=ScrollCoord((i+.5f)*farSpacing,t,.42f,span);
                float w=.72f+.12f*(i%3);
                Box(x,3.38f,w,.16f,farInk);
                Box(x+.11f,3.54f,w*.68f,.08f,Fade(Paper,.13f));
                Box(x,-3.96f,w,.12f,farInk);
            }

            // The closer plane is larger and faster.  Its geometry is kept below
            // y=-3.15 or above y=3.05, away from enemies and projectile reads.
            for(int i=0;i<6;i++)
            {
                // 2.65 world units/s is about 238 virtual pixels/s in the
                // 1600px playfield, clearly faster than the far plane.
                float x=ScrollCoord((i+.5f)*nearSpacing,t,2.65f,span);
                switch(selected)
                {
                    case 0: // Seventh pier: container blocks and crane legs.
                        Box(x,-4.62f,1.18f,.72f,Fade(Hex(0x6c4150),.58f));
                        Box(x+.11f,-4.51f,.17f,.20f,Fade(Flame,.55f));
                        Line(V(x+.19f,-4.22f),V(x+.19f,-3.90f),.035f,Fade(Hot,.43f));
                        Line(V(x+.19f,3.06f),V(x+.19f,3.90f),.035f,Fade(Hot,.43f));
                        Line(V(x+.19f,3.10f),V(x+1.13f,3.10f),.042f,Fade(Hot,.43f));
                        break;
                    case 1: // Blue Line: overhead tunnel ribs and lit train windows.
                        Ring(V(x+.54f,3.14f),1.22f,.075f,Fade(Mint,.40f),0,Mathf.PI,28);
                        Box(x,-4.24f,1.50f,.22f,Fade(Hex(0x2c6b77),.72f));
                        for(int k=0;k<4;k++)Box(x+.16f+k*.31f,-4.02f,.18f,.12f,Fade(Mint,.55f));
                        break;
                    case 2: // Glass Ark: broad dome tops and a few hanging stems.
                        Ring(V(x+.56f,3.30f),1.08f,.12f,Fade(Hex(0x8aa298),.42f),0,Mathf.PI,26);
                        Line(V(x+.25f,3.27f),V(x+.25f,2.98f),.028f,Fade(Mint,.42f));
                        Box(x,-4.55f,.10f,1.08f,Fade(Hex(0x668575),.40f));
                        Diamond(V(x+.06f,-3.42f),.18f,.28f,Fade(Hex(0x9d7186),.58f));
                        break;
                    case 3: // Twin Spire: orbital maintenance trusses.
                        Line(V(x,3.05f),V(x+1.12f,4.65f),.065f,Fade(Slate,.58f));
                        Line(V(x+.42f,3.05f),V(x+1.55f,4.65f),.065f,Fade(Slate,.48f));
                        Line(V(x,3.62f),V(x+1.21f,3.62f),.038f,Fade(Mint,.45f));
                        Line(V(x+.28f,-4.68f),V(x+1.42f,-3.18f),.055f,Fade(Purple,.58f));
                        break;
                    default: // Dead Air: organ pipes and stained glass fragments.
                        for(int k=0;k<3;k++)
                            Box(x+.14f+k*.27f,-4.72f,.13f,1.52f+k*.34f,Fade(Hex(0x96505f),.58f));
                        Diamond(V(x+.62f,3.32f),.31f,.48f,Fade(Flame,.40f));
                        Line(V(x+.62f,3.78f),V(x+.62f,4.68f),.035f,Fade(Hot,.46f));
                        break;
                }
            }

        }
        void Backdrop()
        {
            if(mode==ScreenMode.Flying && selected>0){OtherSky(game.time);return;}
            float t=game.phase==Phase.Title?anim*.65f:game.time;
            Box(-9,-5,18,10,Night);
            // A moving night expressway with two silhouettes and a gridded horizon.
            Quad(V(-9,-3.5f),V(9,-2.1f),V(9,.3f),V(-9,-1.5f),Hex(0x1b2036));
            if(game.phase!=Phase.Title)
            {
                Vector2 moon=V(5.5f-.013f*t,.9f);
                Aura(moon,2.6f,2.6f,Fade(Hot,.13f),36);
                Ring(moon,1.14f,1.14f,Hex(0x332741),0,6.2832f,64);
                for(int j=0;j<8;j++){
                    float y=-1.03f+j*.14f, half=Mathf.Sqrt(1.14f*1.14f-y*y);
                    Box(moon.x-half,moon.y+y,half*2,.063f-j*.006f,Night);
                }
            }
            for(int i=0;i<85;i++)
            {
                float x=Mathf.Repeat(i*7.341f-t*(.14f+i%4*.13f),18)-9;
                float y=Mathf.Repeat(i*i*.117f+i*.782f,10)-5;
                Box(x,y,.014f+i%4*.023f,.014f,Fade(Paper,.19f+i%3*.07f));
            }
            for(int layer=0;layer<2;layer++)
            for(int i=0;i<40;i++)
            {
                float x=Mathf.Repeat(i*.54f-t*(.20f+.20f*layer),21.6f)-10.8f;
                float h=.22f+Mathf.Repeat(i*i*.113f+i*.39f,1.2f);
                float y=-3.35f-layer*.47f;
                Box(x,y,.38f+i%4*.04f,h,layer==0?Hex(0x2a2b45):Hex(0x343048));
                if(layer==1) for(int row=0;row<3;row++)
                    if(i%3!=row)Box(x+.07f+.05f*row,y+h-.14f-row*.13f,.10f,.026f,Fade(i%3==0?Hot:Mint,.24f));
                if(i%3==0){Box(x+.14f,y+h,.019f,.21f,Slate);Box(x+.08f,y+h*.66f,.05f,.023f,Fade(Flame,.45f));}
            }
            Box(-9,-5,18,1.35f,Hex(0x181d30));
            for(int j=0;j<5;j++){float y=-3.65f-j*j*.059f;Line(V(-9,y),V(9,y),.012f,Fade(Purple,.24f));}
            for(int j=-14;j<15;j++)
            {
                float x=j*1.12f-Mathf.Repeat(t*.65f,1.12f);
                Line(V(x*.76f,-3.65f),V(x*1.4f,-5),.016f,Fade(Purple,.23f));
            }
        }
        void Cover()
        {
            Vector2 center=V(4.6f,.36f);
            for(int i=0;i<14;i++)
            {
                float x=2+i*.57f;
                Line(V(x-1.7f,-3.5f),V(x+1.8f,3.8f),i%4==0?.052f:.014f,Fade(Hot,i%4==0?.38f:.23f));
            }
            Ring(center,2.94f,2.94f,Hex(0x38243c),0,6.2832f,120);
            Ring(center,2.95f,.032f,Hot,0,6.2832f,120);
            for(int i=0;i<60;i++)
            {
                float a=i*Mathf.PI/30+anim*.035f;
                Line(center+Rad(a)*3.01f,center+Rad(a)*(i%5==0?3.18f:3.07f),i%5==0?.033f:.014f,i%5==0?Flame:Slate);
            }
            for(int i=0;i<3;i++)Ring(center,2.68f,.075f,Fade(Hot,.66f),i*2.094f+anim*.12f,.91f);
            for(int i=0;i<9;i++)
            {
                float y=-1.7f+i*.43f;
                float w=Mathf.Sqrt(Mathf.Max(0,2.45f*2.45f-y*y));
                Box(center.x-w,center.y+y,w*2,.028f,Fade(Hot,.25f));
            }
            DrawPilot(V(2.42f,-1.64f+Mathf.Sin(anim*2)*.08f),1.62f,0);
            for(int i=0;i<3;i++)Diamond(V(3.38f+i*.42f,-1.64f),.14f,.035f,Mint);
            // Broad ink sweep below the logo keeps the left-hand print legible.
            Quad(V(-9,-3.66f),V(-.64f,-3.66f),V(2.08f,3.78f),V(-9,3.78f),Fade(Night,.94f));
            Word("BURST",-7.76f,2.63f,.272f,Hot);
            Word("BURST",-7.81f,2.70f,.272f,Paper);
            Word("PARADE",-7.76f,.48f,.257f,Hex(0x40534a));
            Word("PARADE",-7.81f,.56f,.257f,Lime);
            Box(-7.75f,3.49f,.65f,.064f,Hot);
            Box(-7.75f,-3.98f,15.51f,.015f,Slate);
            ButtonPlate();
        }
        void ButtonPlate()
        {
            float x=ActionButton.x/90-Side, y=5-ActionButton.yMax/90, w=ActionButton.width/90,h=ActionButton.height/90;
            Quad(V(x,y),V(x+w-.14f,y),V(x+w,y+.14f),V(x+w,y+h),Lime);
            Tri(V(x,y),V(x+w,y+h),V(x,y+h),Lime);
            Vector2 p=V(x+w-.45f,y+h*.5f);
            Line(p+V(-.11f,.12f),p,.045f,Night);Line(p,p+V(-.11f,-.12f),.045f,Night);
        }
        const float FlightHeight=1.96f;
        void DrawPilot(Vector2 core,float height,float tilt)
        {
            var tex=pilotArt[pilot];
            float width=height*tex.width/tex.height;
            WorldIllustration(tex,core,new Rect(-width*.52f,height*.64f,width,-height),tilt);
        }
        Vector2 PilotExhaust()
        {
            // Attachment point on the rear thruster of each illustrated vehicle.
            var local=pilot==2?V(-.39f,-.45f):V(-.77f,-.34f);
            float angle=(lowFX?0:flightBank)*Mathf.Deg2Rad;
            return Rad(angle)*local.x+Rad(angle+Mathf.PI/2)*local.y;
        }
        void DrawCombatPilot(Vector2 p)
        {
            Color thrust=Broadcast.Colors[pilot];
            DrawPilot(p,FlightHeight,lowFX?0:flightBank);
            float radius=Combat.PlayerBulletRadius;
            for(int i=0;i<4;i++){
                float angle=.36f+i*Mathf.PI/2;
                Ring(p,radius+.018f,.047f,Fade(Night,.85f),angle,.79f,6);
                Ring(p,radius,.025f,Paper,angle,.79f,6);
            }
            Diamond(p,.075f,.075f,Night);Diamond(p,.050f,.050f,Lime);
            if(game.invuln>0)Ring(p,.49f,.036f,Fade(Mint,.6f+.22f*Mathf.Sin(game.time*19)));
            if(muzzleFlash>0){
                var emitter=p+V(.69f,0);
                float flash=muzzleFlash/.075f;
                Aura(emitter,.20f*flash,.11f*flash,Fade(thrust,.78f));
                Diamond(emitter,.19f*flash,.048f*flash,Paper);
            }
        }
        void Field()
        {
            Vector2 j=lowFX?Vector2.zero:V(Mathf.Sin(anim*94),Mathf.Sin(anim*121))*shake;
            foreach(var u in game.units)
            {
                var p=u.pos+j;
                if(u.kind==Kind.Bullet)continue;
                if(u.kind==Kind.Shot)
                {
                    Color shotColor=Broadcast.Colors[pilot];var dir=u.vel.normalized;
                    float length=pilot==2?.83f:pilot==1?.25f:.36f;
                    Aura(p-dir*.09f,length*1.4f,.13f,Fade(shotColor,.58f));
                    Line(p-dir*length,p+dir*.07f,pilot==2?.11f:.05f,shotColor);
                    Line(p-dir*length*.58f,p+dir*.08f,.025f,Paper);continue;
                }
                float s=u.kind==Kind.Boss?1.37f:u.kind==Kind.Heavy?.77f:u.kind==Kind.Gunner?.63f:.49f;
                if(u.kind==Kind.Boss)
                {
                    Ring(p,1.50f,.032f,Fade(Hot,.5f),anim*1.2f,2.6f);
                    Ring(p,1.50f,.032f,Fade(Hot,.5f),anim*1.2f+3.14f,2.6f);
                }
                DrawFoe(u,p,s);
                if(u.telegraph>0 && u.kind!=Kind.Scout)
                    Ring(p+V(-.51f*s,0),.10f+(1-u.telegraph)*.29f,.043f,Flame);
                if(u.kind==Kind.Heavy && u.hp<u.maxHp)
                    Box(p.x-.30f,p.y+.80f,.60f*u.hp/u.maxHp,.025f,Flame);
            }
            if(game.phase!=Phase.Defeat)
            {
                var p=game.player+j;
                Color thrust=Broadcast.Colors[pilot];
                Vector2 exhaust=PilotExhaust();
                for(int k=wake.Length-1;k>0;k--)
                {
                    float a=1-k/(float)wake.Length;
                    Vector2 at=wake[k]+j+exhaust+Vector2.left*(k*.12f);
                    Vector2 to=wake[k-1]+j+exhaust+Vector2.left*((k-1)*.12f);
                    Line(at,to,.035f+.16f*a,Fade(thrust,.12f+.38f*a));
                    Line(at,to,.014f+.045f*a,Fade(Paper,.52f*a));
                }
                Vector2 nozzle=p+exhaust;
                float plume=.39f+(lowFX?0:.10f*Mathf.Sin(game.time*30f));
                Tri(nozzle+V(.10f,.09f),nozzle+V(-plume,0),nozzle+V(.10f,-.09f),thrust);
                Aura(nozzle,.31f,.12f,Fade(thrust,.75f));
            }
            foreach(var f in sparks)
            {
                float a=f.life/f.full;Vector2 at=f.p+j,tail=at-f.v*.064f;
                Line(at,tail,f.size*2.1f,Fade(f.c,a*.21f));
                Line(at,tail,f.size,Fade(f.c,a));Diamond(at,f.size*.79f,f.size*.79f,Fade(Paper,a*.7f));
            }
            foreach(var m in marks)
            {
                float u=1-m.life/m.full;
                Aura(m.p+j,(.35f+u)*m.size*2.1f,(.24f+u)*m.size,Fade(m.c,.58f*(1-u)));
                Ring(m.p+j,(.13f+u)*m.size,.012f+(1-u)*.049f,Fade(m.c,1-u),0,6.2832f,24);
                if(m.chain>1) for(int l=0;l<6;l++)
                {
                    Vector2 dir=Rad(l*1.0472f+m.chain*.22f);
                    Vector2 inner=m.p+j+dir*(.15f+u)*m.size, outer=inner+dir*(1-u)*m.size*.85f;
                    Line(inner,outer,.028f*(1-u),Fade(Flame,1-u));
                }
            }
            if(burstWave>=0)
            {
                float u=burstWave/.68f,r=.25f+u*21;
                if(!lowFX && game.phase==Phase.Run)Signature(u,r);
                Aura(burstOrigin,r,r*.70f,Fade(Lime,(1-u)*.25f),32);
                Ring(burstOrigin,r,Mathf.Min(r,.08f+(1-u)*.30f),Fade(Lime,(1-u)*.94f),0,6.2832f,96);
                Ring(burstOrigin,r*.82f,Mathf.Min(r*.8f,.10f),Fade(Mint,1-u),0,6.2832f,96);
                if(!lowFX) for(int i=0;i<28;i++)Line(burstOrigin+Rad(i*.2244f)*r*.65f,burstOrigin+Rad(i*.2244f)*r*.96f,.03f,Fade(Paper,(1-u)*.6f));
            }
            if(game.phase!=Phase.Defeat)DrawCombatPilot(game.player+j);
            // Draw live threats last so a score explosion cannot conceal a bullet.
            foreach(var u in game.units) if(u.kind==Kind.Bullet)
            {
                Vector2 p=u.pos+j;
                Aura(p,.23f,.23f,Fade(Hot,.38f));
                Diamond(p,.154f,.154f,Night);Diamond(p,.122f,.122f,Hot);Diamond(p,.069f,.069f,Paper);
            }
        }
        void Signature(float u,float radius)
        {
            var color=Broadcast.Colors[pilot];float fade=1-u;
            if(pilot==0)
            {
                for(int i=0;i<13;i++){
                    float angle=i*.4833f+.22f;var dir=Rad(angle);var n=Vector2.Perpendicular(dir);
                    var head=burstOrigin+dir*(radius*1.12f);
                    Tri(head,head-dir*(1+radius*.32f)+n*.10f*fade,head-dir*(.68f+radius*.24f)-n*.07f,Fade(i%3==0?Hot:color,.74f*fade));
                }
            }
            else if(pilot==1)
            {
                for(int i=0;i<3;i++){
                    var center=burstOrigin+V(.25f,i-1)*u*3.4f;
                    Ring(center,radius*(.48f+i*.105f),.08f,Fade(i==1?Paper:Mint,fade*.78f),i*1.3f-u,4.8f,60);
                }
            }
            else
            {
                for(int i=-2;i<=2;i++){
                    var a=burstOrigin+V(-5,i*.18f-u*.52f);
                    var b=burstOrigin+V(radius*1.7f,i*(.15f+u*.75f));
                    Line(a,b,.03f+fade*.09f,Fade(i==0?Paper:Purple,fade*.78f));
                    Diamond(b,.25f,.075f,Fade(Flame,fade));
                }
            }
        }
        void HudPlates()
        {
            Box(-9,4.16f,18,.86f,Fade(Night,.97f));Box(-9,4.15f,18,.018f,Slate);
            Box(-9,-5,18,.64f,Night);Box(-9,-4.37f,18,.018f,Slate);
            for(int i=0;i<3;i++)Diamond(V(3.47f+i*.32f,4.54f),.115f,.15f,i<game.hull?Mint:Slate);
            for(int i=0;i<12;i++)
                Box(-7.02f+i*.19f,-4.78f,.145f,.18f,(i+1)/12f<=game.charge+.0001f?Lime:Slate);
            if(game.bossSpawned){Box(-2.65f,4.23f,5.3f,.058f,Slate);Box(-2.65f,4.23f,5.3f*game.bossRatio,.058f,Hot);}
            if(game.comboTime>0)Box(5.93f,-4.41f,2.35f*Mathf.Clamp01(game.comboTime/1.75f),.038f,Hot);
        }
        void ResultPlate()
        {
            Box(-9,-5,18,10,Fade(Night,.77f));
            Quad(V(-8.15f,-3.73f),V(.48f,-3.73f),V(2.58f,3.63f),V(-8.15f,3.63f),Night);
            Color accent=paused?Mint:game.phase==Phase.Clear?Lime:Hot;
            Box(-7.75f,2.79f,.69f,.08f,accent);
            for(int i=0;i<3;i++)Ring(V(4.54f,.20f),2.49f+i*.16f,.033f,Fade(accent,.5f-i*.1f),anim*.13f+i,.8f+i*1.30f);
            if(game.phase==Phase.Defeat)WorldRival(V(4.54f,.20f),5.2f,anim);
            else DrawPilot(V(4.54f,-.55f),4.65f,0);
            ButtonPlate();
        }
    }
}
