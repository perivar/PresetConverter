using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

using Serilog;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Newtonsoft.Json;

namespace PresetConverter
{
    public static class AbletonProject
    {
        private static string? GetValue(XElement xmlData, string varName, string? fallback)
        {
            XElement? xElement = xmlData.XPathSelectElements(varName).FirstOrDefault();
            return xElement?.Attribute("Value")?.Value ?? fallback;
        }

        private static string? GetId(XElement xmlData, string varName, string? fallback)
        {
            XElement? xElement = xmlData.XPathSelectElements(varName).FirstOrDefault();
            return xElement?.Attribute("Id")?.Value ?? fallback;
        }

        private static object UseValueType(string valType, string val)
        {
            if (valType == "string")
            {
                return val;
            }
            else if (valType == "float")
            {
                return float.Parse(val, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            else if (valType == "int")
            {
                return int.Parse(val);
            }
            else if (valType == "bool")
            {
                return bool.Parse(val);
            }
            else
            {
                // Add more cases as needed
                return val;
            }
        }

        private static object GetParam(XElement xmlData, string varName, string varType, double fallback, string[] loc, double? addMul)
        {
            XElement? xElement = xmlData.XPathSelectElements(varName).FirstOrDefault();

            if (xElement != null)
            {
                string inData = GetValue(xElement, "Manual", fallback.ToString());
                int autoNumId = int.Parse(GetId(xElement, "AutomationTarget", null) ?? "0");
                object outData = UseValueType(varType, inData);

                if (autoNumId != 0)
                {
                    // AutoId.InDefine(autoNumId, loc, varType, addMul);
                }

                return outData;
            }
            else
            {
                return fallback;
            }
        }

        private static dynamic CutLoopData(float start, float loopStart, float loopEnd)
        {
            dynamic output = new System.Dynamic.ExpandoObject();

            if (start == 0 && loopStart == 0)
            {
                output.type = "loop";
                output.loopend = loopEnd;
            }
            else if (loopStart == 0)
            {
                output.type = "loop_off";
                output.start = start;
                output.loopend = loopEnd;
            }
            else
            {
                output.type = "loop_adv";
                output.start = start;
                output.loopstart = loopStart;
                output.loopend = loopEnd;
            }

            return output;
        }

        private static Tuple<float, float, float> HexToRgbFloat(string hex)
        {
            // Convert a hexadecimal value #FF00FF to RGB. Returns a tuple of floats between 0 and 1.

            hex = hex.TrimStart('#');
            if (hex.Length != 6)
            {
                throw new ArgumentException("Invalid hexadecimal color code");
            }

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            float factor = 1.0f / 255.0f;

            return Tuple.Create(r * factor, g * factor, b * factor);
        }

        private static void AddCmd(Dictionary<long, List<string>> list, long pos, List<string> cmd)
        {
            if (!list.ContainsKey(pos))
                list[pos] = new List<string>();

            list[pos].AddRange(cmd);
        }

        public static void HandleAbletonLiveContent(XElement root)
        {
            // all credits go to SatyrDiamond and the DawVert code
            // https://raw.githubusercontent.com/SatyrDiamond/DawVert/main/plugin_input/r_ableton.py

            // initialize the color lists
            var colorlist = new List<string>
            {
                "FF94A6", "FFA529", "CC9927", "F7F47C", "BFFB00", "1AFF2F", "25FFA8", "5CFFE8", "8BC5FF", "5480E4",
                "92A7FF", "D86CE4", "E553A0", "FFFFFF", "FF3636", "F66C03", "99724B", "FFF034", "87FF67", "3DC300",
                "00BFAF", "19E9FF", "10A4EE", "007DC0", "886CE4", "B677C6", "FF39D4", "D0D0D0", "E2675A", "FFA374",
                "D3AD71", "EDFFAE", "D2E498", "BAD074", "9BC48D", "D4FDE1", "CDF1F8", "B9C1E3", "CDBBE4", "AE98E5",
                "E5DCE1", "A9A9A9", "C6928B", "B78256", "99836A", "BFBA69", "A6BE00", "7DB04D", "88C2BA", "9BB3C4",
                "85A5C2", "8393CC", "A595B5", "BF9FBE", "BC7196", "7B7B7B", "AF3333", "A95131", "724F41", "DBC300",
                "85961F", "539F31", "0A9C8E", "236384", "1A2F96", "2F52A2", "624BAD", "A34BAD", "CC2E6E", "3C3C3C"
            };

            var colorlistOne = new List<Tuple<float, float, float>>();
            foreach (string hexColor in colorlist)
            {
                var rgbFloatColor = HexToRgbFloat(hexColor);
                colorlistOne.Add(rgbFloatColor);
            }

            // ***************** 
            // start reading
            // ***************** 
            dynamic cvpj = new System.Dynamic.ExpandoObject(); // store in common daw project format (converted project)

            string abletonVersion = root?.Attribute("MinorVersion")?.Value.Split('.')[0];
            if (abletonVersion != "11")
            {
                Console.WriteLine("[error] Ableton version " + abletonVersion + " is not supported.");
                Environment.Exit(0);
            }

            XElement xLiveSet = root.Element("LiveSet");
            XElement xTracks = xLiveSet?.Element("Tracks");
            XElement xMasterTrack = xLiveSet?.Element("MasterTrack");
            XElement xMasterTrackDeviceChain = xMasterTrack?.Element("DeviceChain");
            XElement xMasterTrackMixer = xMasterTrackDeviceChain?.Element("Mixer");
            XElement xMasterTrackDeviceChainInside = xMasterTrackDeviceChain?.Element("DeviceChain");
            XElement xMasterTrackTrackDevices = xMasterTrackDeviceChainInside?.Element("Devices");
            // DoDevices(xMasterTrackTrackDevices, null, new string[] { "master" });

            XElement xMastertrackName = xMasterTrack?.Element("Name");
            string mastertrackName = GetValue(xMastertrackName, "EffectiveName", "");
            var mastertrackColor = colorlistOne[int.Parse(GetValue(xMasterTrack, "Color", "0"))];
            XElement xMastertrackDeviceChain = xMasterTrack?.Element("DeviceChain");
            XElement xMastertrackMixer = xMastertrackDeviceChain?.Element("Mixer");
            float masTrackVol = (float)GetParam(xMastertrackMixer, "Volume", "float", 0, new string[] { "master", "vol" }, null);
            float masTrackPan = (float)GetParam(xMastertrackMixer, "Pan", "float", 0, new string[] { "master", "pan" }, null);
            float tempo = (float)GetParam(xMastertrackMixer, "Tempo", "float", 140, new string[] { "main", "bpm" }, null);

            Log.Debug("Tempo: {0} bpm, MasterTrackName: {1}, Volume: {2}, Pan: {3}", tempo, mastertrackName, masTrackVol, masTrackPan);

            cvpj.track_master = new System.Dynamic.ExpandoObject();
            cvpj.track_master.name = mastertrackName;
            cvpj.track_master.color = mastertrackColor;
            cvpj.track_master.parameters = new System.Dynamic.ExpandoObject();
            cvpj.track_master.parameters.pan = new System.Dynamic.ExpandoObject();
            cvpj.track_master.parameters.pan.name = "Pan";
            cvpj.track_master.parameters.pan.type = "float";
            cvpj.track_master.parameters.pan.value = masTrackPan;
            cvpj.track_master.parameters.vol = new System.Dynamic.ExpandoObject();
            cvpj.track_master.parameters.vol.name = "Volume";
            cvpj.track_master.parameters.vol.type = "float";
            cvpj.track_master.parameters.vol.value = masTrackVol;

            cvpj.parameters = new System.Dynamic.ExpandoObject();
            cvpj.parameters.bpm = new System.Dynamic.ExpandoObject();
            cvpj.parameters.bpm.name = "Tempo";
            cvpj.parameters.bpm.type = "float";
            cvpj.parameters.bpm.value = tempo;

            cvpj.track_data = new Dictionary<string, dynamic>();
            cvpj.track_placements = new Dictionary<string, dynamic>();

            // Read Tracks
            Log.Debug("Found {0} Tracks ...", xTracks.Elements().Count());

            foreach (XElement xTrackData in xTracks.Elements())
            {
                string tracktype = xTrackData.Name.LocalName;

                XElement xTrackDeviceChain = xTrackData.Element("DeviceChain");
                XElement xTrackMixer = xTrackDeviceChain?.Element("Mixer");

                XElement xTrackName = xTrackData.Element("Name");

                string trackId = xTrackData.Attribute("Id")?.Value;
                string trackName = GetValue(xTrackName, "EffectiveName", "");
                var trackColor = colorlistOne[int.Parse(GetValue(xTrackData, "Color", ""))];
                int trackInsideGroup = int.Parse(GetValue(xTrackData, "TrackGroupId", "-1"));

                XElement xTrackSends = xTrackMixer?.Element("Sends");
                IEnumerable<XElement> xTrackSendHolders = xTrackSends?.Elements("TrackSendHolder");

                if (tracktype == "MidiTrack")
                {
                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", 0, new string[] { "track", trackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", 0, new string[] { "track", trackId, "pan" }, null);

                    Log.Debug($"Reading MIDI Track. Id: {trackId}, EffectiveName: {trackName}, Volume: {trackVol}, Pan: {trackPan}");

                    dynamic track = new System.Dynamic.ExpandoObject();
                    track.type = "instrument";
                    track.name = trackName;
                    track.color = trackColor;
                    track.parameters = new System.Dynamic.ExpandoObject();
                    track.parameters.pan = new System.Dynamic.ExpandoObject();
                    track.parameters.pan.name = "Pan";
                    track.parameters.pan.type = "float";
                    track.parameters.pan.value = trackPan;
                    track.parameters.vol = new System.Dynamic.ExpandoObject();
                    track.parameters.vol.name = "Volume";
                    track.parameters.vol.type = "float";
                    track.parameters.vol.value = trackVol;

                    if (trackInsideGroup != -1)
                    {
                        track.group = "group_" + trackInsideGroup;
                    }

                    cvpj.track_data.Add(trackId, track);

                    XElement xTrackMainSequencer = xTrackDeviceChain?.Element("MainSequencer");
                    XElement xTrackClipTimeable = xTrackMainSequencer?.Element("ClipTimeable");
                    XElement xTrackArrangerAutomation = xTrackClipTimeable?.Element("ArrangerAutomation");
                    XElement xTrackEvents = xTrackArrangerAutomation?.Element("Events");
                    IEnumerable<XElement> xTrackMidiClips = xTrackEvents?.Elements("MidiClip");

                    var notesList = new List<dynamic>();

                    foreach (XElement xTrackMidiClip in xTrackMidiClips)
                    {
                        float notePlacementPos = float.Parse(GetValue(xTrackMidiClip, "CurrentStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementDur = float.Parse(GetValue(xTrackMidiClip, "CurrentEnd", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        string notePlacementName = GetValue(xTrackMidiClip, "Name", "");
                        var notePlacementColor = colorlistOne[int.Parse(GetValue(xTrackMidiClip, "Color", "0"))];
                        bool notePlacementMuted = bool.Parse(GetValue(xTrackMidiClip, "Disabled", "false"));

                        Log.Debug($"Reading MidiClip. CurrentStart: {notePlacementPos}, CurrentEnd: {notePlacementDur}, Name: {notePlacementName}, Color: {notePlacementColor}, Disabled: {notePlacementMuted}");

                        dynamic notePlacement = new System.Dynamic.ExpandoObject();
                        notePlacement.position = notePlacementPos * 4;
                        notePlacement.duration = notePlacementDur * 4 - (notePlacementPos * 4);
                        notePlacement.name = notePlacementName;
                        notePlacement.color = notePlacementColor;
                        notePlacement.muted = notePlacementMuted;

                        XElement xTrackMidiClipLoop = xTrackMidiClip.Element("Loop");
                        float notePlacementLoopLStart = float.Parse(GetValue(xTrackMidiClipLoop, "LoopStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementLoopLEnd = float.Parse(GetValue(xTrackMidiClipLoop, "LoopEnd", "1"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementLoopStart = float.Parse(GetValue(xTrackMidiClipLoop, "StartRelative", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        bool notePlacementLoopOn = bool.Parse(GetValue(xTrackMidiClipLoop, "LoopOn", "false"));

                        Log.Debug($"Reading MidiLoop. LoopStart: {notePlacementLoopLStart}, LoopEnd: {notePlacementLoopLEnd}, StartRelative: {notePlacementLoopStart}, LoopOn: {notePlacementLoopOn}");

                        if (notePlacementLoopOn)
                        {
                            notePlacement.cut = CutLoopData(notePlacementLoopStart * 4, notePlacementLoopLStart * 4, notePlacementLoopLEnd * 4);
                        }
                        else
                        {
                            notePlacement.cut = new System.Dynamic.ExpandoObject();
                            notePlacement.cut.type = "cut";
                            notePlacement.cut.start = notePlacementLoopLStart * 4;
                            notePlacement.cut.end = notePlacementLoopLEnd * 4;
                        }

                        // Log.Debug($"notePlacement: {JsonConvert.SerializeObject(notePlacement, Formatting.Indented)}");

                        XElement xTrackMidiClipNotes = xTrackMidiClip.Element("Notes");
                        XElement xTrackMidiClipKT = xTrackMidiClipNotes?.Element("KeyTracks");

                        Log.Debug("Found {0} KeyTracks ...", xTrackMidiClipKT?.Elements("KeyTrack").Count());

                        var notes = new Dictionary<int, dynamic>();

                        foreach (XElement xTrackMidiClipKTKTs in xTrackMidiClipKT?.Elements("KeyTrack"))
                        {
                            int midiKey = int.Parse(GetValue(xTrackMidiClipKTKTs, "MidiKey", "60"));
                            int abletonNoteKey = midiKey - 60;

                            XElement xTrackMidiClipKT_KT_Notes = xTrackMidiClipKTKTs.Element("Notes");

                            Log.Debug("Reading {0} MidiNoteEvents ...", xTrackMidiClipKT_KT_Notes?.Elements("MidiNoteEvent").Count());

                            foreach (XElement xTrackMidiClipMNE in xTrackMidiClipKT_KT_Notes?.Elements("MidiNoteEvent"))
                            {
                                float noteTime = float.Parse(xTrackMidiClipMNE.Attribute("Time").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                float noteDuration = float.Parse(xTrackMidiClipMNE.Attribute("Duration").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                float noteVelocity = float.Parse(xTrackMidiClipMNE.Attribute("Velocity").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                float noteOffVelocity = float.Parse(xTrackMidiClipMNE.Attribute("OffVelocity").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                float noteProbablity = float.Parse(xTrackMidiClipMNE.Attribute("Probability").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                bool noteIsEnabled = bool.Parse(xTrackMidiClipMNE.Attribute("IsEnabled").Value);
                                int noteId = int.Parse(xTrackMidiClipMNE.Attribute("NoteId").Value);

                                Log.Debug($"Reading MidiNoteEvent. Time: {noteTime}, Duration: {noteDuration}, MidiKey: {midiKey}, Velocity: {noteVelocity}, OffVelocity: {noteOffVelocity}, NoteId: {noteId}");

                                dynamic noteData = new System.Dynamic.ExpandoObject();
                                noteData.key = abletonNoteKey;
                                noteData.position = noteTime * 4;
                                noteData.duration = noteDuration * 4;
                                noteData.vol = noteVelocity / 100;
                                noteData.off_vol = noteOffVelocity / 100;
                                noteData.probability = noteProbablity;
                                noteData.enabled = noteIsEnabled;

                                // Log.Debug($"noteData: {JsonConvert.SerializeObject(noteData, Formatting.Indented)}");

                                notes[noteId] = noteData;
                            }
                        }

                        XElement xTrackMidiClipNES = xTrackMidiClipNotes.Element("PerNoteEventStore");
                        XElement xTrackMidiClipNES_EL = xTrackMidiClipNES?.Element("EventLists");

                        foreach (XElement xNoteNEvent in xTrackMidiClipNES_EL?.Elements("PerNoteEventList") ?? Enumerable.Empty<XElement>())
                        {
                            int autoNoteId = int.Parse(xNoteNEvent.Attribute("NoteId")?.Value ?? "0");
                            int autoNoteCC = int.Parse(xNoteNEvent.Attribute("CC")?.Value ?? "0");

                            notes[autoNoteId] = new System.Dynamic.ExpandoObject();
                            notes[autoNoteId].notemod = new System.Dynamic.ExpandoObject();
                            notes[autoNoteId].notemod.auto = new System.Dynamic.ExpandoObject();

                            if (autoNoteCC == -2)
                            {
                                notes[autoNoteId].notemod.auto.pitch = new List<dynamic>();
                                var cvpjNoteAutoPitch = notes[autoNoteId].notemod.auto.pitch;
                                var xNoteNEvent_EV = xNoteNEvent.Element("Events");

                                foreach (var abletonPoint in xNoteNEvent_EV.Elements("PerNoteEvent"))
                                {
                                    float apPos = float.Parse(abletonPoint.Attribute("TimeOffset").Value);
                                    float apVal = float.Parse(abletonPoint.Attribute("Value").Value);
                                    cvpjNoteAutoPitch.Add(new { position = apPos * 4, value = apVal / 170 });
                                }
                            }
                        }

                        // convert notes dictionary to values list since we dont need the notes-id 
                        notePlacement.notelist = notes.Values.ToList();
                        notesList.Add(notePlacement);
                    }

                    dynamic notesGroup = new System.Dynamic.ExpandoObject();
                    notesGroup.notes = notesList;
                    cvpj.track_placements.Add(trackId, notesGroup);
                }
            }

            // Log.Error($"cvpj: {JsonConvert.SerializeObject(cvpj, Formatting.Indented)}");
            ConvertToMidi(cvpj);
        }

        public static void ConvertToMidi(dynamic cvpj)
        {
            // all credits go to SatyrDiamond and the DawVert code
            // https://github.com/SatyrDiamond/DawVert/blob/main/plugin_output/midi.py

            double tempo = cvpj.parameters.bpm.value;

            // https://gist.github.com/melanchall/d4142f5f0fb36ab86e46110d69966fed
            var midiFile = new MidiFile();

            // Set tempo map of the file using tempo of X BPM. See
            // https://github.com/melanchall/drywetmidi/wiki/Tempo-map 
            // to learn more about managing tempo map in DryWetMIDI
            // Set the ticks per beat and BPM
            // midi_tempo = 
            short ticksPerBeat = 480;
            var midiTimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerBeat);
            var midiTempo = Tempo.FromBeatsPerMinute(tempo); // 128 bpm should give 468750
            var midiTimeSignature = new TimeSignature(4, 4);

            // var tempoMap = TempoMap.Create(midiTempo);
            var tempoMap = TempoMap.Create(midiTimeDivision, midiTempo, midiTimeSignature);

            midiFile.ReplaceTempoMap(tempoMap);

            int trackNum = 0;
            foreach (var track in cvpj.track_placements)
            {
                var trackId = track.Key;
                string trackName = cvpj.track_data[trackId].name;

                Log.Debug($"Creating MIDI track: {trackName} with id: {trackId}");

                // Create a track with name
                var trackChunk = new TrackChunk();
                midiFile.Chunks.Add(trackChunk);
                trackChunk.Events.Add(new SequenceTrackNameEvent(trackName));

                var notes = track.Value.notes;

                var noteList = new Dictionary<long, List<string>>(); // list holding the notes

                if (notes != null)
                {
                    foreach (dynamic notePlacement in notes)
                    {
                        if (!notePlacement.muted)
                        {
                            foreach (dynamic noteData in notePlacement.notelist)
                            {
                                // add midi notes
                                int midiNotePos = (int)((float)notePlacement.position * 4 + (float)noteData.position * 4) * 30;
                                int midiNoteDur = (int)((float)noteData.duration * 4) * 30;
                                int midiNoteKey = (int)noteData.key + 60;
                                int midiNoteVol = Math.Clamp((int)((float)noteData.vol * 127), 0, 127);

                                AddCmd(noteList, midiNotePos, new List<string> { "note_on", midiNoteKey.ToString(), midiNoteVol.ToString() });
                                AddCmd(noteList, midiNotePos + midiNoteDur, new List<string> { "note_off", midiNoteKey.ToString() });
                            }
                        }
                    }
                }

                // Sorting the dictionary by key
                var sortedList = noteList.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                int prevPos = 0;
                foreach (var noteListElement in sortedList)
                {
                    if ("note_on".Equals(noteListElement.Value[0]))
                    {
                        trackChunk.Events.Add(new NoteOnEvent { DeltaTime = noteListElement.Key - prevPos, NoteNumber = (SevenBitNumber)int.Parse(noteListElement.Value[1]), Velocity = (SevenBitNumber)int.Parse(noteListElement.Value[2]) });
                    }
                    else if ("note_off".Equals(noteListElement.Value[0]))
                    {
                        trackChunk.Events.Add(new NoteOffEvent { DeltaTime = noteListElement.Key - prevPos, NoteNumber = (SevenBitNumber)int.Parse(noteListElement.Value[1]), Velocity = (SevenBitNumber)0 });
                    }

                    prevPos = (int)noteListElement.Key;
                }

                trackNum++;
            }

            // Save the MIDI file to disk
            string filePath = "output.mid";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            midiFile.Write(filePath);
        }
    }
}