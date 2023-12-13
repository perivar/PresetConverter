using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

using Serilog;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CommonUtils;

namespace PresetConverter
{
    // output booleans as 1 and 0 instead of true and false
    class BooleanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((bool)value ? 1 : 0);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    class MidiChannelManager
    {
        private int unusedChannel = 0;

        public MidiChannelManager()
        {
            this.unusedChannel = 0;
        }

        public MidiChannelManager(int firstChannel)
        {
            if (firstChannel > 1)
            {
                this.unusedChannel = firstChannel - 1; // midi channels are zero indexed
            }
            else
            {
                throw new ArgumentException("First channel must be 1 or more");
            }
        }

        public int GetUnusedChannel()
        {
            unusedChannel++;
            if (unusedChannel == 10)
            {
                unusedChannel++;
            }
            if (unusedChannel == 16)
            {
                unusedChannel = 1;
            }
            return unusedChannel - 1; // midi channels are zero indexed
        }
    }

    public static class AbletonProject
    {
        private static string GetXPath(XElement? element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var ancestors = element.AncestorsAndSelf().Select(e => e.Name.ToString());
            return string.Join("/", ancestors.Reverse());
        }

        private static string GetValue(XElement xmlData, string varName, string fallback)
        {
            XElement? xElement = xmlData.Descendants(varName).FirstOrDefault();
            return xElement?.Attribute("Value")?.Value ?? fallback;
        }

