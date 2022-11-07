using System.IO;

namespace MiX
{
    public class Chart
    {
        public class Event
        {
            public enum Difficulty { EASY, MEDIUM, HARD, EXPERT };
            public long timeStart;
            public long timeEnd;
            public byte note;
            public bool isHammer;
            public int length() => (int)(this.timeEnd - this.timeStart);
            public Difficulty difficulty;
            public override string ToString()
            {
                return $"{this.timeStart} {this.note} {this.isHammer}";
            }
        }

        public class Tempo
        {
            public UInt32 ticks;
            public float beatsPerMinute;
            public UInt32 microSecondsPerQuarter() => (UInt32)(60000000.0d / this.beatsPerMinute);
            public override string ToString()
            {
                return $"{this.ticks} {this.beatsPerMinute} {this.microSecondsPerQuarter()}";
            }
        }

        public class TimeSignature
        {
            public UInt32 ticks;
            public int numerator;
            public int denominator;
            public override string ToString()
            {
                return $"{this.ticks} {this.numerator}";
            }
        }

        public short resolution;
        public List<Event> events;
        public List<Tempo> tempos;
        public List<TimeSignature> timeSignatures;

        public Chart(string path)
        {
            events = new List<Event>();
            tempos = new List<Tempo>();
            timeSignatures = new List<TimeSignature>();
            ParseChart(path);
        }

        private void ParseChart(string path)
        {
            string[] chart;
            chart = File.ReadAllLines(path);

            for (int i = 0; i < chart.Length; i++)
            {
                if (chart[i].Contains("[Song]")) { i = ParseChartSong(chart, i); continue; }
                if (chart[i].Contains("[SyncTrack]")) { i = ParseChartTempo(chart, i); continue; }
                if (chart[i].Contains("[EasyGHLGuitar]")) { i = ParseChartNotes(chart, i, Event.Difficulty.EASY); continue; }
                if (chart[i].Contains("[MediumGHLGuitar]")) { i = ParseChartNotes(chart, i, Event.Difficulty.MEDIUM); continue; }
                if (chart[i].Contains("[HardGHLGuitar]")) { i = ParseChartNotes(chart, i, Event.Difficulty.HARD); continue; }
                if (chart[i].Contains("[ExpertGHLGuitar]")) { i = ParseChartNotes(chart, i, Event.Difficulty.EXPERT); continue; }
            }
        }

        private int ParseChartSong(string[] chart, int i)
        {
            int timeOut = 100000;
            while (i < timeOut)
            {
                if (chart[i].Contains("Resolution")) resolution = short.Parse(chart[i].Split(" = ")[1]);
                if (chart[i].Contains('}')) break;
                i++;
            }
            return i;
        }

        private int ParseChartTempo(string[] chart, int i)
        {
            int timeOut = 100000;
            while (i < timeOut)
            {
                if (chart[i].Contains(" = "))
                {
                    // tick[0] : tick
                    //  com[0] : event
                    //  com[1] : value
                    //  com[2] : value ?
                    string[] tick = chart[i].Split(" = ");
                    string[] com = tick[1].Split(" ");

                    switch (com[0])
                    {
                        case "TS":
                            TimeSignature ts = new TimeSignature();
                            ts.ticks = UInt32.Parse(tick[0]);
                            ts.numerator = int.Parse(com[1]);
                            ts.denominator = com.Length > 2 ? int.Parse(com[2]) : 4;
                            timeSignatures.Add(ts);
                            break;
                        
                        case "B":
                            Tempo t = new Tempo();
                            t.ticks = UInt32.Parse(tick[0]);
                            t.beatsPerMinute = float.Parse(com[1])/1000;
                            tempos.Add(t);
                            break;
                    }
                }
                if (chart[i].Contains('}')) break;
                i++;
            }
            return i;
        }

        private int ParseChartNotes(string[] chart, int i, Event.Difficulty diff)
        {
            int timeOut = 100000;
            while (i < timeOut)
            {
                if (chart[i].Contains(" = "))
                {
                    // tick[0] : tick
                    //  com[0] : event
                    //  com[1] : value
                    //  com[2] : length
                    string[] tick = chart[i].Split(" = ");
                    string[] com = tick[1].Split(" ");

                    switch (com[0])
                    {
                        case "N":
                            Event n = new Event();
                            n.timeStart = long.Parse(tick[0]);
                            n.timeEnd = long.Parse(com[2])+n.timeStart;
                            n.note = byte.Parse(com[1]);
                            n.difficulty = diff;
                            if ((new byte[] {5, 6}).Contains(n.note))
                            {
                                events.FindAll(
                                    x => x.timeStart == n.timeStart && // equal timestamp
                                    x.note != 7 && // not an open
                                    x.difficulty == n.difficulty // same difficulty
                                ).ForEach(x => x.isHammer = true);
                                break;
                            }
                            events.Add(n);
                            break;

                        case "S":
                            Event s = new Event();
                            s.timeStart = long.Parse(tick[0]);
                            s.timeEnd = long.Parse(com[2])+s.timeStart;
                            s.note = 69;
                            s.difficulty = diff;
                            events.Add(s);
                            break;

                        case "E":
                            // Solo markers? No use
                            break;
                        
                    }
                }
                if (chart[i].Contains('}')) break;
                i++;
            }
            return i;
        }
    }
}