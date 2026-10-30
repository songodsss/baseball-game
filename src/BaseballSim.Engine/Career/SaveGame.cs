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
        /// 20: daily missions and the album on the club; club and career can be saved apart (player slots);
        /// main story progress on the career and each retired player's ending on the club.
        public const int Version = 20;

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

        // ---------- Club and career apart (player slots) ----------

        /// <summary>The club alone, shared by every slot.</summary>
        public static byte[] ClubToBytes(Club club) => Wrap(w => WriteClub(w, club));

        public static Club ClubFromBytes(byte[] data) => Unwrap(data, (r, version) => ReadClub(r, version));

        /// <summary>One slot's career; the club it belongs to is saved separately.</summary>
        public static byte[] CareerToBytes(Career career, Club club) => Wrap(w => WriteCareer(w, career, club));

        public static Career CareerFromBytes(byte[] data, Club club, IRandom random) =>
            Unwrap(data, (r, version) => ReadCareer(r, random, club, version));

        static byte[] Wrap(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    w.Write(Encoding.ASCII.GetBytes(Magic));
                    w.Write(Version);
                    write(w);
                }
                return stream.ToArray();
            }
        }

        static T Unwrap<T>(byte[] data, Func<BinaryReader, int, T> read)
        {
            using (var stream = new MemoryStream(data))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(Magic.Length)) != Magic)
                    throw new InvalidDataException("Not a save file.");
                int version = r.ReadInt32();
                if (version > Version)
                    throw new InvalidDataException($"Save version {version} is newer than this game ({Version}).");
                return read(r, version);
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
            // Added in version 10.
            WriteList(w, club.Roster, p =>
            {
                w.Write(p.AllStars);
                w.Write(p.HallOfFameScore);
                w.Write(p.HallOfFame);
                w.Write(p.NumberRetiredBy ?? "");
                w.Write(p.Donation);
                w.Write(p.DonationPoints);
            });
            // Added in version 18.
            WriteList(w, club.Roster, p =>
            {
                w.Write((int)p.Position);
                w.Write(p.Number);
                w.Write(p.TeamNumber);
                w.Write(p.MainClub ?? "");
                WriteOptional(w, p.CareerPitching);
            });
            WriteList(w, club.ChallengesWon.ToList(), t => w.Write((int)t));
            w.Write(club.ChallengeWins);
            // Added in version 19.
            WriteList(w, club.Roster, p => w.Write((int)p.Role));
            // Added in version 20.
            var d = club.Daily;
            w.Write(d.Date);
            WriteList(w, d.Today, m =>
            {
                w.Write((int)m.Kind);
                w.Write(m.Goal);
                w.Write(m.Progress);
                w.Write(m.Claimed);
                w.Write((int)m.Reward);
                w.Write(m.Amount);
            });
            w.Write(d.LastCheckIn);
            w.Write(d.Streak);
            w.Write(d.TotalCheckIns);
            w.Write(d.RecoveryTickets);
            WriteList(w, club.LegendsMet.ToList(), w.Write);
            WriteList(w, club.RivalsMet, w.Write);
            WriteList(w, club.Roster, p => w.Write((int)p.Ending));
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
            if (version >= 10)
            {
                int i = 0;
                ReadList(r, () =>
                {
                    var p = club.Roster[i++];
                    p.AllStars = r.ReadInt32();
                    p.HallOfFameScore = r.ReadInt32();
                    p.HallOfFame = r.ReadBoolean();
                    var by = r.ReadString();
                    p.NumberRetiredBy = by.Length == 0 ? null : by;
                    p.Donation = r.ReadInt32();
                    p.DonationPoints = r.ReadInt32();
                    return p;
                });
            }
            if (version == 6)
            {
                ReadList(r, r.ReadString); // uniforms owned, since removed
                r.ReadString();            // uniform worn
            }
            if (version >= 18)
            {
                int i = 0;
                ReadList(r, () =>
                {
                    var p = club.Roster[i++];
                    p.Position = (Position)r.ReadInt32();
                    p.Number = r.ReadInt32();
                    p.TeamNumber = r.ReadInt32();
                    var main = r.ReadString();
                    p.MainClub = main.Length == 0 ? null : main;
                    p.CareerPitching = ReadOptional(r, version);
                    return p;
                });
                club.ChallengesWon = new HashSet<ChallengeTier>(ReadList(r, () => (ChallengeTier)r.ReadInt32()));
                club.ChallengeWins = r.ReadInt32();
            }
            if (version >= 19)
            {
                int i = 0;
                ReadList(r, () => club.Roster[i++].Role = (PitcherRole)r.ReadInt32());
            }
            if (version >= 20)
            {
                var d = club.Daily;
                d.Date = r.ReadString();
                d.Today = ReadList(r, () => new Mission
                {
                    Kind = (MissionKind)r.ReadInt32(),
                    Goal = r.ReadInt32(),
                    Progress = r.ReadInt32(),
                    Claimed = r.ReadBoolean(),
                    Reward = (RewardKind)r.ReadInt32(),
                    Amount = r.ReadInt32(),
                });
                d.LastCheckIn = r.ReadString();
                d.Streak = r.ReadInt32();
                d.TotalCheckIns = r.ReadInt32();
                d.RecoveryTickets = r.ReadInt32();
                club.LegendsMet = new HashSet<string>(ReadList(r, r.ReadString));
                club.RivalsMet = ReadList(r, r.ReadString);
                int e = 0;
                ReadList(r, () => club.Roster[e++].Ending = (StoryEnding)r.ReadInt32());
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

            // Added in version 9.
            w.Write(career.InFarm);
            WriteLine(w, career.FarmLine);
            w.Write(career.FarmDays);
            WriteLine(w, career.StintLine);
            w.Write(career.StintDays);
            w.Write(career.TradedThisSeason);
            WriteList(w, career.SeasonNotes, w.Write);
            w.Write(career.Released);
            WriteList(w, career.PendingOffers, o =>
            {
                w.Write(league.Teams.IndexOf(o.Team));
                w.Write(o.Salary);
                w.Write(o.Years);
                w.Write(o.Tryout);
            });
            WriteList(w, career.Me.History, h =>
            {
                WriteList(w, h.Notes, w.Write);
                w.Write(h.FarmDays);
            });

            // Added in version 10.
            w.Write(career.Me.AllStarSelections);
            w.Write(career.Me.AllStarMvps);

            // Added in version 11.
            w.Write(career.Me.ReadsRight);
            w.Write(career.DecideAtBats);

            // Added in version 12.
            WriteList(w, career.Me.Traits, w.Write);
            WriteStats(w, career.Me.MyStats.ToDictionary(s => s, s => career.Me.BreakthroughProgress(s)));
            WriteStats(w, career.Me.MyStats.ToDictionary(s => s, s => career.Me.BreakthroughCount(s)));

            // Added in version 13.
            w.Write(career.PendingSalary != null);
            if (career.PendingSalary != null)
            {
                w.Write(career.PendingSalary.OldSalary);
                w.Write(career.PendingSalary.Offer);
                w.Write(career.PendingSalary.Worth);
                w.Write(career.PendingSalary.Titles);
            }

            // Added in version 14.
            w.Write(career.Me.GoldMedals);

            // Added in version 15.
            w.Write(career.Goal != null);
            if (career.Goal != null)
            {
                w.Write((int)career.Goal.Kind);
                w.Write((int)career.Goal.Tier);
                w.Write(career.Goal.Target);
            }

            // Added in version 16.
            w.Write(career.Rival != null);
            if (career.Rival != null)
            {
                w.Write(career.Rival.Name);
                w.Write(career.Rival.Age);
                WriteStats(w, career.Rival.Potential);
            }
            WriteList(w, career.Me.History, h =>
            {
                w.Write(h.RivalLine != null);
                if (h.RivalLine != null) WriteLine(w, h.RivalLine);
            });

            // Added in version 17.
            w.Write(career.MilitaryDone);
            w.Write(career.InMilitary);
            WriteList(w, career.Me.History, h => w.Write(h.Military));

            // Added in version 18.
            var me = career.Me;
            w.Write((int)me.Position);
            w.Write(me.Number);
            w.Write((int)me.Injury);
            w.Write(me.LingerDays);
            w.Write(me.FanPoints);
            WritePitcherLine(w, career.SeasonPitching);
            WritePitcherLine(w, career.FarmPitching);
            WritePitcherLine(w, career.StintPitching);
            w.Write(career.DaysSinceStart);
            WriteList(w, me.History, h => WriteOptional(w, h.Pitching));
            var teams = career.League.Teams;
            WriteList(w, career.League.Pitching.Values.ToList(), p =>
            {
                w.Write(p.Id);
                w.Write(p.Name);
                w.Write(teams.IndexOf(p.Team));
                WritePitcherLine(w, p.Line);
            });

            // Added in version 19.
            w.Write((int)me.Role);

            // Added in version 20.
            var story = career.Story;
            WriteList(w, story.Seen, w.Write);
            foreach (var b in MainStory.AllBonds) w.Write(story.BondOf(b));
            WriteList(w, story.Flags, w.Write);
            WriteList(w, story.Log, w.Write);
            w.Write(story.LastSeason);
            w.Write(story.LastDay);
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
            if (version >= 9)
            {
                career.InFarm = r.ReadBoolean();
                career.FarmLine = ReadLine(r);
                career.FarmDays = r.ReadInt32();
                career.StintLine = ReadLine(r);
                career.StintDays = r.ReadInt32();
                career.TradedThisSeason = r.ReadBoolean();
                career.SeasonNotes = ReadList(r, r.ReadString);
                career.Released = r.ReadBoolean();
                career.PendingOffers = ReadList(r, () => new ContractOffer { Team = teams[r.ReadInt32()], Salary = r.ReadInt32(), Years = r.ReadInt32(), Tryout = r.ReadBoolean() });
                int j = 0;
                ReadList(r, () =>
                {
                    var h = me.History[j++];
                    h.Notes = ReadList(r, r.ReadString);
                    h.FarmDays = r.ReadInt32();
                    return h;
                });
            }
            if (version >= 10)
            {
                me.AllStarSelections = r.ReadInt32();
                me.AllStarMvps = r.ReadInt32();
            }
            if (version >= 11)
            {
                me.ReadsRight = r.ReadInt32();
                career.DecideAtBats = r.ReadBoolean();
            }
            if (version >= 12)
            {
                me.Traits = ReadList(r, r.ReadString);
                me.BreakthroughExp = ReadStats(r);
                me.Breakthroughs = ReadStats(r);
            }
            if (version >= 13 && r.ReadBoolean())
                career.PendingSalary = new SalaryTalk { OldSalary = r.ReadInt32(), Offer = r.ReadInt32(), Worth = r.ReadInt32(), Titles = r.ReadInt32() };
            if (version >= 14) me.GoldMedals = r.ReadInt32();
            if (version >= 15 && r.ReadBoolean())
                career.Goal = new SeasonGoal { Kind = (GoalKind)r.ReadInt32(), Tier = (GoalTier)r.ReadInt32(), Target = r.ReadDouble() };
            if (version >= 16)
            {
                if (r.ReadBoolean()) career.Rival = new RivalState { Name = r.ReadString(), Age = r.ReadInt32(), Potential = ReadStats(r) };
                int k = 0;
                ReadList(r, () =>
                {
                    var h = me.History[k++];
                    if (r.ReadBoolean()) h.RivalLine = ReadLine(r);
                    return h;
                });
            }
            if (version >= 17)
            {
                career.MilitaryDone = r.ReadBoolean();
                career.InMilitary = r.ReadBoolean();
                int m = 0;
                ReadList(r, () => me.History[m++].Military = r.ReadBoolean());
            }
            if (version >= 18)
            {
                me.Position = (Position)r.ReadInt32();
                me.Number = r.ReadInt32();
                me.Injury = (InjuryKind)r.ReadInt32();
                me.LingerDays = r.ReadInt32();
                me.FanPoints = r.ReadInt32();
                career.SeasonPitching = ReadPitcherLine(r, version);
                career.FarmPitching = ReadPitcherLine(r, version);
                career.StintPitching = ReadPitcherLine(r, version);
                career.DaysSinceStart = r.ReadInt32();
                int n = 0;
                ReadList(r, () => me.History[n++].Pitching = ReadOptional(r, version));
                foreach (var p in ReadList(r, () => new PitcherSeason { Id = r.ReadString(), Name = r.ReadString(), Team = teams[r.ReadInt32()], Line = ReadPitcherLine(r, version) }))
                    career.League.Pitching[p.Id] = p;
            }
            if (version >= 19) me.Role = (PitcherRole)r.ReadInt32();
            if (version >= 20)
            {
                var story = career.Story;
                story.Seen = ReadList(r, r.ReadString);
                foreach (var b in MainStory.AllBonds) story.Bonds[b] = r.ReadInt32();
                story.Flags = ReadList(r, r.ReadString);
                story.Log = ReadList(r, r.ReadString);
                story.LastSeason = r.ReadInt32();
                story.LastDay = r.ReadInt32();
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

        // Saves, holds and blown saves were added in version 19.
        static void WritePitcherLine(BinaryWriter w, PitcherLine l)
        {
            foreach (int v in new[] { l.Games, l.Starts, l.Wins, l.Losses, l.Outs, l.Hits, l.Runs, l.Walks, l.Strikeouts, l.HomeRuns, l.Shutouts, l.Saves, l.Holds, l.BlownSaves }) w.Write(v);
        }

        static PitcherLine ReadPitcherLine(BinaryReader r, int version)
        {
            var l = new PitcherLine
            {
                Games = r.ReadInt32(), Starts = r.ReadInt32(), Wins = r.ReadInt32(), Losses = r.ReadInt32(), Outs = r.ReadInt32(), Hits = r.ReadInt32(),
                Runs = r.ReadInt32(), Walks = r.ReadInt32(), Strikeouts = r.ReadInt32(), HomeRuns = r.ReadInt32(), Shutouts = r.ReadInt32(),
            };
            if (version >= 19) { l.Saves = r.ReadInt32(); l.Holds = r.ReadInt32(); l.BlownSaves = r.ReadInt32(); }
            return l;
        }

        static void WriteOptional(BinaryWriter w, PitcherLine l)
        {
            w.Write(l != null);
            if (l != null) WritePitcherLine(w, l);
        }

        static PitcherLine ReadOptional(BinaryReader r, int version) => r.ReadBoolean() ? ReadPitcherLine(r, version) : null;

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
