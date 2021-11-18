using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class Brush
    {
        public int id;
        public List<Side> side;
        public Editor editor;

        public bool fakeBrushCreatedFromOverlay;

        public static List<int> allBrushIds = new List<int>();


        public Brush(IVNode brush)
        {
            id = int.Parse(brush.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            side = brush.Body.Where(x => x.Name == "side")?.Select(x => new Side(x, id)).ToList();
            editor = new Editor(brush.Body.FirstOrDefault(x => x.Name == "editor"));

            allBrushIds.Add(id);

            fakeBrushCreatedFromOverlay = false;
        }


        // for overlays becoming fake brushes
        public Brush(int entityId, bool isOverlay, Vertices vertices0, Vertices vertices1, Vertices vertices2, Vertices vertices3)
        {
            if (!isOverlay)
            {
                Logger.LogWarning($"Brush constructor called for creating fake brush from overlay, but isOverlay == false? Entity ID: {entityId}");
                return;
            }

            var newIdTrying = int.MaxValue - entityId;
            while (allBrushIds.Any(x => x == newIdTrying))
            {
                newIdTrying--;

                if (newIdTrying < 0)
                {
                    newIdTrying = int.MaxValue - 1;
                }
            }
            id = newIdTrying;

            var allVertices = new List<Vertices>() { vertices0, vertices1, vertices2, vertices3 };
            side = new List<Side>() { new Side(id, allVertices) };

            fakeBrushCreatedFromOverlay = true;
        }
    }
}
