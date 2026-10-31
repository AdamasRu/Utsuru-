#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class InjectNPCResponses : Editor
{
    [MenuItem("Tools/Utsuru/Update All NPC Dialogue & Responses")]
    public static void UpdateAllDialogue()
    {
        NPCSpawner spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[Utsuru] NPCSpawner tidak ditemukan di scene yang sedang aktif!");
            return;
        }

        Undo.RecordObject(spawner, "Update All NPC Dialogue & Responses");

        if (spawner.spawnLocations == null || spawner.spawnLocations.Length < 11)
        {
            Debug.LogWarning("[Utsuru] spawnLocations kurang dari 11 titik. Memastikan array berukuran 11...");
            System.Array.Resize(ref spawner.spawnLocations, 11);
        }

        // 1. Vending Machine 1
        ConfigureLocation(spawner, 0, "VendingMachine", "Aoi",
            "「自販機にお札が入らないな…小銭持ってる？」\n(Jihanki ni osatsu ga hairanai na... Kozeni motteru?)\n\"Mesinnya nggak nerima uang kertas... Punya koin?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「100円玉ならありますよ。」 (Ada koin 100 yen.)",
                    isCorrect = true,
                    npcResponse = "「ありがとう！助かったよ、これでジュースが買える！」\n(Arigatou! Tasukatta yo, kore de juusu ga kaeru!)\n\"Makasih! Terbantu banget, jadi bisa beli jus nih!\""
                },
                new DialogueChoice {
                    choiceText = "「自販機は嫌いです。」 (Saya benci mesin ini.)",
                    isCorrect = false,
                    npcResponse = "「えっ、自販機が嫌いなの…！？便利なのに〜！」\n(E', jihanki ga kirai na no...!? Benri na no ni~!)\n\"Eh, benci vending machine...!? Padahal praktis lho〜!\""
                },
                new DialogueChoice {
                    choiceText = "「お茶が飲みたいです。」 (Saya mau minum teh.)",
                    isCorrect = false,
                    npcResponse = "「あはは、君もお茶が飲みたいんだね！でも小銭がないんだ…」\n(Ahaha, kimi mo ocha ga nomitai n da ne! Demo kozeni ga nai n da...)\n\"Hahaha, kamu juga mau minum teh ya! Tapi aku nggak punya koin nih...\""
                }
            });

        // 2. Toko Kelontong
        ConfigureLocation(spawner, 1, "Toko", "Ren",
            "「いらっしゃい！何を探しているのかい？」\n(Irasshai! Nani o sagashite iru no kai?)\n\"Selamat datang! Sedang cari apa?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「おすすめのお菓子をください。」 (Minta rekomendasi kuenya.)",
                    isCorrect = true,
                    npcResponse = "「毎度あり！このお煎餅が焼きたてでおすすめだよ！」\n(Maido ari! Kono osenbei ga yakitate de osusume da yo!)\n\"Terima kasih! Kerupuk senbei panggang baru matang ini rekomended banget!\""
                },
                new DialogueChoice {
                    choiceText = "「何も買いません。」 (Nggak beli apa-apa.)",
                    isCorrect = false,
                    npcResponse = "「見るだけでも大歓迎だよ！ゆっくりしていってね。」\n(Miru dake demo daikangei da yo! Yukkuri shite itte ne.)\n\"Lihat-lihat saja juga boleh banget kok! Santai saja ya.\""
                },
                new DialogueChoice {
                    choiceText = "「ここはどこですか？」 (Di sini mana ya?)",
                    isCorrect = false,
                    npcResponse = "「ここは駄菓子屋だよ！迷子になっちゃったのかい？」\n(Koko wa dagashiya da yo! Maigo ni natchatta no kai?)\n\"Ini toko jajanan tradisional! Kamu tersesat ya?\""
                }
            });

        // 3. Sampah Botol Plastik
        ConfigureLocation(spawner, 2, "Sampah", "Sora",
            "「ペットボトルのキャップは外して捨てるんだっけ？」\n(Pettobotoru no kyappu wa hazushite suterun dakke?)\n\"Tutup botol plastik harus dicopot dulu ya?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「川に捨てましょう。」 (Buang ke sungai saja.)",
                    isCorrect = false,
                    npcResponse = "「コラコラ！川にゴミを捨てちゃ絶対にダメだよ！」\n(Kora kora! Kawa ni gomi o sutecha zettai ni dame da yo!)\n\"Eits jangan! Nggak boleh buang sampah ke sungai ya!\""
                },
                new DialogueChoice {
                    choiceText = "「はい、キャップは別ですよ。」 (Iya, tutupnya dipisah.)",
                    isCorrect = true,
                    npcResponse = "「やっぱりそうだよね！分別ルールを教えてくれて助かったよ。」\n(Yappari sou da yo ne! Bunbetsu ruuru o oshiete kurete tasukatta yo.)\n\"Tuh kan bener! Makasih ya udah ngingetin aturan pilah sampah.\""
                },
                new DialogueChoice {
                    choiceText = "「水が冷たいですね。」 (Airnya dingin ya.)",
                    isCorrect = false,
                    npcResponse = "「えっ？水の話…？今はゴミの分別の話だよ〜！」\n(E'? Mizu no hanashi...? Ima wa gomi no bunbetsu no hanashi da yo~)\n\"Eh? Air dingin...? Kan kita lagi ngomongin sampah plastik〜!\""
                }
            });

        // 4. Bunga di Depan Rumah
        ConfigureLocation(spawner, 3, "Bunga", "Kaoru",
            "「このお花、綺麗に咲いたでしょう？」\n(Kono ohana, kirei ni saita deshou?)\n\"Bunga ini mekar dengan indah, kan?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「お花を食べます。」 (Saya makan bunganya.)",
                    isCorrect = false,
                    npcResponse = "「食べちゃダメだよ！観賞用のお花なんだから〜！」\n(Tabecha dame da yo! Kanshou-you no ohana nan dakara~!)\n\"Jangan dimakan dong! Ini kan bunga hiasan〜!\""
                },
                new DialogueChoice {
                    choiceText = "「ちょっと邪魔ですね。」 (Agak sempit jalannya.)",
                    isCorrect = false,
                    npcResponse = "「うーん、道にはみ出さないように少し片付けようかな…」\n(Uun, michi ni hamidasanai you ni sukoshi katazukeyou kana...)\n\"Hmm, mungkin nanti aku rapikan sedikit biar nggak halangi jalan...\""
                },
                new DialogueChoice {
                    choiceText = "「手入れが素晴らしいですね！」 (Perawatannya luar biasa!)",
                    isCorrect = true,
                    npcResponse = "「ありがとう！毎朝水をあげて大切に育てているんだよ。」\n(Arigatou! Maiasa mizu o agete taisetsu ni sodatete iru n da yo.)\n\"Makasih! Tiap pagi kusiram dan kurawat dengan baik lho.\""
                }
            });

        // 5. Parkir Mobil
        ConfigureLocation(spawner, 4, "Mobil", "Makoto",
            "「ここに少し車を停めても平気かな？」\n(Koko ni sukoshi kuruma o tometemo heiki kana?)\n\"Parkir mobil sebentar di sini boleh nggak ya?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「寝ても大丈夫です。」 (Tidur juga boleh.)",
                    isCorrect = false,
                    npcResponse = "「ここで寝たら怒られちゃうよ！車を停める場所を探してるんだ。」\n(Koko de netara okorarechau yo! Kuruma o tomeru basho o sagashiteru n da.)\n\"Kalau tidur di sini bisa dimarahin dong haha! Aku nyari parkiran nih.\""
                },
                new DialogueChoice {
                    choiceText = "「駐車禁止ですよ。」 (Dilarang parkir di sini.)",
                    isCorrect = true,
                    npcResponse = "「危ない危ない、標識を見落としてたよ！コインパーキング探すね。」\n(Abunai abunai, hyoushiki o miotoshiteta yo! Koin paakingu sagasu ne.)\n\"Wah hampir saja, nggak liat rambunya! Aku cari parkiran koin aja.\""
                },
                new DialogueChoice {
                    choiceText = "「私の車じゃないです。」 (Bukan mobil saya.)",
                    isCorrect = false,
                    npcResponse = "「あ、うん…私の車なんだけど、ここに停めていいか聞きたくてね。」\n(A, un... Watashi no kuruma nan dakedo, koko ni tomete ii ka kikitakute ne.)\n\"Oh, iya sih... Ini mobilku, cuma mau nanya boleh parkir nggak hehe.\""
                }
            });

        // 6. Zebracross / Jalan Santai
        ConfigureLocation(spawner, 5, "Zebracross", "Haru",
            "「いい天気ですね。どちらへお出かけですか？」\n(Ii tenki desu ne. Dochira e odekake desu ka?)\n\"Cuaca bagus ya. Mau pergi ke mana?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「昨日雨が降りました。」 (Kemarin hujan.)",
                    isCorrect = false,
                    npcResponse = "「昨日は雨だったね！でも今日は晴れてよかったよ〜。」\n(Kinou wa ame datta ne! Demo kyou wa harete yokatta yo~.)\n\"Kemarin hujan ya! Tapi untung hari ini cerah banget〜.\""
                },
                new DialogueChoice {
                    choiceText = "「傘を買いました。」 (Saya beli payung.)",
                    isCorrect = false,
                    npcResponse = "「晴れてるから傘はいらないかもね！日傘なら使えるかな？」\n(Hareteru kara kasa wa iranai kamo ne! Higasa nara tsukaeru kana?)\n\"Lagi cerah gini payung biasa nggak perlu kali ya! Mungkin payung teduh?\""
                },
                new DialogueChoice {
                    choiceText = "「近くを散歩中です。」 (Lagi jalan-jalan santai.)",
                    isCorrect = true,
                    npcResponse = "「散歩日和で気持ちいいよね！街の景色を楽しんでいってね。」\n(Sanpo biyori de kimochi ii yo ne! Machi no keshiki o tanoshinde itte ne.)\n\"Enak banget ya jalan-jalan pas cuaca cerah! Selamat menikmati suasana kota ya.\""
                }
            });

        // 7. Vending Machine 2 (Minuman Panas & Dingin)
        ConfigureLocation(spawner, 6, "VendingMachine2", "Hikaru",
            "「喉が渇いたな。どっちがいいと思う？」\n(Nodo ga kawaita na. Docchi ga ii to omou?)\n\"Tenggorokan haus nih. Enakan yang mana ya?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「冷たいお茶がおすすめです！」 (Teh dingin rekomended!)",
                    isCorrect = true,
                    npcResponse = "「いいね！冷たい緑茶でスッキリしよう！ナイスなチョイスだね。」\n(Ii ne! Tsumetai ryokucha de sukkiri shiyou! Naisu na choisu da ne.)\n\"Mantap! Minum teh hijau dingin biar segar! Pilihan yang bagus.\""
                },
                new DialogueChoice {
                    choiceText = "「お金を拾いました。」 (Saya nemu uang.)",
                    isCorrect = false,
                    npcResponse = "「えっ、交番に届けたほうがいいよ！ジュースどころじゃないよ〜！」\n(E', kouban ni todoketa hou ga ii yo! Juusu dokoro ja nai yo~!)\n\"Eh, buruan lapor ke pos polisi (kouban) dong! Bukan saatnya beli jus〜!\""
                },
                new DialogueChoice {
                    choiceText = "「お腹いっぱいです。」 (Saya kenyang.)",
                    isCorrect = false,
                    npcResponse = "「お腹いっぱいでも飲み物は別腹だよ〜！何にしようかな。」\n(Onaka ippai demo nomimono wa betsubara da yo~! Nani ni shiyou kana.)\n\"Kenyang juga kalau minuman masih muat kok〜! Beli apa ya enaknya.\""
                }
            });

        // 8. Toko Tutup
        ConfigureLocation(spawner, 7, "TokoTutup", "Yuuki",
            "「シャッターが閉まってる…今日休みかな？」\n(Shattaa ga shimatteru... Kyou yasumi kana?)\n\"Pintunya tertutup... Hari ini libur ya?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「鍵を壊しましょう。」 (Rusak saja kuncinya.)",
                    isCorrect = false,
                    npcResponse = "「捕まっちゃうよ！！冗談でもそんなこと言っちゃダメ〜！」\n(Tsukamatchau yo!! Joudan demo sonna koto itchya dame~)\n\"Bisa ditangkap polisi!! Bercanda pun nggak boleh gitu dong〜!\""
                },
                new DialogueChoice {
                    choiceText = "「靴を脱いでください。」 (Lepas sepatu Anda.)",
                    isCorrect = false,
                    npcResponse = "「外で靴を脱いだら靴下が汚れちゃうよ！お店にも入れないし…」\n(Soto de kutsu o nuidara kutsushita ga yogorechau yo! Omise ni mo hairenai shi...)\n\"Kalau copot sepatu di jalan ya kotor kaos kakinya! Lagian tokonya tutup...\""
                },
                new DialogueChoice {
                    choiceText = "「水曜定休と書いてありますよ。」 (Tertulis 'Rabu Libur' lho.)",
                    isCorrect = true,
                    npcResponse = "「本当だ！看板をちゃんと見てくれてありがとう。明日また来るよ！」\n(Hontou da! Kanban o chanto mite kurete arigatou. Ashita mata kuru yo!)\n\"Bener juga! Makasih udah cermat baca papannya. Besok aku balik lagi deh!\""
                }
            });

        // 9. Pipa & Gas
        ConfigureLocation(spawner, 8, "PipadanGas", "Ibuki",
            "「壁にガスメーターがたくさん並んでいて面白いね。」\n(Kabe ni gasu meetaa ga takusan narande ite omoshiroi ne.)\n\"Banyak meteran gas berjejer di dinding, unik ya.\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「パイプを引っ張りましょう。」 (Tarik saja pipanya.)",
                    isCorrect = false,
                    npcResponse = "「絶対ダメ！ガス漏れしたら大爆発しちゃうよ〜！」\n(Zettai dame! Gasu more shitara daibakuhatsu shichau yo~!)\n\"Jangan sekali-kali! Kalau gasnya bocor bisa meledak〜!\""
                },
                new DialogueChoice {
                    choiceText = "「点検しやすいからですよ。」 (Biar mudah dicek petugas.)",
                    isCorrect = true,
                    npcResponse = "「なるほど！検針員さんが家に入らなくても確認できるからか！物知りだね。」\n(Naruhodo! Kenshin'in-san ga ie ni hairanakutemo kakunin dekiru kara ka! Monoshiri da ne.)\n\"Ooh begitu! Jadi petugas nggak perlu masuk rumah ya! Pintar banget kamu.\""
                },
                new DialogueChoice {
                    choiceText = "「家にガスがないです。」 (Di rumah nggak ada gas.)",
                    isCorrect = false,
                    npcResponse = "「へぇ、オール電化の家なのかな？最近は増えてるらしいね。」\n(Hee, ooru denka no ie na no kana? Saikin wa fueteru rashii ne.)\n\"Wah, rumahmu serba listrik ya? Zaman sekarang makin banyak sih.\""
                }
            });

        // 10. Sampah Daur Ulang
        ConfigureLocation(spawner, 9, "Sampah2", "Chihiro",
            "「青いゴミ箱があるね。今日は資源ごみの日？」\n(Aoi gomibako ga aru ne. Kyou wa shigen-gomi no hi?)\n\"Ada tempat sampah biru. Hari ini daur ulang?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「道に捨てましょう。」 (Buang di jalan saja.)",
                    isCorrect = false,
                    npcResponse = "「ポイ捨ては絶対にダメだよ！日本の街は綺麗に保たなきゃね。」\n(Poisute wa zettai ni dame da yo! Nihon no machi wa kirei ni tamotanakya ne.)\n\"Buang sampah sembarangan dilarang keras! Kota harus tetap bersih.\""
                },
                new DialogueChoice {
                    choiceText = "「青色は嫌いです。」 (Saya nggak suka biru.)",
                    isCorrect = false,
                    npcResponse = "「色の好みの話じゃないよ〜！ゴミ箱の色で分別が決まってるんだよ。」\n(Iro no konomi no hanashi ja nai yo~! Gomibako no iro de bunbetsu ga kimatteru n da yo.)\n\"Bukan soal suka warnanya〜! Warna tong sampah itu tanda jenis sampahnya.\""
                },
                new DialogueChoice {
                    choiceText = "「はい、リサイクルの日ですよ。」 (Iya, hari daur ulang.)",
                    isCorrect = true,
                    npcResponse = "「よかった！缶やペットボトルをしっかり分別して捨ててくるね！」\n(Yokatta! Kan ya pettobotoru o shikkari bunbetsu shite sutete kuru ne!)\n\"Syukurlah! Aku buang kaleng dan botol plastik sesuai tempatnya deh!\""
                }
            });

        // 11. Lampu Merah
        ConfigureLocation(spawner, 10, "LampuMerah", "Rin",
            "「車は通ってないけど、青になるまで待とうね。」\n(Kuruma wa toottenai kedo, ao ni naru made matou ne.)\n\"Mobil nggak ada sih, tapi tunggu lampu hijau ya.\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「車が好きです。」 (Saya suka mobil.)",
                    isCorrect = false,
                    npcResponse = "「私も車は好きだけど、今は交通ルールの話だよ〜！」\n(Watashi mo kuruma wa suki dako, ima wa koutsuu ruuru no hanashi da yo~)\n\"Aku juga suka mobil sih, tapi sekarang lagi bahas aturan jalan raya〜!\""
                },
                new DialogueChoice {
                    choiceText = "「そうですね、待ちましょう！」 (Iya, ayo kita tunggu!)",
                    isCorrect = true,
                    npcResponse = "「うん！誰も見ていなくてもマナーを守るのが大事だよね！」\n(Un! Dare mo mite inakutemo manaa o mamoru no ga daiji da yo ne!)\n\"Yup! Tetap tertib meski sepi itu baru tata krama yang baik!\""
                },
                new DialogueChoice {
                    choiceText = "「急いで渡っちゃおう！」 (Ayo buru-buru nyebrang!)",
                    isCorrect = false,
                    npcResponse = "「ダメダメ危ないよ！急に車が曲がってくるかもしれないからね。」\n(Dame dame abunai yo! Kyuu ni kuruma ga magatte kuru kamoshirenai kara ne.)\n\"Eits bahaya! Siapa tahu tiba-tiba ada mobil belok kencang.\""
                }
            });

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        EditorSceneManager.SaveScene(spawner.gameObject.scene);
        Debug.Log("<color=green>[Utsuru] SUKSES! Seluruh 11 soal, pilihan, dan balasan NPC berhasil di-inject ke NPCSpawner dan Scene disimpan!</color>");
    }

    private static void ConfigureLocation(NPCSpawner spawner, int index, string locName, string npcName, string question, DialogueChoice[] choices)
    {
        if (spawner.spawnLocations[index] == null)
            spawner.spawnLocations[index] = new ContextualSpawnPoint();

        spawner.spawnLocations[index].locationName = locName;

        NPCData data = new NPCData();
        data.npcName = npcName;
        data.questionText = question;
        data.choices = choices;
        data.scoreReward = 100;
        data.basePitch = 1.0f;

        spawner.spawnLocations[index].locationQuestions = new NPCData[] { data };
    }
}
#endif

