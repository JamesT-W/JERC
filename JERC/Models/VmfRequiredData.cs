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
        
        public List<Brush> brushesRemove;
        public List<Brush> brushesPath;
        public List<Brush> brushesCover;
        public List<Brush> brushesOverlap;

        public List<Side> displacementsSidesRemove;
        public List<Side> displacementsSidesPath;
        public List<Side> displacementsSidesCover;
        public List<Side> displacementsSidesOverlap;

        public List<Brush> displacementsRemove;
        public List<Brush> displacementsPath;
        public List<Brush> displacementsCover;
        public List<Brush> displacementsOverlap;

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
            IEnumerable<IVNode> brushesRemoveIVNodes, IEnumerable<IVNode> brushesPathIVNodes, IEnumerable<IVNode> brushesCoverIVNodes, IEnumerable<IVNode> brushesOverlapIVNodes,
            IEnumerable<IVNode> displacementsRemoveIVNodes, IEnumerable<IVNode> displacementsPathIVNodes, IEnumerable<IVNode> displacementsCoverIVNodes, IEnumerable<IVNode> displacementsOverlapIVNodes,
            IEnumerable<IVNode> buyzoneBrushEntitiesIVNodes, IEnumerable<IVNode> bombsiteBrushEntitiesIVNodes, IEnumerable<IVNode> rescueZoneBrushEntitiesIVNodes, IEnumerable<IVNode> hostageEntitiesIVNodes, IEnumerable<IVNode> ctSpawnEntitiesIVNodes, IEnumerable<IVNode> tSpawnEntitiesIVNodes,
            IEnumerable<IVNode> jercConfigureEntitiesIVNodes, IEnumerable<IVNode> jercDividerEntitiesIVNodes
        )
        {
            // world brushes
            brushesRemove = brushesRemoveIVNodes.Any() ? brushesRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesPath = brushesPathIVNodes.Any() ? brushesPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesCover = brushesCoverIVNodes.Any() ? brushesCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesOverlap = brushesOverlapIVNodes.Any() ? brushesOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var brushesSidesRemoveUnordered = brushesRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var brushesSidesPathUnordered = brushesPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var brushesSidesCoverUnordered = brushesCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var brushesSidesOverlapUnordered = brushesOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            brushesSidesRemove = OrderListOfSidesByVerticesMin(brushesSidesRemoveUnordered);
            brushesSidesPath = OrderListOfSidesByVerticesMin(brushesSidesPathUnordered);
            brushesSidesCover = OrderListOfSidesByVerticesMin(brushesSidesCoverUnordered);
            brushesSidesOverlap = OrderListOfSidesByVerticesMin(brushesSidesOverlapUnordered);

            // displacements
            displacementsRemove = displacementsRemoveIVNodes.Any() ? displacementsRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsPath = displacementsPathIVNodes.Any() ? displacementsPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsCover = displacementsCoverIVNodes.Any() ? displacementsCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsOverlap = displacementsOverlapIVNodes.Any() ? displacementsOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var displacementsSidesRemoveUnordered = displacementsRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var displacementsSidesPathUnordered = displacementsPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var displacementsSidesCoverUnordered = displacementsCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var displacementsSidesOverlapUnordered = displacementsOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();

            displacementsSidesRemove = OrderListOfSidesByVerticesMin(displacementsSidesRemoveUnordered);
            displacementsSidesPath = OrderListOfSidesByVerticesMin(displacementsSidesPathUnordered);
            displacementsSidesCover = OrderListOfSidesByVerticesMin(displacementsSidesCoverUnordered);
            displacementsSidesOverlap = OrderListOfSidesByVerticesMin(displacementsSidesOverlapUnordered);

            // entities
            var entitiesBuyzoneModelled = buyzoneBrushEntitiesIVNodes.Any() ? buyzoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesBombsiteModelled = bombsiteBrushEntitiesIVNodes.Any() ? bombsiteBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            var entitiesRescueZoneModelled = rescueZoneBrushEntitiesIVNodes.Any() ? rescueZoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();

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

            this.bombsiteBrushEntities = bombsiteBrushEntitiesIVNodes.Any() ? bombsiteBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.hostageEntities = hostageEntitiesIVNodes.Any() ? hostageEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.ctSpawnEntities = ctSpawnEntitiesIVNodes.Any() ? ctSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.tSpawnEntities = tSpawnEntitiesIVNodes.Any() ? tSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();

            this.jercConfigureEntities = jercConfigureEntitiesIVNodes.Any() ? jercConfigureEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            this.jercDividerEntities = jercDividerEntitiesIVNodes.Any() ? jercDividerEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
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
