using System;
using System.Collections.Generic;
using System.IO;
using AbletonLiveConverter;

public static class REVerenceVSTPresetGenerator
{
    /// <summary>
    /// Generate REVerence preset
    /// </summary>
    /// <param name="wavFilePath">input wav</param>
    /// <param name="imagePaths">optional images (null or zero for automatic)</param>
    /// <param name="outputDirectoryPath">output directory</param>
    /// <param name="filePrefix">file name prefix (null for 'Imported')</param>
    /// <param name="includeParentDirsInName">if number > 0 we are using the number of parent directories in the filename</param>    
    public static void CreatePreset(string wavFilePath, List<string> imagePaths, string outputDirectoryPath, string filePrefix = null, int includeParentDirsInName = 0)
    {
        if (filePrefix == null) filePrefix = "Imported_";
        bool automaticImageMode = imagePaths == null || imagePaths.Count == 0 ? true : false;
        var images = new List<string>();

        if (automaticImageMode)
        {
            // Rule, look in parent directory for file
            var imageFiles = new DirectoryInfo(wavFilePath).Parent.GetFiles("*.jpg");

            if (null != imageFiles && imageFiles.Length > 0)
            {
                foreach (FileInfo fi in imageFiles)
                {
                    images.Add(fi.FullName);
                    Console.Out.WriteLine("Found image file to use: {0}", fi.Name);
                }
            }
        }
        else
        {
            if (imagePaths != null && imagePaths.Count > 0) images.AddRange(imagePaths);
        }

        if (images.Count == 0)
        {
            Console.WriteLine("Not using any images.");
        }
        else
        {
            Console.WriteLine("Using {0} images.", images.Count);
        }

        // build preset
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
        if (includeParentDirsInName > 0)
        {
            outputFileName = GetParentPrefix(outputFileName, new DirectoryInfo(wavFilePath), includeParentDirsInName);
        }

        // remove the Quad term from the file name
        outputFileName = outputFileName.Replace("_Quad", "");
        outputFileName = outputFileName.Replace("Quad", "");

        reverence.WavFilePath1 = wavFilePath;
        reverence.WavFilePath2 = wavFilePath;
        reverence.WavFileName = outputFileName;

        CreateDirectoryIfNotExist(Path.Combine(outputDirectoryPath, "REVerence"));
        string outputFilePath = Path.Combine(outputDirectoryPath, "REVerence", filePrefix + outputFileName + ".vstpreset");

        reverence.Write(outputFilePath);
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
            fileName = String.Format("{0} - {1}", parent.Name, fileName);
            dirNode = parent;
            currentLevel++;
        }
        return fileName;
    }
}

