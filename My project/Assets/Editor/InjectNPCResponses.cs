#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class InjectNPCResponses : Editor
{
    [MenuItem("Tools/Utsuru/Update All NPC Dialogue & Responses (English NPC, Romaji Choices)")]
    public static void UpdateAllDialogue()
    {
        UpdateAllDialogue(saveScene: true);
    }

    public static void UpdateAllDialogue(bool saveScene)
    {
        NPCSpawner spawner = Object.FindAnyObjectByType<NPCSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[Utsuru] NPCSpawner tidak ditemukan di scene yang sedang aktif!");
            return;
        }

        Undo.RecordObject(spawner, "Update All NPC Dialogue & Responses");

        NPCSpawner.ApplyCanonicalSpawnPoints(spawner);

        if (spawner.spawnLocations == null || spawner.spawnLocations.Length < 11)
        {
            System.Array.Resize(ref spawner.spawnLocations, 11);
        }

        GameObject spParent = GameObject.Find("SpawnPoints");

        // 1. Vending Machine 1
        ConfigureLocation(spawner, 0, "VendingMachine", "Aoi", "Local Resident", spParent,
            "「自販機にお札が入らないな…小銭持ってる？」",
            "(Jihanki ni osatsu ga hairanai na... Kozeni motteru?)",
            "\"The vending machine won't take bills... Do you have any coins?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「100円玉ならありますよ。」",
                    choiceRomaji = "(Hyaku-en dama nara arimasu yo.)",
                    isCorrect = true,
                    npcResponse = "「ありがとう！助かったよ、これでジュースが買える！」",
                    npcResponseRomaji = "(Arigatou! Tasukatta yo, kore de juusu ga kaeru!)",
                    npcResponseEnglish = "\"Thank you! You saved me, now I can buy juice!\""
                },
                new DialogueChoice {
                    choiceText = "「自販機は嫌いです。」",
                    choiceRomaji = "(Jihanki wa kirai desu.)",
                    isCorrect = false,
                    npcResponse = "「えっ、自販機が嫌いなの…！？便利なのに〜！」",
                    npcResponseRomaji = "(E', jihanki ga kirai na no...!? Benri na no ni~!)",
                    npcResponseEnglish = "\"Eh, you hate vending machines...!? Even though they're so convenient〜!\""
                },
                new DialogueChoice {
                    choiceText = "「お茶が飲みたいです。」",
                    choiceRomaji = "(Ocha ga nomitai desu.)",
                    isCorrect = false,
                    npcResponse = "「あはは、君もお茶が飲みたいんだね！でも小銭がないんだ…」",
                    npcResponseRomaji = "(Ahaha, kimi mo ocha ga nomitai n da ne! Demo kozeni ga nai n da...)",
                    npcResponseEnglish = "\"Hahaha, you want to drink tea too! But I don't have any coins...\""
                }
            });

        // 2. Toko Kelontong
        ConfigureLocation(spawner, 1, "Toko", "Ren", "Shopkeeper", spParent,
            "「いらっしゃい！何を探しているのかい？」",
            "(Irasshai! Nani o sagashite iru no kai?)",
            "\"Welcome! What are you looking for?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「おすすめのお菓子をください。」",
                    choiceRomaji = "(Osusume no okashi o kudasai.)",
                    isCorrect = true,
                    npcResponse = "「毎度あり！このお煎餅が焼きたてでおすすめだよ！」",
                    npcResponseRomaji = "(Maido ari! Kono osenbei ga yakitate de osusume da yo!)",
                    npcResponseEnglish = "\"Thank you! These freshly baked rice crackers are my recommendation!\""
                },
                new DialogueChoice {
                    choiceText = "「何も買いません。」",
                    choiceRomaji = "(Nanimo kaimasen.)",
                    isCorrect = false,
                    npcResponse = "「見るだけでも大歓迎だよ！ゆっくりしていってね。」",
                    npcResponseRomaji = "(Miru dake demo daikangei da yo! Yukkuri shite itte ne.)",
                    npcResponseEnglish = "\"Just looking is very welcome too! Take your time.\""
                },
                new DialogueChoice {
                    choiceText = "「ここはどこですか？」",
                    choiceRomaji = "(Koko wa doko desu ka.)",
                    isCorrect = false,
                    npcResponse = "「ここは駄菓子屋だよ！迷子になっちゃったのかい？」",
                    npcResponseRomaji = "(Koko wa dagashiya da yo! Maigo ni natchatta no kai?)",
                    npcResponseEnglish = "\"This is a traditional candy shop! Did you get lost?\""
                }
            });

        // 3. Sampah Botol Plastik
        ConfigureLocation(spawner, 2, "Sampah", "Sora", "Eco Neighbor", spParent,
            "「ペットボトルのキャップは外して捨てるんだっけ？」",
            "(Pettobotoru no kyappu wa hazushite suterun dakke?)",
            "\"Do we take off the plastic bottle cap before throwing it away?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「川に捨てましょう。」",
                    choiceRomaji = "(Kawa ni sutemashou.)",
                    isCorrect = false,
                    npcResponse = "「コラコラ！川にゴミを捨てちゃ絶対にダメだよ！」",
                    npcResponseRomaji = "(Kora kora! Kawa ni gomi o sutecha zettai ni dame da yo!)",
                    npcResponseEnglish = "\"Hey now! You must never throw trash into the river!\""
                },
                new DialogueChoice {
                    choiceText = "「はい、キャップは別ですよ。」",
                    choiceRomaji = "(Hai, kyappu wa betsu desu yo.)",
                    isCorrect = true,
                    npcResponse = "「やっぱりそうだよね！分別ルールを教えてくれて助かったよ。」",
                    npcResponseRomaji = "(Yappari sou da yo ne! Bunbetsu ruuru o oshiete kurete tasukatta yo.)",
                    npcResponseEnglish = "\"I knew it! Thanks for reminding me of the sorting rules.\""
                },
                new DialogueChoice {
                    choiceText = "「水が冷たいですね。」",
                    choiceRomaji = "(Mizu ga tsumetai desu ne.)",
                    isCorrect = false,
                    npcResponse = "「えっ？水の話…？今はゴミの分別の話だよ〜！」",
                    npcResponseRomaji = "(E'? Mizu no hanashi...? Ima wa gomi no bunbetsu no hanashi da yo~)",
                    npcResponseEnglish = "\"Eh? Cold water...? We're talking about trash sorting right now〜!\""
                }
            });

        // 4. Bunga di Depan Rumah
        ConfigureLocation(spawner, 3, "Bunga", "Kaoru", "Garden Lover", spParent,
            "「このお花、綺麗に咲いたでしょう？」",
            "(Kono ohana, kirei ni saita deshou?)",
            "\"These flowers bloomed beautifully, don't you think?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「お花を食べます。」",
                    choiceRomaji = "(Ohana o tabemasu.)",
                    isCorrect = false,
                    npcResponse = "「食べちゃダメだよ！観賞用のお花なんだから〜！」",
                    npcResponseRomaji = "(Tabecha dame da yo! Kanshou-you no ohana nan dakara~!)",
                    npcResponseEnglish = "\"Don't eat them! They're decorative flowers〜!\""
                },
                new DialogueChoice {
                    choiceText = "「ちょっと邪魔ですね。」",
                    choiceRomaji = "(Chotto jama desu ne.)",
                    isCorrect = false,
                    npcResponse = "「うーん、道にはみ出さないように少し片付けようかな…」",
                    npcResponseRomaji = "(Uun, michi ni hamidasanai you ni sukoshi katazukeyou kana...)",
                    npcResponseEnglish = "\"Hmm, maybe I should tidy them up so they don't block the path...\""
                },
                new DialogueChoice {
                    choiceText = "「手入れが素晴らしいですね！」",
                    choiceRomaji = "(Teire ga subarashii desu ne!)",
                    isCorrect = true,
                    npcResponse = "「ありがとう！毎朝水をあげて大切に育てているんだよ。」",
                    npcResponseRomaji = "(Arigatou! Maiasa mizu o agete taisetsu ni sodatete iru n da yo.)",
                    npcResponseEnglish = "\"Thank you! I water and take good care of them every morning.\""
                }
            });

        // 5. Parkir Mobil
        ConfigureLocation(spawner, 4, "Mobil", "Makoto", "Driver", spParent,
            "「ここに少し車を止めても平気かな？」",
            "(Koko ni sukoshi kuruma o tometemo heiki kana?)",
            "\"Is it okay if I park my car here for a bit?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「駐車場にとめましょう。」",
                    choiceRomaji = "(Chuushajou ni tomemashou.)",
                    isCorrect = true,
                    npcResponse = "「そうだよね、他の車の邪魔になっちゃうもんね！移動するよ。」",
                    npcResponseRomaji = "(Sou da yo ne, hoka no kuruma no jama ni natchau mon ne! Idou suru yo.)",
                    npcResponseEnglish = "\"You're right, it would get in the way of other cars! I'll move it.\""
                },
                new DialogueChoice {
                    choiceText = "「車を蹴ります。」",
                    choiceRomaji = "(Kuruma o kerimasu.)",
                    isCorrect = false,
                    npcResponse = "「ひええっ、車を蹴らないで！すぐ移動させるから〜！」",
                    npcResponseRomaji = "(Hieee, kuruma o keranaide! Sugu idou saseru kara~!)",
                    npcResponseEnglish = "\"Eeeek, don't kick the car! I'll move it right away〜!\""
                },
                new DialogueChoice {
                    choiceText = "「車が大きいです。」",
                    choiceRomaji = "(Kuruma ga ookii desu.)",
                    isCorrect = false,
                    npcResponse = "「あはは、確かにちょっと大きいかも！だからこそちゃんと駐車場に入れなきゃね。」",
                    npcResponseRomaji = "(Ahaha, tashika ni chotto ookii kamo! Dakara koso chanto chuushajou ni irenakya ne.)",
                    npcResponseEnglish = "\"Hahaha, it is a bit big indeed! All the more reason to park properly in a parking lot.\""
                }
            });

        // 6. Zebracross
        ConfigureLocation(spawner, 5, "Zebracross", "Haru", "Pedestrian", spParent,
            "「いい天気ですね。どちらへお出かけですか？」",
            "(Ii tenki desu ne. Dochira e odekake desu ka?)",
            "\"Nice weather, isn't it? Where are you headed?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「散歩をしているところです。」",
                    choiceRomaji = "(Sanpo o shite iru tokoro desu.)",
                    isCorrect = true,
                    npcResponse = "「いいですね！風が気持ちいいから、お散歩には最高の日ですよ。」",
                    npcResponseRomaji = "(Ii desu ne! Kaze ga kimochi ii kara, osanpo ni wa saikou no hi desu yo.)",
                    npcResponseEnglish = "\"Sounds nice! The breeze is pleasant, it's a great day for a walk.\""
                },
                new DialogueChoice {
                    choiceText = "「天気が悪いです。」",
                    choiceRomaji = "(Tenki ga warui desu.)",
                    isCorrect = false,
                    npcResponse = "「えっ！？こんなに晴れているのに…？不思議な人ですね〜！」",
                    npcResponseRomaji = "(E'!? Konna ni harete iru no ni...? Fushigi na hito desu ne~!)",
                    npcResponseEnglish = "\"Eh!? Even though it's so sunny...? You're an interesting person〜!\""
                },
                new DialogueChoice {
                    choiceText = "「家に帰りたいです。」",
                    choiceRomaji = "(Ie ni kaeritai desu.)",
                    isCorrect = false,
                    npcResponse = "「あらら、疲れちゃったのかな？無理せずお家で休んでね。」",
                    npcResponseRomaji = "(Arara, tsukarechatta no kana? Muri sezu ouchi de yasunde ne.)",
                    npcResponseEnglish = "\"Oh dear, are you tired? Don't push yourself and rest at home.\""
                }
            });

        // 7. Vending Machine 2
        ConfigureLocation(spawner, 6, "VendingMachine2", "Hikaru", "Passerby", spParent,
            "「喉が渇いたな。どっちがいいと思う？」",
            "(Nodo ga kawaita na. Docchi ga ii to omou?)",
            "\"I'm thirsty. Which one do you think is good?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「お茶がおすすめですよ。」",
                    choiceRomaji = "(Ocha ga osusume desu yo.)",
                    isCorrect = true,
                    npcResponse = "「おっ、緑茶いいね！スッキリして喉の渇きが潤いそう！」",
                    npcResponseRomaji = "(O', ryokucha ii ne! Sukkiri shite nodo no kawaki ga uruoisou!)",
                    npcResponseEnglish = "\"Oh, green tea is great! Refreshing and it will quench my thirst!\""
                },
                new DialogueChoice {
                    choiceText = "「何も飲まないでください。」",
                    choiceRomaji = "(Nanimo nomanaide kudasai.)",
                    isCorrect = false,
                    npcResponse = "「えぇーっ！？熱中症になっちゃうよ！水分補給は大事だよ〜！」",
                    npcResponseRomaji = "(Eee-!? Netchuushou ni natchau yo! Suibun hokyuu wa daiji da yo~!)",
                    npcResponseEnglish = "\"Eeeh!? You'll get heatstroke! Staying hydrated is important〜!\""
                },
                new DialogueChoice {
                    choiceText = "「水が好きです。」",
                    choiceRomaji = "(Mizu ga suki desu.)",
                    isCorrect = false,
                    npcResponse = "「水も健康的でいいね！でも今日はちょっと味があるものが飲みたいな〜。」",
                    npcResponseRomaji = "(Mizu mo kenkou-teki de ii ne! Demo kyou wa chotto aji ga aru mono ga nomitai na~.)",
                    npcResponseEnglish = "\"Water is healthy too! But today I feel like drinking something with flavor〜.\""
                }
            });

        // 8. Toko Tutup
        ConfigureLocation(spawner, 7, "TokoTutup", "Yuuki", "Curious Shopper", spParent,
            "「シャッターが閉まってる…今日休みかな？」",
            "(Shattaa ga shimatteru... Kyou yasumi kana?)",
            "\"The shutters are closed... Is it closed today?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「ドアを壊しましょう。」",
                    choiceRomaji = "(Doa o kowashimashou.)",
                    isCorrect = false,
                    npcResponse = "「警察呼ばれちゃうよ！絶対に壊しちゃダメだよ〜！」",
                    npcResponseRomaji = "(Keisatsu yobarechau yo! Zettai ni kowashicha dame da yo~!)",
                    npcResponseEnglish = "\"The police will be called! You must never break it〜!\""
                },
                new DialogueChoice {
                    choiceText = "「水曜日が定休日みたいですよ。」",
                    choiceRomaji = "(Suiyoubi ga teikyuubi mitai desu yo.)",
                    isCorrect = true,
                    npcResponse = "「あっ、本当だ！看板に書いてあるね。教えてくれてありがとう！」",
                    npcResponseRomaji = "(A', hontou da! Kanban ni kaite aru ne. Oshiete kurete arigatou!)",
                    npcResponseEnglish = "\"Oh, you're right! It's written on the sign. Thank you for letting me know!\""
                },
                new DialogueChoice {
                    choiceText = "「お腹が空きました。」",
                    choiceRomaji = "(Onaka ga sukimashita.)",
                    isCorrect = false,
                    npcResponse = "「私もお腹空いてたのに閉まってて残念…他のお店を探そう！」",
                    npcResponseRomaji = "(Watashi mo onaka suiteta no ni shimattete zannen... Hoka no omise o sagasou!)",
                    npcResponseEnglish = "\"I was hungry too, such a shame it's closed... Let's look for another shop!\""
                }
            });

        // 9. Pipa dan Gas
        ConfigureLocation(spawner, 8, "PipadanGas", "Ibuki", "Local Resident", spParent,
            "「壁にガスメーターがたくさん並んでいて面白いね。」",
            "(Kabe ni gasu meetaa ga takusan narande ite omoshiroi ne.)",
            "\"There are lots of gas meters lined up on the wall, interesting isn't it.\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「触ると危ないですよ。」",
                    choiceRomaji = "(Sawaru to abunai desu yo.)",
                    isCorrect = true,
                    npcResponse = "「そうだね！ガス設備だから、見るだけにしておこうね。」",
                    npcResponseRomaji = "(Sou da ne! Gasu setsubi dakara, miru dake ni shite okou ne.)",
                    npcResponseEnglish = "\"You're right! Since it's gas equipment, let's just look without touching.\""
                },
                new DialogueChoice {
                    choiceText = "「ボタンを押しましょう！」",
                    choiceRomaji = "(Botan o oshimashou.)",
                    isCorrect = false,
                    npcResponse = "「ダメダメ！勝手に触ったら近所迷惑になっちゃうよ！」",
                    npcResponseRomaji = "(Dame dame! Katte ni sawattara kinjo meiwaku ni natchau yo!)",
                    npcResponseEnglish = "\"No no! Touching it without permission will cause trouble for the neighbors!\""
                },
                new DialogueChoice {
                    choiceText = "「家にガスがないです。」",
                    choiceRomaji = "(Ie ni gasu ga nai desu.)",
                    isCorrect = false,
                    npcResponse = "「へぇ、オール電化の家なのかな？最近は増えてるらしいね。」",
                    npcResponseRomaji = "(Hee, ooru denka no ie na no kana? Saikin wa fueteru rashii ne.)",
                    npcResponseEnglish = "\"Oh, is your home all-electric? I hear that's becoming more common lately.\""
                }
            });

        // 10. Sampah Daur Ulang
        ConfigureLocation(spawner, 9, "Sampah2", "Chihiro", "Eco Volunteer", spParent,
            "「青いゴミ箱があるね。今日は資源ごみの日？」",
            "(Aoi gomibako ga aru ne. Kyou wa shigen-gomi no hi?)",
            "\"There's a blue trash bin. Is today recyclable waste day?\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「道に捨てましょう。」",
                    choiceRomaji = "(Michi ni sutemashou.)",
                    isCorrect = false,
                    npcResponse = "「ポイ捨ては絶対にダメだよ！日本の街は綺麗に保たなきゃね。」",
                    npcResponseRomaji = "(Poisute wa zettai ni dame da yo! Nihon no machi wa kirei ni tamotanakya ne.)",
                    npcResponseEnglish = "\"Littering is strictly forbidden! We must keep Japanese streets clean.\""
                },
                new DialogueChoice {
                    choiceText = "「青色は嫌いです。」",
                    choiceRomaji = "(Aoiro wa kirai desu.)",
                    isCorrect = false,
                    npcResponse = "「色の好みの話じゃないよ〜！ゴミ箱の色で分別が決まってるんだよ。」",
                    npcResponseRomaji = "(Iro no konomi no hanashi ja nai yo~! Gomibako no iro de bunbetsu ga kimatteru n da yo.)",
                    npcResponseEnglish = "\"It's not about what color you like〜! The bin color indicates the type of waste.\""
                },
                new DialogueChoice {
                    choiceText = "「はい、リサイクルの日ですよ。」",
                    choiceRomaji = "(Hai, risaikuru no hi desu yo.)",
                    isCorrect = true,
                    npcResponse = "「よかった！缶やペットボトルをしっかり分別して捨ててくるね！」",
                    npcResponseRomaji = "(Yokatta! Kan ya pettobotoru o shikkari bunbetsu shite sutete kuru ne!)",
                    npcResponseEnglish = "\"Great! I'll sort the cans and plastic bottles properly and throw them away!\""
                }
            });

        // 11. Lampu Merah
        ConfigureLocation(spawner, 10, "LampuMerah", "Rin", "Pedestrian", spParent,
            "「車は通ってないけど、青になるまで待とうね。」",
            "(Kuruma wa toottenai kedo, ao ni naru made matou ne.)",
            "\"There are no cars passing, but let's wait until the light turns green.\"",
            new DialogueChoice[]
            {
                new DialogueChoice {
                    choiceText = "「車が好きです。」",
                    choiceRomaji = "(Kuruma ga suki desu.)",
                    isCorrect = false,
                    npcResponse = "「私も車は好きだけど、今は交通ルールの話だよ〜！」",
                    npcResponseRomaji = "(Watashi mo kuruma wa suki dakedo, ima wa koutsuu ruuru no hanashi da yo~)",
                    npcResponseEnglish = "\"I like cars too, but right now we're talking about traffic rules〜!\""
                },
                new DialogueChoice {
                    choiceText = "「そうですね、待ちましょう！」",
                    choiceRomaji = "(Sou desu ne, machimashou!)",
                    isCorrect = true,
                    npcResponse = "「うん！誰も見ていなくてもマナーを守るのが大事だよね！」",
                    npcResponseRomaji = "(Un! Dare mo mite inakutemo manaa o mamoru no ga daiji da yo ne!)",
                    npcResponseEnglish = "\"Yup! Following manners even when no one is watching is what's truly important!\""
                },
                new DialogueChoice {
                    choiceText = "「急いで渡っちゃおう！」",
                    choiceRomaji = "(Isoide watatchaou!)",
                    isCorrect = false,
                    npcResponse = "「ダメダメ危ないよ！急に車が曲がってくるかもしれないからね。」",
                    npcResponseRomaji = "(Dame dame abunai yo! Kyuu ni kuruma ga magatte kuru kamoshirenai kara ne.)",
                    npcResponseEnglish = "\"No no, that's dangerous! A car might suddenly turn the corner.\""
                }
            });

        // Pasang 14 Avatar Karakter BoZo Otomatis ke NPCSpawner
        string[] avatarNames = new string[] {
            "Ben", "Chloe", "Dave", "Galvon", "Javi", "Jennifer", "John",
            "Mary", "Sally", "Tristian", "Yuki", "Zulle", "1DefaultFemale", "1DefaultMale"
        };
        List<Sprite> avatars = new List<Sprite>();
        foreach (var avName in avatarNames)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/BoZo_StylizedModularCharacters/CustomCharacters/Icons/Base/{avName}.png");
            if (s != null) avatars.Add(s);
        }
        SetPrivateField(spawner, "characterAvatars", avatars.ToArray());

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        if (saveScene)
        {
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
        }
        Debug.Log("<color=green>[Utsuru] SUKSES! Soal telah dipisah rapi ke field Question Japanese, Romaji, dan English di Inspector!</color>");
    }

    private static void ConfigureLocation(NPCSpawner spawner, int index, string locName, string npcName, string roleTag, GameObject spParent, string jpQ, string rmQ, string enQ, DialogueChoice[] choices)
    {
        if (spawner.spawnLocations[index] == null)
            spawner.spawnLocations[index] = new ContextualSpawnPoint();

        spawner.spawnLocations[index].locationName = locName;

        if (spawner.spawnLocations[index].pointTransform == null && spParent != null)
        {
            Transform pt = spParent.transform.Find($"Point_{index + 1}");
            if (pt != null)
                spawner.spawnLocations[index].pointTransform = pt;
        }

        NPCData data = new NPCData();
        data.npcName = npcName;
        data.roleTag = roleTag;
        data.jlptTag = "Formality: Casual & Friendly • JLPT N5";
        data.questionJapanese = jpQ;
        data.questionRomaji = rmQ;
        data.questionEnglish = enQ;
        data.questionText = "";
        data.choices = choices;
        data.scoreReward = 100;
        data.basePitch = 1.0f;

        spawner.spawnLocations[index].locationQuestions = new NPCData[] { data };
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
#endif