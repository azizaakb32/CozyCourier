using System.Collections.Generic;

namespace CozyCourier
{
    // ===================== Turlar =====================
    public class RestDef
    {
        public string id; public string[] name; public string color; public string[][] dishes; public int lvl;
    }

    public class District
    {
        public string id; public string[] name; public int lvl; public float mult;
        public int numMul, numAdd;
        public Dictionary<string, RestDef> rests;
        public string[] parks; public Dictionary<string, string> marks; public string[] extra;
        public int[,] lights, gai;
        public string[] walls; public float[] heights; public float trees;
        public Shot[] intro;
        public int Num(int n) => n * numMul + numAdd;
    }

    public class Shot
    {
        public string bg;            // office, street, kitchen, door, yard
        public string[] chars;
        public string[][] lines;     // {kim, uz, ru, en}
        public int title;            // 0 = yo'q, 1 = bob sarlavhasi, 2 = maxsus sarlavha
        public string[] tTitle, tSub;
        public char plate = 'R';
        public bool closed;
    }

    public class StoryOrder { public string rest, house; public string[] dish; }

    public class Chapter
    {
        public string[] title, sub;
        public Shot[] intro, mid, outro;
        public StoryOrder story;
    }

    public class CharDef
    {
        public string[] name; public string col, coat, pants, skin, hair, hat, beard, stripe, scarf;
        public float wide = 1f, s = 1f;
        public bool glasses, cane, apron, dress, bun, badge, vest;
    }

    public class Upg { public string id; public string[] name, desc; public int[] prices; public string col; }
    public class LookItem { public string id; public string[] name; public string c0, c1; public int price, lvl; public bool ikat, vip; }
    public class Reno { public string id; public string[] name, desc; public int price; public string[][] lines; }

    // ===================== Ma'lumotlar =====================
    public static class Data
    {
        public const string HOME_R = "5,0", HOME_S = "1,3";

        static string[] T(string uz, string ru, string en) => new[] { uz, ru, en };
        static string[] Ln(string who, string uz, string ru, string en) => new[] { who, uz, ru, en };
        static Shot S(string bg, string[] chars, params string[][] lines) => new Shot { bg = bg, chars = chars, lines = lines };
        static Shot SP(string bg, char plate, bool closed, string[] chars, params string[][] lines) => new Shot { bg = bg, plate = plate, closed = closed, chars = chars, lines = lines };
        static Shot ChTitle() => new Shot { title = 1, lines = new string[0][] };
        static RestDef R(string id, string[] name, string col, params string[][] dishes) => new RestDef { id = id, name = name, color = col, dishes = dishes };

        // ---------- Personajlar ----------
        public static readonly Dictionary<string, CharDef> Chars = new Dictionary<string, CharDef>
        {
            {"N", new CharDef{ name=T("Hikoyachi","Рассказчик","Narrator"), col="#94a9ac" }},
            {"jasur", new CharDef{ name=T("Jasur","Жасур","Jasur"), col="#3fc2b2", coat="#2f9e92", pants="#34424c", skin="#e0b48c", hair="#2b2018", hat="cap" }},
            {"karim", new CharDef{ name=T("Karim aka","Карим-ака","Karim aka"), col="#f4a259", coat="#7a4e32", stripe="#b07a4e", pants="#3a3330", skin="#d9a87e", hat="doppi", beard="#8f8f8f", wide=1.18f, s=1.02f }},
            {"rustam", new CharDef{ name=T("Rustam bobo","Дедушка Рустам","Grandpa Rustam"), col="#c3a6e8", coat="#5c6b7a", pants="#3b3f45", skin="#d6a684", hat="doppi", beard="#f0efe9", glasses=true, cane=true, s=.95f }},
            {"malika", new CharDef{ name=T("Malika","Малика","Malika"), col="#ee9583", coat="#c0506a", pants="#c0506a", skin="#e8bf98", hat="scarf", scarf="#e9c46a", apron=true, dress=true, s=.96f }},
            {"sardor", new CharDef{ name=T("Sardor","Сардор","Sardor"), col="#7ec8e3", coat="#4f97c9", pants="#2e3d55", skin="#e6b98f", hair="#2b2018", s=.66f }},
            {"dilnoza", new CharDef{ name=T("Dilnoza","Дильноза","Dilnoza"), col="#8cc4ff", coat="#eef3f5", pants="#5a8fb8", skin="#e6bd96", hair="#3a2418", bun=true, badge=true, s=.95f }},
            {"bahodir", new CharDef{ name=T("Bahodir usta","Мастер Баходир","Master Bahodir"), col="#e9c46a", coat="#f1ece2", pants="#4a3f36", skin="#d39a70", hat="doppi", beard="#3a2a20", apron=true, wide=1.15f }},
            {"anvar", new CharDef{ name=T("Inspektor Anvar","Инспектор Анвар","Officer Anvar"), col="#c8e04a", coat="#2f4f7f", pants="#24324a", skin="#d9a87e", hat="police", vest=true, s=1.02f }},
        };

        // ---------- Qo'shimcha restoranlar (daraja bilan ochiladi) ----------
        public static readonly RestDef[] Extra =
        {
            new RestDef{ id="choyxona", lvl=2, name=T("Choyxona","Чайхана","Teahouse"), color="#2f9e92", dishes=new[]{ T("Ko'k choy va non","Зелёный чай с лепёшкой","Green tea and bread"), T("Qaymoq va patir","Каймак с патыром","Cream and patyr bread"), T("Chuchvara","Чучвара","Chuchvara dumplings") } },
            new RestDef{ id="shashlik", lvl=4, name=T("Shashlikxona","Шашлычная","Kebab House"), color="#b5403a", dishes=new[]{ T("Jigar kabob","Шашлык из печени","Liver kebab"), T("Qiyma shashlik","Люля из фарша","Minced kebab"), T("Lula kabob","Люля-кебаб","Lula kebab") } },
        };

