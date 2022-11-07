using System;

namespace MiX
{
    public class ChartParser
    {
        public Chart chart;
        private Xmk xmk;

        public ChartParser(string path)
        {
            chart = new Chart(path);
            xmk = new Xmk(true);
        }

        private byte chartNoteValues(Chart.Event.Difficulty diff, byte note)
        {
            Dictionary<Chart.Event.Difficulty,Dictionary<byte,byte>> values = new Dictionary<Chart.Event.Difficulty, Dictionary<byte, byte>>()
            {
                {
                    Chart.Event.Difficulty.EASY, new Dictionary<byte,byte>() 
                    { {0, 6}, {1, 8}, {2, 10}, {3, 5}, {4, 7}, {7, 15}, {8, 9} }
                },
                {
                    Chart.Event.Difficulty.MEDIUM, new Dictionary<byte, byte>()
                    { {0, 24}, {1, 26}, {2, 28}, {3, 23}, {4, 25}, {7, 33}, {8, 27} }
                },
                {
                    Chart.Event.Difficulty.HARD, new Dictionary<byte, byte>()
                    { {0, 42}, {1, 44}, {2, 46}, {3, 41}, {4, 43}, {7, 51}, {8, 45} }
                },
                {
                    Chart.Event.Difficulty.EXPERT, new Dictionary<byte, byte>()
                    { {0, 60}, {1, 62}, {2, 64}, {3, 59}, {4, 61}, {7, 69}, {8, 63} }
                }
            };
            return values[diff][note];
        }

        private int ticksToMeasures(UInt32 ticks)
        {
            int prevNum = chart.timeSignatures.First().numerator;
            int prevDen = chart.timeSignatures.First().denominator;
            uint prevTicks = 0;
            int measure = 1;

            for (int i = 1; i < chart.timeSignatures.Count; i++)
            {
                Chart.TimeSignature ts = chart.timeSignatures[i];
                if (ts.ticks > ticks) break;

                int newMeasures = (int)Math.Round((float)(ts.ticks-prevTicks)/prevNum*prevDen/(chart.resolution*4), MidpointRounding.AwayFromZero);
                measure += newMeasures == 0 ? 1 : newMeasures;

                prevNum = ts.numerator;
                prevDen = ts.denominator;
                prevTicks = ts.ticks;
            }
            return measure;
        }

        private double ticksToSeconds(UInt32 ticks)
        {
            float seconds = 0.0f;
            float tempo = chart.tempos.First().beatsPerMinute;
            UInt32 previous = 0;
            List<Chart.Tempo> tempos = chart.tempos.FindAll(x => x.ticks < ticks);
            foreach (Chart.Tempo t in tempos)
            {
                seconds += (t.ticks-previous) * (60 / (tempo * chart.resolution));
                previous = t.ticks;
                tempo = t.beatsPerMinute;
            }
            return seconds + (ticks-previous) * (60 / (tempo * chart.resolution));
        }

        public Xmk ParseChart()
        {
            foreach (Chart.Tempo tempo in chart.tempos)
            {
                Xmk.Tempo t = new Xmk.Tempo();
                t.ticks = (uint)tempo.ticks*(960/(uint)chart.resolution);
                t.start = (float)ticksToSeconds(tempo.ticks);
                t.microSecondsPerQuarter = tempo.microSecondsPerQuarter();
                xmk.tempos.Add(t);
            }

            foreach (Chart.TimeSignature timeSig in chart.timeSignatures)
            {
                Xmk.TimeSignature ts = new Xmk.TimeSignature();
                ts.ticks = (uint)timeSig.ticks*(960/(uint)chart.resolution);
                ts.measure = ticksToMeasures(timeSig.ticks);
                ts.numerator = timeSig.numerator;
                ts.denominator = timeSig.denominator;
                xmk.timeSignatures.Add(ts);
            }

            foreach (Chart.Event e in chart.events)
            {
                switch (e.note)
                {
                    case <9:
                        Xmk.Event xmkEvent = new Xmk.Event();
                        xmkEvent.note = chartNoteValues(e.difficulty, e.note);
                        xmkEvent.type = (byte)xmk.typeGroup(xmkEvent.note);
                        xmkEvent.type |= e.length() > (chart.resolution/4) ? (byte)Xmk.Event.sustain : (byte)0;
                        xmkEvent.type |= e.isHammer ? (byte)Xmk.Event.hammer : (byte)0;
                        xmkEvent.start = (float)ticksToSeconds((uint)e.timeStart);
                        xmkEvent.end = (float)ticksToSeconds((uint)e.timeEnd);
                        xmkEvent.offset = 100;
                        xmkEvent.timeStart = e.timeStart * (960 / chart.resolution);
                        xmkEvent.timeEnd = e.timeEnd * (960 / chart.resolution);
                        xmkEvent.fileGroups.Add(Xmk.Event.FileGroup.GUITAR_3X2);
                        xmkEvent.indexGroup = Xmk.Event.IndexGroup.NOTE;
                        xmk.events.Add(xmkEvent);
                        break;
                    
                    case 69:
                        foreach (byte b in new byte[] {20, 38, 56, 74})
                        {
                            Xmk.Event xmkHPEvent = new Xmk.Event();
                            xmkHPEvent.note = b;
                            xmkHPEvent.type = (byte)xmk.typeGroup(xmkHPEvent.note);
                            xmkHPEvent.start = (float)ticksToSeconds((uint)e.timeStart);
                            xmkHPEvent.end = (float)ticksToSeconds((uint)e.timeEnd);
                            xmkHPEvent.offset = 100;
                            xmkHPEvent.timeStart = e.timeStart * (960 / chart.resolution);
                            xmkHPEvent.timeEnd = e.timeEnd * (960 / chart.resolution);
                            xmkHPEvent.fileGroups.Add(Xmk.Event.FileGroup.GUITAR_3X2);
                            xmkHPEvent.indexGroup = Xmk.Event.IndexGroup.HERO_POWER;
                            xmk.events.Add(xmkHPEvent);
                        }
                        break;
                    
                    case 70:
                        Xmk.Event s = new Xmk.Event();
                        s.type = 4;
                        s.note = 128;
                        s.start = (float)ticksToSeconds((uint)e.timeStart);
                        s.end = s.start;
                        s.unk2 = Xmk.unk2;
                        s.timeStart = e.timeStart * (960 / chart.resolution);
                        s.timeEnd = e.timeEnd * (960 / chart.resolution);
                        s.fileGroups.Add(Xmk.Event.FileGroup.CONTROL);
                        s.fileGroups.Add(Xmk.Event.FileGroup.GUITAR_3X2);
                        s.indexGroup = Xmk.Event.IndexGroup.SECTION;
                        xmk.info.sections.Add(e.name);
                        xmk.events.Add(s);
                        break;
                }
            }
            return xmk;
        }
    }
}