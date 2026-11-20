namespace Parade
{
    /// <summary>
    /// The five women behind the chapter bosses. The role is intentionally short so the
    /// duel card can read as a job title while the portrait carries the visual identity.
    /// </summary>
    internal sealed class Rival
    {
        public string name, role, entry, reply, phaseTwo, eventTitle, eventLine;
        public int replyPilot;

        public Rival(string name, string role, string entry, string reply, string phaseTwo,
            string eventTitle, string eventLine, int replyPilot)
        {
            this.name = name;
            this.role = role;
            this.entry = entry;
            this.reply = reply;
            this.phaseTwo = phaseTwo;
            this.eventTitle = eventTitle;
            this.eventLine = eventLine;
            this.replyPilot = replyPilot;
        }
    }

    internal static class Rivals
    {
        public static readonly Rival[] All =
        {
            new Rival("ナギ","第七埠頭の灯台守",
                "『安全です』。妹が最後に聞いたのは、\nあんたと同じ局の声だった。灯は貸せない。",
                "じゃあ、マイクはナギさんが持って。\n私の声が嘘かどうか、海に聞いてよ。",
                "あの子が信じた灯なんだ！","二人で点す灯",
                "そこ、押さえてて。……見えた？\n沖の船が、灯を返してくれた。",0),
            new Rival("スズ","海底環状線の車掌",
                "この時計は、最後の一人が降りるまで止めない。\n『空車』の放送は、もう結構です。",
                "あの放送をしたのは私です。\n今夜は、一人ずつ名前を聞かせてください。",
                "発車はまだ。席が埋まってる！","十三ミリの約束",
                "お預かり物です、ミオさん。\n持ち手の傷まで、お母さまのままですよ。",1),
            new Rival("リラ","浮遊温室の記録係",
                "指揮者さん。また花を摘みに来たの？\n去年、あなたの声で七鉢が枯れたわ。",
                "今日は命令書も鋏も持ってきてない。\nあなたの許しが出るまで、ここで聞く。",
                "根を張るまで何年かかったと思う？","耳もとの白い蘭",
                "茎に指を添えて。ほら、震えた。\nその声は、あなたに返事をしたかったのよ。",2),
            new Rival("トワ","対衛星の通信士",
                "どっちの線も貸さない。姉ちゃんが呼ぶのが\n右か左か、鳴るまでわかんないんだ。",
                "だったら二つを同じ時計にしよう。\n片方が鳴れば、もう片方から返せる。",
                "二度も聞き逃せるわけないじゃん！","同じ秒を刻む",
                "……今の、姉ちゃんの咳払い。\nミオ、あんたにも聞こえた？",1),
            new Rival("イオ","中央放送の指揮者",
                "命令の最後の音が、私の最初の音だった。\n棒を下ろしたら、私はどこにいればいい？",
                "その四秒、私の隣にいて。\n次に何を言うかは、あなたが決めて。",
                "まだ終わりの音を鳴らさないで！","二人の休符",
                "棒って、こんなに軽かったんだ。\nセナ。あと四秒、手を離さないで。",2)
        };
    }
}
