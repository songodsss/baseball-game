using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class DialogueTests
{
    static CareerState NewCareer(StoryRoute route = StoryRoute.LastPick)
    {
        var random = new SeededRandom(3);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random, 0, Position.CenterField, 7, PitcherRole.Starter), league, league.Teams[0], random);
        career.Story.Route = route;
        return career;
    }

    [Fact]
    public void NarrationAndSpeechAreSplitWithTheSpeakerFromTheLineBefore()
    {
        var c = NewCareer();
        var lines = Dialogue.Lines("숙소 옥상. 도현이 캔커피 두 개를 들고 올라왔다.\n\"너 요즘 얼굴 장난 아니다.\"\n\n한참 말이 없다가, 도현이 툭 던졌다.\n\"너만 그런 거 아니야.\"", c);
        Assert.Equal(4, lines.Count);
        Assert.Equal(SpeakerKind.None, lines[0].Kind);
        Assert.Equal("윤도현", lines[1].Speaker);
        Assert.Equal(SpeakerKind.Player, lines[1].Kind);
        Assert.Equal("\"너만 그런 거 아니야.\"", lines[3].Text);
        Assert.Equal("윤도현", lines[3].Speaker);
    }

    [Fact]
    public void ASubjectAfterTheQuoteSpeaksAndRidesAlongAsAnAside()
    {
        var c = NewCareer();
        var line = Dialogue.Lines("\"왔냐.\" 최강훈이었다.\n\"내일 새벽 5시. 늦으면 그냥 집에 가라.\"", c);
        Assert.Equal(2, line.Count);
        Assert.Equal("최강훈", line[0].Speaker);
        Assert.Equal("최강훈이었다.", line[0].Aside);
        Assert.Equal("최강훈", line[1].Speaker); // nobody else spoke in between
    }

    [Fact]
    public void LegendsSelfIntroductionsAndQuotedPhrasesAreTold()
    {
        var c = NewCareer();
        var legend = Dialogue.Lines("장태산이 또 왔다.\n\"벌써 반이야, 반!\"", c)[1];
        Assert.Equal(SpeakerKind.Legend, legend.Kind);
        Assert.Equal("jang", legend.PortraitKey);

        var intro = Dialogue.Lines("수첩을 든 기자가 다가왔다.\n\"스포츠 데일리 한소라입니다. 한마디만 부탁드려요!\"", c)[1];
        Assert.Equal("한소라", intro.Speaker);

        // A quote glued to a particle is part of the narration, not a spoken line.
        var phrase = Dialogue.Lines("\"내 기록 깨는 놈 보면 은퇴식 한 번 더 하겠다\"는 게 입버릇.", c);
        Assert.Single(phrase);
        Assert.Equal(SpeakerKind.None, phrase[0].Kind);
    }

    [Fact]
    public void AnAnswerFallsBackToWhoeverWasTalkingButPoliteLinesStayUnnamed()
    {
        var c = NewCareer();
        string scene = "구단 버스 앞에서 한 사내가 손을 내밀었다. 2군 코치 최강훈.\n\"너 뽑자고 우긴 사람이 나다.\"";
        Assert.Equal("최강훈", Dialogue.Lines("\"고3 여름, 혼자 남아서 배트 돌리던 놈. 그게 너였다.\"", c, scene + "\n\n\"왜 저를 뽑으셨어요?\"")[0].Speaker);
        var polite = Dialogue.Lines("\"야구만 하겠습니다.\"", c, scene)[0];
        Assert.Equal(SpeakerKind.Unknown, polite.Kind);
        Assert.Null(polite.Speaker);

        var mine = Dialogue.MyChoice("\"반드시 증명하겠습니다\"", c);
        Assert.Equal(SpeakerKind.Me, mine.Kind);
        Assert.Null(Dialogue.MyChoice("내기를 받는다", c));
    }

    [Fact]
    public void EveryEventTextSplitsIntoNonEmptyLines()
    {
        foreach (var route in MainStory.AllRoutes)
        {
            var c = NewCareer(route);
            var texts = new List<string>();
            foreach (var slot in MainStory.SceneIds)
                for (int v = 0; v < MainStory.VariantCount(c, slot); v++)
                {
                    var ev = MainStory.Preview(c, slot, v);
                    texts.Add(ev.TextFor(c));
                    texts.AddRange(ev.Choices.Select(ch => StoryEvent.Fill(ch.Result, c)));
                }
            foreach (var ev in StoryEvents.All)
            {
                texts.Add(ev.TextFor(c));
                texts.AddRange(ev.Choices.Select(ch => StoryEvent.Fill(ch.Result, c)));
            }
            foreach (var text in texts.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var lines = Dialogue.Lines(text, c);
                Assert.NotEmpty(lines);
                Assert.All(lines, l => Assert.False(string.IsNullOrWhiteSpace(l.Text)));
                Assert.All(lines.Where(l => l.Speaker != null), l => Assert.True(l.IsSpeech));
            }
        }
    }
}
