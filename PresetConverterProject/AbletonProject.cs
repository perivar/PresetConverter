using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;

using Serilog;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
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
            cvpj.track_order = new List<dynamic>();
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
                                var xNoteNEvent_EV = xNoteNEvent.Element("Events");

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
                        dynamic notesGroup = new System.Dynamic.ExpandoObject();
                        notesGroup.notes = notesList;
                        cvpj.track_placements.Add(trackId, notesGroup);
                    }

                    // handle plugin presets
                    HandlePluginPresets(xTrackDeviceChain, trackName, file, outputDirectoryPath);
                }
            }

            // JObject sortedCvpj = SortJObjectAlphabetically(jcvpj);
            JObject jcvpj = JObject.FromObject(cvpj);
            WriteJsonToFile("output.json", jcvpj);

            ConvertToMidi(cvpj, file, outputDirectoryPath);

            CompareJson();
        }

        private static void HandlePluginPresets(XElement xTrackDeviceChain, string trackName, string file, string outputDirectoryPath)
        {
            // Path: Ableton/LiveSet/Tracks/[Audio|Group|Midi]Track/DeviceChain/DeviceChain/Devices/*
            // where * is internal plugins like <Eq8>, <Limiter> 
            // as well as <PluginDevice Id="X"> elements 

            XElement xDeviceChain = xTrackDeviceChain?.Element("DeviceChain");
            XElement xDeviceChainDevices = xDeviceChain?.Element("Devices");

            // Read Tracks
            Log.Debug("Found {0} DeviceChain Devices ...", xDeviceChainDevices.Elements().Count());

            foreach (XElement xDevice in xDeviceChainDevices.Elements())
            {
                string deviceType = xDevice.Name.LocalName;

                if (deviceType == "PluginDevice")
                {
                    // Handle Plugin Presets
                    // Path: PluginDevice/PluginDesc/VstPluginInfo/Preset/VstPreset

                    int pluginDeviceId = int.Parse(xDevice?.Attribute("Id")?.Value ?? "0");
                    Log.Debug($"PluginDevice Id: {pluginDeviceId}");

                    // check if it's on
                    XElement xOn = xDevice?.Element("On");
                    bool isOn = bool.Parse(GetValue(xOn, "Manual", "false"));

                    if (!isOn)
                    {
                        Log.Debug($"Skipping PluginDevice {pluginDeviceId} since it is disabled!");
                        continue;
                    }

                    XElement xPluginDesc = xDevice?.Element("PluginDesc");
                    XElement xVstPluginInfo = xPluginDesc?.Element("VstPluginInfo");
                    int vstPluginInfoId = int.Parse(xVstPluginInfo?.Attribute("Id")?.Value ?? "0");
                    string vstPlugName = GetValue(xVstPluginInfo, "PlugName", "Empty");
                    Log.Debug($"VstPluginInfo Id: {vstPluginInfoId}, VstPluginName: {vstPlugName}");

                    XElement xPreset = xVstPluginInfo?.Element("Preset");
                    XElement xVstPreset = xPreset?.Element("VstPreset");
                    int vstPresetId = int.Parse(xVstPreset?.Attribute("Id")?.Value ?? "0");
                    Log.Debug($"VstPreset Id: {vstPresetId}");

                    // read the byte data buffer
                    XElement xVstPluginBuffer = xVstPreset?.Element("Buffer");
                    byte[] vstPluginBufferBytes = GetInnerValueAsByteArray(xVstPluginBuffer);

                    string outputFileName = string.Format("{0} - {1}-{2}-{3}", Path.GetFileNameWithoutExtension(file), trackName, pluginDeviceId, StringUtils.MakeValidFileName($"{vstPlugName}"));
                    string outputFilePath = Path.Combine(outputDirectoryPath, "Ableton - " + outputFileName);

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
                    Log.Information($"Writing Plugin Preset: {outputFileName}");
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
                            Log.Error($"Could not save preset as fxp since I did not recognize vstplugin: {vstPlugName}");
                            BinaryFile.ByteArrayToFile(outputFilePath + ".dat", vstPluginBufferBytes);
                            break;
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

        private static void CompareJson()
        {
            string jsonFilePath1 = "C:\\Users\\periv\\Projects\\PresetConverter\\output.json";
            // string jsonFilePath2 = "C:\\Users\\periv\\Projects\\DawVert\\out.cvpj";
            string jsonFilePath2 = "C:\\Users\\periv\\Projects\\DawVert\\midiinput.cvpj";

            // Read JSON files
            string jsonContent1 = File.ReadAllText(jsonFilePath1);
            string jsonContent2 = File.ReadAllText(jsonFilePath2);

            // Parse JSON content
            JObject jsonObject1 = JObject.Parse(jsonContent1);

            // ignore the first "CONVPROJ****" line
            jsonContent2 = string.Join("\n", jsonContent2.Split('\n').Skip(1));
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
            WriteJsonToFile("pres_conv.json", sortedJsonObject1);

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
            WriteJsonToFile("daw_conv.json", sortedJsonObject2);
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

        public static void ConvertToMidi(dynamic cvpj, string file, string outputDirectoryPath)
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

            // LogMidiFile(midiFile);
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
        private static void LogMidiFile(MidiFile midiFile)
        {
            string logFilePath = "midifile.log";

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
}