        private static string GetId(XElement xmlData, string varName, string fallback)
        {
            XElement? xElement = xmlData.Descendants(varName).FirstOrDefault();
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

        private static object GetParam(XElement xmlData, string varName, string varType, string fallback, string[]? loc, double? addMul)
        {
            XElement? xElement = xmlData.Descendants(varName).FirstOrDefault();

            if (xElement != null)
            {
                string inData = GetValue(xElement, "Manual", fallback);
                int autoNumId = int.Parse(GetId(xElement, "AutomationTarget", null) ?? "0");
                object outData = UseValueType(varType, inData);

                if (autoNumId != 0)
                {
                    InDefine(autoNumId, loc, varType, addMul);
                }

                return outData;
            }
            else
            {
                return fallback;
            }
        }

        public static void GetAuto(XElement xTrackData)
        {
            dynamic trackAutomationEnvelopes = xTrackData.Element("AutomationEnvelopes")?.Elements("Envelopes").FirstOrDefault();

            if (trackAutomationEnvelopes != null)
            {
                foreach (dynamic automationEnvelope in trackAutomationEnvelopes.Elements("AutomationEnvelope"))
                {
                    dynamic envEnvelopeTarget = automationEnvelope.Element("EnvelopeTarget");
                    dynamic envAutomation = automationEnvelope.Element("Automation");
                    dynamic envAutoEvents = envAutomation.Element("Events");

                    int autoTarget = int.Parse(GetValue(envEnvelopeTarget, "PointeeId", "-1"));

                    var cvpjAutoPoints = new List<dynamic>();
                    foreach (var envAutoEvent in envAutoEvents.Elements())
                    {
                        if (envAutoEvent.Name == "FloatEvent")
                        {
                            dynamic point = new System.Dynamic.ExpandoObject();
                            point.position = Math.Max(0, double.Parse(envAutoEvent.Attribute("Time").Value, NumberStyles.Any, CultureInfo.InvariantCulture) * 4);
                            point.value = double.Parse(envAutoEvent.Attribute("Value").Value, NumberStyles.Any, CultureInfo.InvariantCulture);

                            cvpjAutoPoints.Add(point);
                        }
                        else if (envAutoEvent.Name == "BoolEvent")
                        {
                            dynamic point = new System.Dynamic.ExpandoObject();
                            point.position = Math.Max(0, double.Parse(envAutoEvent.Attribute("Time").Value, NumberStyles.Any, CultureInfo.InvariantCulture) * 4);
                            point.value = (double)(bool.Parse(envAutoEvent.Attribute("Value").Value) ? 1.0 : 0.0);

                            cvpjAutoPoints.Add(point);
                        }
                    }

                    if (autoTarget > 0 && cvpjAutoPoints.Count > 0)
                    {
                        InAddPointList(autoTarget, ToPointList(cvpjAutoPoints));
                    }
                }
            }
        }

        private static dynamic ToPointList(List<dynamic> pointsData)
        {
            // auto_nopl.py: def to_pl(pointsdata):

            dynamic autoPointList = new System.Dynamic.ExpandoObject();
            var durPos = AbletonFunctions.GetDurPos(pointsData, 0);

            autoPointList.position = durPos.Item1;
            autoPointList.duration = durPos.Item2 - durPos.Item1 + 4;
            autoPointList.points = AbletonFunctions.TrimMove(pointsData, durPos.Item1, durPos.Item1 + durPos.Item2);

            return autoPointList;
        }

        static SortedDictionary<int, dynamic> inData = new SortedDictionary<int, dynamic>();
        static SortedDictionary<int, dynamic> automationTargetLookup = new SortedDictionary<int, dynamic>();

        // ------------------------ autoid to cvpjauto ------------------------

        private static void InDefine(int id, string[]? loc, string type, double? addMul)
        {
            // auto_id.py: def in_define(i_id, i_loc, i_type, i_addmul):

            // if (!inData.ContainsKey(id))
            // {
            //     // inData[id] = new List<dynamic?> { loc, type, addMul, new List<dynamic>() };
            //     inData[id] = new List<dynamic>();
            // }
            // else
            // {
            //     // inData[id][0] = loc;
            //     // inData[id][1] = type;
            //     // inData[id][2] = addMul;
            // }
        }

        private static void InAddPointList(int id, dynamic autoPointList)
        {
            // auto_id.py: def in_add_pl(i_id, i_autopl):

            if (!inData.ContainsKey(id))
            {
                // even if we have no targets added, create a default one
                // inData[id] = new List<dynamic?> { new string[] { id.ToString() }, "float", null, new List<dynamic>() };
                inData[id] = new List<dynamic>();
            }

            // inData[id][3].Add(autoPl);
            inData[id].Add(autoPointList);
        }

        private static Tuple<string, string> SplitXPath(string input, string pattern, string removePrefix, string removeSuffix)
        {
            int lastIndexOfPattern = input.LastIndexOf(pattern);

            string beforeLastPattern = lastIndexOfPattern != -1 ? input.Substring(0, lastIndexOfPattern) : input;
            string afterLastPattern = lastIndexOfPattern != -1 ? input.Substring(lastIndexOfPattern + pattern.Length) : input;

            // Remove the specified suffix from afterLastPattern
            afterLastPattern = afterLastPattern.EndsWith(removeSuffix) ? afterLastPattern.Substring(0, afterLastPattern.Length - removeSuffix.Length) : afterLastPattern;

            // Remove the specified prefix from beforeLastPattern
            beforeLastPattern = beforeLastPattern.StartsWith(removePrefix) ? beforeLastPattern.Substring(removePrefix.Length) : beforeLastPattern;

            return Tuple.Create(beforeLastPattern, afterLastPattern);
        }

        private static void InOutput(dynamic cvpj)
        {
            foreach (var id in inData.Keys)
            {
                // var outAutoLoc = inData[id][0];
                // var outAutoType = inData[id][1];
                // var outAutoAddMul = inData[id][2];
                // var outAutoData = inData[id][3];

                // if ((inData[id][0] != null || inData[id][1] != null || inData[id][2] != null) && outAutoData.Count > 0)
                // {
                //     if (outAutoAddMul != null)
                //     {
                //         outAutoData = AbletonFunctions.Multiply(outAutoData, outAutoAddMul[0], outAutoAddMul[1]);
                //     }

                //     AddPointList(cvpj, outAutoType, outAutoLoc, outAutoData);
                // }

                var outAutoData = inData[id];
                if (outAutoData.Count > 0)
                {
                    // AddPointList(cvpj, outAutoData);

                    // lookup the target
                    bool isFound = automationTargetLookup.TryGetValue(id, out dynamic autoTarget);
                    if (isFound)
                    {
                        string trackId = autoTarget.trackid;
                        string trackName = autoTarget.trackname;
                        string[] fxLoc = autoTarget.loc;
                        string[] fxLocDetails = autoTarget.details;
                        string path = autoTarget.path;

                        AddPointList(cvpj, "float", fxLoc.Concat(new[] { trackName, path }).ToArray(), outAutoData);
                    }
                    else
                    {

                    }
                }
            }
        }

        private static void AddPointList(dynamic cvpj, string valType, string[] autoLocation, List<dynamic> inAutoPoints)
        {
            if (autoLocation != null)
            {
                DataValues.NestedDictAddValue(cvpj, new[] { "automation" }.Concat(autoLocation).Concat(new[] { "type" }).ToArray(), valType);
                DataValues.NestedDictAddToList(cvpj, new[] { "automation" }.Concat(autoLocation).Concat(new[] { "placements" }).ToArray(), inAutoPoints);
            }
        }


        private static dynamic CutLoopData(double start, double loopStart, double loopEnd)
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

        private static double[] HexToRgbDouble(string hex)
        {
            // Convert a hexadecimal value #FF00FF to RGB. Returns a array of double between 0 and 1.

            hex = hex.TrimStart('#');
            if (hex.Length != 6)
            {
                throw new ArgumentException("Invalid hexadecimal color code");
            }

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            double factor = 1.0 / 255.0f;

            return new double[] { r * factor, g * factor, b * factor };
        }

        private static byte[] RgbDoubleToRgbBytes(double[] rgb)
        {
            return new byte[] { (byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255) };
        }

        private static void AddCmd(Dictionary<long, List<List<string>>> list, long pos, List<string> cmd)
        {
            if (!list.ContainsKey(pos))
                list[pos] = new List<List<string>>();

            list[pos].Add(cmd);
        }

        public static void HandleAbletonLiveContent(XElement rootXElement, string file, string outputDirectoryPath)
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

            var colorlistOne = new List<double[]>();
            foreach (string hexColor in colorlist)
            {
                var rgbFloatColor = HexToRgbDouble(hexColor);
                colorlistOne.Add(rgbFloatColor);
            }

            // ***************** 
            // start reading
            // ***************** 
            dynamic cvpj = new System.Dynamic.ExpandoObject(); // store in common daw project format (converted project)

            string abletonVersion = rootXElement?.Attribute("MinorVersion")?.Value.Split('.')[0];
            if (abletonVersion != "11")
            {
                Log.Error("Ableton version " + abletonVersion + " is not supported.");
                return;
            }

            XElement xLiveSet = rootXElement.Element("LiveSet");
            XElement xTracks = xLiveSet?.Element("Tracks");
            XElement xMasterTrack = xLiveSet?.Element("MasterTrack");
            XElement xMasterTrackDeviceChain = xMasterTrack?.Element("DeviceChain");
            XElement xMasterTrackMixer = xMasterTrackDeviceChain?.Element("Mixer");
            XElement xMasterTrackDeviceChainInside = xMasterTrackDeviceChain?.Element("DeviceChain");
            XElement xMasterTrackTrackDevices = xMasterTrackDeviceChainInside?.Element("Devices");
            DoDevices(rootXElement, xMasterTrackTrackDevices, null, "Master", new string[] { "master", "master_1" }, outputDirectoryPath, file);

            XElement xMastertrackName = xMasterTrack?.Element("Name");
            string mastertrackName = GetValue(xMastertrackName, "EffectiveName", "");
            var mastertrackColor = colorlistOne[int.Parse(GetValue(xMasterTrack, "Color", "0"))];
            float masTrackVol = (float)GetParam(xMasterTrackMixer, "Volume", "float", "0", new string[] { "master", "vol" }, null);
            float masTrackPan = (float)GetParam(xMasterTrackMixer, "Pan", "float", "0", new string[] { "master", "pan" }, null);
            float tempo = (float)GetParam(xMasterTrackMixer, "Tempo", "float", "140", new string[] { "main", "bpm" }, null);

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
            cvpj.track_order = new List<dynamic>();
            cvpj.track_placements = new Dictionary<string, dynamic>();

            // Read Tracks
            Log.Debug("Found {0} Tracks ...", xTracks.Elements().Count());

            int returnId = 1;

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

                var fxLoc = new string[0];

                if (tracktype == "MidiTrack")
                {
                    string midiTrackId = "midi_" + trackId.ToString();
                    fxLoc = new string[] { "track", midiTrackId };
                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", "0", new string[] { "track", trackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", "0", new string[] { "track", trackId, "pan" }, null);

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
                    cvpj.track_order.Add(trackId);

                    XElement xTrackMainSequencer = xTrackDeviceChain?.Element("MainSequencer");
                    XElement xTrackClipTimeable = xTrackMainSequencer?.Element("ClipTimeable");
                    XElement xTrackArrangerAutomation = xTrackClipTimeable?.Element("ArrangerAutomation");
                    XElement xTrackEvents = xTrackArrangerAutomation?.Element("Events");
                    IEnumerable<XElement> xTrackMidiClips = xTrackEvents?.Elements("MidiClip");

                    var notesList = new List<dynamic>();

                    foreach (XElement xTrackMidiClip in xTrackMidiClips)
                    {
                        double notePlacementPos = double.Parse(GetValue(xTrackMidiClip, "CurrentStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        double notePlacementDur = double.Parse(GetValue(xTrackMidiClip, "CurrentEnd", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
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
                        double notePlacementLoopLStart = double.Parse(GetValue(xTrackMidiClipLoop, "LoopStart", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        double notePlacementLoopLEnd = double.Parse(GetValue(xTrackMidiClipLoop, "LoopEnd", "1"), NumberStyles.Any, CultureInfo.InvariantCulture);
                        double notePlacementLoopStart = double.Parse(GetValue(xTrackMidiClipLoop, "StartRelative", "0"), NumberStyles.Any, CultureInfo.InvariantCulture);
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

                            // Log.Verbose("Reading {0} MidiNoteEvents ...", xTrackMidiClipKT_KT_Notes?.Elements("MidiNoteEvent").Count());

                            foreach (XElement xTrackMidiClipMNE in xTrackMidiClipKT_KT_Notes?.Elements("MidiNoteEvent"))
                            {
                                double noteTime = double.Parse(xTrackMidiClipMNE.Attribute("Time").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                double noteDuration = double.Parse(xTrackMidiClipMNE.Attribute("Duration").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                double noteVelocity = double.Parse(xTrackMidiClipMNE.Attribute("Velocity").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                double noteOffVelocity = double.Parse(xTrackMidiClipMNE.Attribute("OffVelocity").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                double noteProbablity = double.Parse(xTrackMidiClipMNE.Attribute("Probability").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                bool noteIsEnabled = bool.Parse(xTrackMidiClipMNE.Attribute("IsEnabled").Value);
                                int noteId = int.Parse(xTrackMidiClipMNE.Attribute("NoteId").Value);

                                // Log.Verbose($"Reading MidiNoteEvent. Time: {noteTime}, Duration: {noteDuration}, MidiKey: {midiKey}, Velocity: {noteVelocity}, OffVelocity: {noteOffVelocity}, NoteId: {noteId}");

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
                                var xNoteNEvent_EV = xNoteNEvent.Descendants("Events");

                                foreach (var abletonPoint in xNoteNEvent_EV.Elements("PerNoteEvent"))
                                {
                                    double apPos = double.Parse(abletonPoint.Attribute("TimeOffset").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                    double apVal = double.Parse(abletonPoint.Attribute("Value").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                                    cvpjNoteAutoPitch.Add(new { position = apPos * 4, value = apVal / 170 });
                                }
                            }
                        }

                        // convert notes dictionary to values list since we don't need the notes-id 
                        notePlacement.notelist = notes.Values.ToList();
                        notesList.Add(notePlacement);
                    }

                    // add the track if there are notes
                    if (notesList.Count > 0)
                    {
                        DataValues.NestedDictAddToList(cvpj, new[] { "track_placements", trackId, "notes" }, notesList);
                    }
                }

                if (tracktype == "AudioTrack")
                {
                    string audioTrackId = "audio_" + trackId.ToString();
                    fxLoc = new string[] { "track", audioTrackId };
                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", "0", new string[] { "track", trackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", "0", new string[] { "track", trackId, "pan" }, null);

                    Log.Debug($"Reading Audio Track. Id: {trackId}, EffectiveName: {trackName}, Volume: {trackVol}, Pan: {trackPan}");
                }

                if (tracktype == "ReturnTrack")
                {
                    string returnTrackId = "return_" + returnId.ToString();
                    fxLoc = new string[] { "return", returnTrackId };
                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", "0", new string[] { "return", returnTrackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", "0", new string[] { "return", returnTrackId, "pan" }, null);

                    Log.Debug($"Reading Return Track. Id: {trackId}, EffectiveName: {trackName}, Volume: {trackVol}, Pan: {trackPan}");
                    returnId++;
                }

                if (tracktype == "GroupTrack")
                {
                    string groupTrackId = "group_" + trackId.ToString();
                    fxLoc = new string[] { "group", groupTrackId };
                    float trackVol = (float)GetParam(xTrackMixer, "Volume", "float", "0", new string[] { "group", groupTrackId, "vol" }, null);
                    float trackPan = (float)GetParam(xTrackMixer, "Pan", "float", "0", new string[] { "group", groupTrackId, "pan" }, null);

                    Log.Debug($"Reading Group Track. Id: {trackId}, EffectiveName: {trackName}, Volume: {trackVol}, Pan: {trackPan}");
                }

                if (fxLoc.Length > 0)
                {
                    GetAuto(xTrackData);

                    XElement xTrackDeviceChainInside = xTrackDeviceChain?.Element("DeviceChain");
                    XElement xTrackDevices = xTrackDeviceChainInside?.Element("Devices");
                    DoDevices(rootXElement, xTrackDevices, trackId, trackName, fxLoc, outputDirectoryPath, file);
                }
            }

            GetAuto(xMasterTrack);
            InOutput(cvpj);

            // JObject jcvpj = JObject.FromObject(cvpj);
            // WriteJsonToFile("output_pre_compat.json", jcvpj);

            // fix output
            Compat(cvpj);

            // JObject jcvpj2 = JObject.FromObject(cvpj);
            // WriteJsonToFile("output_post_compat.json", jcvpj2);

            // string jsonFilePath1 = "output_pre_compat.json";
            // string jsonFilePath2 = "output_post_compat.json";
            // CompareCvpJson(jsonFilePath1, jsonFilePath2, false, false);
            // string jsonFilePath1 = "output_post_compat.json";
            // string jsonFilePath2 = "..\\DawVert\\midiinput.cvpj";
            // // string jsonFilePath2 = "..\\DawVert\\out.cvpj";
            // CompareCvpJson(jsonFilePath1, jsonFilePath2, false, true);

            ConvertToMidi(cvpj, file, outputDirectoryPath, false);

            ConvertAutomationToMidi(cvpj, file, outputDirectoryPath, true);
        }

        private static void AddAutomationTargets(XElement rootXElement, XElement xDevice, string? trackId, string? trackName, string[] fxLoc, string[] fxLocDetails)
        {
            // add all AutomationTargets
            var xAutomationTargets = xDevice.Descendants("AutomationTarget");
            foreach (var xAutomationTarget in xAutomationTargets)
            {
                int autoNumId = int.Parse(xAutomationTarget?.Attribute("Id")?.Value ?? "0");
                if (autoNumId > 0)
                {
                    // get the path
                    string path = GetXPath(xAutomationTarget);

                    // Get path elements
                    // "Ableton/LiveSet/Tracks/GroupTrack/DeviceChain/DeviceChain/Devices/AudioEffectGroupDevice/Branches/AudioEffectBranch/DeviceChain/AudioToAudioDeviceChain/Devices/AutoFilter/Cutoff/AutomationTarget"
                    // "Ableton/LiveSet/Tracks/GroupTrack/DeviceChain/DeviceChain/Devices/AudioEffectGroupDevice/Branches/AudioEffectBranch/DeviceChain/AudioToAudioDeviceChain/Devices/PluginDevice/ParameterList/PluginFloatParameter/ParameterValue/AutomationTarget"

                    // Split the path into before and after the last "Devices/" and trim first and last element
                    var paths = SplitXPath(path, "Devices/", "Ableton/", "/AutomationTarget");

                    // Replace / with _
                    string pathFixed = paths.Item2;
                    pathFixed = pathFixed.Replace("/", "_");

                    if (pathFixed.StartsWith("PluginDevice"))
                    {
                        // lookup VST Plugin Name using the path
                        var lookupXPath = $"{paths.Item1}Devices/PluginDevice";

                        // Use XPathSelectElement with the full path
                        XElement? xFoundElement = rootXElement.XPathSelectElement(lookupXPath);
                        if (xFoundElement != null)
                        {
                            XElement? xPluginDesc = xFoundElement?.Element("PluginDesc");
                            XElement? xVstPluginInfo = xPluginDesc?.Element("VstPluginInfo");
                            string vstPlugName = GetValue(xVstPluginInfo, "PlugName", "Unknown");

                            // remove PluginDevice/
                            // pathFixed = pathFixed.Replace("PluginDevice", "");

                            // and add back the path suffix
                            // pathFixed = $"{vstPlugName}{pathFixed}";

                            pathFixed = vstPlugName;
                        }
                    }

                    // add
                    dynamic autoTarget = new System.Dynamic.ExpandoObject();
                    autoTarget.trackid = trackId;
                    autoTarget.trackname = trackName;
                    autoTarget.loc = fxLoc;
                    autoTarget.details = fxLocDetails;
                    autoTarget.path = pathFixed;

                    automationTargetLookup.Add(autoNumId, autoTarget);
                }
            }
        }

        public static void DoDevices(XElement rootXElement, XElement xTrackDevices, string? trackId, string? trackName, string[] fxLoc, string outputDirectoryPath, string file)
        {
            // Path for MasterTrack: Ableton/LiveSet/MasterTrack/DeviceChain/DeviceChain/Devices/*            
            // Path for Tracks: Ableton/LiveSet/Tracks/[Audio|Group|Midi]Track/DeviceChain/DeviceChain/Devices/*
            // where * is internal plugins like <Eq8>, <Limiter> 
            // as well as <PluginDevice Id="X"> elements 

            // Read Tracks
            Log.Debug("Found {0} Devices for track: {1} {2}", xTrackDevices.Elements().Count(), trackName, trackId ?? "");

            string fileNameNoExtension = Path.GetFileNameWithoutExtension(file);

            foreach (XElement xDevice in xTrackDevices.Elements())
            {
                // reset for each element
                string outputFileName = string.IsNullOrEmpty(trackName) ? fileNameNoExtension : string.Format($"{fileNameNoExtension} - {trackName}");
                string outputFilePath;

                string deviceType = xDevice.Name.LocalName;
                int deviceId = int.Parse(xDevice?.Attribute("Id")?.Value ?? "0");

                // check if it's on
                XElement xOn = xDevice?.Element("On");
                bool isOn = bool.Parse(GetValue(xOn, "Manual", "false"));

                if (!isOn)
                {
                    Log.Debug($"Skipping Device {deviceId} {deviceType} since it is disabled!");
                    continue;
                }
                else
                {
                    Log.Debug($"Processing Device {deviceId} {deviceType} ...");
                }

                AddAutomationTargets(rootXElement, xDevice, trackId, trackName, fxLoc, new string[] { deviceType, deviceId.ToString() });

                switch (deviceType)
                {
                    case "Eq8":
                        // Convert EQ8 to Steinberg Frequency
                        var eq = new AbletonEq8(xDevice);
                        var steinbergFrequency = eq.ToSteinbergFrequency();
                        outputFilePath = Path.Combine(outputDirectoryPath, "Frequency", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Frequency"));

                        Log.Information($"Writing EQ8 Preset: {outputFileName}");

                        steinbergFrequency.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", steinbergFrequency.ToString());

                        // convert to Fabfilter Pro Q3 as well
                        var fabfilterProQ3 = eq.ToFabfilterProQ3();
                        outputFileName = string.Format($"{outputFileName} - {deviceId} EQ8ToFabfilterProQ3");
                        outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);
                        fabfilterProQ3.WriteFFP(outputFilePath + ".ffp");
                        break;
                    case "Compressor2":
                        // Convert Compressor2 to Steinberg Compressor
                        var compressor = new AbletonCompressor(xDevice);
                        var steinbergCompressor = compressor.ToSteinbergCompressor();
                        outputFilePath = Path.Combine(outputDirectoryPath, "Compressor", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "Compressor"));

                        Log.Information($"Writing Compressor2 Preset: {outputFileName}");

                        steinbergCompressor.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", steinbergCompressor.ToString());
                        break;
                    case "GlueCompressor":
                        // Convert Glue compressor to Waves SSL Compressor
                        var glueCompressor = new AbletonGlueCompressor(xDevice);
                        var wavesSSLComp = glueCompressor.ToWavesSSLComp();
                        outputFilePath = Path.Combine(outputDirectoryPath, "SSLComp Stereo", "Ableton - " + outputFileName);
                        IOUtils.CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "SSLComp Stereo"));

                        Log.Information($"Writing GlueCompressor Preset: {outputFileName}");

                        wavesSSLComp.Write(outputFilePath + ".vstpreset");

                        // and dump the text info as well
                        File.WriteAllText(outputFilePath + ".txt", wavesSSLComp.ToString());
                        break;

                    case "PluginDevice":
                        // Handle Plugin Presets
                        // Path: PluginDevice/PluginDesc/VstPluginInfo/Preset/VstPreset

                        XElement xPluginDesc = xDevice?.Element("PluginDesc");
                        XElement xVstPluginInfo = xPluginDesc?.Element("VstPluginInfo");
                        int vstPluginInfoId = int.Parse(xVstPluginInfo?.Attribute("Id")?.Value ?? "0");
                        string vstPlugName = GetValue(xVstPluginInfo, "PlugName", "Unknown");
                        Log.Debug($"VstPluginInfo Id: {vstPluginInfoId}, VstPluginName: {vstPlugName}");

                        XElement xPreset = xVstPluginInfo?.Element("Preset");
                        XElement xVstPreset = xPreset?.Element("VstPreset");
                        int vstPresetId = int.Parse(xVstPreset?.Attribute("Id")?.Value ?? "0");
                        Log.Debug($"VstPreset Id: {vstPresetId}");

                        // read the byte data buffer
                        XElement xVstPluginBuffer = xVstPreset?.Element("Buffer");
                        byte[] vstPluginBufferBytes = GetInnerValueAsByteArray(xVstPluginBuffer);

                        outputFileName = string.Format($"{outputFileName} - {deviceId} {StringUtils.MakeValidFileName(vstPlugName)}");
                        outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);

                        // check if this is a zlib file
                        // Serum presets are zlib compressed, but don't deflate
                        // if (vstPluginBufferBytes[0] == 0x78 && vstPluginBufferBytes[1] == 0x01)
                        // {
                        //     Log.Debug($"Found ZLib compressed file! VstPluginName: {vstPluginName}");

                        //     // Skip the first two bytes as per
                        //     // https://stackoverflow.com/a/62756204
                        //     byte[] vstPluginBufferBytesTrimmed = new byte[vstPluginBufferBytes.Length - 2];
                        //     Array.Copy(vstPluginBufferBytes, 2, vstPluginBufferBytesTrimmed, 0, vstPluginBufferBytesTrimmed.Length);

                        //     byte[] vstPluginBytes = IOUtils.Deflate(vstPluginBufferBytesTrimmed);
                        //     BinaryFile.ByteArrayToFile(outputFilePath + "_deflated.dat", vstPluginBytes);
                        // }

                        // save preset
                        Log.Information($"Writing PluginDevice ({vstPlugName}) Preset: {outputFileName}");
                        switch (vstPlugName)
                        {
                            case "Sylenth1":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "syl1");
                                break;
                            case "Serum_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "XfsX");
                                break;
                            case "FabFilter Saturn 2":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "FS2a");
                                break;
                            case "FabFilter Pro-Q 3":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "FQ3p");
                                break;
                            case "FabFilter Pro-L 2":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "FL2p");
                                break;
                            case "OTT_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "XfTT");
                                break;
                            case "Endless Smile 64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "ENDS");
                                break;
                            case "soothe2_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "SthB");
                                break;
                            case "CamelCrusher":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "CaCr");
                                break;
                            case "Kickstart-64bit":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "CNKS");
                                break;
                            case "LFOTool_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "XffO");
                                break;
                            case "ValhallaRoom_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "Ruum");
                                break;
                            case "ValhallaVintageVerb_x64":
                                FXP.WriteRaw2FXP(outputFilePath + ".fxp", vstPluginBufferBytes, "vee3");
                                break;
                            default:
                                Log.Error($"Could not save PluginDevice Preset as FXP since I did not recognize vstplugin: {vstPlugName}");
                                BinaryFile.ByteArrayToFile(outputFilePath + ".dat", vstPluginBufferBytes);
                                break;
                        }
                        break;
                    case "MultibandDynamics":
                    case "AutoFilter":
                    case "Reverb":
                    case "Saturator":
                    case "Tuner":
                    case "StereoGain":
                    default:
                        outputFileName = string.Format($"{outputFileName} - {deviceId} {deviceType}");
                        outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);

                        Log.Information($"Writing {deviceType} Preset: {outputFileName}");
                        xDevice.Save(outputFilePath + ".xml");
                        break;
                }
            }
        }

        private static bool Compat(dynamic cvpj)
        {
            // this does the song_compat from DawVert
            // all credits go to SatyrDiamond and the DawVert code
            // https://github.com/SatyrDiamond/DawVert
            // song_compat.py: def makecompat(cvpj_l, cvpj_type):

            // loops_remove.py: def process_r(projJ, out__placement_loop)
            foreach (var track in cvpj.track_placements)
            {
                var trackId = track.Key;
                string trackName = cvpj.track_data[trackId].name;
                var notes = track.Value.notes;
                if (notes != null)
                {
                    Log.Debug("[compat] RemoveLoops: non-laned: {0} - {1}", trackId, trackName);
                    track.Value.notes = RemoveLoopsDoPlacements(notes, new HashSet<string>());
                }
            }

            // removecut.py: def process_r(projJ)
            foreach (var track in cvpj.track_placements)
            {
                var trackId = track.Key;
                string trackName = cvpj.track_data[trackId].name;
                var notes = track.Value.notes;
                if (notes != null)
                {
                    Log.Debug("[compat] RemoveCut: non-laned: {0} - {1}", trackId, trackName);
                    RemoveCutDoPlacements(notes);
                }
            }

            return true;
        }

        private static List<dynamic> RemoveLoopsDoPlacements(dynamic notePlacements, HashSet<string> outPlacementLoop)
        {
            List<dynamic> newPlacements = new List<dynamic>();

            foreach (var notePlacement in notePlacements)
            {
                if (notePlacement.cut != null)
                {
                    string cutType = notePlacement.cut.type;

                    if ((cutType == "loop" || cutType == "loop_off" || cutType == "loop_adv") && !outPlacementLoop.Contains(cutType))
                    {
                        dynamic notePlacementBase = AbletonFunctions.CloneExpandoObject(notePlacement);
                        dynamic notePlacementCut = notePlacement.cut;

                        // cast to dictionary to be able to check for and remove fields
                        var notePlacementBaseDict = (IDictionary<string, object>)notePlacementBase;
                        var notePlacementCutDict = (IDictionary<string, object>)notePlacementCut;

                        notePlacementBaseDict.Remove("cut");
                        notePlacementBaseDict.Remove("position");
                        notePlacementBaseDict.Remove("duration");

                        double loopBasePosition = notePlacement.position;
                        double loopBaseDuration = notePlacement.duration;

                        double loopStart = 0;
                        double loopLoopStart = 0;
                        double loopLoopEnd = loopBaseDuration;
                        if (notePlacementCutDict.ContainsKey("start")) loopStart = notePlacementCut.start;
                        if (notePlacementCutDict.ContainsKey("loopstart")) loopLoopStart = notePlacementCut.loopstart;
                        if (notePlacementCutDict.ContainsKey("loopend")) loopLoopEnd = notePlacementCut.loopend;

                        List<double[]> cutpoints = XtraMath.CutLoop(loopBasePosition, loopBaseDuration, loopStart, loopLoopStart, loopLoopEnd);

                        foreach (var cutpoint in cutpoints)
                        {
                            dynamic notePlacementCutted = AbletonFunctions.CloneExpandoObject(notePlacementBase);
                            notePlacementCutted.position = cutpoint[0];
                            notePlacementCutted.duration = cutpoint[1];
                            notePlacementCutted.cut = new System.Dynamic.ExpandoObject();
                            notePlacementCutted.cut.type = "cut";
                            notePlacementCutted.cut.start = cutpoint[2];
                            notePlacementCutted.cut.end = cutpoint[3];

                            newPlacements.Add(notePlacementCutted);
                        }
                    }
                    else
                    {
                        newPlacements.Add(notePlacement);
                    }
                }
                else
                {
                    newPlacements.Add(notePlacement);
                }
            }

            return newPlacements;
        }

        private static void RemoveCutDoPlacements(dynamic notePlacements)
        {
            foreach (var notePlacement in notePlacements)
            {
                if (notePlacement.cut != null)
                {
                    double cutEnd = notePlacement.duration;

                    if (notePlacement.cut.type == "cut")
                    {
                        double cutStart = notePlacement.cut.start ?? 0;
                        cutEnd += cutStart;

                        notePlacement.notelist = AbletonFunctions.TrimMove(notePlacement.notelist, cutStart, cutEnd);
                        // remove field cannot be done setting the field to null: notePlacement.cut = null;
                        ((IDictionary<string, object>)notePlacement).Remove("cut");
                    }
                }
            }
        }

        public static byte[] GetInnerValueAsByteArray(XElement? xVstPluginBuffer)
        {
            if (xVstPluginBuffer == null) return new byte[0];

            string vstPluginBuffer = xVstPluginBuffer.Value;
            vstPluginBuffer = vstPluginBuffer.Replace(" ", string.Empty)
                            .Replace("\n", string.Empty)
                            .Replace("\r", string.Empty)
                            .Replace("\t", string.Empty);

            // convert from string to byte array
            byte[] vstPluginBufferBytes = new byte[vstPluginBuffer.Length / 2];
            for (int i = 0; i < vstPluginBufferBytes.Length; i++)
            {
                vstPluginBufferBytes[i] = Convert.ToByte(vstPluginBuffer.Substring(i * 2, 2), 16);
            }

            return vstPluginBufferBytes;
        }

        private static void CompareCvpJson(string jsonFilePath1, string jsonFilePath2, bool doSkipFirstLineJson1, bool doSkipFirstLineJson2)
        {
            // Read JSON files
            string jsonContent1 = File.ReadAllText(jsonFilePath1);
            string jsonContent2 = File.ReadAllText(jsonFilePath2);

            // Parse JSON content
            // ignore the first line (e.g. "CONVPROJ****")
            if (doSkipFirstLineJson1) jsonContent1 = string.Join("\n", jsonContent1.Split('\n').Skip(1));
            JObject jsonObject1 = JObject.Parse(jsonContent1);

            // ignore the first line (e.g. "CONVPROJ****")
            if (doSkipFirstLineJson2) jsonContent2 = string.Join("\n", jsonContent2.Split('\n').Skip(1));
            JObject jsonObject2 = JObject.Parse(jsonContent2);

            // Filter out objects with a "notes" array
            List<JProperty> filteredObjects1 = jsonObject1["track_placements"]
                .OfType<JProperty>()
                .Where(property => property.Value["notes"] != null)
                .ToList();

            // Create an object with the filtered list
            JObject filteredObjectContainer1 = new JObject
            {
                { "track_placements", new JObject(filteredObjects1) }
            };

            JObject sortedJsonObject1 = SortJObjectAlphabetically(filteredObjectContainer1);
            WriteJsonToFile("compare_cvp_1.json", sortedJsonObject1);

            // Filter out objects with a "notes" array
            List<JProperty> filteredObjects2 = jsonObject2["track_placements"]
                .OfType<JProperty>()
                .Where(property => property.Value["notes"] != null)
                .ToList();

            // Create an object with the filtered list
            JObject filteredObjectContainer2 = new JObject
            {
                { "track_placements", new JObject(filteredObjects2) }
            };

            JObject sortedJsonObject2 = SortJObjectAlphabetically(filteredObjectContainer2);
            WriteJsonToFile("compare_cvp_2.json", sortedJsonObject2);
        }

        private static JToken SortJTokenAlphabetically(JToken token)
        {
            if (token is JObject obj)
            {
                var sortedObj = new JObject();

                foreach (var property in obj.Properties().OrderBy(p => p.Name))
                {
                    sortedObj.Add(property.Name, SortJTokenAlphabetically(property.Value));
                }

                return sortedObj;
            }
            else if (token is JArray array)
            {
                var sortedArray = new JArray(array.Select(SortJTokenAlphabetically));
                return sortedArray;
            }
            else
            {
                return token;
            }
        }

        private static JObject SortJObjectAlphabetically(JObject jsonObject)
        {
            var sortedObject = new JObject();

            foreach (var property in jsonObject.Properties().OrderBy(p => p.Name))
            {
                sortedObject.Add(property.Name, SortJTokenAlphabetically(property.Value));
            }

            return sortedObject;
        }

        private static void WriteJsonToFile(string filePath, JObject data)
        {
            var settings = new JsonSerializerSettings()
            {
                Converters = { new BooleanConverter() },
                Formatting = Formatting.Indented
            };

            // Serialize
            string json = JsonConvert.SerializeObject(data, settings);

            File.WriteAllText(filePath, json);
            Log.Debug($"Data written to: {filePath}");
        }

        private static double ScaleValue(double value, double minValue, double maxValue)
        {
            return (value - minValue) / (maxValue - minValue) * 127;
        }

        public static void ConvertAutomationToMidi(dynamic cvpj, string file, string outputDirectoryPath, bool doOutputDebugFile)
        {
            double tempo = cvpj.parameters.bpm.value;

            // Set the ticks per beat and BPM
            short ticksPerBeat = 480;
            byte numerator = 4;
            byte denominator = 4;
            var midiTimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerBeat);
            var midiTempo = Tempo.FromBeatsPerMinute(tempo); // 128 bpm should give 468750

            int fileNum = 0;
            foreach (var trackType in cvpj.automation)
            {
                var trackTypeKey = trackType.Key;
                var trackTypeValue = (IDictionary<string, object>)trackType.Value;

                foreach (var trackTypeEntry in trackTypeValue)
                {
                    var trackTypeEntryKey = trackTypeEntry.Key;
                    var trackTypeEntryValue = (IDictionary<string, object>)trackTypeEntry.Value;

                    foreach (var trackName in trackTypeEntryValue)
                    {
                        var trackNameKey = trackName.Key;
                        var trackNameValue = (IDictionary<string, object>)trackName.Value;

                        // Include the time division when creating the midi file
                        var midiFile = new MidiFile()
                        {
                            TimeDivision = midiTimeDivision
                        };

                        // Set the timesignature and tempo manually instead of ReplaceTempoMap 
                        // to make sure the time signature event is included
                        // var midiTimeSignature = new TimeSignature(4, 4);
                        // var tempoMap = TempoMap.Create(midiTimeDivision, midiTempo, midiTimeSignature);
                        // midiFile.ReplaceTempoMap(tempoMap);
                        midiFile.Chunks.Add(new TrackChunk(
                                new TimeSignatureEvent(numerator, denominator),
                                new SetTempoEvent(midiTempo.MicrosecondsPerQuarterNote)
                            )
                        );

                        Log.Debug($"Creating MIDI file: {trackNameKey} with {midiTimeDivision} at {midiTempo}");

                        var midiChannelManager = new MidiChannelManager();

                        foreach (var pluginName in trackNameValue)
                        {
                            var pluginNameKey = pluginName.Key;
                            dynamic pluginNameValue = pluginName.Value;

                            string midiTrackName = pluginNameKey;
                            int midiChannel = midiChannelManager.GetUnusedChannel();

                            Log.Debug($"Adding MIDI track: {midiTrackName} ({trackTypeEntryKey}, {trackNameKey}, {pluginNameKey}) on Channel: {midiChannel}");

                            // Create a track
                            var trackChunk = new TrackChunk();
                            midiFile.Chunks.Add(trackChunk);

                            // set track name
                            trackChunk.Events.Add(new SequenceTrackNameEvent(midiTrackName));

                            string type = pluginNameValue.type;
                            List<dynamic> placements = pluginNameValue.placements;

                            foreach (var placement in placements)
                            {
                                double placementPos = placement.position;
                                double placementDuration = placement.duration;
                                List<dynamic> points = placement.points;

                                var maxValue = points.Max(point => (double)point.value);
                                var minValue = points.Min(point => (double)point.value);

                                long prevPos = 0;
                                foreach (var point in points)
                                {
                                    double pointPosition = point.position;
                                    double pointValue = point.value;

                                    long midiPointPos = (long)(placementPos * 4 + pointPosition * 4) * 30;

                                    int scaledValue;
                                    if (maxValue > 1)
                                    {
                                        // Scale between 0 and 127 using minValue and maxValue
                                        scaledValue = (int)ScaleValue((double)point.value, minValue, maxValue);
                                    }
                                    else
                                    {
                                        // Clamp and multiply with 127 if maxValue is 1 and minValue is 0
                                        scaledValue = Math.Clamp((int)((double)point.value * 127), 0, 127);
                                    }

                                    // Calculate delta time
                                    long deltaTime = midiPointPos - prevPos;

                                    // Create a control change event at the specified time
                                    var controlChangeEvent = new ControlChangeEvent
                                    {
                                        DeltaTime = deltaTime,
                                        Channel = (FourBitNumber)midiChannel,
                                        ControlNumber = (SevenBitNumber)11,
                                        ControlValue = (SevenBitNumber)scaledValue
                                    };
                                    trackChunk.Events.Add(controlChangeEvent);

                                    // Update prevPos with the current value for the next iteration
                                    prevPos = midiPointPos;
                                }
                            }
                        }

                        // Save the MIDI file to disk
                        string outputFileName = string.Format("{0}_automation_{1}.mid", Path.GetFileNameWithoutExtension(file), trackNameKey);
                        string outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);

                        Log.Information($"Writing MIDI track: {outputFileName}");
                        if (File.Exists(outputFilePath))
                        {
                            File.Delete(outputFilePath);
                        }

                        // no compression
                        // see https://github.com/melanchall/drywetmidi/issues/59        
                        midiFile.Write(outputFilePath);

                        if (doOutputDebugFile) LogMidiFile(midiFile, $"midifile_{fileNum}.log");

                        fileNum++;
                    }
                }

            }
        }

        public static void ConvertToMidi(dynamic cvpj, string file, string outputDirectoryPath, bool doOutputDebugFile)
        {
            // all credits go to SatyrDiamond and the DawVert code
            // https://github.com/SatyrDiamond/DawVert/blob/main/plugin_output/midi.py

            double tempo = cvpj.parameters.bpm.value;

            // Set the ticks per beat and BPM
            short ticksPerBeat = 480;
            byte numerator = 4;
            byte denominator = 4;
            var midiTimeDivision = new TicksPerQuarterNoteTimeDivision(ticksPerBeat);
            var midiTempo = Tempo.FromBeatsPerMinute(tempo); // 128 bpm should give 468750

            // Include the time division when creating the midi file
            var midiFile = new MidiFile()
            {
                TimeDivision = midiTimeDivision
            };

            // Set the timesignature and tempo manually instead of ReplaceTempoMap 
            // to make sure the time signature event is included
            // var midiTimeSignature = new TimeSignature(4, 4);
            // var tempoMap = TempoMap.Create(midiTimeDivision, midiTempo, midiTimeSignature);
            // midiFile.ReplaceTempoMap(tempoMap);
            midiFile.Chunks.Add(new TrackChunk(
                    new TimeSignatureEvent(numerator, denominator),
                    new SetTempoEvent(midiTempo.MicrosecondsPerQuarterNote)
                )
            );

            var midiChannelManager = new MidiChannelManager();

            int trackNum = 0;
            foreach (var track in cvpj.track_placements)
            {
                var trackId = track.Key;
                string trackName = cvpj.track_data[trackId].name;
                int midiChannel = midiChannelManager.GetUnusedChannel();

                Log.Debug($"Creating MIDI track: {trackName} with id: {trackId} on channel: {midiChannel}");

                // Create a track
                var trackChunk = new TrackChunk();
                midiFile.Chunks.Add(trackChunk);

                // set track name
                trackChunk.Events.Add(new SequenceTrackNameEvent(trackName));

                // set track color
                var trackColor = cvpj.track_data[trackId].color;
                if (trackColor != null)
                {
                    byte[] midiTrackColor = RgbDoubleToRgbBytes(trackColor);

                    var s1 = new SequencerSpecificEvent(new byte[] { 83, 105, 103, 110, 1, 255 }
                        .Concat(midiTrackColor.Reverse()).ToArray()); // from Signal MIDI Editor
                    trackChunk.Events.Add(s1);

                    var s2 = new SequencerSpecificEvent(new byte[] { 80, 114, 101, 83, 1, 255 }
                        .Concat(midiTrackColor.Reverse()).ToArray()); // from Studio One
                    trackChunk.Events.Add(s2);

                    byte red_p1 = (byte)(midiTrackColor[0] >> 2);
                    byte red_p2 = (byte)((midiTrackColor[0] << 5) & 0x7F);
                    byte green_p1 = (byte)(midiTrackColor[1] >> 3);
                    byte green_p2 = (byte)((midiTrackColor[1] << 4) & 0x7F);
                    byte blue_p1 = (byte)(midiTrackColor[2] >> 4);
                    byte blue_p2 = (byte)(midiTrackColor[2] & 0x0F);

                    byte[] anvilcolor = { blue_p2, (byte)(green_p2 + blue_p1), (byte)(red_p2 + green_p1), red_p1 };
                    var s3 = new SequencerSpecificEvent(new byte[] { 5, 15, 52 }
                        .Concat(anvilcolor).Append((byte)0).ToArray()); // from Anvil Studio
                    trackChunk.Events.Add(s3);
                }

                // add program change
                trackChunk.Events.Add(new ProgramChangeEvent()
                {
                    ProgramNumber = (SevenBitNumber)0, // 'Acoustic Grand Piano' in GM
                    Channel = (FourBitNumber)midiChannel
                });

                var notes = track.Value.notes;

                var noteList = new Dictionary<long, List<List<string>>>(); // list holding the notes

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
                                int midiNoteOffVol = Math.Clamp((int)((float)noteData.off_vol * 127), 0, 127);

                                AddCmd(noteList, midiNotePos, new List<string> { "note_on", midiNoteKey.ToString(), midiNoteVol.ToString() });
                                AddCmd(noteList, midiNotePos + midiNoteDur, new List<string> { "note_off", midiNoteKey.ToString(), midiNoteOffVol.ToString() });
                            }
                        }
                    }
                }

                // Sorting the dictionary by key (= time)
                var sortedList = noteList.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                long prevPos = 0;
                foreach (var iListKey in sortedList.Keys)
                {
                    foreach (var midiNoteData in sortedList[iListKey])
                    {
                        if (midiNoteData[0] == "note_on")
                        {
                            trackChunk.Events.Add(new NoteOnEvent
                            {
                                DeltaTime = iListKey - prevPos,
                                NoteNumber = (SevenBitNumber)int.Parse(midiNoteData[1]),
                                Velocity = (SevenBitNumber)int.Parse(midiNoteData[2]),
                                Channel = (FourBitNumber)midiChannel
                            });
                        }
                        else if (midiNoteData[0] == "note_off")
                        {
                            trackChunk.Events.Add(new NoteOffEvent
                            {
                                DeltaTime = iListKey - prevPos,
                                NoteNumber = (SevenBitNumber)int.Parse(midiNoteData[1]),
                                // Velocity = (SevenBitNumber)int.Parse(midiNoteData[2]),
                                Velocity = (SevenBitNumber)64, // 64 is the default off value
                                Channel = (FourBitNumber)midiChannel
                            });
                        }

                        prevPos = iListKey;
                    }
                }

                trackNum++;
            }

            // Save the MIDI file to disk
            string outputFileName = string.Format("{0}.mid", Path.GetFileNameWithoutExtension(file));
            string outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);

            Log.Information($"Writing MIDI track: {outputFileName}");
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            // no compression
            // see https://github.com/melanchall/drywetmidi/issues/59        
            midiFile.Write(outputFilePath);

            if (doOutputDebugFile) LogMidiFile(midiFile);
        }

        // this code outputs in the same way as python mido format
        // i.e. the same code as
        // log_file_path = 'midiobj.log'
        // with open(log_file_path, 'w') as log_file:
        //     log_file.write("MIDI File Content:\n")
        //     for i, track in enumerate(midiobj.tracks):
        //         log_file.write(f"Track {i + 1}:\n")
        //         for msg in track:
        //             log_file.write(str(msg) + '\n')
        //         log_file.write('\n')
        // print(f"MIDI content written to {log_file_path}")
        private static void LogMidiFile(MidiFile midiFile, string logFilePath = "midifile.log")
        {
            using (var logFile = new StreamWriter(logFilePath))
            {
                logFile.WriteLine("MIDI File Content:");

                int trackNumber = 1;
                foreach (TrackChunk t in midiFile.GetTrackChunks())
                {
                    logFile.WriteLine($"Track {trackNumber}:");

                    long cumulativeDeltaTime = 0; // To keep track of cumulative delta time
                    foreach (var midiEvent in t.GetTimedEvents())
                    {
                        // Calculate delta time based on the Time property
                        var deltaTime = midiEvent.Time - cumulativeDeltaTime;

                        // Use a switch statement to categorize events
                        switch (midiEvent.Event)
                        {
                            case ChannelEvent channelEvent:
                                switch (channelEvent)
                                {
                                    case ProgramChangeEvent programChangeEvent:
                                        logFile.WriteLine($"program_change channel={programChangeEvent.Channel} program={programChangeEvent.ProgramNumber} time={deltaTime}");
                                        break;
                                    case ControlChangeEvent controlChangeEvent:
                                        logFile.WriteLine($"control_change channel={controlChangeEvent.Channel} number={controlChangeEvent.ControlNumber} value={controlChangeEvent.ControlValue} time={deltaTime}");
                                        break;
                                    case NoteOnEvent noteOnEvent:
                                        logFile.WriteLine($"note_on channel={noteOnEvent.Channel} note={noteOnEvent.NoteNumber} velocity={noteOnEvent.Velocity} time={deltaTime}");
                                        break;
                                    case NoteOffEvent noteOffEvent:
                                        logFile.WriteLine($"note_off channel={noteOffEvent.Channel} note={noteOffEvent.NoteNumber} velocity={noteOffEvent.Velocity} time={deltaTime}");
                                        break;
                                    default:
                                        logFile.WriteLine($"ChannelEvent: {channelEvent.GetType().Name} {channelEvent} time={deltaTime}");
                                        break;
                                }
                                break;
                            case MetaEvent metaEvent:
                                switch (metaEvent)
                                {
                                    case SequenceTrackNameEvent sequenceTrackNameEvent:
                                        logFile.WriteLine($"MetaMessage('track_name', name='{sequenceTrackNameEvent.Text}', time={deltaTime})");
                                        break;
                                    case SequencerSpecificEvent sequencerSpecificEvent:
                                        logFile.WriteLine($"MetaMessage('sequencer_specific', data=({string.Join(", ", sequencerSpecificEvent.Data)}), time={deltaTime})");
                                        break;
                                    case TimeSignatureEvent timeSignatureEvent:
                                        logFile.WriteLine($"MetaMessage('time_signature', numerator={timeSignatureEvent.Numerator}, denominator={timeSignatureEvent.Denominator}, clocks_per_click={timeSignatureEvent.ClocksPerClick}, notated_32nd_notes_per_beat={timeSignatureEvent.ThirtySecondNotesPerBeat}, time={deltaTime})");
                                        break;
                                    case SetTempoEvent setTempoEvent:
                                        logFile.WriteLine($"MetaMessage('set_tempo', tempo={setTempoEvent.MicrosecondsPerQuarterNote}, time={deltaTime})");
                                        break;
                                    default:
                                        logFile.WriteLine($"MetaEvent: {metaEvent.GetType().Name} {metaEvent} time={deltaTime}");
                                        break;
                                }
                                break;
                            default:
                                logFile.WriteLine($"Unknown Event: {midiEvent.Event.GetType().Name} {midiEvent.Event} time={deltaTime}");
                                break;
                        }

                        // Update cumulative delta time
                        cumulativeDeltaTime = midiEvent.Time;
                    }

                    logFile.WriteLine();
                    trackNumber++;
                }
            }

            Console.WriteLine($"MIDI content written to {logFilePath}");
        }
    }

    public static class AbletonFunctions
    {
        public static dynamic CloneExpandoObject(dynamic srcObject)
        {
            dynamic destObject = new System.Dynamic.ExpandoObject();

            foreach (var kvp in (IDictionary<string, object>)srcObject)
            {
                ((IDictionary<string, object>)destObject).Add(kvp);
            }

            return destObject;
        }

        public static bool HasProperty(dynamic item, string propertyName)
        {
            // Check if the object is an ExpandoObject and if it contains the specified property
            if (item is System.Dynamic.ExpandoObject eo)
            {
                return (eo as IDictionary<string, object>).ContainsKey(propertyName);
            }
            else
            {
                return item.GetType().GetProperty(propertyName);
            }
        }

        public static List<dynamic> Move(List<dynamic> list, double pos)
        {
            List<dynamic> newList = new List<dynamic>();

            foreach (var element in list)
            {
                dynamic newElement = CloneExpandoObject(element);
                newElement.position = newElement.position + pos;

                if (newElement.position >= 0)
                {
                    newList.Add(newElement);
                }
            }

            return newList;
        }

        public static List<dynamic> Trim(List<dynamic> list, double pos)
        {
            List<dynamic> newList = new List<dynamic>();

            foreach (var element in list)
            {
                if (element.position < pos)
                {
                    newList.Add(element);
                }
            }

            return newList;
        }

        public static List<dynamic> TrimMove(List<dynamic> list, double? startAt, double? endAt)
        {
            List<dynamic> newList = new List<dynamic>(list);

            if (endAt != null)
            {
                newList = Trim(list, (double)endAt);
            }

            if (startAt != null)
            {
                newList = Move(list, -(double)startAt);
            }

            return newList;
        }

        public static double GetDuration(List<dynamic> list)
        {
            double durationFinal = 0;

            foreach (var element in list)
            {
                double endPos = element.position + element.duration;

                if (durationFinal < endPos)
                {
                    durationFinal = endPos;
                }
            }

            return durationFinal;
        }

        public static Tuple<double, double> GetDurPos(List<dynamic> list, double startPos)
        {
            double durationFinal = 0;
            double posFinal = 100000000; // double.MaxValue

            foreach (var element in list)
            {
                double pos = element.position;

                if (durationFinal < pos)
                {
                    durationFinal = pos;
                }

                if (posFinal > pos)
                {
                    posFinal = pos;
                }
            }

            return Tuple.Create(posFinal, durationFinal);
        }

        public static List<dynamic> Multiply(List<dynamic> list, double addVal, double mulVal)
        {
            foreach (var element in list)
            {
                if (element.points != null)
                {
                    foreach (var point in element.points)
                    {
                        if (point.value != null)
                        {
                            point.value = (point.value + addVal) * mulVal;
                        }
                    }
                }
            }

            return list;
        }
    }

    public static class XtraMath
    {
        //  -------------------------------------------- placement_loop --------------------------------------------

        public static List<double[]> LoopBefore(double blPPos, double blPDur, double blPStart, double blLStart, double blLEnd)
        {
            List<double[]> cutpoints = new List<double[]>();
            double tempPos = Math.Min(blLEnd, blPDur);

            cutpoints.Add(new double[]
            {
                (blPPos + blPStart) - blPStart,
                tempPos - blPStart,
                blPStart,
                Math.Min(blLEnd, blPDur)
            });

            blPDur += blPStart;
            double placementLoopSize = blLEnd - blLStart;

            if (blLEnd < blPDur && blLEnd > blLStart)
            {
                double remainingCuts = (blPDur - blLEnd) / placementLoopSize;

                while (remainingCuts > 0)
                {
                    double outDur = Math.Min(remainingCuts, 1);
                    cutpoints.Add(new double[]
                    {
                        (blPPos + tempPos) - blPStart,
                        placementLoopSize * outDur,
                        blLStart,
                        blLEnd * outDur
                    });

                    tempPos += placementLoopSize;
                    remainingCuts -= 1;
                }
            }

            return cutpoints;
        }

        public static List<double[]> LoopAfter(double blPPos, double blPDur, double blPStart, double blLStart, double blLEnd)
        {
            List<double[]> cutpoints = new List<double[]>();
            double placementLoopSize = blLEnd - blLStart;

            double blPDurMo = blPDur - blLStart;
            double blPStartMo = blPStart - blLStart;
            double blLStartMo = blLStart - blLStart;
            double blLEndMo = blLEnd - blLStart;

            double remainingCuts = (blPDurMo + blPStartMo) / placementLoopSize;
            double tempPos = blPPos;
            tempPos -= blPStartMo;

            bool flagFirstPl = true;

            while (remainingCuts > 0)
            {
                double outDur = Math.Min(remainingCuts, 1);

                if (flagFirstPl)
                {
                    cutpoints.Add(new double[]
                    {
                        tempPos + blPStartMo,
                        (outDur * placementLoopSize) - blPStartMo,
                        blLStart + blPStartMo,
                        outDur * blLEnd
                    });
                }

                if (!flagFirstPl)
                {
                    cutpoints.Add(new double[]
                    {
                        tempPos,
                        outDur * placementLoopSize,
                        blLStart,
                        outDur * blLEnd
                    });
                }

                tempPos += placementLoopSize;
                remainingCuts -= 1;
                flagFirstPl = false;
            }

            return cutpoints;
        }

        public static List<double[]> CutLoop(double position, double duration, double startOffset, double loopStart, double loopEnd)
        {
            List<double[]> cutpoints;

            if (loopStart > startOffset)
            {
                cutpoints = LoopBefore(position, duration, startOffset, loopStart, loopEnd);
            }
            else
            {
                cutpoints = LoopAfter(position, duration, startOffset, loopStart, loopEnd);
            }

            return cutpoints;
        }
    }

    public static class DataValues
    {
        // data_values.py

        public static void NestedDictAddValue(dynamic dict, string[] keys, dynamic value)
        {
            if (keys.Length == 1)
            {
                var lastKey = keys[0];
                if (!(dict is IDictionary<string, dynamic>))
                {
                    dict = new System.Dynamic.ExpandoObject();
                }
                ((IDictionary<string, dynamic>)dict)[lastKey] = value;
            }
            else
            {
                var key = keys[0];
                if (!(dict is IDictionary<string, dynamic>))
                {
                    dict = new System.Dynamic.ExpandoObject();
                }

                if (!((IDictionary<string, dynamic>)dict).ContainsKey(key))
                {
                    ((IDictionary<string, dynamic>)dict)[key] = new System.Dynamic.ExpandoObject();
                }

                NestedDictAddValue(((IDictionary<string, dynamic>)dict)[key], keys[1..], value);
            }
        }

        public static void NestedDictAddToList(dynamic dict, string[] keys, dynamic value)
        {
            if (keys.Length == 1)
            {
                var lastKey = keys[0];

                if (!(dict is IDictionary<string, dynamic>))
                {
                    dict = new System.Dynamic.ExpandoObject();
                }

                if (!((IDictionary<string, dynamic>)dict).ContainsKey(lastKey))
                {
                    ((IDictionary<string, dynamic>)dict).Add(lastKey, new List<dynamic>());
                }

                if (value is List<dynamic>)
                {
                    ((IDictionary<string, dynamic>)dict)[lastKey] = value;
                }
                else
                {
                    ((List<dynamic>)((IDictionary<string, dynamic>)dict)[lastKey]).Add(value);
                }
            }
            else
            {
                var key = keys[0];

                if (!(dict is IDictionary<string, dynamic>))
                {
                    dict = new System.Dynamic.ExpandoObject();
                }

                if (!((IDictionary<string, dynamic>)dict).ContainsKey(key))
                {
                    ((IDictionary<string, dynamic>)dict)[key] = new System.Dynamic.ExpandoObject();
                }

                NestedDictAddToList(((IDictionary<string, dynamic>)dict)[key], keys[1..], value);
            }
        }
    }
}