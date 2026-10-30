using Funkin.Data.Song;
using Funkin.Play;
using Funkin.Util;
using Xunit;

namespace Funkin.Tests;

public class ConductorTests
{
    private static Conductor MakeConductor(float bpm = 120f, int num = 4, int den = 4)
    {
        var conductor = new Conductor();
        conductor.MapTimeChanges(new[] { new SongTimeChange(0, bpm, 0, num, den) });
        return conductor;
    }

    [Fact]
    public void CrochetMatchesBpm()
    {
        var conductor = MakeConductor(120f);
        Assert.Equal(500f, conductor.BeatLengthMs, 3);
        Assert.Equal(125f, conductor.StepLengthMs, 3);
        Assert.Equal(2000f, conductor.MeasureLengthMs, 3);
    }

    [Fact]
    public void StepAndBeatCountersAdvanceWithTime()
    {
        var conductor = MakeConductor(120f);
        conductor.Update(0, applyOffsets: false);
        Assert.Equal(0, conductor.CurrentStep);

        conductor.Update(500, applyOffsets: false);
        Assert.Equal(4, conductor.CurrentStep);
        Assert.Equal(1, conductor.CurrentBeat);

        conductor.Update(2000, applyOffsets: false);
        Assert.Equal(16, conductor.CurrentStep);
        Assert.Equal(4, conductor.CurrentBeat);
        Assert.Equal(1, conductor.CurrentMeasure);
    }

    [Fact]
    public void SignalsFireOnBeatAndStepChanges()
    {
        var conductor = MakeConductor(120f);
        int steps = 0, beats = 0, measures = 0;
        conductor.OnStepHit.Add(() => steps++);
        conductor.OnBeatHit.Add(() => beats++);
        conductor.OnMeasureHit.Add(() => measures++);

        conductor.Update(0, applyOffsets: false);
        steps = 0;
        beats = 0;
        measures = 0;

        for (int i = 1; i <= 16; i++)
        {
            conductor.Update(i * 125, applyOffsets: false);
        }

        Assert.Equal(16, steps);
        Assert.Equal(4, beats);
        Assert.Equal(1, measures);
    }

    [Fact]
    public void TimeAndStepConversionsRoundTrip()
    {
        var conductor = MakeConductor(150f);
        // 150 BPM: one beat is 400ms, so 1000ms is 2.5 beats = 10 steps.
        Assert.Equal(10f, conductor.GetTimeInSteps(1000), 2);
        Assert.Equal(1000f, conductor.GetStepTimeInMs(10), 2);
        Assert.Equal(2000f, conductor.GetStepTimeInMs(20), 2);
    }

    [Fact]
    public void MultipleTimeChangesAreRespected()
    {
        var conductor = new Conductor();
        conductor.MapTimeChanges(new[]
        {
            new SongTimeChange(0, 120f, 0),
            new SongTimeChange(2000f, 240f, 4),
        });

        // At 120bpm, 2000ms is exactly 4 beats = 16 steps.
        Assert.Equal(16f, conductor.GetTimeInSteps(2000), 2);
        // After the change, a beat is 250ms, so 2250ms is beat 5 = step 20.
        Assert.Equal(20f, conductor.GetTimeInSteps(2250), 2);
        Assert.Equal(2250f, conductor.GetStepTimeInMs(20), 2);
    }

    [Fact]
    public void TimeSignatureAffectsMeasureLength()
    {
        var conductor = MakeConductor(120f, 3, 4);
        Assert.Equal(1500f, conductor.MeasureLengthMs, 3);
        Assert.Equal(12, conductor.StepsPerMeasure);
    }
}
