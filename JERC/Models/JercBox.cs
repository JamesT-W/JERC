using JERC.Constants;
using System.Drawing;

namespace JERC.Models
{
    public class JercBox
    {
        public Entity entity;

        public int id;
        public int orderNum;
        public Color rendercolor;
        public Color colourStroke;
        public int strokeWidth;

        public JercBox(Entity entity)
        {
            this.entity = entity;

            id = entity.id;

            entity.orderNum ??= "4";
            entity.orderNum = (int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture) < JercBoxValues.MinJercBoxOrderNumValue) ? JercBoxValues.MinJercBoxOrderNumValue.ToString() : entity.orderNum;
            entity.orderNum = (int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture) > JercBoxValues.MaxJercBoxOrderNumValue) ? JercBoxValues.MaxJercBoxOrderNumValue.ToString() : entity.orderNum;
            orderNum = int.Parse(entity.orderNum, Globalization.Style, Globalization.Culture);

            entity.rendercolor ??= "255 255 255";
            entity.colourAlpha ??= "255";
            entity.colourAlpha = (int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture) < JercBoxValues.MinJercBoxColourAlphaValue) ? JercBoxValues.MinJercBoxColourAlphaValue.ToString() : entity.colourAlpha;
            entity.colourAlpha = (int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture) > JercBoxValues.MaxJercBoxColourAlphaValue) ? JercBoxValues.MaxJercBoxColourAlphaValue.ToString() : entity.colourAlpha;

            var colourSplit = entity.rendercolor.Split(" ");
            rendercolor = Color.FromArgb(
                int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[0], Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[1], Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[2], Globalization.Style, Globalization.Culture)
            );

            entity.colourStroke ??= "0 0 0";
            entity.colourStrokeAlpha ??= "255";
            entity.colourStrokeAlpha = (int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture) < JercBoxValues.MinJercBoxColourStrokeAlphaValue) ? JercBoxValues.MinJercBoxColourStrokeAlphaValue.ToString() : entity.colourStrokeAlpha;
            entity.colourStrokeAlpha = (int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture) > JercBoxValues.MaxJercBoxColourStrokeAlphaValue) ? JercBoxValues.MaxJercBoxColourStrokeAlphaValue.ToString() : entity.colourStrokeAlpha;

            var colourStrokeSplit = entity.colourStroke.Split(" ");
            colourStroke = Color.FromArgb(
                int.Parse(entity.colourStrokeAlpha, Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[0], Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[1], Globalization.Style, Globalization.Culture),
                int.Parse(colourStrokeSplit[2], Globalization.Style, Globalization.Culture)
            );

            entity.strokeWidth ??= "10";
            strokeWidth = int.Parse(entity.strokeWidth, Globalization.Style, Globalization.Culture);
        }
    }
}
