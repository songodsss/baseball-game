using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BaseballSim.Career
{
    /// <summary>
    /// Saves and loads everything a player would lose by closing the app: the club with its retired
    /// players, and the career in progress with its league. Plain binary with a version number, so
    /// it needs no JSON library in Unity and later versions can still read old saves.
    /// The random generator isn't saved; a loaded career continues with a fresh one.
    /// </summary>
    public static class SaveGame
    {
        const string Magic = "BBSV";
        /// <summary>
        /// 2: morale, slump streak, coach slot. 3: facilities. 4: titles, achievements, league batting.
        /// 5: legends met; the coach slot is no longer written. 6: uniforms. 7: uniforms dropped again.
        /// </summary>
        public const int Version = 8;

        public static byte[] ToBytes(Club club, Career career)
        {
            using (var stream = new MemoryStream())
            {
                Write(stream, club, career);
                return stream.ToArray();
            }
        }

        public static (Club Club, Career Career) FromBytes(byte[] data, IRandom random)
        {
            using (var stream = new MemoryStream(data))
                return Read(stream, random);
        }

        /// <summary>Writes the club and, if one is under way, the career (null for none).</summary>
        public static void Write(Stream stream, Club club, Career career)
        {
            using (var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                w.Write(Encoding.ASCII.GetBytes(Magic));
                w.Write(Version);
                WriteClub(w, club);
                w.Write(career != null);
                if (career != null) WriteCareer(w, career, club);
            }
        }

        /// <exception cref="InvalidDataException">Not a save file, or from a newer version of the game.</exception>
        public static (Club Club, Career Career) Read(Stream stream, IRandom random)
        {
            using (var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(Magic.Length)) != Magic)
                    throw new InvalidDataException("Not a save file.");
                int version = r.ReadInt32();
                if (version > Version)
                    throw new InvalidDataException($"Save version {version} is newer than this game ({Version}).");

                var club = ReadClub(r, version);
                var career = r.ReadBoolean() ? ReadCareer(r, random, club, version) : null;
                return (club, career);
            }
        }

        // ---------- Club ----------

        static void WriteClub(BinaryWriter w, Club club)
        {
            w.Write(club.LegacyPoints);
            WriteList(w, club.Roster, p =>
            {
                w.Write(p.Name);
                w.Write(p.Seasons);
                WriteLine(w, p.CareerLine);
                WriteStats(w, p.PeakRatings);
                w.Write(p.LegacyPoints);
            });
            // Added in version 4.
            WriteList(w, club.Roster, p =>
            {
                WriteList(w, p.Achievements, w.Write);
                WriteList(w, p.Titles, w.Write);
            });
        }

        static Club ReadClub(BinaryReader r, int version)
        {
            var club = new Club { LegacyPoints = r.ReadInt32() };
            club.Roster = ReadList(r, () => new RetiredPlayer
            {
                Name = r.ReadString(),
                Seasons = r.ReadInt32(),
                CareerLine = ReadLine(r),
                PeakRatings = ReadStats(r),
                LegacyPoints = r.ReadInt32(),
            });
            if (version >= 4)
            {
                int i = 0;
                ReadList(r, () =>
                {
                    var p = club.Roster[i++];
                    p.Achievements = ReadList(r, r.ReadString);
                    p.Titles = ReadList(r, r.ReadString);
                    return p;
                });
            }
            if (version == 6)
            {
                ReadList(r, r.ReadString); // uniforms owned, since removed
                r.ReadString();            // uniform worn
            }
            return club;
        }

        // ---------- Career ----------

        static void WriteCareer(BinaryWriter w, Career career, Club club)
        {
            w.Write(career.Season);
            w.Write(career.Day);
            w.Write(career.LineupSlot);
            WriteLine(w, career.SeasonLine);
            WriteMe(w, career.Me);
            w.Write(career.HitlessStreak);

            var league = career.League;
            w.Write(league.GamesPerSeason);
            WriteList(w, league.Teams, team =>
            {
                w.Write(team.Name);
                WriteList(w, team.Lineup, p => WritePlayer(w, p));
                WriteList(w, team.Rotation, p => WritePlayer(w, p));
                WriteList(w, team.Bullpen, p => WritePlayer(w, p));
                var s = league.Standings[team];
                w.Write(s.Wins);
                w.Write(s.Losses);
                w.Write(s.Ties);
                w.Write(league.StartsMade(team));
            });
            w.Write(league.Teams.IndexOf(career.MyTeam));

            // Added in version 4.
            w.Write(career.InjuredThisSeason);
            WriteList(w, career.Me.Achievements, w.Write);
            WriteList(w, career.Me.History, h =>
            {
                WriteList(w, h.Titles, w.Write);
                w.Write(h.InjuryFree);
            });
            WriteList(w, league.Batting.Values.ToList(), b =>
            {
                w.Write(b.Id);
                w.Write(b.Name);
                w.Write(league.Teams.IndexOf(b.Team));
                WriteLine(w, b.Line);
            });

            // Added in version 5.
            WriteList(w, career.Me.MetLegends, w.Write);

            // Added in version 8.
            w.Write(career.ContractSeasonsLeft);
            WriteList(w, career.Me.Clubs, w.Write);
            WriteList(w, career.Me.History, h => w.Write(h.Club ?? ""));
        }

        static Career ReadCareer(BinaryReader r, IRandom random, Club club, int version)
        {
            int season = r.ReadInt32();
            int day = r.ReadInt32();
            int lineupSlot = r.ReadInt32();
            var seasonLine = ReadLine(r);
            var me = ReadMe(r, version);
            int hitlessStreak = version >= 2 ? r.ReadInt32() : 0;
            if (version >= 2 && version < 5) r.ReadInt32(); // coach, since removed

            int games = r.ReadInt32();
            var standings = new List<(int Wins, int Losses, int Ties, int Starts)>();
            var teams = ReadList(r, () =>
            {
                var team = new Team(r.ReadString())
                {
                    Lineup = ReadList(r, () => ReadPlayer(r)),
                    Rotation = ReadList(r, () => ReadPlayer(r)),
                    Bullpen = ReadList(r, () => ReadPlayer(r)),
                };
                standings.Add((r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32()));
                return team;
            });

            var league = new League(teams, games);
            for (int i = 0; i < teams.Count; i++)
            {
                var s = league.Standings[teams[i]];
                (s.Wins, s.Losses, s.Ties, _) = standings[i];
                league.SetStartsMade(teams[i], standings[i].Starts);
            }

            var career = new Career(me, league, teams[r.ReadInt32()], random)
            {
                Season = season,
                Day = day,
                LineupSlot = lineupSlot,
                SeasonLine = seasonLine,
                HitlessStreak = hitlessStreak,
            };

            if (version >= 4)
            {
                career.InjuredThisSeason = r.ReadBoolean();
                me.Achievements = ReadList(r, r.ReadString);
                int i = 0;
                ReadList(r, () =>
                {
                    var h = me.History[i++];
                    h.Titles = ReadList(r, r.ReadString);
                    h.InjuryFree = r.ReadBoolean();
                    return h;
                });
                foreach (var b in ReadList(r, () => new BatterSeason { Id = r.ReadString(), Name = r.ReadString(), Team = teams[r.ReadInt32()], Line = ReadLine(r) }))
                    league.Batting[b.Id] = b;
            }
            if (version >= 5) me.MetLegends = ReadList(r, r.ReadString);
            if (version >= 8)
            {
                career.ContractSeasonsLeft = r.ReadInt32();
                me.Clubs = ReadList(r, r.ReadString);
                int i = 0;
                ReadList(r, () =>
                {
                    var club = r.ReadString();
                    me.History[i++].Club = club.Length == 0 ? null : club;
                    return club;
                });
            }
            return career;
        }

        static void WriteMe(BinaryWriter w, CareerPlayer me)
        {
            w.Write(me.Id);
            w.Write(me.Name);
            w.Write(me.Age);
            WriteStats(w, me.Ratings);
            WriteStats(w, me.Potential);
            WriteStats(w, me.Exp);
            WriteStats(w, me.PeakRatings);
            w.Write(me.Fatigue);
            w.Write(me.InjuryDaysLeft);
            w.Write(me.Money);
            w.Write(me.Salary);
            w.Write(me.Morale);
            WriteList(w, me.Facilities.ToList(), f => { w.Write((int)f.Key); w.Write(f.Value); });
            WriteList(w, me.Owned, e => w.Write(e.Id));
            WriteList(w, me.Equipped.Values.ToList(), e => w.Write(e.Id));
            WriteList(w, me.History, h =>
            {
                w.Write(h.Season);
                w.Write(h.Age);
                w.Write(h.Games);
                WriteLine(w, h.Line);
                WriteStats(w, h.Ratings);
                w.Write(h.Salary);
            });
        }

        static CareerPlayer ReadMe(BinaryReader r, int version)
        {
            var me = new CareerPlayer
            {
                Id = r.ReadString(),
                Name = r.ReadString(),
                Age = r.ReadInt32(),
                Ratings = ReadStats(r),
                Potential = ReadStats(r),
                Exp = ReadStats(r),
                PeakRatings = ReadStats(r),
                Fatigue = r.ReadInt32(),
                InjuryDaysLeft = r.ReadInt32(),
                Money = r.ReadInt32(),
                Salary = r.ReadInt32(),
            };
            if (version >= 2) me.Morale = r.ReadInt32();
            if (version >= 3) me.Facilities = ReadList(r, () => ((Facility)r.ReadInt32(), r.ReadInt32())).ToDictionary(f => f.Item1, f => f.Item2);
            // Gear is saved by id; anything since removed from the catalog is dropped.
            me.Owned = ReadList(r, () => EquipmentCatalog.Find(r.ReadString())).Where(e => e != null).ToList();
            foreach (var e in ReadList(r, () => EquipmentCatalog.Find(r.ReadString())))
                if (e != null) me.Equipped[e.Slot] = e;
            me.History = ReadList(r, () => new SeasonRecord
            {
                Season = r.ReadInt32(),
                Age = r.ReadInt32(),
                Games = r.ReadInt32(),
                Line = ReadLine(r),
                Ratings = ReadStats(r),
                Salary = r.ReadInt32(),
            });
            return me;
        }

        // ---------- Pieces ----------

        static void WritePlayer(BinaryWriter w, Player p)
        {
            w.Write(p.Id);
            w.Write(p.Name);
            var b = p.Batting;
            w.Write(b.Contact); w.Write(b.Power); w.Write(b.Eye); w.Write(b.Speed); w.Write(b.Defense);
            w.Write(p.IsPitcher);
            if (p.IsPitcher)
            {
                var pi = p.Pitching;
                w.Write(pi.Velocity); w.Write(pi.Control); w.Write(pi.Breaking); w.Write(pi.Stamina);
            }
            w.Write(p.Condition);
        }

        static Player ReadPlayer(BinaryReader r)
        {
            var p = new Player(r.ReadString(), r.ReadString());
            p.Batting = new BattingRatings
            {
                Contact = r.ReadInt32(), Power = r.ReadInt32(), Eye = r.ReadInt32(), Speed = r.ReadInt32(), Defense = r.ReadInt32(),
            };
            if (r.ReadBoolean())
                p.Pitching = new PitchingRatings
                {
                    Velocity = r.ReadInt32(), Control = r.ReadInt32(), Breaking = r.ReadInt32(), Stamina = r.ReadInt32(),
                };
            p.Condition = r.ReadInt32();
            return p;
        }

        static void WriteLine(BinaryWriter w, BattingLine l)
        {
            foreach (int v in new[] { l.PlateAppearances, l.AtBats, l.Hits, l.Doubles, l.Triples, l.HomeRuns, l.Walks, l.Strikeouts, l.Runs, l.RunsBattedIn })
                w.Write(v);
        }

        static BattingLine ReadLine(BinaryReader r) => new BattingLine
        {
            PlateAppearances = r.ReadInt32(), AtBats = r.ReadInt32(), Hits = r.ReadInt32(), Doubles = r.ReadInt32(), Triples = r.ReadInt32(),
            HomeRuns = r.ReadInt32(), Walks = r.ReadInt32(), Strikeouts = r.ReadInt32(), Runs = r.ReadInt32(), RunsBattedIn = r.ReadInt32(),
        };

        static void WriteStats(BinaryWriter w, Dictionary<Stat, int> stats) =>
            WriteList(w, stats.ToList(), pair => { w.Write((int)pair.Key); w.Write(pair.Value); });

        static Dictionary<Stat, int> ReadStats(BinaryReader r) =>
            ReadList(r, () => ((Stat)r.ReadInt32(), r.ReadInt32())).ToDictionary(p => p.Item1, p => p.Item2);

        static void WriteList<T>(BinaryWriter w, List<T> items, Action<T> write)
        {
            w.Write(items.Count);
            foreach (var item in items) write(item);
        }

        static List<T> ReadList<T>(BinaryReader r, Func<T> read)
        {
            int count = r.ReadInt32();
            var list = new List<T>(count);
            for (int i = 0; i < count; i++) list.Add(read());
            return list;
        }
    }
}
