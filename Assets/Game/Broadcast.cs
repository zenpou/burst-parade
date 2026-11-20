using UnityEngine;
using static Parade.Ink;

namespace Parade
{
    internal readonly struct Voice
    {
        public readonly int person;
        public readonly string words;
        public Voice(int person,string words){this.person=person;this.words=words;}
    }
    internal sealed class Episode
    {
        public string call, place, title, boss, objective;
        public Rival rival;
        public Color color;
        public Voice[] before, after, air;
        public Episode(string call,string place,string title,string boss,string objective,Color color,Rival rival,Voice[] before,Voice[] after,Voice[] air)
        {this.call=call;this.place=place;this.title=title;this.boss=boss;this.objective=objective;this.color=color;this.rival=rival;this.before=before;this.after=after;this.air=air;}
    }
    internal static class Broadcast
    {
        public static readonly string[] Names={"朱野 アカリ","深波 ミオ","白鐘 セナ"};
        public static readonly string[] Short={"アカリ","ミオ","セナ"};
        public static readonly string[] Calls={"EMBER","TIDE","FORK"};
        public static readonly string[] Guns={"高速連射","三方向ショット","高威力ランス"};
        public static readonly string[] Bursts={"REDLINE RIOT","TIDAL ECHO","FINAL CHORD"};
        public static readonly string[] Shouts={"閃光、オンエア！","潮騒、全開！","終止符を打つ。"};
        public static readonly string[] PhaseTwo={"CROSSWIND","RAIL CROSS","PETAL VORTEX","DOUBLE EXPOSURE","FOURTH BEAT"};
        public static readonly string[] Bios={"港育ちの違法ラジオDJ。閉局ベルを一度も鳴らしたことがない。","海底線の音響技師。雑音だけで列車の居場所を聞き分ける。","元・中央放送指揮者。自分の声で出した命令を、自分で取り消しに来た。"};
        public static readonly Color[] Colors={Flame,Mint,Purple};
        static Voice A(string s)=>new Voice(0,s);
        static Voice M(string s)=>new Voice(1,s);
        static Voice S(string s)=>new Voice(2,s);
        public static readonly Episode[] All={
            new Episode("SALT PORT","第七埠頭","消灯時刻のリクエスト","MANTA-01","港の妨害翼を止め、遭難信号の行き先を探せ。",Flame,Rivals.All[0],
              new[]{A("閉局まであと一曲。宛名なし、曲名なし。\n……ミオ、このベル、曲のイントロじゃないよね。"),
                    M("三回、間を空けて二回。海底線の非常ベルだ。\n港で途切れてる。灯台守が中継器を切ったんだ。"),
                    A("回線開けて。マイクを置くのは、\nこのリクエストに返事してからにしよ。")},
              new[]{M("灯台の向こうで三回、二回。今度は車輪も聞こえる。\nベルの主は、海底線の最後尾にいる。"),
                    A("ナギさん、帰りも灯を頼める？\n私たち、今からあの列車に追いつくから。")},
              new[]{M("赤い灯は港の救難標識。動く菱形が敵弾だよ。"),A("港のスピーカーからも、ベルが聞こえた！"),M("ナギさん、妨害翼を展開した。射線をずらして。")}),
            new Episode("BLUE LINE","海底環状線","最後尾の空席","LEVIATHAN","車掌の装甲を解除し、最後尾にマイクを届けよう。",Mint,Rivals.All[1],
              new[]{M("車両の表示、まだ『空車』なんだ。\n十年前、母の最終列車をそう報告したのは私。"),
                    A("窓、見て。曇ったガラスに誰かが指で丸を描いてる。\n今夜の乗客は、そこにいるよ。"),
                    M("スズさんは連結器の前だ。あの人から見れば、\n放送局のマークをつけた私たちこそ敵だよね。")},
              new[]{M("工具箱のふたの裏に、母の小さな字。\n『ミオへ。怖くても、本当の音を』だって。"),
                    S("……その人が私を逃がした。私は温室にいる。\n庭園の白い蘭を探して。消された声が残ってる。")},
              new[]{M("レールの振動が強い。機雷から距離を取って。"),A("最後尾に子どもがいる。あの丸、また増えた！"),M("スズさんの懐中時計だ。ベルの後に横へ来る。")}),
            new Episode("GLASS ARK","浮遊温室群","硝子の檻に歌う","ORCHID QUEEN","記録花を隔離する花弁を外し、セナを迎えに行こう。",Ink.Hex(0xa6df77),Rivals.All[2],
              new[]{S("温室の花は、声を覚えるの。中央卓で私が消した\n救難放送を、リラが一鉢ずつ育て直した。"),
                    M("水をやって、枯れたら植え替えて……十年も？\nそれを『記録を回収します』じゃ、渡せないよね。"),
                    A("セナ、放送席は三つ。空いてる席まで、\n自分の足で来てよ。リラさんにも直接会おう。")},
              new[]{S("蘭をマイクの前に置くね。リラが譲ってくれた\n地上回線は一本。整備班か、街の増幅塔か。"),
                    M("どちらを呼んでも、もう『誰もいない』とは言わない。\nマイクは生きてる。あなたが呼んで。")},
              new[]{S("白い花弁が合わさる前に、外側から。"),M("花の奥で、昔の発車ベルが鳴ってる。"),A("リラさん、花から手を離さない。囲いだけ外そう。")}),
            new Episode("TWIN SPIRE","軌道昇降塔","ふたつの呼び声","GEMINI","二つの衛星を同期させ、姉妹の回線を残せ。",Purple,Rivals.All[3],
              new[]{S("乗員登録、白鐘セナ。指揮者席は返上したわ。\n……この機体でよければ、操縦も任せて。"),
                    M("軌道塔から二つの呼出音。地上のトワと、\n空にいるお姉さん。時計が少しずつずれてる。"),
                    A("帰ったら何をかけたい、セナ？"),
                    S("無音を四秒。そのあと拍手を。\nずっと、命令のない四秒がほしかった。")},
              new[]{M("『ごはん食べた？』だって。何万キロも越えて、\n一言目がそれ。トワさん、笑いながら怒ってる。"),
                    A("今夜の最後の曲、まだ決まってないよ。\nそうだ。街じゅうから一音ずつもらおう。")},
              new[]{S("衛星は交互に撃つ。撃ち終わりを見て。"),M("二つの呼出音、もう少しで重なるよ。"),A("トワさん、輪を広げた。二重奏ならこっちは三人！")}),
            new Episode("DEAD AIR","中央放送聖堂","四秒のあとで","CODA","イオの指揮を解き、街に四秒の静けさを返せ。",Hot,Rivals.All[4],
              new[]{S("中央卓から私の声で『黙れ』って。……待って。\n録音の終わりに、私の知らない息継ぎがある。"),
                    M("名前を聞いたら『イオ』だって。\n同じ命令を十年繰り返すうちに、自分でつけたの。"),
                    A("本日最後のリクエスト、全市同時に受付開始。\nセナ、マイクは二本あるよ。一本はイオさんに。")},
              new[]{S("こちら白鐘。命令を取り消します。\nイオ、静かになってもマイクはここにあるよ。"),
                    M("一秒。二秒。三秒。……四秒。"),
                    A("聞こえる？　鍋、窓、電車のベル。\nその奥で『もう一回』って。イオさんの声だ。")},
              new[]{A("最終周波数、良好。録音ボタンは押さなくていい。"),S("私の声と、イオの声。もう聞き分けられる。"),M("赤い指揮棒を見て。四拍目が合図。")})
        };
        public static Voice BranchLine(int route)=>route==0?M("整備班に接続。工具箱を開く音が返ってきた。\n後半は各章一度、耐久が残り1になったら応急修理。"):A("増幅塔に接続。街じゅうのアンテナがこっちを向いた。\n後半は各章の開戦18秒後、バーストを満充填！");
        public static Voice LastLine(int route)=>route==0?M("朝いちばんの海底線、私が車内放送をする。\n『おかえりなさい』。今度は、ちゃんと言える。"):A("次の日曜、街角放送を始めるよ。\n一本目の録音は、あの四秒。誰の声も消さずに。");
        public static readonly string[] NightTitles={"裏返しのカセット","十三ミリのレンチ","空席の指揮者"};
        public static readonly Voice[][] NightShows={
            new[]{M("そのテープ、ラベルが裏返し。何度も貼り直した跡がある。"),
                  A("最初の放送、誰も聴いてなかったの。で、自分で\n葉書を書いて、自分で一曲リクエストした。"),
                  A("その日の声。恥ずかしくて上書きできない。\n……今夜はほんとの誰かが鳴らしてくれたんだね。"),
                  M("一人は聴いてたよ。海底線の整備室で。\n二曲目、頭がちょっと切れてた。覚えてる。")},
            new[]{A("十三ミリなんて一本で十分でしょ。なんで二本？"),
                  M("片方、母の名前が彫ってある。撫でると十三の\n『三』だけ浅いの。手が滑った跡まで同じ。"),
                  M("もう一本は私が彫った。工具を渡す相手ができたら、\nこっちを貸すって決めてた。アカリ、手、出して。"),
                  A("……重っ。そっか。借りたからには、帰って返すね。")},
            new[]{S("指揮台ってね、客席の顔がまったく見えないの。\n背中に拍手が刺さるから、振り返れなかった。"),
                  A("ここの席は回るよ。ほら、床を蹴って。"),
                  S("……本当だ。窓も、ミオの背中も見える。\n椅子ひとつで、こんなに違うものなのね。"),
                  M("ストッパー、付けておく？"),S("このままで。次に誰かが呼んだら、振り向きたい。")}
        };
    }
}
