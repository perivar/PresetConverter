using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

using Serilog;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;

namespace PresetConverter
{
    public static class AbletonProject
    {
        private static string GetValue(XElement xmldata, string varname, string fallback)
        {
            XElement? xml_e = xmldata.XPathSelectElements(varname).FirstOrDefault();
            return xml_e?.Attribute("Value")?.Value ?? fallback;
        }

        private static string? GetId(XElement xmldata, string varname, string? fallback)
        {
            XElement? xml_e = xmldata.XPathSelectElements(varname).FirstOrDefault();
            return xml_e?.Attribute("Id")?.Value ?? fallback;
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

        private static object GetParam(XElement xmldata, string varname, string vartype, double fallback, string[] i_loc, double? i_addmul)
        {
            XElement? param_data = xmldata.XPathSelectElements(varname).FirstOrDefault();

            if (param_data != null)
            {
                string out_value = GetValue(param_data, "Manual", fallback.ToString());
                int autonumid = int.Parse(GetId(param_data, "AutomationTarget", null) ?? "0");
                object outdata = UseValueType(vartype, out_value);

                if (autonumid != 0)
                {
                    // AutoId.InDefine(autonumid, i_loc, vartype, i_addmul);
                }

                return outdata;
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

        public static void HandleAbletonLiveContent(XElement root)
        {
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

            // start reading
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

            // TracksMaster.Create(cvpj, masTrackVol);
            // TracksMaster.Visual(cvpj, name: mastertrackName, color: mastertrackColor);
            // TracksMaster.ParamAdd(cvpj, "pan", masTrackPan, "float");
            // Song.AddParam(cvpj, "bpm", tempo);

            Log.Debug("Tempo: {0} bpm, MasterTrackName: {1}, Volume: {2}, Pan: {3}", tempo, mastertrackName, masTrackVol, masTrackPan);

            // https://gist.github.com/melanchall/d4142f5f0fb36ab86e46110d69966fed
            var midiFile = new MidiFile();

            // Set tempo map of the file using tempo of X BPM. See
            // https://github.com/melanchall/drywetmidi/wiki/Tempo-map to learn more about managing
            // tempo map in DryWetMIDI
            var tempoMap = TempoMap.Create(Tempo.FromBeatsPerMinute(tempo));
            midiFile.ReplaceTempoMap(tempoMap);

            // Read Tracks
            // int sendnum = 1;
            // int returnid = 1;

            // var abletondatadefParams = new Dictionary<string, object>();
            // var abletondatadefData = new Dictionary<string, object>();
            var cvpj = new Dictionary<string, object>();

            Log.Debug("Reading {0} Tracks ...", xTracks.Elements().Count());

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
                    //  resetting the time offset data
                    float timeOffset = 0;
                    float nextTimeOffset = 0;

                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", 0, new string[] { "track", trackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", 0, new string[] { "track", trackId, "pan" }, null);

                    Log.Debug("Reading MIDI Track. Id: {0}, Name: {1}, Vol: {2}, Pan: {3}", trackId, trackName, trackVol, trackPan);

                    // TracksR.TrackCreate(cvpj, trackId, "instrument");
                    // TracksR.TrackVisual(cvpj, trackId, name: trackName, color: trackColor);
                    // TracksR.TrackParamAdd(cvpj, trackId, "vol", trackVol, "float");
                    // TracksR.TrackParamAdd(cvpj, trackId, "pan", trackPan, "float");

                    Log.Debug("Creating MIDI Track: {0}", trackName);

                    // Create a track with name
                    var trackChunk = new TrackChunk();
                    midiFile.Chunks.Add(trackChunk);
                    trackChunk.Events.Add(new SequenceTrackNameEvent(trackName));

                    if (trackInsideGroup != -1)
                    {
                        // TracksR.TrackGroup(cvpj, trackId, "group_" + trackInsideGroup);
                    }

                    XElement xTrackMainSequencer = xTrackDeviceChain?.Element("MainSequencer");
                    XElement xTrackClipTimeable = xTrackMainSequencer?.Element("ClipTimeable");
                    XElement xTrackArrangerAutomation = xTrackClipTimeable?.Element("ArrangerAutomation");
                    XElement xTrackEvents = xTrackArrangerAutomation?.Element("Events");
                    IEnumerable<XElement> xTrackMidiClips = xTrackEvents?.Elements("MidiClip");

                    foreach (XElement xTrackMidiClip in xTrackMidiClips)
                    {
                        // raising the time offset for the next clip inside this track
                        timeOffset = timeOffset + nextTimeOffset;

                        float notePlacementPos = float.Parse(GetValue(xTrackMidiClip, "CurrentStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementDur = float.Parse(GetValue(xTrackMidiClip, "CurrentEnd", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        string notePlacementName = GetValue(xTrackMidiClip, "Name", "");
                        var notePlacementColor = colorlistOne[int.Parse(GetValue(xTrackMidiClip, "Color", "0"))];
                        bool notePlacementMuted = bool.Parse(GetValue(xTrackMidiClip, "Disabled", "false"));

                        Log.Debug("Reading MidiClip. pos: {0}, dur: {1}, name: {2}, color: {3}, muted: {4}", notePlacementPos, notePlacementDur, notePlacementName, notePlacementColor, notePlacementMuted);

                        var notePlacement = new Dictionary<string, object>
                        {
                            { "position", notePlacementPos * 4 },
                            { "duration", notePlacementDur * 4 - (notePlacementPos * 4) },
                            { "name", notePlacementName },
                            { "color", notePlacementColor },
                            { "muted", notePlacementMuted }
                        };

                        XElement xTrackMidiClipLoop = xTrackMidiClip.Element("Loop");
                        float notePlacementLoopLStart = float.Parse(GetValue(xTrackMidiClipLoop, "LoopStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementLoopLEnd = float.Parse(GetValue(xTrackMidiClipLoop, "LoopEnd", "1"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        float notePlacementLoopStart = float.Parse(GetValue(xTrackMidiClipLoop, "StartRelative", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        bool notePlacementLoopOn = bool.Parse(GetValue(xTrackMidiClipLoop, "LoopOn", "false"));

                        Log.Debug("Reading MidiLoop. lstart: {0}, lend: {1}, start: {2}, on: {3}", notePlacementLoopLStart, notePlacementLoopLEnd, notePlacementLoopStart, notePlacementLoopOn);

                        // store the next time offset
                        nextTimeOffset = notePlacementLoopLEnd;

                        if (notePlacementLoopOn)
                        {
                            notePlacement["cut"] = CutLoopData(notePlacementLoopStart * 4, notePlacementLoopLStart * 4, notePlacementLoopLEnd * 4);
                        }
                        else
                        {
                            notePlacement["cut"] = new Dictionary<string, object>
                            {
                                { "type", "cut" },
                                { "start", notePlacementLoopLStart * 4 },
                                { "end", notePlacementLoopLEnd * 4}
                            };
                        }

                        Log.Debug("notePlacement: {0}", notePlacement);

                        XElement xTrackMidiClipNotes = xTrackMidiClip.Element("Notes");
                        XElement xTrackMidiClipKT = xTrackMidiClipNotes?.Element("KeyTracks");

                        Log.Debug("Reading {0} KeyTracks ...", xTrackMidiClipKT?.Elements("KeyTrack").Count());

                        var notes = new Dictionary<int, Dictionary<string, object>>();

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

                                Log.Debug("Reading MidiNoteEvent. time: {0}, dur: {1}, key: {2}, vel: {3}, off_vel: {4}, noteId: {5}, timeOffset: {6}", noteTime, noteDuration, midiKey, noteVelocity, noteOffVelocity, noteId, timeOffset);

                                var noteData = new Dictionary<string, object>
                                {
                                    { "key", abletonNoteKey },
                                    { "position", noteTime * 4 },
                                    { "duration", noteDuration * 4 },
                                    { "vol", noteVelocity / 100 },
                                    { "off_vol", noteOffVelocity / 100 },
                                    { "probability", noteProbablity },
                                    { "enabled", noteIsEnabled }
                                };

                                Log.Debug("noteData: {0}", noteData);

                                notes[noteId] = noteData;

                                // add midi notes
                                int midiNotePos = (int)((float)notePlacement["position"] + (float)noteData["position"]);// * 30;
                                int midiNoteDur = (int)((float)noteData["duration"]);// * 30;
                                int midiNoteKey = (int)noteData["key"] + 60;
                                int midiNoteVol = Math.Clamp((int)((float)noteData["vol"] * 127), 0, 127);

                                long deltaTimeOn = midiNotePos;
                                long deltaTimeOff = midiNotePos + midiNoteDur;

                                Log.Debug("Creating MidiNoteEvent. deltaTimeOn: {0}, deltaTimeOff: {1}, midiNotePos: {2}, midiNoteDur: {3}, midiNoteKey: {4}, midiNoteVol: {5}", deltaTimeOn, deltaTimeOff, midiNotePos, midiNoteDur, midiNoteKey, midiNoteVol);

                                trackChunk.Events.Add(new NoteOnEvent { DeltaTime = deltaTimeOn, NoteNumber = (SevenBitNumber)midiNoteKey, Velocity = (SevenBitNumber)midiNoteVol });
                                trackChunk.Events.Add(new NoteOffEvent { DeltaTime = deltaTimeOff, NoteNumber = (SevenBitNumber)midiNoteKey, Velocity = (SevenBitNumber)0 });

                                // long deltaTimeOn = (long)(noteTime + timeOffset);
                                // long deltaTimeOff = (long)(noteDuration * tempo);
                                // trackChunk.Events.Add(new NoteOnEvent { DeltaTime = deltaTimeOn, NoteNumber = (SevenBitNumber)midiKey, Velocity = (SevenBitNumber)noteVelocity });
                                // trackChunk.Events.Add(new NoteOffEvent { DeltaTime = deltaTimeOff, NoteNumber = (SevenBitNumber)midiKey, Velocity = (SevenBitNumber)noteOffVelocity });
                            }
                        }

                        // XElement xTrackMidiClipNES = xTrackMidiClipNotes.Element("PerNoteEventStore");
                        // XElement xTrackMidiClipNES_EL = xTrackMidiClipNES?.Element("EventLists");

                        // foreach (XElement xNoteNEvent in xTrackMidiClipNES_EL?.Elements("PerNoteEventList") ?? Enumerable.Empty<XElement>())
                        // {
                        //     int autoNoteId = int.Parse(xNoteNEvent.Attribute("NoteId")?.Value ?? "0");
                        //     int autoNoteCC = int.Parse(xNoteNEvent.Attribute("CC")?.Value ?? "0");

                        //     notes[autoNoteId]["notemod"] = new Dictionary<string, object>
                        //     {
                        //         { "auto", new Dictionary<string, object>() }
                        //     };

                        //     if (autoNoteCC == -2)
                        //     {
                        //         // tNotes[autoNoteId]["notemod"]["auto"]["pitch"] = new List<object>();

                        //         // var cvpjNoteAutoPitch = tNotes[autoNoteId]["notemod"]["auto"]["pitch"];

                        //         // foreach (XElement abletonPoint in xNoteNEvent.Elements("Events").Elements("PerNoteEvent") ?? Enumerable.Empty<XElement>())
                        //         // {
                        //         //     float apPos = float.Parse(abletonPoint.Attribute("TimeOffset")?.Value ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture) * 4;
                        //         //     float apVal = float.Parse(abletonPoint.Attribute("Value")?.Value ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture) / 170;

                        //         //     cvpjNoteAutoPitch.Add(new Dictionary<string, object>
                        //         //     {
                        //         //         { "position", apPos },
                        //         //         { "value", apVal }
                        //         //     });
                        //         // }
                        //     }
                        // }

                        notePlacement["notelist"] = notes;

                        // AddNotes(cvpj, trackId, "notes", notePlacement);
                    }
                }
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