        // ---------- Mahallalar ----------
        public static readonly Dictionary<string, District> Districts = new Dictionary<string, District>
        {
            {"markaz", new District{
                id="markaz", name=T("Markaz","Центр","Central"), lvl=1, mult=1f, numMul=3, numAdd=7,
                rests=new Dictionary<string,RestDef>{
                    {"1,0", R("osh", T("Osh Markaz","Ош Марказ","Osh Markaz"), "#d9694f", T("To'y oshi","Свадебный плов","Wedding plov"), T("Choyxona palovi","Чайханский плов","Teahouse plov"), T("Osh va salat","Плов с салатом","Plov and salad"))},
                    {"4,1", R("somsa", T("Somsaxona","Самсахана","Somsa House"), "#d7a042", T("Tandir somsa x4","Тандыр-самса x4","Tandoor somsa x4"), T("Go'shtli somsa","Самса с мясом","Meat somsa"), T("Kartoshkali somsa","Самса с картошкой","Potato somsa"))},
                    {"0,2", R("lagmon", T("Lag'monchi","Лагманная","Lagman Bar"), "#4f97c9", T("Qovurma lag'mon","Жареный лагман","Fried lagman"), T("Manti x6","Манты x6","Manti x6"), T("Shurva","Шурпа","Shurva soup"))},
                    {"3,3", R("burger", T("Burger Bek","Бургер Бек","Burger Bek"), "#9a6cc4", T("Lavash","Лаваш","Lavash wrap"), T("Burger va kartoshka","Бургер с картошкой","Burger and fries"), T("Hot-dog x2","Хот-дог x2","Hot dog x2"))},
                },
                parks=new[]{"2,1","5,2"}, marks=new Dictionary<string,string>(), extra=new[]{"2,3","0,0"},
                lights=new int[,]{{1,1},{3,1},{5,1},{2,2},{4,2},{1,3},{3,3},{5,3}}, gai=new int[,]{{3,1},{1,3}},
                walls=new[]{"#e9cba9","#cfdac2","#e6b9a3","#c7d3e0","#ead7ae","#dac3d6"}, heights=new[]{34f,52f,42f,62f,38f}, trees=.45f
            }},
            {"chilonzor", new District{
                id="chilonzor", name=T("Chilonzor","Чиланзар","Chilanzar"), lvl=3, mult=1.2f, numMul=4, numAdd=100,
                rests=new Dictionary<string,RestDef>{
                    {"2,0", R("csomsa", T("Chilonzor Somsa","Чиланзар Самса","Chilanzar Somsa"), "#d7a042", T("Qatlama somsa","Слоёная самса","Layered somsa"), T("Tovuqli somsa","Самса с курицей","Chicken somsa"), T("Somsa va choy","Самса и чай","Somsa and tea"))},
                    {"5,1", R("pitsa", T("Pitsa Uy","Пицца Уй","Pizza Uy"), "#d9694f", T("Margarita pitsa","Пицца Маргарита","Margherita pizza"), T("Go'shtli pitsa","Мясная пицца","Meat pizza"), T("Pitsa bo'lagi x3","Кусок пиццы x3","Pizza slice x3"))},
                    {"1,2", R("bahor", T("Kafe Bahor","Кафе Бахор","Cafe Bahor"), "#5aa36b", T("Mastava","Мастава","Mastava soup"), T("Achchiq-chuchuk salat","Салат Аччик-чучук","Tomato-onion salad"), T("Qovurdoq","Ковурдак","Kovurdak stew"))},
                    {"4,3", R("manti", T("Manti Markazi","Манты Центр","Manti Center"), "#4f97c9", T("Qovoqli manti","Манты с тыквой","Pumpkin manti"), T("Go'shtli manti x6","Манты с мясом x6","Meat manti x6"), T("Xonim","Ханум","Khanum roll"))},
                },
                parks=new[]{"3,1"}, marks=new Dictionary<string,string>(), extra=new[]{"0,0","5,3"},
                lights=new int[,]{{1,1},{2,1},{3,1},{4,1},{5,1},{1,3},{2,3},{3,3},{4,3},{5,3},{2,2},{4,2}}, gai=new int[,]{{2,1},{4,3},{2,2}},
                walls=new[]{"#d5dbe3","#c9d3df","#e1d7c9","#cfd6cc","#dcd0d8","#e6e1d3"}, heights=new[]{60f,74f,66f,82f,58f}, trees=.6f,
                intro=new[]{ S("street", new[]{"jasur","dilnoza"},
                    Ln("N","Chilonzor. Baland ko'p qavatli uylar, keng ko'chalar va kechasi ham o'chmaydigan derazalar.","Чиланзар. Высокие многоэтажки, широкие улицы и окна, которые не гаснут даже ночью.","Chilanzar. Tall apartment blocks, wide streets and windows that stay lit all night."),
                    Ln("dilnoza","Siz «Tez Yetkaz»danmisiz? Men Dilnozaman, kasalxonada tungi smenada ishlayman.","Вы из «Тез Етказ»? Я Дильноза, работаю в больнице в ночную смену.","Are you from Tez Yetkaz? I'm Dilnoza, I work night shifts at the hospital."),
                    Ln("dilnoza","Bu yerda katta ilovaning kuryerlari ko'p. Lekin ovqatni ko'pincha sovutib olib kelishadi.","Здесь много курьеров большого приложения. Но еду часто привозят остывшей.","There are lots of couriers from the big app here. But the food often arrives cold."),
                    Ln("jasur","Bizda unday bo'lmaydi. Issiq va jilmayib!","У нас так не бывает. Горячо и с улыбкой!","Not with us. Hot, and with a smile!"),
                    Ln("dilnoza","Unda bugundan boshlab men sizning mijozingizman.","Тогда с сегодняшнего дня я ваш клиент.","Then from today, I'm your customer."),
                    Ln("N","Chilonzorda har bir buyurtma uchun 20% ko'proq to'lanadi. Svetofor va YHXB postlari ham ko'proq.","В Чиланзаре за каждый заказ платят на 20% больше. Светофоров и постов ДПС тоже больше.","In Chilanzar every order pays 20% more. There are also more traffic lights and police posts.")) }
            }},
            {"eski", new District{
                id="eski", name=T("Eski shahar","Старый город","Old City"), lvl=5, mult=1.4f, numMul=5, numAdd=3,
                rests=new Dictionary<string,RestDef>{
                    {"0,0", R("tandir", T("Tandir Non","Тандыр Нон","Tandoor Bread"), "#c98a4b", T("Issiq tandir non","Горячая тандырная лепёшка","Hot tandoor bread"), T("Patir non","Патыр","Patyr bread"), T("Non va qaymoq","Лепёшка с каймаком","Bread and cream"))},
                    {"3,1", R("chorsu", T("Chorsu Osh","Чорсу Ош","Chorsu Osh"), "#d9694f", T("Choyxona oshi","Чайханский плов","Teahouse plov"), T("Bayram oshi","Праздничный плов","Holiday plov"), T("Osh va norin","Плов и нарын","Plov and norin"))},
                    {"5,2", R("halim", T("Halimxona","Халимхона","Halim House"), "#9a6cc4", T("Halim","Халим","Halim porridge"), T("Sumalak","Сумаляк","Sumalak"), T("Ko'k somsa","Самса с зеленью","Herb somsa"))},
                    {"2,3", R("norin", T("Norin Uyi","Дом Нарына","Norin House"), "#4f97c9", T("Norin","Нарын","Norin noodles"), T("Hasip","Хасып","Hasip sausage"), T("Qazi bilan non","Казы с лепёшкой","Kazy with bread"))},
                },
                parks=new[]{"1,1"}, marks=new Dictionary<string,string>{{"4,0","bazar"}}, extra=new[]{"4,3","0,3"},
                lights=new int[,]{{2,1},{4,2},{2,3},{5,1},{1,2}}, gai=new int[,]{{2,1}},
                walls=new[]{"#e9c9a0","#e6bb93","#efd5b0","#dfb38c","#ecc7a2","#e3c29b"}, heights=new[]{24f,30f,28f,34f,26f}, trees=.75f,
                intro=new[]{ S("street", new[]{"jasur","bahodir"},
                    Ln("N","Eski shahar. Tor ko'chalar, tandir hidi va Chorsu bozorining moviy gumbazi.","Старый город. Узкие улочки, запах тандыра и голубой купол базара Чорсу.","The Old City. Narrow lanes, the smell of the tandoor and the blue dome of Chorsu Bazaar."),
                    Ln("bahodir","Xush kelibsan, o'g'lim! Men Bahodir ustaman, o'ttiz yildan beri tandirda non yopaman.","Добро пожаловать, сынок! Я мастер Баходир, тридцать лет пеку лепёшки в тандыре.","Welcome, son! I'm Master Bahodir. I've been baking bread in a tandoor for thirty years."),
                    Ln("bahodir","Bu yerning ko'chalari tor, lekin odamlari keng qalbli.","Улицы здесь узкие, зато сердца у людей широкие.","The streets here are narrow, but the people have big hearts."),
                    Ln("jasur","Karim aka sizni ko'p maqtagan edi.","Карим-ака вас очень хвалил.","Karim aka speaks very highly of you."),
                    Ln("bahodir","Karim? U bilan bir maktabda o'qiganmiz! Qani, ishingga omad!","Карим? Мы с ним в одной школе учились! Ну, удачи в работе!","Karim? We went to school together! Well, good luck out there!"),
                    Ln("N","Eski shaharda har bir buyurtma uchun 40% ko'proq to'lanadi.","В Старом городе за каждый заказ платят на 40% больше.","In the Old City every order pays 40% more.")) }
            }},
        };
        public static readonly string[] DistrictOrder = { "markaz", "chilonzor", "eski" };

