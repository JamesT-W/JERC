using JERC.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class GameConfigurationValues
    {
        public string csgoFolderPath;
        public string binFolderPath;
        public string overviewsFolderPath;
        public string vmfFilepath;
        public string vmfFilepathDirectory;


        public GameConfigurationValues(string[] args)
        {
            if (args.Length > 4)
                return;

            for (int i = 0; i < args.Length; i += 2)
            {
                switch (args[i].ToLower())
                {
                    case "-g":
                        csgoFolderPath = args[i + 1];
                        if (string.IsNullOrWhiteSpace(csgoFolderPath))
                            return;
                        if (csgoFolderPath.ToCharArray().LastOrDefault() != '\\')
                            csgoFolderPath += '\\';
                        binFolderPath = Path.Combine(Directory.GetParent(csgoFolderPath).Parent.FullName, @"bin\");
                        overviewsFolderPath = Path.Combine(csgoFolderPath, @"resource\overviews\");
                        break;
                    case "-vmffilepath":
                        vmfFilepath = args[i + 1];
                        if (string.IsNullOrWhiteSpace(vmfFilepath))
                            return;
                        if (!vmfFilepath.Contains(".vmf"))
                            vmfFilepath += ".vmf";
                        vmfFilepathDirectory = Path.GetDirectoryName(vmfFilepath);
                        break;
                    default:
                        return;
                }
            }

            Logger.LogMessage("---- Game Configuration Values ----");
            Logger.LogMessageKey("csgo Directory: ");
            Logger.LogMessage(csgoFolderPath);
            Logger.LogMessageKey("bin Directory: ");
            Logger.LogMessage(binFolderPath);
            Logger.LogMessageKey("overviews Directory: ");
            Logger.LogMessage(overviewsFolderPath);
            Logger.LogMessageKey("vmf Filepath: ");
            Logger.LogMessage(vmfFilepath);
            Logger.LogMessageKey("vmf Directory: ");
            Logger.LogMessage(vmfFilepathDirectory);
            Logger.LogNewLine();
        }


        public bool VerifyAllValuesSet()
        {
            if (string.IsNullOrWhiteSpace(csgoFolderPath) ||
                string.IsNullOrWhiteSpace(binFolderPath) ||
                string.IsNullOrWhiteSpace(overviewsFolderPath) ||
                string.IsNullOrWhiteSpace(vmfFilepath) ||
                string.IsNullOrWhiteSpace(vmfFilepathDirectory)
            )
            {
                return false;
            }

            return true;
        }


        public bool VerifyAllDirectoriesAndFilesExist()
        {
            if (!Directory.Exists(csgoFolderPath) ||
                !Directory.Exists(binFolderPath) ||
                !Directory.Exists(overviewsFolderPath) ||
                !File.Exists(vmfFilepath) ||
                !Directory.Exists(vmfFilepathDirectory)
            )
            {
                return false;
            }

            return true;
        }
    }
}
