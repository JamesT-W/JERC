using JAR.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JAR.Models
{
    public class VmfRequiredData
    {
        public List<Entity> propsLayout;
        public List<Entity> propsCover;
        public List<Entity> propsNegative;
        public List<Entity> propsOverlap;

        public List<Side> brushesSidesLayout;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesNegative;
        public List<Side> brushesSidesOverlap;
        
        public List<Entity> buyzoneBrushes;
        public List<Entity> bombsiteBrushes;
        public List<Entity> rescueZoneBrushes;
        public List<Entity> hostageEntities;

        public VmfRequiredData(
            IEnumerable<IVNode> propsLayout, IEnumerable<IVNode> propsCover, IEnumerable<IVNode> propsNegative, IEnumerable<IVNode> propsOverlap,
            IEnumerable<IVNode> brushesLayout, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesOverlap,
            IEnumerable<IVNode> buyzoneBrushEntities, IEnumerable<IVNode> bombsiteBrushEntities, IEnumerable<IVNode> RescueZoneBrushEntities, IEnumerable<IVNode> hostageEntities
        )
        {
            this.propsLayout = propsLayout.Select(x => new Entity(x)).ToList();
            this.propsCover = propsCover.Select(x => new Entity(x)).ToList();
            this.propsNegative = propsNegative.Select(x => new Entity(x)).ToList();
            this.propsOverlap = propsOverlap.Select(x => new Entity(x)).ToList();

            var brushesLayoutModelled = brushesLayout.Select(x => new Brush(x)).ToList();
            var brushesCoverModelled = brushesCover.Select(x => new Brush(x)).ToList();
            var brushesNegativeModelled = brushesNegative.Select(x => new Brush(x)).ToList();
            var brushesOverlapModelled = brushesOverlap.Select(x => new Brush(x)).ToList();

            brushesSidesLayout = brushesLayoutModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.LayoutTextureName)).ToList();
            brushesSidesCover = brushesCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            brushesSidesNegative = brushesNegativeModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.NegativeTextureName)).ToList();
            brushesSidesOverlap = brushesOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            this.buyzoneBrushes = buyzoneBrushEntities.Select(x => new Entity(x)).ToList();
            this.bombsiteBrushes = bombsiteBrushEntities.Select(x => new Entity(x)).ToList();
            this.rescueZoneBrushes = RescueZoneBrushEntities.Select(x => new Entity(x)).ToList();
            this.hostageEntities = hostageEntities.Select(x => new Entity(x)).ToList();
        }
    }
}
