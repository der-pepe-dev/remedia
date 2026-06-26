namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class SubtitleCleanupServiceTests
{
    [Fact]
    public void StripSdh_RemovesSquareBrackets()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "[music] Hello there"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "Goodbye [door closes]"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripSdh(cues);

        Assert.Equal("Hello there", result[0].Text.Trim());
        Assert.Equal("Goodbye", result[1].Text.Trim());
    }

    [Fact]
    public void StripSdh_RemovesRoundBrackets()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "(sighs) What now?"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripSdh(cues);

        Assert.Equal("What now?", result[0].Text.Trim());
    }

    [Fact]
    public void StripSdh_RemovesSdhOnlyCue_LeavesEmptyText()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "[music playing]"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripSdh(cues);

        Assert.True(string.IsNullOrWhiteSpace(result[0].Text));
    }

    [Fact]
    public void StripHtmlTags_RemovesItalicAndBold()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "<i>Whispered</i> hello"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "<b>Bold</b> text"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripHtmlTags(cues);

        Assert.Equal("Whispered hello", result[0].Text);
        Assert.Equal("Bold text", result[1].Text);
    }

    [Fact]
    public void StripHtmlTags_RemovesFontTags()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "<font color=\"#ff0000\">Red</font> text"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripHtmlTags(cues);

        Assert.Equal("Red text", result[0].Text);
    }

    [Fact]
    public void FixOverlaps_TrimsOverlappingEnd()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "First"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7), "Second"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.FixOverlaps(cues);

        Assert.Equal(TimeSpan.FromSeconds(3) - TimeSpan.FromMilliseconds(1), result[0].End);
        Assert.Equal(TimeSpan.FromSeconds(3), result[1].Start);
    }

    [Fact]
    public void FixOverlaps_NoOverlap_NoChange()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), "First"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "Second"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.FixOverlaps(cues);

        Assert.Equal(TimeSpan.FromSeconds(2), result[0].End);
    }

    [Fact]
    public void RemoveEmpty_DropsWhitespaceOnlyCues()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "Keep this"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), "   "),
            new(3, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8), "Also keep"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.RemoveEmpty(cues);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Index);
        Assert.Equal(2, result[1].Index);
    }

    [Fact]
    public void CleanAll_CombinesAllOperations()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "<i>[music]</i>"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7), "(laughs) <b>Hello!</b>"),
            new(3, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), "Normal text"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.CleanAll(cues);

        Assert.Equal(2, result.Count);
        Assert.Equal("Hello!", result[0].Text.Trim());
        Assert.Equal("Normal text", result[1].Text);
        Assert.True(result[0].End <= result[1].Start);
    }

    [Fact]
    public void CleanAll_EmptyInput_ReturnsEmpty()
    {
        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.CleanAll([]);

        Assert.Empty(result);
    }

    [Fact]
    public void StripSdh_PreservesNormalBracketsInDialogue()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.Zero, TimeSpan.FromSeconds(2), "He said [quietly] goodbye"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.StripSdh(cues);

        Assert.DoesNotContain("[", result[0].Text);
        Assert.Contains("goodbye", result[0].Text);
    }

    [Fact]
    public void FixOverlaps_MultipleOverlaps_FixesAll()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "A"),
            new(2, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(8), "B"),
            new(3, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(10), "C"),
        ];

        IReadOnlyList<SubtitleCue> result = SubtitleCleanupService.FixOverlaps(cues);

        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].End <= result[i + 1].Start);
        }
    }
}
