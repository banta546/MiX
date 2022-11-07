using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Tools;

namespace MiX
{
    public class MidiParser
    {
        public MidiFile midiFile;
        private short resolution;
        private Xmk xmk;

        public MidiParser(string path)
        {
            midiFile = MidiFile.Read(path);
            resolution = ((TicksPerQuarterNoteTimeDivision)midiFile.TimeDivision).TicksPerQuarterNote;
            xmk = new Xmk(true);
        }

        private Dictionary<int,byte> midiNoteValues = new Dictionary<int, byte>()
        {
            // B1 W1 B2 W2 B3 W3 Open
            {62,  5}, {59,  6}, {63,  7}, {60,  8}, { 64,  9}, {61, 10}, {58, 15}, // Easy
            {74, 23}, {71, 24}, {75, 25}, {72, 26}, { 76, 27}, {73, 28}, {70, 33}, // Medium
            {86, 41}, {83, 42}, {87, 43}, {84, 44}, { 88, 45}, {85, 46}, {82, 51}, // Hard
            {98, 59}, {95, 60}, {99, 61}, {96, 62}, {100, 63}, {97, 64}, {94, 69}  // Expert
        };

        private Dictionary<int,byte> midiForcedHammer = new Dictionary<int, byte>()
        {
            // Easy Medium Hard Expert
            {65, 0b0000_1000}, {77, 0b0001_0000}, {89, 0b0001_1000}, {101, 0b0010_0000}
        };

        private Dictionary<int,byte> midiForcedStrum = new Dictionary<int, byte>()
        {
            // Easy Medium Hard Expert
            {66, 0b0000_1000}, {78, 0b0001_0000}, {90, 0b0001_1000}, {102, 0b0010_0000}
        };

        public Xmk ParseMidi()
        {
            foreach (TrackChunk track in midiFile.GetTrackChunks())
            {
                string trackTitle = "TEMPO"; // Default, tempo tracks can be unnamed
                BaseTextEvent? bte = (BaseTextEvent?)track.Events.FirstOrDefault(e => e is BaseTextEvent);
                if (bte != null) { trackTitle = bte.Text; }
                
                switch (trackTitle)
                {
                    case "TEMPO" or "xmkTempo" or "notes" or "":
                        ParseTempo(track, midiFile.GetTempoMap());
                        break;
                    
                    case "EVENTS":
                        ParseEvents(track, midiFile.GetTempoMap());
                        break;
                    
                    case "PART GUITAR GHL":
                        ParseGuitar(track, midiFile.GetTempoMap());
                        break;
                    
                    default:
                        continue;
                }
            }
            return xmk;
        }

        private void ParseTempo(TrackChunk track, TempoMap tempoMap)
        {
            foreach (var e in track.GetTimedEvents())
            {
                switch (e.Event)
                {
                    case SetTempoEvent:
                        Xmk.Tempo t = new Xmk.Tempo();
                        t.ticks = (UInt32)(e.Time * (960 / resolution));
                        t.start = (float)e.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                        t.microSecondsPerQuarter = (UInt32)((SetTempoEvent)e.Event).MicrosecondsPerQuarterNote;
                        xmk.tempos.Add(t);
                        break;
                    
                    case TimeSignatureEvent:
                        Xmk.TimeSignature ts = new Xmk.TimeSignature();
                        ts.ticks = (UInt32)(e.Time * (960 / resolution));
                        ts.measure = (int)e.TimeAs<BarBeatTicksTimeSpan>(tempoMap).Bars+1;
                        ts.numerator = ((TimeSignatureEvent)e.Event).Numerator;
                        ts.denominator = ((TimeSignatureEvent)e.Event).Denominator;
                        xmk.timeSignatures.Add(ts);
                        break;
                }
            }
        }

        private void ParseEvents(TrackChunk track, TempoMap tempoMap)
        {
            List<Xmk.Event.FileGroup> fileGroup = new List<Xmk.Event.FileGroup>() 
            {
                Xmk.Event.FileGroup.CONTROL, Xmk.Event.FileGroup.GUITAR_3X2
            };

            long ticks = 0;

            foreach (var t in track.Events)
            {
                ticks += t.DeltaTime;
                if (t is SequenceTrackNameEvent) continue;
                Xmk.Event e = new Xmk.Event();
                e.type = 3;
                e.note = 128;
                e.start = (float)(new TimedEvent(t, ticks)).TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                e.end = e.start;
                e.unk2 = Xmk.unk2;
                e.timeStart = ticks * (960 / resolution);
                e.indexGroup = Xmk.Event.IndexGroup.SECTION;
                foreach (Xmk.Event.FileGroup fg in fileGroup) e.fileGroups.Add(fg);
                xmk.info.sections.Add(((BaseTextEvent)t).Text);
                xmk.events.Add(e);
            }
        }

