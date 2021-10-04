using JERC.Constants;
using System.Collections.Generic;
using System.Linq;
using VMFParser;

namespace JERC.Models
{
    public class VmfRequiredData
    {
        public List<Brush> brushesRemove;
        public List<Brush> brushesPath;
        public List<Brush> brushesCover;
        public List<Brush> brushesOverlap;
        public List<Brush> brushesDoor;
        public List<Brush> brushesLadder;

        public List<Side> brushesSidesRemove;
        public List<Side> brushesSidesPath;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesOverlap;
        public List<Side> brushesSidesDoor;
        public List<Side> brushesSidesLadder;

        public List<Brush> displacementsRemove;
        public List<Brush> displacementsPath;
        public List<Brush> displacementsCover;
        public List<Brush> displacementsOverlap;
        public List<Brush> displacementsDoor;
        public List<Brush> displacementsLadder;

        public List<Side> displacementsSidesRemove;
        public List<Side> displacementsSidesPath;
        public List<Side> displacementsSidesCover;
        public List<Side> displacementsSidesOverlap;
        public List<Side> displacementsSidesDoor;
        public List<Side> displacementsSidesLadder;

        public List<Entity> buyzoneBrushEntities;
        public List<Entity> bombsiteBrushEntities;
        public List<Entity> rescueZoneBrushEntities;
        public List<Entity> hostageEntities;
        public List<Entity> ctSpawnEntities;
        public List<Entity> tSpawnEntities;

        public Dictionary<int, List<Side>> entitiesSidesByEntityBuyzoneId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityBombsiteId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityRescueZoneId = new Dictionary<int, List<Side>>();

        public List<Entity> jercConfigEntities;
        public List<Entity> jercDividerEntities;
        public List<Entity> jercFloorEntities;
        public List<Entity> jercCeilingEntities;


