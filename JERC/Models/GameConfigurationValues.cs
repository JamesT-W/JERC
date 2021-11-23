using JERC.Constants;
using System.IO;
using System.Linq;

namespace JERC.Models
{
    public static class GameConfigurationValues
    {
        public static bool? isVanillaHammer = null;

        public static string csgoFolderPath;
        public static string binFolderPath;
        public static string overviewsFolderPath;
        public static string dzTabletFolderPath;
        public static string dzSpawnselectFolderPath;

        public static string extrasFolderPath;

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
                        if (software.ToLower() == "hammer" || software.ToLower() == "vanilla")
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
                        dzTabletFolderPath = Path.Combine(csgoFolderPath, @"materials\models\weapons\v_models\tablet\");
                        dzSpawnselectFolderPath = Path.Combine(csgoFolderPath, @"materials\panorama\images\survival\spawnselect\");
                        extrasFolderPath = Path.Combine(csgoFolderPath, @"jerc_extras\");
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
            Logger.LogMessageKey("csgo Directory: ");
            Logger.LogMessage(csgoFolderPath);
            Logger.LogMessageKey("bin Directory: ");
            Logger.LogMessage(binFolderPath);
            Logger.LogMessageKey("overviews Directory: ");
            Logger.LogMessage(overviewsFolderPath);
            Logger.LogMessageKey("dz tablet Directory: ");
            Logger.LogMessage(dzTabletFolderPath);
            Logger.LogMessageKey("dz spawn select Directory: ");
            Logger.LogMessage(dzSpawnselectFolderPath);
            Logger.LogMessageKey("jerc extras Directory: ");
            Logger.LogMessage(extrasFolderPath);
            Logger.LogMessageKey("vmf Filepath: ");
            Logger.LogMessage(vmfFilepath);
            Logger.LogMessageKey("vmf Directory: ");
            Logger.LogMessage(vmfFilepathDirectory);
            Logger.LogNewLine();

            if (isVanillaHammer != null)
            {
                if (isVanillaHammer == true)
                {
                    Logger.LogMessage("VMF saved with Vanilla Hammer (provided)");
                }
                else
                {
                    Logger.LogMessage("VMF saved with Hammer++ (provided)");
                }

                Logger.LogNewLine();
            }
        }


        public static bool VerifyAllValuesSet() // ignores isVanillaHammer
        {
            if (string.IsNullOrWhiteSpace(csgoFolderPath) ||
                string.IsNullOrWhiteSpace(binFolderPath) ||
                string.IsNullOrWhiteSpace(overviewsFolderPath) ||
                string.IsNullOrWhiteSpace(dzTabletFolderPath) ||
                string.IsNullOrWhiteSpace(dzSpawnselectFolderPath) ||
                string.IsNullOrWhiteSpace(extrasFolderPath) ||
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
                !Directory.Exists(dzTabletFolderPath) ||
                !Directory.Exists(dzSpawnselectFolderPath) ||
                !Directory.Exists(extrasFolderPath) ||
                !File.Exists(vmfFilepath) ||
                !Directory.Exists(vmfFilepathDirectory)
            )
            {
                return false;
            }

            return true;
        }


        public static void CreateAnyGameDirectoriesThatDontExist()
        {
            CreateDirectoryIfDoesntExist(csgoFolderPath);
            CreateDirectoryIfDoesntExist(binFolderPath);
            CreateDirectoryIfDoesntExist(overviewsFolderPath);
            CreateDirectoryIfDoesntExist(dzTabletFolderPath);
            CreateDirectoryIfDoesntExist(dzSpawnselectFolderPath);
            CreateDirectoryIfDoesntExist(extrasFolderPath);
            CreateDirectoryIfDoesntExist(vmfFilepathDirectory);
        }


        private static void CreateDirectoryIfDoesntExist(string filepath)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
        }
    }
}