        private void ParseGuitar(TrackChunk track, TempoMap tempoMap)
        {
            Xmk.Event.FileGroup fileGroup = Xmk.Event.FileGroup.GUITAR_3X2;
            Xmk.Event.IndexGroup indexGroup;

            // Use MidiEvent base class instead?
            foreach (var n in track.GetNotes())
            {
                if (midiNoteValues.ContainsKey(n.NoteNumber))
                {
                    indexGroup = Xmk.Event.IndexGroup.NOTE;
                    Xmk.Event e = new Xmk.Event();
                    e.note = midiNoteValues[n.NoteNumber];
                    e.type = (byte)xmk.typeGroup(e.note); // Difficulty Group
                    e.type |= n.Length > (resolution/4) ? (byte)Xmk.Event.sustain : (byte)0;
                    e.start = (float)n.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    e.end = (float)n.EndTimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    e.offset = 100;
                    e.timeStart = n.Time * (960 / resolution);
                    e.timeEnd = n.EndTime * (960 / resolution);
                    e.fileGroups.Add(fileGroup);
                    e.indexGroup = indexGroup;
                    xmk.events.Add(e);
                }

                if (n.NoteNumber == 116)
                {
                    indexGroup = Xmk.Event.IndexGroup.HERO_POWER;
                    foreach (byte i in new byte[] {20, 38, 56, 74})
                    {
                        Xmk.Event e = new Xmk.Event();
                        e.note = i;
                        e.type = (byte)xmk.typeGroup(e.note);
                        e.start = (float)(n.TimeAs<MetricTimeSpan>(tempoMap)).TotalSeconds;
                        e.end = (float)(n.EndTimeAs<MetricTimeSpan>(tempoMap)).TotalSeconds;
                        e.offset = 100;
                        e.timeStart = n.Time * (960 / resolution);
                        e.timeEnd = n.EndTime * (960 / resolution);
                        e.fileGroups.Add(fileGroup);
                        e.indexGroup = indexGroup;
                        xmk.events.Add(e);
                    }
                }

                if (midiForcedHammer.ContainsKey(n.NoteNumber))
                {
                    xmk.events.FindAll(
                        e => e.isSameGroup(e.type, midiForcedHammer[n.NoteNumber]) &&
                        xmk.barPairs.ContainsKey(e.note) &&
                        e.timeStart == n.Time * (960 / resolution)
                    ).ForEach(x => x.type |= Xmk.Event.hammer);
                }

                if (midiForcedStrum.ContainsKey(n.NoteNumber))
                {
                    xmk.events.FindAll(
                        e => e.isSameGroup(e.type, midiForcedStrum[n.NoteNumber]) &&
                        xmk.barPairs.ContainsKey(e.note) &&
                        e.timeStart == n.Time * (960 / resolution)
                    ).ForEach(x => x.forcedStrum = true);
                }
            }

            int tapOn = 0;
            int tapOff = 0;
            int time = 0;
            foreach (var e in track.Events)
            {
                // Toggle Taps
                // TODO refactor using MidiEvent base class instead of Note
                time += (int)e.DeltaTime;
                if (e is SysExEvent)
                {
                    SysExEvent s = (SysExEvent)e;
                    switch (s.Data[6])
                    {
                        case 1:
                            tapOn = (int)time;
                            break;
                        case 0:
                            tapOff = (int)time;
                            xmk.events.FindAll(
                                x => xmk.barPairs.ContainsKey(x.note) &&
                                x.timeStart >= tapOn &&
                                x.timeStart < tapOff)
                                .ForEach(x => x.type |= 128);
                            break;
                    }
                }
                if (e is BaseTextEvent)
                {
                    switch (((BaseTextEvent)e).Text)
                    {
                        case "hwd":
                            Xmk.Event xmkEvent = new Xmk.Event();
                            xmkEvent.note = 78;
                            xmkEvent.type = 56;
                            xmkEvent.start = (float)(new TimedEvent(e, time)).TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                            xmkEvent.offset = 100;
                            xmkEvent.timeStart = time * (960 / resolution);
                            xmkEvent.fileGroups.Add(Xmk.Event.FileGroup.GUITAR_3X2);
                            xmkEvent.indexGroup = Xmk.Event.IndexGroup.HIGHWAY;
                            xmk.events.Insert(xmk.events.FindLastIndex(x => x.timeStart <= xmkEvent.timeStart), xmkEvent);
                            break;
                        
                        case "hwu":
                            int i = xmk.events.FindLastIndex(x => x.indexGroup == Xmk.Event.IndexGroup.HIGHWAY);
                            xmk.events[i].end = (float)(new TimedEvent(e, time)).TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                            break;
                    }
                }
            }
        }
    }
}