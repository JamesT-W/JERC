using JERC.Constants;
using JERC.Enums;
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

        public Dictionary<int, List<Side>> entitiesSidesByEntityBuyzoneId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityBombsiteId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityRescueZoneId = new Dictionary<int, List<Side>>();

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

            foreach (var entity in entitiesBuyzoneModelled)
            {
                entitiesSidesByEntityBuyzoneId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }
            foreach (var entity in entitiesBombsiteModelled)
            {
                entitiesSidesByEntityBombsiteId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }
            foreach (var entity in entitiesRescueZoneModelled)
            {
                entitiesSidesByEntityRescueZoneId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }

            this.hostageEntities = hostageEntities.Any() ? hostageEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
        }
    }
}
