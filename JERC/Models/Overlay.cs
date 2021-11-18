using JERC.Constants;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace JERC.Models
{
    public class Overlay
    {
        public Entity entity;

        // info_overlay
        public int id;
        public string classname;
        public Angle angles;
        public string basisNormal;
        public string basisOrigin;
        public string basisU;
        public string basisV;
        public int endU;
        public int endV;
        public string fademindist;
        public string fademaxdist;
        public string material;
        public List<int> sides;
        public int startU;
        public int startV;
        public Vertices uv0;
        public Vertices uv1;
        public Vertices uv2;
        public Vertices uv3;
        public Vertices origin;
        public int renderOrder;
        public Editor editor;

        // jerc_info_overlay
        public int orderNum;
        public bool overrideColour;
        public Color rendercolor;
        public Color colourStroke;
        public int strokeWidth;

        // overlays
        public Vertices jim_vertices0;
        public Vertices jim_vertices1;
        public Vertices jim_vertices2;
        public Vertices jim_vertices3;


        public Overlay(ConfigurationValues configurationValues, Entity entity)
        {
            this.entity = entity;

            // info_overlay
            id = entity.id;
            classname = entity.classname;
            angles = new Angle(entity.angles);
            basisNormal = entity.basisNormal;
            basisOrigin = entity.basisOrigin;
            basisU = entity.basisU;
            basisV = entity.basisV;
            _ = int.TryParse(entity.endU, out endU);
            _ = int.TryParse(entity.endV, out endV);
            fademindist = entity.fademindist;
            fademaxdist = entity.fademaxdist;
            material = entity.material;
            sides = entity.sides != null && entity.sides.Any() ? entity.sides.Split(" ").Select(x => int.Parse(x))?.ToList() : new List<int>();
            _ = int.TryParse(entity.startU, out startU);
            _ = int.TryParse(entity.startV, out startV);
            uv0 = new Vertices(entity.uv0);
            uv1 = new Vertices(entity.uv1);
            uv2 = new Vertices(entity.uv2);
            uv3 = new Vertices(entity.uv3);
            origin = new Vertices(entity.origin);
            _ = int.TryParse(entity.renderOrder, out renderOrder);
            editor = entity.editor;

            // jerc_info_overlay
            entity.orderNum ??= "2";
            entity.orderNum = (int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture) < JercBoxValues.MinJercBoxOrderNumValue) ? JercBoxValues.MinJercBoxOrderNumValue.ToString() : entity.orderNum;
            entity.orderNum = (int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture) > JercBoxValues.MaxJercBoxOrderNumValue) ? JercBoxValues.MaxJercBoxOrderNumValue.ToString() : entity.orderNum;
            orderNum = int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture);

            overrideColour = entity.overrideColour;

            entity.rendercolor ??= "0 100 100";
            entity.colourAlpha ??= "255";
            entity.colourAlpha = (int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture) < OverlayValues.MinOverlayColourAlphaValue) ? OverlayValues.MinOverlayColourAlphaValue.ToString() : entity.colourAlpha;
            entity.colourAlpha = (int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture) > OverlayValues.MaxOverlayColourAlphaValue) ? OverlayValues.MaxOverlayColourAlphaValue.ToString() : entity.colourAlpha;

            if (overrideColour)
            {
                var colourSplit = entity.rendercolor.Split(" ");
                rendercolor = Color.FromArgb(
                    int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture),
                    int.Parse(colourSplit[0], Globalization.Style, Globalization.Culture),
                    int.Parse(colourSplit[1], Globalization.Style, Globalization.Culture),
                    int.Parse(colourSplit[2], Globalization.Style, Globalization.Culture)
                );
            }
            else
            {
                rendercolor = configurationValues.overlaysColour;
            }

            entity.colourStroke ??= "0 0 0";
            entity.colourStrokeAlpha ??= "255";
            entity.colourStrokeAlpha = (int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture) < OverlayValues.MinOverlayColourStrokeAlphaValue) ? OverlayValues.MinOverlayColourStrokeAlphaValue.ToString() : entity.colourStrokeAlpha;
            entity.colourStrokeAlpha = (int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture) > OverlayValues.MaxOverlayColourStrokeAlphaValue) ? OverlayValues.MaxOverlayColourStrokeAlphaValue.ToString() : entity.colourStrokeAlpha;

            var colourStrokeSplit = entity.colourStroke.Split(" ");
            colourStroke = Color.FromArgb(
                int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[0], Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[1], Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[2], Globalization.Style, Globalization.Culture)
            );

            entity.strokeWidth ??= "10";
            strokeWidth = int.Parse(entity.strokeWidth, Globalization.Style, Globalization.Culture);


            // overlays
            jim_vertices0 = new Vertices(entity.jim_vertices0);
            jim_vertices1 = new Vertices(entity.jim_vertices1);
            jim_vertices2 = new Vertices(entity.jim_vertices2);
            jim_vertices3 = new Vertices(entity.jim_vertices3);
        }
    }
}
