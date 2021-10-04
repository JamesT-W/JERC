using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public string Hostage3_x;
        public string Hostage3_y;
        public string Hostage4_x;
        public string Hostage4_y;
        public string Hostage5_x;
        public string Hostage5_y;
        public string Hostage6_x;
        public string Hostage6_y;
        public string Hostage7_x;
        public string Hostage7_y;
        public string Hostage8_x;
        public string Hostage8_y;


        public OverviewTxt(
            string material, string pos_x, string pos_y, string scale, string rotate, string zoom,
            string inset_left, string inset_top, string inset_right, string inset_bottom,
            string CTSpawn_x, string CTSpawn_y, string TSpawn_x, string TSpawn_y,
            string bombA_x, string bombA_y, string bombB_x, string bombB_y,
            string Hostage1_x, string Hostage1_y, string Hostage2_x, string Hostage2_y, string Hostage3_x, string Hostage3_y, string Hostage4_x, string Hostage4_y,
            string Hostage5_x, string Hostage5_y, string Hostage6_x, string Hostage6_y, string Hostage7_x, string Hostage7_y, string Hostage8_x, string Hostage8_y
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
            this.Hostage3_x = Hostage3_x;
            this.Hostage3_y = Hostage3_y;
            this.Hostage4_x = Hostage4_x;
            this.Hostage4_y = Hostage4_y;
            this.Hostage5_x = Hostage5_x;
            this.Hostage5_y = Hostage5_y;
            this.Hostage6_x = Hostage6_x;
            this.Hostage6_y = Hostage6_y;
            this.Hostage7_x = Hostage7_x;
            this.Hostage7_y = Hostage7_y;
            this.Hostage8_x = Hostage8_x;
            this.Hostage8_y = Hostage8_y;
        }


        public List<string> GetInExportableFormat(JercConfigValues jercConfigValues, List<LevelHeight> levelHeights, string mapName)
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            var lines = new List<string>()
            {
                string.Concat("\"", mapName, "\""),
                "{",
                    string.Concat("\t\"material\"\t\t\"overviews/", material, "\""),
                    string.Concat("\t\"pos_x\"\t\t\t\"", float.Parse(pos_x).ToString("N", nfi), "\""),
                    string.Concat("\t\"pos_y\"\t\t\t\"", float.Parse(pos_y).ToString("N", nfi), "\""),
                    string.Concat("\t\"scale\"\t\t\t\"", float.Parse(scale).ToString("N", nfi), "\""),
                    //string.Concat("\t\t\t\"rotate\"\t\"", float.Parse(rotate).ToString("N", nfi), "\""),
                    //string.Concat("\t\t\t\"zoom\"\t\"", float.Parse(zoom).ToString("N", nfi), "\""),
                    string.Empty,
                    //string.Concat("\t\"inset_left\"\t\"", float.Parse(inset_left).ToString("N", nfi), "\""),
                    //string.Concat("\t\"inset_top\"\t\t\"", float.Parse(inset_top).ToString("N", nfi), "\""),
                    //string.Concat("\t\"inset_right\"\t\"", float.Parse(inset_right).ToString("N", nfi), "\""),
                    //string.Concat("\t\"inset_bottom\"\t\"", float.Parse(inset_bottom).ToString("N", nfi), "\""),
                    //string.Empty,
            };

            if (CTSpawn_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Concat("\t\"CTSpawn_x\"\t\t\"", (CTSpawn_x != null ? float.Parse(CTSpawn_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"CTSpawn_y\"\t\t\"", (CTSpawn_y != null ? float.Parse(CTSpawn_y).ToString("N", nfi) : string.Empty), "\""),
                });
            }

            if (TSpawn_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Concat("\t\"TSpawn_x\"\t\t\"", (TSpawn_x != null ? float.Parse(TSpawn_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"TSpawn_y\"\t\t\"", (TSpawn_y != null ? float.Parse(TSpawn_y).ToString("N", nfi) : string.Empty), "\""),
                });
            }

            if (bombA_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    string.Concat("\t\"bombA_x\"\t\t\"", (bombA_x != null ? float.Parse(bombA_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"bombA_y\"\t\t\"", (bombA_y != null ? float.Parse(bombA_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"bombB_x\"\t\t\"", (bombB_x != null ? float.Parse(bombB_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"bombB_y\"\t\t\"", (bombB_y != null ? float.Parse(bombB_y).ToString("N", nfi) : string.Empty), "\""),
                });
            }

            if (Hostage1_x != null)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    string.Concat("\t\"Hostage1_x\"\t\"", (Hostage1_x != null ? float.Parse(Hostage1_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage1_y\"\t\"", (Hostage1_y != null ? float.Parse(Hostage1_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage2_x\"\t\"", (Hostage2_x != null ? float.Parse(Hostage2_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage2_y\"\t\"", (Hostage2_y != null ? float.Parse(Hostage2_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage3_x\"\t\"", (Hostage3_x != null ? float.Parse(Hostage3_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage3_y\"\t\"", (Hostage3_y != null ? float.Parse(Hostage3_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage4_x\"\t\"", (Hostage4_x != null ? float.Parse(Hostage4_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage4_y\"\t\"", (Hostage4_y != null ? float.Parse(Hostage4_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage5_x\"\t\"", (Hostage5_x != null ? float.Parse(Hostage5_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage5_y\"\t\"", (Hostage5_y != null ? float.Parse(Hostage5_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage6_x\"\t\"", (Hostage6_x != null ? float.Parse(Hostage6_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage6_y\"\t\"", (Hostage6_y != null ? float.Parse(Hostage6_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage7_x\"\t\"", (Hostage7_x != null ? float.Parse(Hostage7_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage7_y\"\t\"", (Hostage7_y != null ? float.Parse(Hostage7_y).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage8_x\"\t\"", (Hostage8_x != null ? float.Parse(Hostage8_x).ToString("N", nfi) : string.Empty), "\""),
                    string.Concat("\t\"Hostage8_y\"\t\"", (Hostage8_y != null ? float.Parse(Hostage8_y).ToString("N", nfi) : string.Empty), "\""),
                });
            }

            if (jercConfigValues.exportRadarAsSeparateLevels && levelHeights != null && levelHeights.Count() > 1)
            {
                lines.AddRange(new List<string>()
                {
                    string.Empty,
                    "\t\"verticalsections\"",
                    "\t{",
                });

                foreach (var levelHeight in levelHeights)
                {
                    lines.AddRange(new List<string>()
                    {
                        string.Concat("\t\t\"", levelHeight.levelName, "\""),
                        "\t\t{",
                        string.Concat("\t\t\t\"AltitudeMin\"\t\"", levelHeight.zMinForTxt, "\""),
                        string.Concat("\t\t\t\"AltitudeMax\"\t\"", levelHeight.zMaxForTxt, "\""),
                        "\t\t}",
                    });
                }

                lines.Add("\t}");
            }

            lines.Add("}");

            return lines;
        }
    }
}
