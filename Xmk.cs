using System;

namespace MiX
{
    public class Xmk
    {
        public Xmk(bool convert=false)
        {
            info = new Info();
            events = new List<Event>();
            tempos = new List<Tempo>();
            timeSignatures = new List<TimeSignature>();

            info.version = 8;
            info.checksum = unchecked((int)0xDEADBEEF);

            if (convert)
            {
                Event e = new Event();
                e.type = 4;
                e.unk2 = unk2;
                e.fileGroups.Add(Event.FileGroup.CONTROL);
                e.fileGroups.Add(Event.FileGroup.GUITAR_3X2);
                e.indexGroup = Event.IndexGroup.HOPODETECTION;
                info.sections.Add("HOPODETECTIONGAP_SEMIQUAVER");
                events.Add(e);
            }
        }

        public class Info
        {
            public int version; 
            public int checksum;
            public int eventCount;
            public int tempoCount;
            public int timeSigCount;
            public List<string> sections = new List<string>();
            public int sectionCount() => sections.Count();
            public int unk;
        }

        public class Event
        {
            public uint index;
            public ushort chord;
            public byte type;
            public byte note;
            public float start;
            public float end;
            public UInt32 unk2;
            public UInt32 offset;
            public long timeStart;
            public long timeEnd;
            public bool forcedStrum;
            public List<FileGroup> fileGroups = new List<FileGroup>();
            public IndexGroup indexGroup;
            public enum FileGroup { CONTROL, GUITAR_3X2, TOUCH_GUITAR, TOUCH_DRUMS, VOCALS }
            public enum IndexGroup { NOTE, HERO_POWER, SECTION, HIGHWAY, HOPODETECTION }
            public const byte typeGroup =   0b0011_1000;
            public const byte sustain =     0b0100_0000;
            public const byte hammer =      0b1000_0000;
            public bool isSameGroup(byte x, byte y) => (x&typeGroup) == (y&typeGroup);
            public bool isSustain() => (this.type&sustain) == sustain;
            public bool isHammer() => (this.type&hammer) == hammer;

            public override string ToString()
            {
                return $"{this.timeStart} {this.chord} {this.type} {this.note} {this.start} {this.end}";
            }
        }

        public class Tempo
        {
            public UInt32 ticks;
            public float start;
            public UInt32 microSecondsPerQuarter;
        }

        public class TimeSignature
        {
            public UInt32 ticks;
            public int measure;
            public int numerator;
            public int denominator;
        }

        public Info info;
        public List<Event> events;
        public List<Tempo> tempos;
        public List<TimeSignature> timeSignatures;
        public const UInt32 unk2 = 8935; // Don't know what this does

        public Dictionary<int,int> barPairs = new Dictionary<int, int>()
        {
            // B1>W1 B2>W2 B3>W3 W1>B1 W2>B2 W3>B3
            { 5,  6}, { 7,  8}, { 9, 10}, { 6,  5}, { 8,  7}, {10,  9}, // Easy
            {23, 24}, {25, 26}, {27, 28}, {24, 23}, {26, 25}, {28, 27}, // Medium
            {41, 42}, {43, 44}, {45, 46}, {42, 41}, {44, 43}, {46, 45}, // Hard
            {59, 60}, {61, 62}, {63, 64}, {60, 59}, {62, 61}, {64, 63}  // Expert
        };

        public int typeGroup(int noteValue)
        {
            int[] easy      = new int[] { 5,  6,  7,  8,  9, 10, 15, 20};
            int[] medium    = new int[] {23, 24, 25, 26, 27, 28, 33, 38};
            int[] hard      = new int[] {41, 42, 43, 44, 45, 46, 51, 56};
            int[] expert    = new int[] {59, 60, 61, 62, 63, 64, 69, 74};

            if (easy.Contains(noteValue))   return 0b0000_1000;
            if (medium.Contains(noteValue)) return 0b0001_0000;
            if (hard.Contains(noteValue))   return 0b0001_1000;
            if (expert.Contains(noteValue)) return 0b0010_0000;
            return 0;
        }

        private int chordValues(int noteValue)
        {
            int[] b1        = new int[] { 5, 23, 41, 59};
            int[] w1        = new int[] { 6, 24, 42, 60};
            int[] b2        = new int[] { 7, 25, 43, 61};
            int[] w2        = new int[] { 8, 26, 44, 62};
            int[] b3        = new int[] { 9, 27, 45, 63};
            int[] w3        = new int[] {10, 28, 46, 64};

            if (b1.Contains(noteValue)) return 0b0000_0100;
            if (b2.Contains(noteValue)) return 0b0000_1000;
            if (b3.Contains(noteValue)) return 0b0001_0000;
            if (w1.Contains(noteValue)) return 0b0010_0000;
            if (w2.Contains(noteValue)) return 0b0100_0000;
            if (w3.Contains(noteValue)) return 0b1000_0000;
            return 0;
        }

        private List<Event> getCrazyChord(List<Event> chord, Event note)
        {
            Event? nextNote = events.Find(
                x => x.isSameGroup(x.type, note.type) && // same difficulty
                !chord.Contains(x) && // not already in the chord
                barPairs.ContainsKey(x.note) &&
                x.timeStart >= note.timeStart && 
                x.timeStart < note.timeEnd
                );
            if (nextNote != null && !chord.Contains(nextNote))
            {
                chord.Add(nextNote);
                chord = getCrazyChord(chord, nextNote);
            }
            return chord;
        }

