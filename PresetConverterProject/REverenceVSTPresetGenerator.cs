using System;
using System.Collections.Generic;
using System.IO;

using Serilog;

namespace PresetConverter
{
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
                // Rule, look in same directory for file
                var imageFiles = new DirectoryInfo(wavFilePath).Parent.GetFiles("*.jpg");

                if (null != imageFiles && imageFiles.Length > 0)
                {
                    foreach (FileInfo fi in imageFiles)
                    {
                        images.Add(fi.FullName);
                        Log.Debug("Found image file to use in same directory: {0}", fi.Name);
                    }
                }
                else
                {
                    //  also check in parent dir
                    var parentImageFiles = new DirectoryInfo(wavFilePath).Parent.Parent.GetFiles("*.jpg");

                    foreach (FileInfo fi in parentImageFiles)
                    {
                        images.Add(fi.FullName);
                        Log.Debug("Found image file to use in parent directory: {0}", fi.Name);
                    }
                }
            }
            else
            {
                if (imagePaths != null && imagePaths.Count > 0) images.AddRange(imagePaths);
            }

            if (images.Count == 0)
            {
                Log.Debug("Not using any images.");
            }
            else
            {
                Log.Debug("Using {0} images.", images.Count);
            }

            // build preset
            var reverence = new SteinbergREVerence();

            // copy the images
            if (images.Count > 0) reverence.Images.AddRange(images);

            // set parameters
            reverence.Parameters["mix"].Number = 100.00;
            reverence.Parameters["predelay"].Number = 0.00;
            reverence.Parameters["time"].Number = 100.00;
            reverence.Parameters["size"].Number = 100.00;
            reverence.Parameters["level"].Number = 0.00;
            reverence.Parameters["ertailsplit"].Number = 35.00;
            reverence.Parameters["ertailmix"].Number = 50.00;
            reverence.Parameters["reverse"].Number = 0.00;
            reverence.Parameters["trim"].Number = 0.00;
            reverence.Parameters["autolevel"].Number = 1.00;
            reverence.Parameters["trimstart"].Number = 80.00;
            reverence.Parameters["trimend"].Number = 80.00;
            reverence.Parameters["eqon"].Number = 0.00;
            reverence.Parameters["lowfilterfreq"].Number = 100.00;
            reverence.Parameters["lowfiltergain"].Number = 0.00;
            reverence.Parameters["peakfreq"].Number = 1000.00;
            reverence.Parameters["peakgain"].Number = 6.00;
            reverence.Parameters["highfilterfreq"].Number = 15000.00;
            reverence.Parameters["highfiltergain"].Number = 0.00;
            reverence.Parameters["lowfilteron"].Number = 1.00;
            reverence.Parameters["peakon"].Number = 1.00;
            reverence.Parameters["highfilteron"].Number = 1.00;
            reverence.Parameters["output"].Number = 0.00;
            reverence.Parameters["predelayoffset"].Number = 0.00;
            reverence.Parameters["timeoffset"].Number = 0.00;
            reverence.Parameters["sizeoffset"].Number = 0.00;
            reverence.Parameters["leveloffset"].Number = 0.00;
            reverence.Parameters["ertailsplitoffset"].Number = 0.00;
            reverence.Parameters["ertailmixoffset"].Number = 0.00;
            reverence.Parameters["store"].Number = 1.00;
            reverence.Parameters["erase"].Number = 0.00;
            reverence.Parameters["autopresetnr"].Number = 0.00;
            reverence.Parameters["channelselect"].Number = 0.00;
            reverence.Parameters["transProgress"].Number = 0.00;
            reverence.Parameters["impulseTrigger"].Number = 0.00;
            reverence.Parameters["bypass"].Number = 0.00;
            reverence.Parameters["allowFading"].Number = 0.00;

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
            catch (Exception)
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
}