        // ---------- YHXB bilan birinchi uchrashuv ----------
        public static readonly Shot[] GaiCut = { S("street", new[]{"jasur","anvar"},
            Ln("anvar","Assalomu alaykum! Yo'l harakati xavfsizligi xizmati, inspektor Anvar.","Здравствуйте! Служба безопасности дорожного движения, инспектор Анвар.","Good evening! Road safety service, Officer Anvar."),
            Ln("anvar","Qizil chiroqqa o'tdingiz. Shoshilish shart emas, ovqat baribir issiq yetib boradi.","Вы проехали на красный. Спешить не нужно: еда всё равно доедет горячей.","You went through a red light. There's no need to rush, the food will still arrive warm."),
            Ln("jasur","Kechirasiz, inspektor. Boshqa takrorlanmaydi.","Извините, инспектор. Больше не повторится.","Sorry, officer. It won't happen again."),
            Ln("anvar","Bu safar kichik shtraf. Yashil chiroqni kuting, hamma narsaning o'z vaqti bor.","В этот раз небольшой штраф. Ждите зелёный, всему своё время.","Just a small fine this time. Wait for green. Everything in its own time.")) };

        // ---------- Hikoya ----------
        public static readonly Chapter[] Story =
        {
            new Chapter{ title=T("1-kun","День 1","Day 1"), sub=T("Birinchi smena","Первая смена","First shift"),
                intro=new[]{ ChTitle(),
                    S("street", new[]{"jasur"},
                        Ln("N","Toshkent. Kuz. Shahar chiroqlari endigina yona boshladi.","Ташкент. Осень. В городе только зажигаются огни.","Tashkent. Autumn. The city lights are just coming on."),
                        Ln("N","Jasur Namangandan poytaxtga ish izlab kelganiga bir hafta bo'ldi.","Прошла неделя с тех пор, как Жасур приехал из Намангана в столицу искать работу.","It's been a week since Jasur came from Namangan to the capital to look for work."),
                        Ln("jasur","Manzil shu... «Tez Yetkaz». Yozuvi ancha eskirib qolibdi-ku.","Вот этот адрес... «Тез Етказ». Вывеска совсем старая.","This is the address... Tez Yetkaz. The sign has seen better days.")),
                    S("office", new[]{"jasur","karim"},
                        Ln("karim","Kiravering, ukam! Eshik ochiq. Choy ham endigina damlandi.","Заходи, братишка! Дверь открыта. И чай только заварился.","Come in, come in! The door's open, and the tea's just brewed."),
                        Ln("karim","Men Karim akaman. Bu xizmatni otam ochgan. O'shanda men xuddi sizdek yosh edim.","Я Карим-ака. Эту службу открыл мой отец. Я тогда был молод, как ты.","I'm Karim. My father started this business when I was about your age."),
                        Ln("jasur","Assalomu alaykum. E'loningizda kuryer kerak deb yozilgan ekan...","Здравствуйте. В объявлении написано, что нужен курьер...","Hello. Your notice said you needed a courier..."),
                        Ln("karim","To'g'ri. Hozir katta ilovalar zamoni. Lekin mahallamiz bizni hali tashlamagan.","Верно. Сейчас время больших приложений. Но наш район нас пока не бросил.","That's right. It's the age of big apps now, but our neighbourhood hasn't given up on us."),
                        Ln("karim","Mening bitta qoidam bor: biz tez emas, mehr bilan yetkazamiz. Kechiksang ham, ovqatni jilmayib topshir.","У меня одно правило: мы возим не быстро, а с душой. Даже если опоздал, отдай еду с улыбкой.","I have one rule: we don't deliver fast, we deliver with care. Even if you're late, hand it over with a smile."),
                        Ln("jasur","Tushunarli. Shoshilmayman.","Понял. Спешить не буду.","Understood. No rushing."),
                        Ln("karim","Chorrahalarda svetofor bor. Qizilda to'xta, YHXB inspektorlari kuzatib turadi.","На перекрёстках есть светофоры. На красный стой, инспекторы ДПС следят.","There are traffic lights at the crossings. Stop on red, the traffic police are watching."),
                        Ln("karim","Topgan puling o'zingniki. Garajda skuteringni yaxshilashing, istasang ofisimizni ham tiklashga yordam berishing mumkin.","Что заработал, то твоё. В гараже можешь улучшить скутер, а захочешь, поможешь восстановить наш офис.","What you earn is yours. You can upgrade your scooter in the garage, and if you like, help restore our office."),
                        Ln("karim","{R}da Rustam bobo yashaydi. Bugun u kishining buyurtmasi albatta keladi. Oltin yulduz bilan belgilanadi.","В доме {R} живёт дедушка Рустам. Сегодня от него обязательно будет заказ. Он отмечен золотой звездой.","Grandpa Rustam lives at {R}. His order will come in today for sure. It's marked with a gold star.")) },
                story=new StoryOrder{ rest="0,2", house=HOME_R, dish=T("Shurva (qalampirsiz)","Шурпа (без перца)","Shurva (no pepper)") },
                mid=new[]{ SP("door",'R',false,new[]{"jasur","rustam"},
                    Ln("rustam","Kelding-mi, bolam? Eshikni ochguncha ancha vaqt ketdi, oyoqlar endi avvalgidek emas.","Пришёл, сынок? Пока дошёл до двери, время прошло. Ноги уже не те.","Is that you, son? Took me a while to reach the door. My legs aren't what they were."),
                    Ln("jasur","Hechqisi yo'q, bobo. Mana shurvangiz, hali issiq.","Ничего страшного, дедушка. Вот ваша шурпа, ещё горячая.","That's alright, Grandpa. Here's your shurva, still hot."),
                    Ln("rustam","Oldingi kuryer sumkani eshik oldiga qo'yib ketardi. Sen esa salom berding.","Прежний курьер оставлял пакет у двери. А ты поздоровался.","The last courier used to leave the bag at the door. You said hello."),
                    Ln("rustam","Ertaga ham o'zing kelasanmi?","Завтра тоже ты придёшь?","Will you come tomorrow too?"),
                    Ln("jasur","Albatta, bobo.","Конечно, дедушка.","Of course, Grandpa.")) },
                outro=new[]{ S("office", new[]{"jasur","karim"},
                    Ln("karim","Birinchi kun ortda! Rustam bobo qo'ng'iroq qildi, seni maqtadi.","Первый день позади! Дедушка Рустам звонил, хвалил тебя.","First day done! Grandpa Rustam called to praise you."),
                    Ln("karim","U kishi yolg'iz yashaydi. Kampiri o'tgan qishda olamdan o'tgan.","Он живёт один. Его жена умерла прошлой зимой.","He lives alone. His wife passed away last winter."),
                    Ln("karim","Har kuni ovqat buyurtma qiladi. Menimcha, ko'proq birov bilan gaplashish uchun.","Он заказывает еду каждый день. Думаю, больше ради того, чтобы с кем-то поговорить.","He orders food every day. I think it's mostly so he can talk to someone."),
                    Ln("jasur","Unda ertaga ham men olib boraman.","Тогда завтра тоже отвезу я.","Then I'll take it to him tomorrow too.")) } },

            new Chapter{ title=T("2-kun","День 2","Day 2"), sub=T("Murabbo","Варенье","Jam"),
                intro=new[]{ ChTitle(),
                    S("kitchen", new[]{"jasur","malika"},
                        Ln("malika","Sen yangi kuryermisan? Men Malikaman. Otam shu Lag'monchining oshpazi.","Ты новый курьер? Я Малика. Мой отец здесь повар.","Are you the new courier? I'm Malika. My dad is the cook here."),
                        Ln("jasur","Jasur. Kecha Rustam boboga shurva olib borgandim.","Жасур. Вчера я возил дедушке Рустаму шурпу.","Jasur. I took Grandpa Rustam his shurva yesterday."),
                        Ln("malika","Bilaman! Bobo bizning eng sodiq mijozimiz. Unga shurvani hamisha qalampirsiz qilamiz.","Знаю! Дедушка наш самый верный клиент. Ему шурпу всегда делаем без перца.","I know! He's our most loyal customer. We always make his shurva without pepper."),
                        Ln("malika","Bugun unga uyda qilingan o'rik murabbosini berib yubormoqchiman. Olib borasanmi?","Сегодня хочу передать ему домашнее абрикосовое варенье. Отвезёшь?","Today I'd like to send him some homemade apricot jam. Will you take it?"),
                        Ln("jasur","Albatta. Buyurtmalar orasida yetkazaman.","Конечно. Отвезу между заказами.","Of course. I'll fit it in between orders.")) },
                story=new StoryOrder{ rest="0,2", house=HOME_R, dish=T("Non va o'rik murabbosi","Лепёшка и абрикосовое варенье","Bread and apricot jam") },
                mid=new[]{ SP("door",'R',false,new[]{"jasur","rustam"},
                    Ln("rustam","O'rik murabbosi... Kampirim ham xuddi shunday pishirardi.","Абрикосовое варенье... Моя жена варила точно такое же.","Apricot jam... My wife used to make it just like this."),
                    Ln("rustam","Sen qayerliksan, bolam?","Откуда ты родом, сынок?","Where are you from, son?"),
                    Ln("jasur","Namanganlikman, bobo.","Из Намангана, дедушка.","From Namangan, Grandpa."),
                    Ln("rustam","Yoshligimda Namanganda ko'prik qurganman! Gullar shahri. Bir kun o'tirib, senga aytib beraman.","В молодости я строил мост в Намангане! Город цветов. Как-нибудь сядем, я тебе расскажу.","I built a bridge in Namangan when I was young! The city of flowers. One day I'll tell you all about it.")) },
                outro=new[]{ S("office", new[]{"jasur","karim"},
                    Ln("karim","Ukam, bugun bir xabar eshitdim.","Братишка, сегодня я кое-что услышал.","I heard some news today."),
                    Ln("karim","«MegaDost» degan katta ilova mahallamizga kirmoqchi. Arzon narx, yuzlab kuryer...","Большое приложение «МегаДост» хочет зайти в наш район. Низкие цены, сотни курьеров...","A big app called MegaDost wants to move into our neighbourhood. Low prices, hundreds of couriers..."),
                    Ln("jasur","Unda biz nima qilamiz?","И что нам делать?","So what do we do?"),
                    Ln("karim","Bilmadim. Hozircha ishlayveramiz. Qani, choy ich, charchading.","Не знаю. Пока просто работаем. Давай, выпей чаю, ты устал.","I don't know. For now, we keep working. Here, have some tea. You're tired.")) } },

            new Chapter{ title=T("3-kun","День 3","Day 3"), sub=T("Kichkina shaxmatchi","Маленький шахматист","The little chess player"),
                intro=new[]{ ChTitle(),
                    S("street", new[]{"jasur","sardor"},
                        Ln("sardor","Amaki! Amaki! Siz Rustam bobonikiga ovqat olib borasizmi?","Дядя! Дядя! Это вы возите еду дедушке Рустаму?","Mister! Mister! Are you the one who brings Grandpa Rustam his food?"),
                        Ln("jasur","Ha. Sen kimsan, qahramon?","Да. А ты кто, герой?","Yes. And who are you, champ?"),
                        Ln("sardor","Men Sardorman, {S}da yashayman. Rustam bobo menga shaxmat o'rgatyapti!","Я Сардор, живу в доме {S}. Дедушка Рустам учит меня шахматам!","I'm Sardor, I live at {S}. Grandpa Rustam is teaching me chess!"),
                        Ln("sardor","Bugun tug'ilgan kunim. Oyim Burger Bekdan buyurtma qildi. Siz olib kelasizmi?","Сегодня мой день рождения. Мама заказала в «Бургер Беке». Вы привезёте?","It's my birthday today. Mum ordered from Burger Bek. Will you bring it?"),
                        Ln("jasur","Unda bugungi eng muhim buyurtma seniki.","Тогда самый важный заказ сегодня твой.","Then yours is the most important order today.")) },
                story=new StoryOrder{ rest="3,3", house=HOME_S, dish=T("Tug'ilgan kun to'plami","Набор на день рождения","Birthday box") },
                mid=new[]{ SP("door",'S',false,new[]{"jasur","sardor"},
                    Ln("sardor","Uraaa! Rahmat, amaki!","Ураа! Спасибо, дядя!","Hooray! Thank you, mister!"),
                    Ln("sardor","Faqat... Rustam bobo bugun shaxmatga chiqmadi. Derazasi ham kun bo'yi yopiq.","Только... дедушка Рустам сегодня не вышел играть. И окно весь день закрыто.","Only... Grandpa Rustam didn't come out for chess today. His window's been shut all day."),
                    Ln("jasur","Balki dam olayotgandir. Ertaga birinchi bo'lib unikiga boraman.","Может, отдыхает. Завтра первым делом заеду к нему.","Maybe he's resting. I'll go to him first thing tomorrow.")) },
                outro=new[]{ S("office", new[]{"jasur","karim"},
                    Ln("jasur","Karim aka, Rustam bobo bugun buyurtma bermadi.","Карим-ака, дедушка Рустам сегодня ничего не заказал.","Karim aka, Grandpa Rustam didn't order anything today."),
                    Ln("karim","Ha, men ham payqadim. Shuncha yilda birinchi marta.","Да, я тоже заметил. Впервые за столько лет.","Yes, I noticed too. First time in all these years."),
                    Ln("karim","Ertaga smenani u kishidan boshla. Xavotir olma, ehtimol shunchaki charchagandir.","Завтра начни смену с него. Не волнуйся, наверное, просто устал.","Start tomorrow's shift with him. Don't worry, he's probably just tired.")) } },

            new Chapter{ title=T("4-kun","День 4","Day 4"), sub=T("Yopiq deraza","Закрытое окно","The closed window"),
                intro=new[]{ ChTitle(),
                    SP("door",'R',true,new[]{"jasur"},
                        Ln("N","Ertalab. {R}. Deraza hali ham yopiq.","Утро. Дом {R}. Окно всё ещё закрыто.","Morning. {R}. The window is still shut."),
                        Ln("jasur","Bobo! Rustam bobo! Bu men, Jasur!","Дедушка! Дедушка Рустам! Это я, Жасур!","Grandpa! Grandpa Rustam! It's me, Jasur!")),
                    SP("door",'R',false,new[]{"jasur","rustam"},
                        Ln("rustam","Kir, bolam... Eshik ochiq. Biroz shamollab qoldim, xolos.","Заходи, сынок... Открыто. Просто немного простыл.","Come in, son... It's open. I've just caught a little cold."),
                        Ln("jasur","Nega hech kimga aytmadingiz?","Почему вы никому не сказали?","Why didn't you tell anyone?"),
                        Ln("rustam","Kimga aytaman? Bolalarim uzoqda, chet elda. Bezovta qilgim kelmadi.","Кому говорить? Дети далеко, за границей. Не хотел беспокоить.","Who would I tell? My children are far away, abroad. I didn't want to be a bother."),
                        Ln("jasur","Hozir Malikaga qo'ng'iroq qilaman. U sizga eng shifobaxsh shurvani pishiradi.","Сейчас позвоню Малике. Она сварит вам самую целебную шурпу.","I'll call Malika right now. She'll make you the most healing shurva there is.")) },
                story=new StoryOrder{ rest="0,2", house=HOME_R, dish=T("Shifobaxsh shurva va limonli choy","Целебная шурпа и чай с лимоном","Healing shurva and lemon tea") },
                mid=new[]{ SP("door",'R',false,new[]{"jasur","rustam"},
                    Ln("rustam","Malika qizim ham, Sardor ham xabar olibdi. Karim ham qo'ng'iroq qildi.","И Малика заходила, и Сардор. Карим тоже звонил.","Malika came by, and Sardor. Karim called too."),
                    Ln("rustam","Yolg'izman deb o'ylardim. Ma'lum bo'lishicha, yolg'iz emas ekanman.","Я думал, что я один. А оказалось, что нет.","I thought I was alone. Turns out I'm not."),
                    Ln("jasur","Tezroq tuzalib keting, bobo. Shaxmat sizni kutyapti.","Выздоравливайте скорее, дедушка. Шахматы вас ждут.","Get well soon, Grandpa. The chessboard is waiting for you.")) },
                outro=new[]{ S("office", new[]{"jasur","karim"},
                    Ln("karim","Ukam, o'tir. Gapim bor.","Братишка, садись. Есть разговор.","Sit down. We need to talk."),
                    Ln("karim","MegaDost bizga taklif yubordi. Xizmatni sotib olishmoqchi. Pul yaxshi.","«МегаДост» прислал предложение. Хотят купить нашу службу. Деньги хорошие.","MegaDost sent us an offer. They want to buy the business. It's good money."),
                    Ln("karim","Men qarib qoldim. Balki bu to'g'ri qarordir...","Я уже немолод. Может, это правильное решение...","I'm getting old. Maybe it's the right thing to do..."),
                    Ln("jasur","Lekin ular Rustam boboning shurvasi qalampirsiz ekanini bilisharmikan?","А они будут знать, что дедушке Рустаму шурпу нужно без перца?","But would they know Grandpa Rustam's shurva has to be made without pepper?"),
                    Ln("karim","...","...","..."),
                    Ln("karim","Ertaga oxirgi qarorimni aytaman. Avval smenani tugataylik.","Завтра скажу окончательное решение. Сначала закончим смену.","I'll give you my final answer tomorrow. Let's finish the shift first.")) } },

            new Chapter{ title=T("5-kun","День 5","Day 5"), sub=T("Bir laganda osh","Плов на одном блюде","Plov on one platter"),
                intro=new[]{ ChTitle(),
                    S("office", new[]{"jasur","karim"},
                        Ln("karim","Bugun Rustam bobo o'zi qo'ng'iroq qildi. Ovozi tetik!","Сегодня дедушка Рустам сам позвонил. Голос бодрый!","Grandpa Rustam called himself today. He sounded well!"),
                        Ln("karim","Osh Markazdan katta lagan osh buyurtma qilibdi. Ko'p kishilik.","Заказал большое блюдо плова в «Ош Марказ». На много человек.","He ordered a big platter of plov from Osh Markaz. Enough for a crowd."),
                        Ln("jasur","Ko'p kishilik? U kishi yolg'iz-ku.","На много человек? Он же живёт один.","For a crowd? But he lives alone."),
                        Ln("karim","Kim biladi. Borib ko'r. Lekin boshqa mijozlarni ham unutma.","Кто знает. Съезди и посмотри. Но и других клиентов не забывай.","Who knows. Go and see. But don't forget the other customers.")) },
                story=new StoryOrder{ rest="1,0", house=HOME_R, dish=T("Katta lagan to'y oshi","Большое блюдо свадебного плова","A big platter of wedding plov") },
                mid=new[]{ SP("door",'R',false,new[]{"jasur","rustam"},
                    Ln("rustam","Keldingmi! Bugun mehmonlarim bor. Sen ham ishingni tugatgach, albatta kel.","Пришёл! Сегодня у меня гости. Закончишь работу, обязательно приходи.","You're here! I'm having guests tonight. Come over when you finish work."),
                    Ln("jasur","Smena tugashi bilan kelaman, bobo.","Приду сразу после смены, дедушка.","I'll come straight after my shift, Grandpa.")) },
                outro=new[]{
                    S("yard", new[]{"karim","malika","rustam","sardor","jasur"},
                        Ln("N","Kechqurun. Rustam boboning hovlisida, tok so'ri ostida dasturxon yozilgan.","Вечер. Во дворе дедушки Рустама, под виноградником, накрыт дастархан.","Evening. In Grandpa Rustam's courtyard, under the grapevine, the table is laid."),
                        Ln("rustam","Hammangiz keldingizmi? Mana endi oilam to'liq.","Все пришли? Вот теперь моя семья в сборе.","Is everyone here? Now my family is complete."),
                        Ln("malika","Bobo, oshni siz buyurdingiz, lekin qozonni otam o'zi qaynatdi!","Дедушка, плов заказали вы, но казан папа варил сам!","Grandpa, you ordered the plov, but my dad cooked the pot himself!"),
                        Ln("sardor","Bobo, keyin shaxmat o'ynaymizmi? Bu safar men yutaman!","Дедушка, потом в шахматы? В этот раз я выиграю!","Grandpa, chess after? I'm going to win this time!"),
                        Ln("karim","Do'stlar, bir yangilik aytay. Bugun MegaDostga javob berdim.","Друзья, у меня новость. Сегодня я ответил «МегаДосту».","Friends, I have news. Today I gave MegaDost my answer."),
                        Ln("karim","Yo'q, dedim. «Tez Yetkaz» mahallaniki. Sotilmaydi.","Я сказал: нет. «Тез Етказ» принадлежит району. Не продаётся.","I said no. Tez Yetkaz belongs to the neighbourhood. It's not for sale."),
                        Ln("jasur","Karim aka! Rahmat!","Карим-ака! Спасибо!","Karim aka! Thank you!"),
                        Ln("karim","Menga emas, senga rahmat. Mehr bilan yetkazish nima ekanini menga qayta eslatding.","Это тебе спасибо. Ты напомнил мне, что значит доставлять с душой.","No, thank you. You reminded me what delivering with care really means."),
                        Ln("karim","Endi esa boshqa mahallalarga ham chiqish vaqti keldi. Chilonzor, Eski shahar. Hammasi seni kutyapti.","А теперь пора и в другие районы. Чиланзар, Старый город. Все ждут.","And now it's time to reach other districts. Chilanzar, the Old City. They're all waiting."),
                        Ln("rustam","Shahar katta, bolalarim. Lekin mahalla bitta oila. Qani, osh sovib qolmasin!","Город большой, дети мои. Но махалля одна семья. Давайте, а то плов остынет!","The city is big, my children. But a neighbourhood is one family. Come on, before the plov gets cold!")),
                    S("street", new[]{"jasur"},
                        Ln("N","Toshkentda yana bir kech. Qayerdadir kimdir buyurtma beryapti...","Ещё один вечер в Ташкенте. Где-то кто-то делает заказ...","Another evening in Tashkent. Somewhere, someone is placing an order..."),
                        Ln("N","...va Jasur shoshilmasdan, jilmayib, yo'lga chiqadi.","...и Жасур, не спеша и с улыбкой, выезжает в путь.","...and Jasur heads out, unhurried, with a smile.")),
                    new Shot{ title=2, lines=new string[0][], tTitle=T("Oxiri","Конец","The End"), tSub=T("Rahmat! Endi erkin smenalarda daraja oshiring va yangi mahallalarni oching.","Спасибо! Теперь повышайте уровень в свободных сменах и открывайте новые районы.","Thank you! Now level up in free shifts and unlock new districts.") } } },
        };