        private void ParseEvents(int resolution=960)
        {
            if (events.Count == 0) return;

            // Apply auto Hammers
            foreach (Event e in events.FindAll(x => barPairs.ContainsKey(x.note)))
            {
                if (events.FindAll(
                    x => x.isSameGroup(x.type, e.type) &&
                    barPairs.ContainsKey(x.note) &&
                    x.timeStart == e.timeStart
                    ).Count > 1) continue; // Ignore chords
                if (e.forcedStrum) continue; // Ignore forced notes
                
                Event? lastNote = events.FindLast(
                    x => x.isSameGroup(x.type, e.type) && // Same group
                    x.timeStart < e.timeStart // Earlier timestamp
                    );

                // check for repeating chords?

                // note exists & different to current & within the tick window
                e.type |= lastNote != null && lastNote.note != e.note && e.timeStart < lastNote.timeStart + (resolution/2) ? (byte)128 : (byte)0;
            }

            // Set Bar Chords
            foreach (Event e in events.FindAll(x => barPairs.ContainsKey(x.note)))
            {
                Event? pairedNote = events.Find(
                    x => !e.Equals(x) && // isn't the same note
                    x.timeStart == e.timeStart && // starts at the same time
                    x.note == barPairs[e.note] // is the correct corresponding note
                    );

                if (pairedNote != null) e.chord |= 2;
            }

            // Set Crazy Chords
            foreach (Event e in events.FindAll(x => (x.type & 0b0100_0000) == 0b0100_0000))
            {
                List<Event> chord = getCrazyChord(new List<Event>(){e}, e);

                if (chord.Count < 2) continue; // not enough notes
                //if (chord.Count == 2 && chord[0].chord == chord[1].chord) continue;
                if (chord.All(x => x.start == chord.First().start &&
                x.end == chord.First().end)) continue; // it's a normal chord

                ushort chordValue = 0;
                chord.ForEach(n => chordValue |= (ushort)chordValues(n.note));
                chord.ForEach(n => events.First(x => x.Equals(n)).chord |= chordValue);
            }
        }

        public int ExportToFile(string path)
        {
            Dictionary<Event.FileGroup,string> fileName = new Dictionary<Event.FileGroup, string>()
            {
                {Event.FileGroup.CONTROL, "control.xmk"},
                {Event.FileGroup.GUITAR_3X2, "guitar_3x2.xmk"},
                {Event.FileGroup.TOUCH_GUITAR, "touchguitar.xmk"},
                {Event.FileGroup.TOUCH_DRUMS, "touchdrums.xmk"},
                {Event.FileGroup.VOCALS, "vocals.xmk"}
            };

            if (events.Count() == 1) return 1;
            events.Sort((x,y) => x.timeStart.CompareTo(y.timeStart));
            ParseEvents();

            foreach (Event.FileGroup file in fileName.Keys)
            {
                List<Event> activeEvents = events.FindAll(e => e.fileGroups.Contains(file));
                if (activeEvents.Count == 0) continue;

                Dictionary<Event.IndexGroup,UInt32> indexGroup = new Dictionary<Event.IndexGroup, uint>()
                {
                    {Event.IndexGroup.NOTE, 0},
                    {Event.IndexGroup.HERO_POWER, 0},
                    {Event.IndexGroup.HIGHWAY, 0},
                    {Event.IndexGroup.SECTION, 0},
                    {Event.IndexGroup.HOPODETECTION, 0}
                };

                BitWriter bw = new BitWriter(path+fileName[file]);

                bw.WriteInt32(info.version);
                bw.WriteInt32(info.checksum);
                bw.WriteInt32(activeEvents.Count());
                bw.WriteInt32(info.sectionCount());
                bw.WriteInt32(info.checksum);
                bw.WriteInt32(tempos.Count());
                bw.WriteInt32(timeSignatures.Count());

                foreach (Tempo t in tempos)
                {
                    bw.WriteUint32((uint)t.ticks);
                    bw.WriteFloat(t.start);
                    bw.WriteUint32((uint)t.microSecondsPerQuarter);
                }

                foreach (TimeSignature ts in timeSignatures)
                {
                    bw.WriteUint32((uint)ts.ticks);
                    bw.WriteInt32(ts.measure);
                    bw.WriteInt32(ts.numerator);
                    bw.WriteInt32(ts.denominator);
                }

                UInt32 offset = (UInt32)(events.Count*24);
                int sectionIndex = 0;

                foreach (Event e in activeEvents)
                {
                    if (e.indexGroup == Event.IndexGroup.SECTION | e.indexGroup == Event.IndexGroup.HOPODETECTION)
                    {
                        e.offset = offset;
                        offset += (UInt32)info.sections[sectionIndex].Length+1;
                        sectionIndex++;
                    }

                    bw.WriteUint32(indexGroup[e.indexGroup]);
                    indexGroup[e.indexGroup]++;
                    bw.WriteInt16(Convert.ToUInt16(e.chord));
                    bw.WriteInt8(e.type);
                    bw.WriteInt8(e.note);
                    bw.WriteFloat(e.start);
                    bw.WriteFloat(e.end);
                    bw.WriteUint32(Convert.ToUInt32(e.unk2));
                    bw.WriteUint32(Convert.ToUInt32(e.offset));
                }

                foreach (string s in info.sections)
                {
                    bw.WriteString(s);
                    bw.WriteInt8(0);
                }
                bw.Close();
            }
            return 0;
        }
    }
}