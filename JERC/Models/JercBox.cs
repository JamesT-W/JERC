using JERC.Constants;
using System.Drawing;

namespace JERC.Models
{
    public class JercBox
    {
        public Entity entity;

        public int id;
        public Color rendercolor;
        public Color colourStroke;
        public int strokeWidth;

        public JercBox(Entity entity)
        {
            this.entity = entity;

            id = entity.id;

            entity.rendercolor ??= "255 0 0";
            entity.colourAlpha ??= "255";

            var colourSplit = entity.rendercolor.Split(" ");
            rendercolor = Color.FromArgb(
                int.Parse(entity.colourAlpha, Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[0], Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[1], Globalization.Style, Globalization.Culture),
                int.Parse(colourSplit[2], Globalization.Style, Globalization.Culture)
            );

            entity.colourStroke ??= "255 0 0";
            entity.colourStrokeAlpha ??= "255";

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
