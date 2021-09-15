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


        public OverviewTxt(
            string material, string pos_x, string pos_y, string scale, string rotate, string zoom,
            string inset_left, string inset_top, string inset_right, string inset_bottom,
            string CTSpawn_x, string CTSpawn_y, string TSpawn_x, string TSpawn_y,
            string bombA_x, string bombA_y, string bombB_x, string bombB_y,
            string Hostage1_x, string Hostage1_y, string Hostage2_x, string Hostage2_y
        )
        {
            this.material = material;
            this.pos_x = pos_x;
            this.pos_y = pos_y;
            this.scale = scale;
            this.rotate = rotate;
            this.zoom = zoom;

            this.inset_left = inset_left;
            this.inset_top = inset_top;
            this.inset_right = inset_right;
            this.inset_bottom = inset_bottom;

            this.CTSpawn_x = CTSpawn_x;
            this.CTSpawn_y = CTSpawn_y;
            this.TSpawn_x = TSpawn_x;
            this.TSpawn_y = TSpawn_y;

            this.bombA_x = bombA_x;
            this.bombA_y = bombA_y;
            this.bombB_x = bombB_x;
            this.bombB_y = bombB_y;

            this.Hostage1_x = Hostage1_x;
            this.Hostage1_y = Hostage1_y;
            this.Hostage2_x = Hostage2_x;
            this.Hostage2_y = Hostage2_y;
        }


        public List<string> GetInExportableFormat(string mapName)
        {
            var lines = new List<string>()
            {
                string.Concat("\"", mapName, "\""),
                "{",
                    string.Concat("\t\"material\"\t\t\"overviews/", material, "\""),
                    string.Concat("\t\"pos_x\"\t\t\t\"", pos_x, "\""),
                    string.Concat("\t\"pos_y\"\t\t\t\"", pos_y, "\""),
                    string.Concat("\t\"scale\"\t\t\t\"", scale, "\""),
                    //string.Concat("\t\t\t\"rotate\"\t\"", rotate, "\""),
                    //string.Concat("\t\t\t\"zoom\"\t\"", zoom, "\""),
                    string.Empty,
                    string.Concat("\t\"inset_left\"\t\"", inset_left, "\""),
                    string.Concat("\t\"inset_top\"\t\t\"", inset_top, "\""),
                    string.Concat("\t\"inset_right\"\t\"", inset_right, "\""),
                    string.Concat("\t\"inset_bottom\"\t\"", inset_bottom, "\""),
                    string.Empty,
                    string.Concat("\t\"CTSpawn_x\"\t\t\"", CTSpawn_x, "\""),
                    string.Concat("\t\"CTSpawn_y\"\t\t\"", CTSpawn_y, "\""),
                    string.Concat("\t\"TSpawn_x\"\t\t\"", TSpawn_x, "\""),
                    string.Concat("\t\"TSpawn_y\"\t\t\"", TSpawn_y, "\""),
            };

            if (bombA_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    string.Concat("\t\"bombA_x\"\t\t\"", bombA_x, "\""),
                    string.Concat("\t\"bombA_y\"\t\t\"", bombA_y, "\""),
                    string.Concat("\t\"bombB_x\"\t\t\"", bombB_x, "\""),
                    string.Concat("\t\"bombB_y\"\t\t\"", bombB_y, "\""),
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
