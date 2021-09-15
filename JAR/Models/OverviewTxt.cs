using System;
using System.Collections.Generic;
using System.Text;

namespace JAR.Models
{
    public class OverviewTxt
    {
        public string material;
        public string pos_x;
        public string pos_y;
        public string scale;
        public string rotate = null;
        public string zoom = null;

        public string inset_left = null;
        public string inset_right = null;
        public string inset_top = null;
        public string inset_bottom = null;

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
    }
}