        public VmfRequiredData(
            IEnumerable<IVNode> brushesRemoveIVNodes, IEnumerable<IVNode> brushesPathIVNodes, IEnumerable<IVNode> brushesCoverIVNodes, IEnumerable<IVNode> brushesOverlapIVNodes, IEnumerable<IVNode> brushesDoorIVNodes, IEnumerable<IVNode> brushesLadderIVNodes,
            IEnumerable<IVNode> displacementsRemoveIVNodes, IEnumerable<IVNode> displacementsPathIVNodes, IEnumerable<IVNode> displacementsCoverIVNodes, IEnumerable<IVNode> displacementsOverlapIVNodes, IEnumerable<IVNode> displacementsDoorIVNodes, IEnumerable<IVNode> displacementsLadderIVNodes,
            IEnumerable<IVNode> buyzoneBrushEntitiesIVNodes, IEnumerable<IVNode> bombsiteBrushEntitiesIVNodes, IEnumerable<IVNode> rescueZoneBrushEntitiesIVNodes, IEnumerable<IVNode> hostageEntitiesIVNodes, IEnumerable<IVNode> ctSpawnEntitiesIVNodes, IEnumerable<IVNode> tSpawnEntitiesIVNodes,
            IEnumerable<IVNode> jercConfigEntitiesIVNodes, IEnumerable<IVNode> jercDividerEntitiesIVNodes, IEnumerable<IVNode> jercFloorEntitiesIVNodes, IEnumerable<IVNode> jercCeilingEntitiesIVNodes
        )
        {
            // world brushes
            brushesRemove = brushesRemoveIVNodes.Any() ? brushesRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesPath = brushesPathIVNodes.Any() ? brushesPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesCover = brushesCoverIVNodes.Any() ? brushesCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesOverlap = brushesOverlapIVNodes.Any() ? brushesOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesDoor = brushesDoorIVNodes.Any() ? brushesDoorIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesLadder = brushesLadderIVNodes.Any() ? brushesLadderIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var brushesSidesRemoveUnordered = brushesRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var brushesSidesPathUnordered = brushesPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var brushesSidesCoverUnordered = brushesCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var brushesSidesOverlapUnordered = brushesOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();
            var brushesSidesDoorUnordered = brushesDoor.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DoorTextureName)).ToList();
            var brushesSidesLadderUnordered = brushesLadder.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.LadderTextureName)).ToList();

            brushesSidesRemove = OrderListOfSidesByVerticesMin(brushesSidesRemoveUnordered);
            brushesSidesPath = OrderListOfSidesByVerticesMin(brushesSidesPathUnordered);
            brushesSidesCover = OrderListOfSidesByVerticesMin(brushesSidesCoverUnordered);
            brushesSidesOverlap = OrderListOfSidesByVerticesMin(brushesSidesOverlapUnordered);
            brushesSidesDoor = OrderListOfSidesByVerticesMin(brushesSidesDoorUnordered);
            brushesSidesLadder = OrderListOfSidesByVerticesMin(brushesSidesLadderUnordered);

            // displacements
            displacementsRemove = displacementsRemoveIVNodes.Any() ? displacementsRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsPath = displacementsPathIVNodes.Any() ? displacementsPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsCover = displacementsCoverIVNodes.Any() ? displacementsCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsOverlap = displacementsOverlapIVNodes.Any() ? displacementsOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsDoor = displacementsDoorIVNodes.Any() ? displacementsDoorIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsLadder = displacementsLadderIVNodes.Any() ? displacementsLadderIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            var displacementsSidesRemoveUnordered = displacementsRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var displacementsSidesPathUnordered = displacementsPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var displacementsSidesCoverUnordered = displacementsCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var displacementsSidesOverlapUnordered = displacementsOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();
            var displacementsSidesDoorUnordered = displacementsDoor.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DoorTextureName)).ToList();
            var displacementsSidesLadderUnordered = displacementsLadder.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.LadderTextureName)).ToList();

            displacementsSidesRemove = OrderListOfSidesByVerticesMin(displacementsSidesRemoveUnordered);
            displacementsSidesPath = OrderListOfSidesByVerticesMin(displacementsSidesPathUnordered);
            displacementsSidesCover = OrderListOfSidesByVerticesMin(displacementsSidesCoverUnordered);
            displacementsSidesOverlap = OrderListOfSidesByVerticesMin(displacementsSidesOverlapUnordered);
            displacementsSidesDoor = OrderListOfSidesByVerticesMin(displacementsSidesDoorUnordered);
            displacementsSidesLadder = OrderListOfSidesByVerticesMin(displacementsSidesLadderUnordered);

            // entities
            buyzoneBrushEntities = buyzoneBrushEntitiesIVNodes.Any() ? buyzoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            bombsiteBrushEntities = bombsiteBrushEntitiesIVNodes.Any() ? bombsiteBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            rescueZoneBrushEntities = rescueZoneBrushEntitiesIVNodes.Any() ? rescueZoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            hostageEntities = hostageEntitiesIVNodes.Any() ? hostageEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            ctSpawnEntities = ctSpawnEntitiesIVNodes.Any() ? ctSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            tSpawnEntities = tSpawnEntitiesIVNodes.Any() ? tSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();

            foreach (var entity in buyzoneBrushEntities)
            {
                entitiesSidesByEntityBuyzoneId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }
            foreach (var entity in bombsiteBrushEntities)
            {
                entitiesSidesByEntityBombsiteId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }
            foreach (var entity in rescueZoneBrushEntities)
            {
                entitiesSidesByEntityRescueZoneId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
            }

            jercConfigEntities = jercConfigEntitiesIVNodes.Any() ? jercConfigEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            jercDividerEntities = jercDividerEntitiesIVNodes.Any() ? jercDividerEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
            jercFloorEntities = jercFloorEntitiesIVNodes.Any() ? jercFloorEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
            jercCeilingEntities = jercCeilingEntitiesIVNodes.Any() ? jercCeilingEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
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


        public float GetLowestVerticesZ()
        {
            var allDisplayedBrushSides = GetAllDisplayedBrushSides();

            return allDisplayedBrushSides.SelectMany(x => x.vertices_plus.Select(x => x.z)).Min();
        }


        public float GetHighestVerticesZ()
        {
            var allDisplayedBrushSides = GetAllDisplayedBrushSides();

            return allDisplayedBrushSides.SelectMany(x => x.vertices_plus.Select(x => x.z)).Max();
        }


        private List<Brush> GetAllDisplayedBrushes()
        {
            return brushesPath
                .Concat(brushesCover)
                .Concat(brushesOverlap)
                .Concat(brushesDoor)
                .Concat(brushesLadder)
                .Concat(displacementsPath)
                .Concat(displacementsCover)
                .Concat(displacementsOverlap)
                .Concat(displacementsDoor)
                .Concat(displacementsLadder)
                .Concat(bombsiteBrushEntities.SelectMany(x => x.brushes))
                .Concat(buyzoneBrushEntities.SelectMany(x => x.brushes))
                .Concat(rescueZoneBrushEntities.SelectMany(x => x.brushes))
                .ToList();
        }


        private List<Side> GetAllDisplayedBrushSides()
        {
            return brushesSidesPath
                .Concat(brushesSidesCover)
                .Concat(brushesSidesOverlap)
                .Concat(brushesSidesDoor)
                .Concat(brushesSidesLadder)
                .Concat(displacementsSidesPath)
                .Concat(displacementsSidesCover)
                .Concat(displacementsSidesOverlap)
                .Concat(displacementsSidesDoor)
                .Concat(displacementsSidesLadder)
                .Concat(entitiesSidesByEntityBuyzoneId.SelectMany(x => x.Value))
                .Concat(entitiesSidesByEntityBombsiteId.SelectMany(x => x.Value))
                .Concat(entitiesSidesByEntityRescueZoneId.SelectMany(x => x.Value))
                .ToList();
        }
    }
}
