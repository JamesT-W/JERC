using JERC.Constants;
using System.Collections.Generic;
using System.Linq;
using VMFParser;

namespace JERC.Models
{
    public class VmfRequiredData
    {
        public List<Side> brushesSidesRemove;
        public List<Side> brushesSidesPath;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesOverlap;

        public List<Side> displacementsSidesRemove;
        public List<Side> displacementsSidesPath;
        public List<Side> displacementsSidesCover;
        public List<Side> displacementsSidesOverlap;

        public Dictionary<int, List<Side>> entitiesSidesByEntityBuyzoneId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityBombsiteId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityRescueZoneId = new Dictionary<int, List<Side>>();

        public List<Entity> bombsiteBrushEntities;
        public List<Entity> hostageEntities;
        public List<Entity> ctSpawnEntities;
        public List<Entity> tSpawnEntities;

        public List<Entity> jercConfigureEntities;
        public List<Entity> jercDividerEntities;


        public VmfRequiredData(
            IEnumerable<IVNode> brushesRemove, IEnumerable<IVNode> brushesPath, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesOverlap,
            IEnumerable<IVNode> displacementsRemove, IEnumerable<IVNode> displacementsPath, IEnumerable<IVNode> displacementsCover, IEnumerable<IVNode> displacementsOverlap,
            IEnumerable<IVNode> buyzoneBrushEntities, IEnumerable<IVNode> bombsiteBrushEntities, IEnumerable<IVNode> rescueZoneBrushEntities, IEnumerable<IVNode> hostageEntities, IEnumerable<IVNode> ctSpawnEntities, IEnumerable<IVNode> tSpawnEntities,
            IEnumerable<IVNode> jercConfigureEntities, IEnumerable<IVNode> jercDividerEntities
        )
        {
            // world brushes
            var brushesRemoveModelled = brushesRemove.Any() ? brushesRemove.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesPathModelled = brushesPath.Any() ? brushesPath.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesCoverModelled = brushesCover.Any() ? brushesCover.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var brushesOverlapModelled = brushesOverlap.Any() ? brushesOverlap.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var brushesSidesRemoveUnordered = brushesRemoveModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var brushesSidesPathUnordered = brushesPathModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var brushesSidesCoverUnordered = brushesCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var brushesSidesOverlapUnordered = brushesOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            brushesSidesRemove = OrderListOfSidesByVerticesMin(brushesSidesRemoveUnordered);
            brushesSidesPath = OrderListOfSidesByVerticesMin(brushesSidesPathUnordered);
            brushesSidesCover = OrderListOfSidesByVerticesMin(brushesSidesCoverUnordered);
            brushesSidesOverlap = OrderListOfSidesByVerticesMin(brushesSidesOverlapUnordered);

            // displacements
            var displacementsRemoveModelled = displacementsRemove.Any() ? displacementsRemove.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsPathModelled = displacementsPath.Any() ? displacementsPath.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsCoverModelled = displacementsCover.Any() ? displacementsCover.Select(x => new Brush(x)).ToList() : new List<Brush>();
            var displacementsOverlapModelled = displacementsOverlap.Any() ? displacementsOverlap.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var displacementsSidesRemoveUnordered = displacementsRemoveModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var displacementsSidesPathUnordered = displacementsPathModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var displacementsSidesCoverUnordered = displacementsCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var displacementsSidesOverlapUnordered = displacementsOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            displacementsSidesRemove = OrderListOfSidesByVerticesMin(displacementsSidesRemoveUnordered);
            displacementsSidesPath = OrderListOfSidesByVerticesMin(displacementsSidesPathUnordered);
            displacementsSidesCover = OrderListOfSidesByVerticesMin(displacementsSidesCoverUnordered);
            displacementsSidesOverlap = OrderListOfSidesByVerticesMin(displacementsSidesOverlapUnordered);

            // entities
            var entitiesBuyzoneModelled = buyzoneBrushEntities.Any() ? buyzoneBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesBombsiteModelled = bombsiteBrushEntities.Any() ? bombsiteBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesRescueZoneModelled = rescueZoneBrushEntities.Any() ? rescueZoneBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();

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

            this.bombsiteBrushEntities = bombsiteBrushEntities.Any() ? bombsiteBrushEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.hostageEntities = hostageEntities.Any() ? hostageEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.ctSpawnEntities = ctSpawnEntities.Any() ? ctSpawnEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.tSpawnEntities = tSpawnEntities.Any() ? tSpawnEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();

            this.jercConfigureEntities = jercConfigureEntities.Any() ? jercConfigureEntities.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.jercDividerEntities = jercDividerEntities.Any() ? jercDividerEntities.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
        }


        // Orders by descending, then uses distinct to ensure that it gets the MAX value first for each side and ignores the rest.
        // Then, it reverses, so it is ascending (MIN value first)
        private static List<Side> OrderListOfSidesByVerticesMin(List<Side> sides)
        {
            var sidesNew = (from x in sides
                            from y in x.vertices_plus
                            orderby y.z descending
                            select x).Distinct().ToList();

            sidesNew.Reverse();

            return sidesNew;
        }
    }
}
