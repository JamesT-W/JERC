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
        public List<Side> brushesSidesPath;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesOverlap;

        public List<Side> displacementsSidesNegative;
        public List<Side> displacementsSidesPath;
        public List<Side> displacementsSidesCover;
        public List<Side> displacementsSidesOverlap;

        public Dictionary<int, List<Side>> entitiesSidesByEntityBuyzoneId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityBombsiteId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityRescueZoneId = new Dictionary<int, List<Side>>();

        public List<Entity> rescueZoneBrushes;
        public List<Entity> hostageEntities;

        public VmfRequiredData(
            IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesPath, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesOverlap,
            IEnumerable<IVNode> displacementsNegative, IEnumerable<IVNode> displacementsPath, IEnumerable<IVNode> displacementsCover, IEnumerable<IVNode> displacementsOverlap,
            IEnumerable<IVNode> buyzoneBrushEntities, IEnumerable<IVNode> bombsiteBrushEntities, IEnumerable<IVNode> RescueZoneBrushEntities, IEnumerable<IVNode> hostageEntities
        )
        {
            // world brushes
            var brushesNegativeModelled = brushesNegative.Any() ? brushesNegative.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesPathModelled = brushesPath.Any() ? brushesPath.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesCoverModelled = brushesCover.Any() ? brushesCover.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesOverlapModelled = brushesOverlap.Any() ? brushesOverlap.Select(x => new Brush(x)).ToList() : new List<Brush>();

            brushesSidesNegative = brushesNegativeModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.NegativeTextureName)).ToList();
            brushesSidesPath = brushesPathModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            brushesSidesCover = brushesCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            brushesSidesOverlap = brushesOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            // displacements
            var displacementsNegativeModelled = displacementsNegative.Any() ? displacementsNegative.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsPathModelled = displacementsPath.Any() ? displacementsPath.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsCoverModelled = displacementsCover.Any() ? displacementsCover.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsOverlapModelled = displacementsOverlap.Any() ? displacementsOverlap.Select(x => new Brush(x)).ToList() : new List<Brush>();

            displacementsSidesNegative = displacementsNegativeModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.NegativeTextureName)).ToList();
            displacementsSidesPath = displacementsPathModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            displacementsSidesCover = displacementsCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            displacementsSidesOverlap = displacementsOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            // entities
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
