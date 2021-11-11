using JERC.Constants;
using System.IO;
using System.Linq;

namespace JERC.Models
{
    public static class GameConfigurationValues
    {
        public static bool isVanillaHammer = false;

        public static string csgoFolderPath;
        public static string binFolderPath;
        public static string overviewsFolderPath;
        public static string vmfFilepath;
        public static string vmfFilepathDirectory;

        private static readonly int maxNumOfDiffArgs = 3;
        private static readonly int maxNumOfArgs = maxNumOfDiffArgs * 2;


        public static void SetArgs(string[] args)
        {
            if (args.Length > maxNumOfArgs)
                return;

            for (int i = 0; i < args.Length; i += 2)
            {
                switch (args[i].ToLower())
                {
                    case "-software":
                        var software = args[i + 1];
                        if (software.ToLower() == "hammer")
                            isVanillaHammer = true;
                        else if (software.ToLower() == "hammer++" || software.ToLower() == "hammerplusplus")
                            isVanillaHammer = false;
                        break;
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
                        vmfFilepathDirectory = Path.GetDirectoryName(vmfFilepath) + @"\";
                        break;
                    default:
                        return;
                }
            }

            Logger.LogMessage("---- Game Configuration Values ----");
            Logger.LogMessageKey("Is Vanilla Hammer: ");
            Logger.LogMessage(isVanillaHammer.ToString());
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


        public static bool VerifyAllValuesSet() // ignores isVanillaHammer
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


        public static bool VerifyAllDirectoriesAndFilesExist()
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