        // ---------- Do'kon ----------
        public static readonly int[] Speeds = { 140, 155, 170, 185 };
        public static readonly int[] Bags = { 3, 4, 5 };
        public static readonly Upg[] Upgrades =
        {
            new Upg{ id="speed", name=T("Kuchli dvigatel","Мощный мотор","Stronger engine"), desc=T("Skuter tezroq yuradi","Скутер едет быстрее","The scooter goes faster"), prices=new[]{30000,60000,100000}, col="#ffd166" },
            new Upg{ id="bag", name=T("Katta sumka","Большая сумка","Bigger bag"), desc=T("Sumkaga ko'proq buyurtma sig'adi","Вмещает больше заказов","Carries more orders"), prices=new[]{40000,90000}, col="#f4a259" },
            new Upg{ id="nav", name=T("Navigator","Навигатор","Navigator"), desc=T("Butun yo'l chiziladi, YHXB postlarini aylanib o'tadi, ikkinchi buyurtma ham ko'rinadi","Весь маршрут, объезжает посты ДПС, видно и второй заказ","Full route that avoids police posts, plus your second order"), prices=new[]{50000}, col="#7ec8e3" },
            new Upg{ id="ins", name=T("Sug'urta","Страховка","Insurance"), desc=T("Barcha shtraflar ikki baravar kam","Все штрафы в два раза меньше","All fines are halved"), prices=new[]{70000}, col="#8fd6a8" },
        };
        public static readonly string[] LookCats = { "scooter", "helmet", "bag" };
        public static readonly Dictionary<string, string[]> LookTitle = new Dictionary<string, string[]>
        {
            {"scooter", T("Skuter","Скутер","Scooter")}, {"helmet", T("Shlem","Шлем","Helmet")}, {"bag", T("Sumka","Сумка","Bag")},
        };
        public static readonly Dictionary<string, LookItem[]> Looks = new Dictionary<string, LookItem[]>
        {
            {"scooter", new[]{
                new LookItem{ id="teal", name=T("Firuza","Бирюза","Turquoise"), c0="#46d0bf", c1="#25877c" },
                new LookItem{ id="red", name=T("Anor","Гранат","Pomegranate"), c0="#ff8a7a", c1="#c2412f", price=15000 },
                new LookItem{ id="violet", name=T("Binafsha","Фиалка","Violet"), c0="#b99cf0", c1="#6f4fb5", price=15000 },
                new LookItem{ id="yellow", name=T("Limon","Лимон","Lemon"), c0="#ffe08a", c1="#d4a72c", price=15000 },
                new LookItem{ id="blue", name=T("Osmon","Небо","Sky"), c0="#8cc4ff", c1="#2f6fc2", price=20000 },
                new LookItem{ id="night", name=T("Tungi","Ночной","Night"), c0="#6a7480", c1="#22282e", price=25000 },
                new LookItem{ id="gold", name=T("Oltin","Золотой","Golden"), c0="#ffe08a", c1="#c9962a", vip=true } }},
            {"helmet", new[]{
                new LookItem{ id="orange", name=T("To'q sariq","Оранжевый","Orange"), c0="#ffcf8a", c1="#e88a3c" },
                new LookItem{ id="white", name=T("Oq","Белый","White"), c0="#ffffff", c1="#c2cbd1", price=10000 },
                new LookItem{ id="green", name=T("Yashil","Зелёный","Green"), c0="#b8f0a0", c1="#4fa35a", price=10000 },
                new LookItem{ id="pink", name=T("Pushti","Розовый","Pink"), c0="#ffc0d6", c1="#d9638e", price=12000 },
                new LookItem{ id="gold", name=T("Oltin","Золотой","Gold"), c0="#fff0a8", c1="#d6a62a", price=40000, lvl=5 } }},
            {"bag", new[]{
                new LookItem{ id="orange", name=T("Klassik","Классика","Classic"), c0="#f8ad66", c1="#e18a42" },
                new LookItem{ id="blue", name=T("Moviy","Синяя","Blue"), c0="#8cc4ff", c1="#3d7fd0", price=12000 },
                new LookItem{ id="ikat", name=T("Atlas naqshli","С узором атлас","Atlas silk pattern"), c0="#e85d75", c1="#3d7fd0", price=30000, ikat=true } }},
        };
        public static LookItem Look(string cat, string id)
        {
            foreach (var it in Looks[cat]) if (it.id == id) return it;
            return Looks[cat][0];
        }

