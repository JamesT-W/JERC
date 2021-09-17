using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class VmfRequiredData
    {
        public List<Side> brushesSidesNegative;
        public List<Side> brushesSidesLayout;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesOverlap;

        public List<Side> entitiesSidesBuyzone;
        public List<Side> entitiesSidesBombsite;
        public List<Side> entitiesSidesRescueZone;

        public List<Entity> rescueZoneBrushes;
        public List<Entity> hostageEntities;

        public VmfRequiredData(
            IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesLayout, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesOverlap,
            IEnumerable<IVNode> buyzoneBrushEntities, IEnumerable<IVNode> bombsiteBrushEntities, IEnumerable<IVNode> RescueZoneBrushEntities, IEnumerable<IVNode> hostageEntities
        )
        {
            var brushesNegativeModelled = brushesNegative.Any() ? brushesNegative.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesLayoutModelled = brushesLayout.Any() ? brushesLayout.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesCoverModelled = brushesCover.Any() ? brushesCover.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesOverlapModelled = brushesOverlap.Any() ? brushesOverlap.Select(x => new Brush(x)).ToList() : new List<Brush>();

            brushesSidesNegative = brushesNegativeModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.NegativeTextureName)).ToList();
            brushesSidesLayout = brushesLayoutModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.LayoutTextureName)).ToList();
            brushesSidesCover = brushesCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            brushesSidesOverlap = brushesOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();


            var entitiesBuyzoneModelled = buyzoneBrushEntities.Any() ? buyzoneBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesBombsiteModelled = bombsiteBrushEntities.Any() ? bombsiteBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesRescueZoneModelled = RescueZoneBrushEntities.Any() ? RescueZoneBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();

            entitiesSidesBuyzone = entitiesBuyzoneModelled.SelectMany(x => x.brush.side).ToList();
            entitiesSidesBombsite = entitiesBombsiteModelled.SelectMany(x => x.brush.side).ToList();
            entitiesSidesRescueZone = entitiesRescueZoneModelled.SelectMany(x => x.brush.side).ToList();


            this.hostageEntities = hostageEntities.Any() ? hostageEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
        }
    }
}
