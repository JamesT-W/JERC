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
                        break;
                    case "-vmffilepath":
                        vmfFilepath = args[i + 1];
                        if (!vmfFilepath.Contains(".vmf"))
                            vmfFilepath += ".vmf";
                        vmfFilepathDirectory = Path.GetDirectoryName(vmfFilepath);
                        break;
                    default:
                        return;
                }
            }
        }


        public bool VerifyAllValuesSet()
        {
            if (string.IsNullOrWhiteSpace(csgoFolderPath) ||
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
                !Directory.Exists(vmfFilepathDirectory) ||
                !File.Exists(vmfFilepathDirectory)
            )
            {
                return false;
            }

            return true;
        }
    }
}
