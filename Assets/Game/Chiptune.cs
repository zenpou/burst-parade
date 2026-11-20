using System;
using UnityEngine;

namespace Parade
{
    internal sealed class Chiptune
    {
        const int Rate=22050;
        AudioSource music, effects;
        readonly AudioClip[] chimes=new AudioClip[8];
        readonly AudioClip[] tracks=new AudioClip[6];
        readonly AudioClip[,] stationIds=new AudioClip[5,2];
        AudioClip fire, pop, hurt, blast, rise, win, fail;
        float lastFire;
        public bool muted { get; private set; }
        public Chiptune(GameObject host)
        {
            music=host.AddComponent<AudioSource>(); effects=host.AddComponent<AudioSource>();
            music.playOnAwake=false; effects.playOnAwake=false;
            music.loop=true; music.volume=.33f; effects.volume=.55f;
            int[] scale={0,2,4,7,9,12,14,16};
            for(int i=0;i<chimes.Length;i++)
            {
                float pitch=Note(72+scale[i]);
                chimes[i]=Tone("chain "+(i+1),.16f,(t,u)=>Sin(t*pitch)*(1-u));
            }
            fire=Tone("shot",.075f,(t,u)=>Tri(t*(700-280*u))*.25f);
            pop=Tone("break",.17f,(t,u)=>(Noise(t)*.48f+Sin(t*(130-75*u))*.7f)*(1-u));
            hurt=Tone("armor",.42f,(t,u)=>Tri(t*(185-98*u))*.85f+Noise(t)*.12f);
            blast=Tone("burst",.72f,(t,u)=>(Sin(t*(160-118*u))*.8f+Noise(t)*.45f+Tri(t*(370+400*u))*.22f)*(1-u));
            rise=Tone("charged",.39f,(t,u)=>Tri(t*Note(69+Mathf.FloorToInt(u*4)*3))*.52f);
            win=Tone("clear",.95f,(t,u)=>Tri(t*Note(64+Mathf.Min(3,(int)(u*6))*3))*.52f);
            fail=Tone("down",.68f,(t,u)=>Tri(t*Note(57-(int)(u*5)*3))*.55f);
            muted=PlayerPrefs.GetInt("mute",0)!=0; Apply();
        }
        static float Sin(float phase)=>Mathf.Sin(phase*6.283185f);
        static float Tri(float phase)=>1-4*Mathf.Abs(Mathf.Repeat(phase,1)-.5f);
        static float Note(int n)=>440*Mathf.Pow(2,(n-69)/12f);
        static float Noise(float t)
        {
            uint n=(uint)(t*Rate)*747796405u+2891336453u;
            n=((n>>((int)(n>>28)+4))^n)*277803737u;
            return ((n^(n>>22))&65535)/32768f-1;
        }
        static AudioClip Tone(string name,float duration,Func<float,float,float> wave)
        {
            var data=new float[(int)(Rate*duration)];
            for(int i=0;i<data.Length;i++)
            {
                float u=i/(float)data.Length;
                data[i]=Mathf.Clamp(wave(i/(float)Rate,u),-1,1)*Mathf.Min(1,i/64f)*(1-u);
            }
            return Clip(name,data);
        }
        static AudioClip Clip(string name,float[] data)
        {
            var clip=AudioClip.Create(name,data.Length,1,Rate,false);
            clip.SetData(data,0); return clip;
        }
        static readonly int[][] BassLines={
            new[]{40,40,36,38,40,43,36,38},new[]{38,34,41,36,38,41,34,33},
            new[]{43,47,40,45,43,38,40,42},new[]{42,38,45,40,42,45,37,40},
            new[]{36,37,43,42,36,39,37,43},new[]{48,43,45,41,48,47,45,43}};
        static readonly int[][] Melodies={
            new[]{0,7,10,14,12,7,3,10},new[]{12,7,2,0,5,7,3,2},
            new[]{4,7,11,14,16,14,11,7},new[]{0,12,7,15,2,14,7,10},
            new[]{0,1,7,12,10,7,3,1},new[]{4,7,9,12,11,7,4,2}};
        static readonly int[] Bpm={148,118,136,160,152,90};
        static AudioClip MakeLoop(int chapter)
        {
            float step=60f/Bpm[chapter]/4;
            int[] roots=BassLines[chapter],arp=Melodies[chapter];
            var data=new float[Mathf.RoundToInt(step*128*Rate)];
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)Rate;
                int tick=(int)(t/step); float sub=t-tick*step; int bar=(tick/16)%8;
                int root=roots[bar];
                float beat=t%(step*4), bass=t%(step*2);
                float drum=Mathf.Sin(6.283185f*(44*beat+8*(1-Mathf.Exp(-beat*21))))*Mathf.Exp(-beat*18)*.47f;
                float hat=Noise(t)*Mathf.Exp(-sub*(tick%2==0?100:55))*.15f;
                float snare=(tick/4%2==1)?Noise(t)*Mathf.Exp(-beat*22)*.25f:0;
                float low=Tri(bass*Note(root))*.36f*Mathf.Exp(-bass*11);
                float melody=Tri(sub*Note(root+24+arp[(tick/2+bar)%8]))*Mathf.Exp(-sub*26)*(tick%2==0?.19f:.06f);
                float lead=chapter==1?Sin(t*Note(root+12))*.14f:chapter==2||chapter==5?Sin(t*Note(root+28))*.085f:
                    chapter==3?Tri(sub*Note(root+36))*Mathf.Exp(-sub*44)*.12f:
                    chapter==4?Tri(beat*Note(root+7))*.15f*Mathf.Exp(-beat*9):0;
                float drums=chapter==5?.34f:chapter==1?.75f:1;
                data[i]=((drum+hat+snare)*drums+low+melody+lead)*Mathf.Min(1,i/64f,(data.Length-i)/64f);
            }
            return Clip("Parade FM "+(chapter+1),data);
        }
        public void PlayRun(int chapter){music.Stop();music.pitch=1;if(!tracks[chapter])tracks[chapter]=MakeLoop(chapter);music.clip=tracks[chapter];music.Play();}
        public void PhaseTwo(){music.pitch=1.055f;effects.PlayOneShot(rise,.65f);}
        public void Stop(){music.Stop();}
        public void Quiet(){music.Stop();effects.Stop();}
        public void Tune(int note){effects.PlayOneShot(chimes[Mathf.Clamp(note,0,7)],.36f);}
        public void StationId(int ch,bool resolved)
        {
            int variation=resolved?1:0;
            var clip=stationIds[ch,variation];
            if(!clip)
            {
                var melody=Melodies[ch];
                float beat=resolved?.32f:.19f;
                clip=Tone("Radio signature "+ch+"-"+variation,beat*4+.38f,(time,u)=>{
                    int n=Mathf.Min(3,(int)(time/beat));
                    float local=time-n*beat;
                    int step=resolved?3-n:n;
                    float hz=Note(BassLines[ch][0]+24+melody[step]);
                    float body=resolved?Sin(local*hz)+.22f*Sin(local*hz*2):Tri(local*hz)+.17f*Sin(local*hz*3);
                    return body*.57f*Mathf.Min(local*150,1)*Mathf.Exp(-local*(resolved?4:8));
                });
                stationIds[ch,variation]=clip;
            }
            effects.PlayOneShot(clip,.77f);
        }
        public void Pause(bool pause){if(pause) music.Pause(); else music.UnPause();}
        public void Toggle(){muted=!muted; PlayerPrefs.SetInt("mute",muted?1:0); Apply();}
        void Apply(){music.mute=muted; effects.mute=muted;}
        public void Event(string name,int value)
        {
            switch(name)
            {
                case "fire": if(Time.unscaledTime-lastFire>.085f){effects.PlayOneShot(fire,.4f);lastFire=Time.unscaledTime;} break;
                case "kill": effects.PlayOneShot(pop,.8f); if(value>=2)effects.PlayOneShot(chimes[Mathf.Clamp(value-2,0,7)],.44f); break;
                case "hurt": effects.PlayOneShot(hurt); break;
                case "burst": effects.PlayOneShot(blast); break;
                case "boss": case "ready": effects.PlayOneShot(rise); break;
                case "clear": music.Stop(); effects.PlayOneShot(win); break;
                case "defeat": music.Stop(); effects.PlayOneShot(fail); break;
            }
        }
    }
}