        public static readonly Reno[] Renos =
        {
            new Reno{ id="sign", name=T("Yangi vyveska","Новая вывеска","New sign"), desc=T("Yorqin, chiroqli vyveska","Яркая вывеска с лампочками","A bright sign with bulbs"), price=40000,
                lines=new[]{ Ln("karim","Yangi vyveska! Endi mahalla bizni uzoqdan ko'radi.","Новая вывеска! Теперь нас видно издалека.","A new sign! Now the whole neighbourhood can see us."), Ln("karim","Otam ko'rsa, juda xursand bo'lardi.","Отец был бы очень рад.","My father would have been so happy.") } },
            new Reno{ id="teapot", name=T("Zarhal choynak","Чайник с позолотой","Gilded teapot"), desc=T("Karim akaga oltin naqshli choynak","Чайник с золотым узором для Карим-ака","A gold-patterned teapot for Karim aka"), price=15000,
                lines=new[]{ Ln("karim","Zarhal naqshli choynak! Endi choyimiz ham, mehmonlarimiz ham ko'payadi.","Чайник с позолотой! Теперь и чая, и гостей будет больше.","A gilded teapot! Now we'll have more tea, and more guests.") } },
            new Reno{ id="map", name=T("Toshkent xaritasi","Карта Ташкента","Map of Tashkent"), desc=T("Ofis devoriga katta xarita","Большая карта на стену офиса","A big map for the office wall"), price=20000,
                lines=new[]{ Ln("karim","Toshkent xaritasi! Qaysi mahallalarga yetib bordik, belgilab qo'yaylik.","Карта Ташкента! Давай отметим районы, где мы уже работаем.","A map of Tashkent! Let's mark the districts we've reached.") } },
            new Reno{ id="office", name=T("Ofis ta'miri","Ремонт офиса","Office renovation"), desc=T("Yangi bo'yoq, pol va gilam","Новая краска, пол и ковёр","Fresh paint, floor and a rug"), price=60000,
                lines=new[]{ Ln("karim","Devorlar yangilandi, pol yaltirab turibdi. Xuddi o'ttiz yil oldingidek!","Стены обновили, пол блестит. Как тридцать лет назад!","New walls, shining floor. Just like thirty years ago!"), Ln("jasur","Endi mijozlar ham kirib choy ichishi mumkin.","Теперь и клиенты могут зайти на чай.","Now customers can drop in for tea too.") } },
            new Reno{ id="chair", name=T("Karim akaga kreslo","Кресло для Карим-ака","An armchair for Karim aka"), desc=T("Qulay yumshoq kreslo","Удобное мягкое кресло","A soft, comfy armchair"), price=35000,
                lines=new[]{ Ln("karim","Kreslo? Ukam, men hali yoshman!","Кресло? Братишка, я ещё молод!","An armchair? I'm still young, you know!"), Ln("karim","...Lekin juda qulay ekan. Rahmat.","...Но очень удобное. Спасибо.","...But it is very comfortable. Thank you.") } },
        };
        public static readonly string[] RenoAll = Ln("karim","Ukam... «Tez Yetkaz» xuddi otam davridagidek chaqnab turibdi. Bu sening mehnating.","Братишка... «Тез Етказ» сияет, как при отце. Это твой труд.","Look at it... Tez Yetkaz shines like it did in my father's day. This is your work.");
    }
}
