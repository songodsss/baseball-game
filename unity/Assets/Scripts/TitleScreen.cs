using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// Home: a small town of buildings in a sunny meadow. The training centre leads to my players (save slots); the
    /// others hold what belongs to the whole club. Lots under construction leave room for more.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public const string GameTitle = "나의 야구 인생";

        // Press Play in any scene and the prototype starts here, on the "touch the screen" page.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<TitleScreen>() == null && FindAnyObjectByType<GrowthScreen>() == null && FindAnyObjectByType<StartScreen>() == null)
                StartScreen.Show();
        }

        public static void Show() => new GameObject("TitleScreen").AddComponent<TitleScreen>();

        IRandom _random;
        Club _club;
        RectTransform _root, _overlay;

        void Start()
        {
            _random = new SeededRandom(System.Environment.TickCount);
            var canvas = UiKit.Canvas(transform, "TitleCanvas");
            _root = UiKit.SafeRoot(canvas.transform);
            var scene = UiKit.Empty(_root.parent, "Scenery");
            UiKit.Stretch(scene);
            scene.SetAsFirstSibling();
            Scenery.Sky(scene);
            Scenery.Cloud(scene, 0.25f, 0.965f, 0.6f, 10);
            Scenery.Cloud(scene, 0.7f, 0.95f, 0.5f, 7);
            UiKit.Column(_root, 40, 18);
            GameAudio.PlayMusic(GameAudio.MusicHome);
            GameAudio.PlayAmbience(null);
            Build();
        }

        int _selected;

        /// <summary>
        /// A building on the home screen. New content is one more entry here: give it a picture at
        /// Resources/Art/Buildings/{Id}.png, or it is drawn from simple shapes until then.
        /// </summary>
        class Building
        {
            public string Id, Name, Status;
            public Color Body, Roof;
            public UnityEngine.Events.UnityAction Open;
            /// <summary>Shown as a construction site until it opens.</summary>
            public bool Soon;
            public int Badge;
        }

        void Build()
        {
            CloseOverlay();
            foreach (Transform child in _root) Destroy(child.gameObject);
            _club = SaveStore.LoadClub(_random);
            if (_club.Daily.Refresh(Today)) SaveStore.SaveClub(_club);

            var slots = Enumerable.Range(1, SaveStore.SlotCount).Select(slot => SaveStore.Peek(slot, _club, _random)).ToList();
            // The player in focus: the one picked, else the last one played, else the first with a player.
            if (_selected < 1 || _selected > slots.Count)
                _selected = !slots[Mathf.Clamp(SaveStore.CurrentSlot, 1, slots.Count) - 1].IsEmpty ? Mathf.Clamp(SaveStore.CurrentSlot, 1, slots.Count)
                    : (slots.FirstOrDefault(x => !x.IsEmpty)?.Slot ?? 1);
            var focus = slots[_selected - 1];

            TopBar();

            // The town: a sunny meadow that scrolls, with the buildings dotted along a winding path.
            var ground = UiKit.Panel(_root, "Town", Scenery.Grass);
            UiKit.Height(ground, 0, 1);
            UiKit.Column(ground, 10, 0);
            var town = UiKit.ScrollList(ground, 0);
            UiKit.Height(town.parent, 0, 1);

            var daily = _club.Daily;
            int done = daily.Today.Count(m => m.Done);
            string trainee = focus.IsEmpty ? "새 선수를 키워 보세요" : $"{focus.Career.Me.Name} · {focus.Career.Me.Age}세 · {focus.Career.MyTeam.Name}";

            // The training centre is where the game is, so it stands alone and biggest.
            var training = new Building
            {
                Id = "training_center", Name = "트레이닝 센터", Status = trainee,
                Body = focus.IsEmpty ? new Color(0.3f, 0.5f, 0.85f) : UiKit.TeamColor(focus.Career.MyTeam.Name), Roof = new Color(0.92f, 0.94f, 0.98f),
                Open = () => ShowPlayers(slots),
            };

            var buildings = new[]
            {
                new Building
                {
                    Id = "club_hq", Name = "구단 본부", Status = $"나만의 팀 · 선수 {_club.Roster.Count}명",
                    Body = new Color(0.62f, 0.2f, 0.18f), Roof = new Color(0.95f, 0.75f, 0.3f), Open = () => RecordsScreen.OpenMyTeam(_club, Build),
                },
                new Building
                {
                    Id = "museum", Name = "야구 박물관", Status = $"도감 · 레전드 {_club.AllLegendsMet(null).Count}/{Legends.All.Count}",
                    Body = new Color(0.85f, 0.8f, 0.68f), Roof = new Color(0.45f, 0.38f, 0.3f), Open = () => CollectionScreen.Open(null, _club),
                },
                new Building
                {
                    Id = "clubhouse", Name = "클럽하우스", Status = $"미션 {done}/{daily.Today.Count} · 패스 Lv.{daily.Pass.Level}",
                    Body = new Color(0.25f, 0.55f, 0.4f), Roof = new Color(0.2f, 0.25f, 0.3f), Open = ShowMissions, Badge = ChallengeScreen.Ready(_club, false),
                },
                new Building
                {
                    Id = "store", Name = "구단 스토어", Status = $"코인 {_club.Coins:N0} · 꾸미기·편의",
                    Body = new Color(0.55f, 0.4f, 0.8f), Roof = new Color(0.95f, 0.45f, 0.55f), Open = () => PremiumScreen.Open(_club, Build),
                },
                new Building
                {
                    Id = "stadium", Name = "메인 스타디움",
                    Status = _club.TeamLeague.Started ? $"내 팀 리그 · {MyTeamLeague.DivisionName(_club.TeamLeague.Division)} {_club.TeamLeague.MyRank}위" : "내 팀 리그",
                    Body = new Color(0.5f, 0.55f, 0.6f), Roof = new Color(0.7f, 0.72f, 0.75f), Open = () => TeamLeagueScreen.Open(_club, Build),
                },
                new Building
                {
                    Id = "scout", Name = "스카우트 센터", Status = $"카드 {_club.CardLevels.Count} · 장비 {_club.GearCopies.Count} · 뽑기권 {_club.DrawTickets}",
                    Body = new Color(0.2f, 0.45f, 0.6f), Roof = new Color(0.95f, 0.85f, 0.4f), Open = () => GachaScreen.Open(_club, null, Build),
                },
                new Building
                {
                    Id = "arena", Name = "결투장", Status = $"{_club.Arena.Tier} · 점수 {_club.Arena.Rating}",
                    Body = new Color(0.55f, 0.22f, 0.25f), Roof = new Color(0.85f, 0.75f, 0.5f), Open = () => ArenaScreen.Open(_club, Build),
                },
            };
            Town(town, training, buildings);
        }
        // ---------- The town ----------

        /// <summary>Where a building stands: x as a fraction of the town's width, y and size in pixels.</summary>
        struct Spot
        {
            public float X, Y, W, H;
            public Spot(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            public float Foot => Y + H / 2;
        }

        /// <summary>Rough width of the town in pixels, used only to space things out.</summary>
        const float MapWidth = 960;
        static readonly Spot TrainingSpot = new Spot(0.5f, 310, 800, 560);

        /// <summary>
        /// The other buildings zig-zag down the meadow, not quite in rows: left, then a little lower on
        /// the right, then further down on the left again, each a slightly different size. A building
        /// past the last spot here carries on the same pattern.
        /// </summary>
        static readonly Spot[] Spots =
        {
            new Spot(0.29f, 830, 420, 420), new Spot(0.72f, 1010, 400, 400),
            new Spot(0.3f, 1390, 410, 410), new Spot(0.72f, 1560, 400, 400),
            new Spot(0.3f, 1950, 430, 430), new Spot(0.74f, 2120, 400, 400),
            new Spot(0.32f, 2500, 420, 420),
        };

        static Spot SpotFor(int i)
        {
            if (i < Spots.Length) return Spots[i];
            var last = Spots[Spots.Length - 1];
            float y = last.Y;
            for (int k = Spots.Length; k <= i; k++) y += k % 2 == 1 ? 180 : 380;
            return new Spot(i % 2 == 0 ? 0.31f : 0.72f, y, 410, 410);
        }

        /// <summary>
        /// Lays out the meadow: soft patches of grass, a pond, a sandy path winding past every door,
        /// flowers, then trees, bushes and buildings from the back of the picture to the front so the
        /// nearer ones overlap the farther. The scenery is the same every visit (fixed seed).
        /// </summary>
        void Town(RectTransform town, Building training, Building[] buildings)
        {
            var spots = new System.Collections.Generic.List<Spot> { TrainingSpot };
            for (int i = 0; i < buildings.Length; i++) spots.Add(SpotFor(i));
            float height = spots.Max(s => s.Foot) + 150;

            var map = UiKit.Empty(town, "Map");
            UiKit.Height(map, height);
            var rng = new System.Random(1103);

            // A painted ground, if there is one, else soft lighter and darker patches on the grass.
            var groundArt = ArtLibrary.Background("town");
            if (groundArt != null) PaintedGround(map, groundArt, height);
            else
                for (int i = 0; i < (int)(height / 180); i++)
                {
                    float w = 300 + rng.Next(280);
                    Scenery.Dot(map, i % 2 == 0 ? Scenery.GrassLight : Scenery.GrassDark, (float)rng.NextDouble(), (float)rng.NextDouble() * height, w, w * 0.55f);
                }

            // The path: a curve through every door, from the top of the meadow to the bottom.
            var doors = new System.Collections.Generic.List<Vector2> { new Vector2(0.5f * MapWidth, -60) };
            doors.AddRange(spots.Select(s => new Vector2(s.X * MapWidth, s.Foot + 16)));
            doors.Add(new Vector2(0.5f * MapWidth, height + 60));
            var path = PathPoints(doors, 18);
            foreach (var p in path) Scenery.Dot(map, Scenery.PathEdge, p.x / MapWidth, p.y, 82, 70);
            foreach (var p in path) Scenery.Dot(map, Scenery.Path, p.x / MapWidth, p.y, 64, 52);
            for (int i = 3; i < path.Count; i += 7) // a few pebbles in the sand
                Scenery.Dot(map, new Color(1f, 0.95f, 0.85f, 0.7f), path[i].x / MapWidth + ((float)rng.NextDouble() - 0.5f) * 0.03f, path[i].y, 12, 9);

            bool Clear(float x, float y, float r)
            {
                if (x < r * 0.5f || x > MapWidth - r * 0.5f || y < r || y > height - 20) return false;
                foreach (var s in spots)
                {
                    float sx = s.X * MapWidth;
                    if (x > sx - s.W / 2 - r && x < sx + s.W / 2 + r && y > s.Y - s.H / 2 - r * 0.3f && y < s.Foot + r) return false;
                }
                foreach (var p in path) if ((p - new Vector2(x, y)).sqrMagnitude < (r + 44) * (r + 44)) return false;
                return true;
            }

            // A pond in the first open space big enough.
            for (int tries = 0; tries < 300; tries++)
            {
                float x = 120 + (float)rng.NextDouble() * (MapWidth - 240), y = 700 + (float)rng.NextDouble() * (height - 900);
                if (!Clear(x, y, 130)) continue;
                Scenery.Pond(map, x / MapWidth, y, 230, 120);
                spots.Add(new Spot(x / MapWidth, y, 260, 150)); // keep the trees off the water
                break;
            }

            // Flowers, then trees and bushes sorted with the buildings by where they stand.
            for (int n = 0, tries = 0; n < height / 70 && tries < 800; tries++)
            {
                float x = (float)rng.NextDouble() * MapWidth, y = (float)rng.NextDouble() * height;
                if (!Clear(x, y, 20)) continue;
                Scenery.Flowers(map, x / MapWidth, y, 50, rng, 3 + rng.Next(3));
                n++;
            }

            var standing = new System.Collections.Generic.List<(float foot, System.Action draw)>();
            var trees = new System.Collections.Generic.List<Vector2>();
            for (int n = 0, tries = 0; n < height / 90 && tries < 1500; tries++)
            {
                bool tree = rng.Next(3) > 0;
                float size = tree ? 100 + rng.Next(50) : 60 + rng.Next(40);
                float x = (float)rng.NextDouble() * MapWidth, y = (float)rng.NextDouble() * height;
                if (!Clear(x, y, size * 0.5f) || trees.Any(t => (t - new Vector2(x, y)).magnitude < size * 0.8f)) continue;
                trees.Add(new Vector2(x, y));
                int seed = rng.Next();
                if (tree) standing.Add((y, () => Scenery.Tree(map, x / MapWidth, y, size, new System.Random(seed))));
                else standing.Add((y, () => Scenery.Bush(map, x / MapWidth, y, size, new System.Random(seed))));
                n++;
            }
            standing.Add((TrainingSpot.Foot, () => BuildingTile(Place(map, TrainingSpot, training.Id), training, big: true)));
            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                var s = SpotFor(i);
                standing.Add((s.Foot, () => BuildingTile(Place(map, s, b.Id), b)));
            }
            foreach (var item in standing.OrderBy(x => x.foot)) item.draw();
        }

        static RectTransform Place(RectTransform map, Spot s, string id)
        {
            var tile = UiKit.Empty(map, id);
            tile.anchorMin = tile.anchorMax = new Vector2(s.X, 1);
            tile.sizeDelta = new Vector2(s.W, s.H);
            tile.anchoredPosition = new Vector2(0, -s.Y);
            return tile;
        }

        /// <summary>A painted ground picture repeated down the whole meadow, full width.</summary>
        static void PaintedGround(RectTransform map, Sprite art, float height)
        {
            float tile = MapWidth * art.rect.height / art.rect.width;
            // Every other copy is flipped upside down so the joins mirror each other and no seam shows.
            int n = 0;
            for (float y = 0; y < height; y += tile, n++)
            {
                var image = UiKit.Empty(map, "Ground").gameObject.AddComponent<Image>();
                image.sprite = art;
                image.raycastTarget = false;
                var rt = image.rectTransform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(0, tile + 2);
                rt.anchoredPosition = new Vector2(0, -y - tile / 2);
                if (n % 2 == 1) rt.localScale = new Vector3(1, -1, 1);
            }
        }

        /// <summary>Points every <paramref name="step"/> pixels along a smooth curve through the given points (Catmull-Rom).</summary>
        static System.Collections.Generic.List<Vector2> PathPoints(System.Collections.Generic.List<Vector2> through, float step)
        {
            var points = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i + 1 < through.Count; i++)
            {
                Vector2 p0 = through[Mathf.Max(0, i - 1)], p1 = through[i], p2 = through[i + 1], p3 = through[Mathf.Min(through.Count - 1, i + 2)];
                int n = Mathf.Max(2, Mathf.CeilToInt((p2 - p1).magnitude / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3));
                }
            }
            return points;
        }

        // ---------- Home pieces ----------

        /// <summary>Game name on the left; what my whole club holds on the right.</summary>
        void TopBar()
        {
            var bar = UiKit.Empty(_root, "TopBar");
            UiKit.Height(bar, 100);
            var row = UiKit.Row(bar, 14);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            ArtLibrary.Icon(bar, ArtLibrary.Get("App/app_icon"), 84);
            var title = UiKit.Label(bar, GameTitle, 46);
            title.fontStyle = FontStyle.Bold;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.16f, 0.32f, 0.85f);
            outline.effectDistance = new Vector2(2, -2);
            UiKit.Width(title, 0, 1);
            Chip(bar, $"코인 {_club.Coins:N0}", new Color(1f, 0.82f, 0.3f));
            Chip(bar, $"명예 {_club.LegacyPoints:N0}", Gold);
            Chip(bar, $"회복권 {_club.Daily.RecoveryTickets}", new Color(0.75f, 0.62f, 1f));
        }

        static void Chip(Transform parent, string text, Color color)
        {
            var chip = UiKit.Panel(parent, "Chip", Scenery.Plate);
            UiKit.Width(chip, 200);
            UiKit.Height(chip, 64);
            var label = UiKit.Label(chip, text, 26, TextAnchor.MiddleCenter, color);
            label.fontStyle = FontStyle.Bold;
            UiKit.Stretch(label.rectTransform);
        }

        /// <summary>One building: its picture (or a drawn stand-in) with a name plate under it.</summary>
        void BuildingTile(RectTransform tile, Building b, bool big = false)
        {
            var button = tile.gameObject.AddComponent<Button>();
            var hit = tile.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            if (b.Open != null && !b.Soon)
            {
                button.onClick.AddListener(UiSound.Tap);
                button.onClick.AddListener(b.Open);
            }
            tile.gameObject.AddComponent<PressScale>();

            // Ground shadow first, then the building on it.
            var shadow = UiKit.Panel(tile, "Shadow", new Color(0, 0, 0, 0.3f));
            shadow.GetComponent<Image>().sprite = UiKit.CircleSprite;
            shadow.GetComponent<Image>().type = Image.Type.Simple;
            shadow.anchorMin = new Vector2(0.1f, 0.2f);
            shadow.anchorMax = new Vector2(0.9f, 0.3f);
            shadow.offsetMin = shadow.offsetMax = Vector2.zero;

            var art = ArtLibrary.Get("Buildings/" + b.Id);
            var house = UiKit.Empty(tile, "House");
            house.anchorMin = new Vector2(0.06f, 0.24f);
            house.anchorMax = new Vector2(0.94f, 0.98f);
            house.offsetMin = house.offsetMax = Vector2.zero;
            if (art != null)
            {
                var image = house.gameObject.AddComponent<Image>();
                image.sprite = art;
                image.preserveAspect = true;
                image.raycastTarget = false;
                if (b.Soon) image.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            }
            else DrawBuilding(house, b, big);

            // The name plate.
            var plate = UiKit.Panel(tile, "Plate", b.Soon ? new Color(0.2f, 0.22f, 0.26f, 0.9f) : new Color(0.1f, 0.17f, 0.32f, 0.9f));
            plate.anchorMin = new Vector2(0.02f, 0f);
            plate.anchorMax = new Vector2(0.98f, 0f);
            plate.pivot = new Vector2(0.5f, 0);
            plate.sizeDelta = new Vector2(0, big ? 130 : 110);
            plate.anchoredPosition = Vector2.zero;
            var name = UiKit.Label(plate, $"<b>{b.Name}</b>\n<size={(big ? 26 : 22)}><color=#BAC4D8>{b.Status}</color></size>", big ? 40 : 32,
                TextAnchor.MiddleCenter, b.Soon ? UiKit.Muted : Color.white);
            UiKit.Stretch(name.rectTransform);
            name.rectTransform.offsetMin = new Vector2(10, 0);
            name.rectTransform.offsetMax = new Vector2(-10, 0);

            if (b.Badge > 0)
            {
                var dot = UiKit.Panel(tile, "Badge", new Color(0.9f, 0.25f, 0.3f));
                dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
                dot.GetComponent<Image>().type = Image.Type.Simple;
                dot.anchorMin = dot.anchorMax = new Vector2(0.88f, 0.9f);
                dot.sizeDelta = new Vector2(56, 56);
                var n = UiKit.Label(dot, b.Badge.ToString(), 30, TextAnchor.MiddleCenter);
                n.fontStyle = FontStyle.Bold;
                UiKit.Stretch(n.rectTransform);
                UiAnim.Pulse(dot, 1.2f, 0.5f);
            }
            UiAnim.FadeIn(tile, 0.3f);
        }

        /// <summary>
        /// A stand-in building from boxes: body, roof, windows, a door and a flag. Each building
        /// keeps its own colours so they read apart until the pictures arrive.
        /// </summary>
        static void DrawBuilding(RectTransform house, Building b, bool big)
        {
            Color body = b.Soon ? UiKit.Shade(b.Body, 0.6f) : b.Body;
            Color roof = b.Soon ? UiKit.Shade(b.Roof, 0.6f) : b.Roof;

            Box(house, "Body", body, new Vector2(0.08f, 0f), new Vector2(0.92f, 0.62f));
            Box(house, "Roof", roof, new Vector2(0.02f, 0.58f), new Vector2(0.98f, 0.76f));
            Box(house, "Top", UiKit.Shade(roof, 0.85f), new Vector2(0.25f, 0.74f), new Vector2(0.75f, 0.86f));

            if (b.Soon)
            {
                // Scaffolding stripes and a sign, for a lot still being built.
                for (int i = 0; i < 4; i++)
                    Box(house, "Stripe", i % 2 == 0 ? new Color(0.95f, 0.75f, 0.2f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.9f),
                        new Vector2(0.08f + i * 0.21f, 0.22f), new Vector2(0.29f + i * 0.21f, 0.32f));
                var sign = UiKit.Label(house, "<b>공사 중</b>", big ? 40 : 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
                sign.rectTransform.anchorMin = new Vector2(0.1f, 0.36f);
                sign.rectTransform.anchorMax = new Vector2(0.9f, 0.56f);
                sign.rectTransform.offsetMin = sign.rectTransform.offsetMax = Vector2.zero;
                return;
            }

            // Two rows of lit windows, and a door in the middle.
            int columns = big ? 6 : 4;
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < columns; c++)
                {
                    float x0 = 0.14f + c * (0.72f / columns), x1 = x0 + 0.72f / columns * 0.62f;
                    float y0 = 0.32f - r * 0.18f + 0.1f, y1 = y0 + 0.12f;
                    if (r == 1 && (c == columns / 2 - 1 || c == columns / 2)) continue; // the door goes here
                    Box(house, "Window", new Color(1f, 0.92f, 0.6f, 0.9f), new Vector2(x0, y0), new Vector2(x1, y1));
                }
            Box(house, "Door", UiKit.Shade(body, 0.45f), new Vector2(0.42f, 0f), new Vector2(0.58f, 0.2f));

            // A flagpole with a pennant in the roof colour.
            Box(house, "Pole", new Color(0.85f, 0.85f, 0.88f), new Vector2(0.495f, 0.86f), new Vector2(0.505f, 1f));
            var flag = Box(house, "Flag", Gold, new Vector2(0.505f, 0.92f), new Vector2(0.6f, 0.99f));
            UiAnim.Pulse(flag, 1.08f, 1.2f);
        }

        static RectTransform Box(RectTransform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var box = UiKit.Panel(parent, name, color);
            box.anchorMin = min;
            box.anchorMax = max;
            box.offsetMin = box.offsetMax = Vector2.zero;
            box.GetComponent<Image>().raycastTarget = false;
            return box;
        }

        /// <summary>The training centre's door: pick a player (save slot) to continue, start or delete.</summary>
        void ShowPlayers(System.Collections.Generic.List<SlotSummary> slots)
        {
            CloseOverlay();
            _overlay = UiKit.Dim(_root.parent.parent);
            var card = UiKit.PopupCard(_overlay, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            UiKit.Column(card, 32, 16);
            var heading = UiKit.Label(card, "트레이닝 센터", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 80);
            UiKit.Height(UiKit.Label(card, "키울 선수를 골라요. 다른 선수는 그 자리에서 기다려요.", 26, TextAnchor.MiddleCenter, UiKit.Muted), 50);
            foreach (var summary in slots) PlayerRow(card, summary);
            if (!Premium.Owns(Premium.ExtraSlots))
                UiKit.Height(UiKit.Button(card, $"+ 슬롯 {Premium.ExtraSlotCount}개 더 (유료 테스트)", () => PremiumScreen.Open(_club, Build), new Color(0.55f, 0.4f, 0.8f, 0.7f)), 90);
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 100);
        }

        void PlayerRow(RectTransform parent, SlotSummary summary)
        {
            var career = summary.Career;
            Color team = career == null ? UiKit.Background : UiKit.TeamColor(career.MyTeam.Name);
            var row = UiKit.Panel(parent, $"Slot {summary.Slot}", UiKit.Shade(UiKit.Card, 1.3f));
            UiKit.Height(row, 170);
            var layout = UiKit.Row(row, 16);
            layout.padding = new RectOffset(0, 20, 0, 0);
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var face = UiKit.Panel(row, "Face", UiKit.Shade(team, 0.8f));
            UiKit.Width(face, 150);
            var art = career == null ? null : ArtLibrary.Portrait(career.Me);
            if (art != null)
            {
                var img = UiKit.Empty(face, "Picture").gameObject.AddComponent<Image>();
                img.sprite = art;
                img.preserveAspect = true;
                UiKit.Stretch(img.rectTransform);
            }
            else
            {
                var l = UiKit.Label(face, career == null ? "+" : career.Me.Name.Substring(0, 1), 64, TextAnchor.MiddleCenter);
                UiKit.Stretch(l.rectTransform);
            }

            string text;
            if (career == null) text = $"<size=24><color=#BAC4D8>슬롯 {summary.Slot}</color></size>\n<b>빈 자리</b>";
            else
            {
                var me = career.Me;
                string position = me.IsPitcher ? Positions.RoleName(me.Role) : Positions.Name(me.Position);
                string where = career.InMilitary ? "군 복무 중" : career.InFarm ? "2군" : career.MyTeam.Name;
                text = $"<size=24><color=#BAC4D8>슬롯 {summary.Slot}</color></size>\n<b>{me.Name}</b> <size=26>{position} · {me.Age}세</size>\n" +
                       $"<size=24><color=#BAC4D8>{where} · {career.Year} 시즌 {career.Day + 1}일차</color></size>";
            }
            var label = UiKit.Label(row, text, 36);
            UiKit.Width(label, 0, 1);

            int slot = summary.Slot;
            var play = UiKit.Button(row, career == null ? "새 선수" : "이어하기", () => Play(slot), career == null ? UiKit.Accent : new Color(0.2f, 0.65f, 0.45f));
            play.GetComponentInChildren<Text>().fontSize = 30;
            UiKit.Width(play, 190);
            UiKit.Height(play, 110);
            if (career != null)
            {
                var delete = UiKit.Button(row, "삭제", () => ConfirmDelete(slot, career.Me.Name), UiKit.Background);
                delete.GetComponentInChildren<Text>().fontSize = 24;
                UiKit.Width(delete, 100);
                UiKit.Height(delete, 110);
            }
        }

        static string Today => DailyMissions.DateOf(System.DateTime.Now);

        /// <summary>Today's missions from home. Rewards that are money need a player, so those wait for the growth screen.</summary>
        void ShowMissions() => ChallengeScreen.Open(_club, null, Build);

        void Play(int slot)
        {
            GrowthScreen.Open(slot);
            Destroy(gameObject);
        }

        void ConfirmDelete(int slot, string playerName)
        {
            CloseOverlay();
            _overlay = UiKit.Dim(_root.parent.parent);
            var card = UiKit.PopupCard(_overlay, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.7f));
            UiKit.Column(card, 40, 20);
            var heading = UiKit.Label(card, $"슬롯 {slot} 지우기", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);
            UiKit.Height(UiKit.Label(card, $"{playerName} 선수의 진행이 사라져요. 되돌릴 수 없어요.\n이미 은퇴해서 내 팀에 들어온 선수와 도감은 그대로예요.", 30, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "지우기", () =>
            {
                SaveStore.DeleteSlot(slot);
                CloseOverlay();
                Build();
            }, UiKit.Warning), 110);
            UiKit.Height(UiKit.Button(card, "취소", CloseOverlay, UiKit.Muted), 110);
        }

        void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }
    }
}
