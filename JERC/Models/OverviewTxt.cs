using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Models
{
    public class OverviewTxt
    {
        public string material;
        public string pos_x;
        public string pos_y;
        public string scale;
        public string rotate;
        public string zoom;

        public string inset_left;
        public string inset_top;
        public string inset_right;
        public string inset_bottom;

        public string CTSpawn_x;
        public string CTSpawn_y;
        public string TSpawn_x;
        public string TSpawn_y;

        public string bombA_x;
        public string bombA_y;
        public string bombB_x;
        public string bombB_y;
        
        public string Hostage1_x;
        public string Hostage1_y;
        public string Hostage2_x;
        public string Hostage2_y;


        public OverviewTxt() { }


        public List<string> GetInExportableFormat(string mapName)
        {
            var lines = new List<string>()
            {
                string.Concat("\"", mapName, "\""),
                "{",
                    string.Concat("\t\"material\"\t\"overviews/", material, "\""),
                    string.Concat("\t\"pos_x\"\t\"", pos_x, "\""),
                    string.Concat("\t\"pos_y\"\t\"", pos_y, "\""),
                    string.Concat("\t\"scale\"\t\"", scale, "\""),
                    //string.Concat("\t\"rotate\"\t\"", rotate, "\""),
                    //string.Concat("\t\"zoom\"\t\"", zoom, "\""),
                    string.Empty,
                    string.Concat("\t\"inset_left\"\t\"", inset_left, "\""),
                    string.Concat("\t\"inset_top\"\t\"", inset_top, "\""),
                    string.Concat("\t\"inset_right\"\t\"", inset_right, "\""),
                    string.Concat("\t\"inset_bottom\"\t\"", inset_bottom, "\""),
                    string.Empty,
                    string.Concat("\t\"CTSpawn_x\"\t\"", CTSpawn_x, "\""),
                    string.Concat("\t\"CTSpawn_y\"\t\"", CTSpawn_y, "\""),
                    string.Concat("\t\"TSpawn_x\"\t\"", TSpawn_x, "\""),
                    string.Concat("\t\"TSpawn_y\"\t\"", TSpawn_y, "\""),
            };

            if (bombA_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    string.Concat("\t\"bombA_x\"\t\"", bombA_x, "\""),
                    string.Concat("\t\"bombA_y\"\t\"", bombA_y, "\""),
                    string.Concat("\t\"bombB_x\"\t\"", bombB_x, "\""),
                    string.Concat("\t\"bombB_y\"\t\"", bombB_y, "\""),
                });
            }

            if (Hostage1_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    string.Concat("\t\"Hostage1_x\"\t\"", Hostage1_x, "\""),
                    string.Concat("\t\"Hostage1_y\"\t\"", Hostage1_y, "\""),
                    string.Concat("\t\"Hostage2_x\"\t\"", Hostage2_x, "\""),
                    string.Concat("\t\"Hostage2_y\"\t\"", Hostage2_y, "\""),
                });
            }

            lines.AddRange(new List<string>()
            {
                "}",
                string.Empty
            });

            return lines;
        }
    }
}
