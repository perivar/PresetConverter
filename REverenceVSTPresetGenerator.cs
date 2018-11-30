/* 
	REVerenceVSTPresetGenerator 
	Copyright  Per Ivar Nerseth 2011 
	Version 1.6
    http://192.168.10.159/backup/
*/
using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using CommonUtils;
using AbletonLiveConverter;

public static class REverenceVSTPresetGenerator
{
    public static void CreatePreset(string wavFilePath, string imageFilePath, string inputDirectoryPath, string outputDirectoryPath)
    {
        bool automaticImageMode = imageFilePath == null ? true : false;
        var images = new List<string>();

        // Altiverb mode?
        // guess image name based on the image content in the parent directory?
        // i.e 
        // Scoring Stages (Orchestral Studios)\Todd-AO - California US:
        // Todd-AO-2779.jpg
        // Todd-AO-2782.jpg
        // Todd-AO-2813.jpg
        // Todd-AO-Marcs-layout.jpg
        // TODD-stats.jpg
        //
        // Cathedrals\Caen - Saint-Etienne:
        // caen  interior 1.jpg
        // caen  interior 2 .jpg
        // caen  interior 3.jpg
        // caen  exterior.jpg
        // caen IR stats.jpg
        // St Etienne Caen.mov ?!!!
        //
        // Tombs\Vigeland Mausoleum (Oslo):
        // 1 Vigeland-Museum-interior.jpg
        // Vigeland-Museum-exterior.jpg
        // Vigeland-Museum-stats.jpg				
        if (automaticImageMode)
        {
            // Rule, look in parent directory for file
            var imageFiles = new DirectoryInfo(wavFilePath).Parent.GetFiles("*.jpg");

            if (null != imageFiles && imageFiles.Length > 0)
            {
                foreach (FileInfo fi in imageFiles)
                {
                    images.Add(fi.FullName);
                    Console.Out.WriteLine("Altiverb Mode - Found image file to use: {0}", fi.Name);
                    break;
                }
            }
        }
        else
        {
            if (imageFilePath != null) images.Add(imageFilePath);
        }

        if (images.Count == 0)
        {
            Console.WriteLine("Not using any images.");
        }
        else
        {
            Console.WriteLine("Using images: {0}", images.Count);
        }

        var reverence = new SteinbergREVerence();

        // copy the images
        if (images.Count > 0) reverence.Images.AddRange(images);

        // set parameters
        reverence.Parameters["mix"].NumberValue = 100.00;
        reverence.Parameters["predelay"].NumberValue = 0.00;
        reverence.Parameters["time"].NumberValue = 100.00;
        reverence.Parameters["size"].NumberValue = 100.00;
        reverence.Parameters["level"].NumberValue = 0.00;
        reverence.Parameters["ertailsplit"].NumberValue = 35.00;
        reverence.Parameters["ertailmix"].NumberValue = 100.00;
        reverence.Parameters["reverse"].NumberValue = 0.00;
        reverence.Parameters["trim"].NumberValue = 0.00;
        reverence.Parameters["autolevel"].NumberValue = 1.00;
        reverence.Parameters["trimstart"].NumberValue = 80.00;
        reverence.Parameters["trimend"].NumberValue = 80.00;
        reverence.Parameters["eqon"].NumberValue = 0.00;
        reverence.Parameters["lowfilterfreq"].NumberValue = 100.00;
        reverence.Parameters["lowfiltergain"].NumberValue = 0.00;
        reverence.Parameters["peakfreq"].NumberValue = 1000.00;
        reverence.Parameters["peakgain"].NumberValue = 6.00;
        reverence.Parameters["highfilterfreq"].NumberValue = 15000.00;
        reverence.Parameters["highfiltergain"].NumberValue = 0.00;
        reverence.Parameters["lowfilteron"].NumberValue = 1.00;
        reverence.Parameters["peakon"].NumberValue = 1.00;
        reverence.Parameters["highfilteron"].NumberValue = 1.00;
        reverence.Parameters["output"].NumberValue = 0.00;
        reverence.Parameters["predelayoffset"].NumberValue = 0.00;
        reverence.Parameters["timeoffset"].NumberValue = 0.00;
        reverence.Parameters["sizeoffset"].NumberValue = 0.00;
        reverence.Parameters["leveloffset"].NumberValue = 0.00;
        reverence.Parameters["ertailsplitoffset"].NumberValue = 0.00;
        reverence.Parameters["ertailmixoffset"].NumberValue = 0.00;
        reverence.Parameters["store"].NumberValue = 1.00;
        reverence.Parameters["erase"].NumberValue = 0.00;
        reverence.Parameters["autopresetnr"].NumberValue = 0.00;
        reverence.Parameters["channelselect"].NumberValue = 0.00;
        reverence.Parameters["transProgress"].NumberValue = 0.00;
        reverence.Parameters["impulseTrigger"].NumberValue = 0.00;
        reverence.Parameters["bypass"].NumberValue = 0.00;
        reverence.Parameters["allowFading"].NumberValue = 0.00;

        string outputFileName = Path.GetFileNameWithoutExtension(wavFilePath);
        outputFileName = GetParentPrefix(outputFileName, new DirectoryInfo(wavFilePath), 1).Replace("_Quad", "");

        reverence.WavFilePath1 = wavFilePath;
        reverence.WavFilePath2 = wavFilePath;
        reverence.WavFileName = outputFileName;

        string outputFilePath = Path.Combine(outputDirectoryPath, "REVerence", "Imported_" + outputFileName);
        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));

        reverence.Write(outputFilePath + ".vstpreset");
    }

    private static void CreateDirectoryIfNotExist(string filePath)
    {
        try
        {
            Directory.CreateDirectory(filePath);
        }
        catch (Exception ex)
        {
            // handle them here
        }
    }

    private static string GetParentPrefix(string fileName, DirectoryInfo dirNode, int intMaxLevels)
    {
        int currentLevel = 0;
        while (currentLevel < intMaxLevels)
        {
            DirectoryInfo parent = dirNode.Parent;
            fileName = String.Format("{0}_{1}", parent.Name, fileName);
            dirNode = parent;
            currentLevel++;
        }
        return fileName;
    }
}

