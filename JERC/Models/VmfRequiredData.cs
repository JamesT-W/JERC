using JERC.Constants;
using System.Collections.Generic;
using System.Linq;
using VMFParser;

namespace JERC.Models
{
    public class VmfRequiredData
    {
        public List<Brush> brushesIgnore;
        public List<Brush> brushesRemove;
        public List<Brush> brushesPath;
        public List<Brush> brushesCover;
        public List<Brush> brushesOverlap;
        public List<Brush> brushesDoor;
        public List<Brush> brushesLadder;
        public List<Brush> brushesDanger;

        public List<Brush> brushesBuyzone;
        public List<Brush> brushesBombsiteA;
        public List<Brush> brushesBombsiteB;
        public List<Brush> brushesRescueZone;

        public List<Brush> brushesHostage;
        public List<Brush> brushesTSpawn;
        public List<Brush> brushesCTSpawn;


        public List<Side> brushesSidesIgnore;
        public List<Side> brushesSidesRemove;
        public List<Side> brushesSidesPath;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesOverlap;
        public List<Side> brushesSidesDoor;
        public List<Side> brushesSidesLadder;
        public List<Side> brushesSidesDanger;

        /*public List<Side> brushesSidesBuyzone;
        public List<Side> brushesSidesBombsiteA;
        public List<Side> brushesSidesBombsiteB;
        public List<Side> brushesSidesRescueZone;

        public List<Side> brushesSidesHostage;
        public List<Side> brushesSidesTSpawn;
        public List<Side> brushesSidesCTSpawn;*/


        public List<Brush> displacementsIgnore;
        public List<Brush> displacementsRemove;
        public List<Brush> displacementsPath;
        public List<Brush> displacementsCover;
        public List<Brush> displacementsOverlap;
        public List<Brush> displacementsDoor;
        public List<Brush> displacementsLadder;
        public List<Brush> displacementsDanger;

        public List<Brush> displacementsBuyzone;
        public List<Brush> displacementsBombsiteA;
        public List<Brush> displacementsBombsiteB;
        public List<Brush> displacementsRescueZone;

        public List<Brush> displacementsHostage;
        public List<Brush> displacementsTSpawn;
        public List<Brush> displacementsCTSpawn;


        public List<Side> displacementsSidesIgnore;
        public List<Side> displacementsSidesRemove;
        public List<Side> displacementsSidesPath;
        public List<Side> displacementsSidesCover;
        public List<Side> displacementsSidesOverlap;
        public List<Side> displacementsSidesDoor;
        public List<Side> displacementsSidesLadder;
        public List<Side> displacementsSidesDanger;

        /*public List<Side> displacementsSidesBuyzone;
        public List<Side> displacementsSidesBombsiteA;
        public List<Side> displacementsSidesBombsiteB;
        public List<Side> displacementsSidesRescueZone;

        public List<Side> displacementsSidesHostage;
        public List<Side> displacementsSidesTSpawn;
        public List<Side> displacementsSidesCTSpawn;*/


        public List<Entity> buyzoneBrushEntities;
        public List<Entity> bombsiteBrushEntities;
        public List<Entity> rescueZoneBrushEntities;
        public List<Entity> hostageEntities;
        public List<Entity> ctSpawnEntities;
        public List<Entity> tSpawnEntities;

        public List<Entity> jercBoxBrushEntities;

        public Dictionary<int, List<Side>> entitiesSidesByEntityBuyzoneId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityBombsiteId = new Dictionary<int, List<Side>>();
        public Dictionary<int, List<Side>> entitiesSidesByEntityRescueZoneId = new Dictionary<int, List<Side>>();

        public Dictionary<int, List<Side>> entitiesSidesByEntityJercBoxId = new Dictionary<int, List<Side>>();

        public Dictionary<int, JercBox> jercBoxByEntityJercBoxId = new Dictionary<int, JercBox>();

        public List<Entity> jercConfigEntities;
        public List<Entity> jercDividerEntities;
        public List<Entity> jercFloorEntities;
        public List<Entity> jercCeilingEntities;
        public List<Entity> jercDispRotationEntities;


        public VmfRequiredData(
            IEnumerable<IVNode> brushesIgnoreIVNodes, IEnumerable<IVNode> brushesRemoveIVNodes, IEnumerable<IVNode> brushesPathIVNodes, IEnumerable<IVNode> brushesCoverIVNodes, IEnumerable<IVNode> brushesOverlapIVNodes, IEnumerable<IVNode> brushesDoorIVNodes, IEnumerable<IVNode> brushesLadderIVNodes, IEnumerable<IVNode> brushesDangerIVNodes,
            IEnumerable<IVNode> brushesBuyzoneIVNodes,IEnumerable<IVNode> brushesBombsiteAIVNodes, IEnumerable<IVNode> brushesBombsiteBIVNodes, IEnumerable<IVNode> brushesRescueZoneIVNodes, IEnumerable<IVNode> brushesHostageIVNodes, IEnumerable<IVNode> brushesTSpawnIVNodes, IEnumerable<IVNode> brushesCTSpawnIVNodes,
            IEnumerable<IVNode> displacementsIgnoreIVNodes, IEnumerable<IVNode> displacementsRemoveIVNodes, IEnumerable<IVNode> displacementsPathIVNodes, IEnumerable<IVNode> displacementsCoverIVNodes, IEnumerable<IVNode> displacementsOverlapIVNodes, IEnumerable<IVNode> displacementsDoorIVNodes, IEnumerable<IVNode> displacementsLadderIVNodes, IEnumerable<IVNode> displacementsDangerIVNodes,
            IEnumerable<IVNode> displacementsBuyzoneIVNodes, IEnumerable<IVNode> displacementsBombsiteAIVNodes, IEnumerable<IVNode> displacementsBombsiteBIVNodes, IEnumerable<IVNode> displacementsRescueZoneIVNodes, IEnumerable<IVNode> displacementsHostageIVNodes, IEnumerable<IVNode> displacementsTSpawnIVNodes, IEnumerable<IVNode> displacementsCTSpawnIVNodes,
            IEnumerable<IVNode> buyzoneBrushEntitiesIVNodes, IEnumerable<IVNode> bombsiteBrushEntitiesIVNodes, IEnumerable<IVNode> rescueZoneBrushEntitiesIVNodes, IEnumerable<IVNode> hostageEntitiesIVNodes, IEnumerable<IVNode> ctSpawnEntitiesIVNodes, IEnumerable<IVNode> tSpawnEntitiesIVNodes,
            IEnumerable<IVNode> brushesIgnoreBrushEntitiesIVNodes, IEnumerable<IVNode> brushesRemoveBrushEntitiesIVNodes, IEnumerable<IVNode> brushesPathBrushEntitiesIVNodes, IEnumerable<IVNode> brushesCoverBrushEntitiesIVNodes, IEnumerable<IVNode> brushesOverlapBrushEntitiesIVNodes, IEnumerable<IVNode> brushesDoorBrushEntitiesIVNodes, IEnumerable<IVNode> brushesLadderBrushEntitiesIVNodes, IEnumerable<IVNode> brushesDangerBrushEntitiesIVNodes,
            IEnumerable<IVNode> brushesBuyzoneBrushEntitiesIVNodes, IEnumerable<IVNode> brushesBombsiteABrushEntitiesIVNodes, IEnumerable<IVNode> brushesBombsiteBBrushEntitiesIVNodes, IEnumerable<IVNode> brushesRescueZoneBrushEntitiesIVNodes, IEnumerable<IVNode> brushesHostageBrushEntitiesIVNodes, IEnumerable<IVNode> brushesTSpawnBrushEntitiesIVNodes, IEnumerable<IVNode> brushesCTSpawnBrushEntitiesIVNodes,
            IEnumerable<IVNode> jercBoxBrushEntitiesIVNodes,
            IEnumerable<IVNode> jercConfigEntitiesIVNodes, IEnumerable<IVNode> jercDividerEntitiesIVNodes, IEnumerable<IVNode> jercFloorEntitiesIVNodes, IEnumerable<IVNode> jercCeilingEntitiesIVNodes, IEnumerable<IVNode> jercDispRotationEntitiesIVNodes
        )
        {
            Logger.LogNewLine();
            Logger.LogMessage("Sorting the data...");

            // world brushes (brush entity brushes are concatinated on)
            brushesIgnore = brushesIgnoreIVNodes.Any() ? brushesIgnoreIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesRemove = brushesRemoveIVNodes.Any() ? brushesRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesPath = brushesPathIVNodes.Any() ? brushesPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesCover = brushesCoverIVNodes.Any() ? brushesCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesOverlap = brushesOverlapIVNodes.Any() ? brushesOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesDoor = brushesDoorIVNodes.Any() ? brushesDoorIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesLadder = brushesLadderIVNodes.Any() ? brushesLadderIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesDanger = brushesDangerIVNodes.Any() ? brushesDangerIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            brushesBuyzone = brushesBuyzoneIVNodes.Any() ? brushesBuyzoneIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesBombsiteA = brushesBombsiteAIVNodes.Any() ? brushesBombsiteAIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesBombsiteB = brushesBombsiteBIVNodes.Any() ? brushesBombsiteBIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesRescueZone = brushesRescueZoneIVNodes.Any() ? brushesRescueZoneIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesHostage = brushesHostageIVNodes.Any() ? brushesHostageIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesTSpawn = brushesTSpawnIVNodes.Any() ? brushesTSpawnIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            brushesCTSpawn = brushesCTSpawnIVNodes.Any() ? brushesCTSpawnIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            // adds to world brushes from certain brush entities that use the corresponding textures
            if (brushesIgnoreBrushEntitiesIVNodes.Any())
                brushesIgnore.AddRange(brushesIgnoreBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesRemoveBrushEntitiesIVNodes.Any())
                brushesRemove.AddRange(brushesRemoveBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesPathBrushEntitiesIVNodes.Any())
                brushesPath.AddRange(brushesPathBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesCoverBrushEntitiesIVNodes.Any())
                brushesCover.AddRange(brushesCoverBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesOverlapBrushEntitiesIVNodes.Any())
                brushesOverlap.AddRange(brushesOverlapBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesDoorBrushEntitiesIVNodes.Any())
                brushesDoor.AddRange(brushesDoorBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesLadderBrushEntitiesIVNodes.Any())
                brushesLadder.AddRange(brushesLadderBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesDangerBrushEntitiesIVNodes.Any())
                brushesDanger.AddRange(brushesDangerBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());

            if (brushesBuyzoneBrushEntitiesIVNodes.Any())
                brushesBuyzone.AddRange(brushesBuyzoneBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesBombsiteABrushEntitiesIVNodes.Any())
                brushesBombsiteA.AddRange(brushesBombsiteABrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesBombsiteBBrushEntitiesIVNodes.Any())
                brushesBombsiteB.AddRange(brushesBombsiteBBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesRescueZoneBrushEntitiesIVNodes.Any())
                brushesRescueZone.AddRange(brushesRescueZoneBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesHostageBrushEntitiesIVNodes.Any())
                brushesHostage.AddRange(brushesHostageBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesTSpawnBrushEntitiesIVNodes.Any())
                brushesTSpawn.AddRange(brushesTSpawnBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());
            if (brushesCTSpawnBrushEntitiesIVNodes.Any())
                brushesCTSpawn.AddRange(brushesCTSpawnBrushEntitiesIVNodes.Select(x => new Brush(x)).ToList());

            // calculate vertices_plus for every brush side for vanilla hammer vmfs, as hammer++ adds vertices itself when saving a vmf
            if (GameConfigurationValues.isVanillaHammer)
            {
                VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushSides(
                    brushesIgnore.SelectMany(x => x.side).Concat(brushesRemove.SelectMany(x => x.side)).Concat(brushesPath.SelectMany(x => x.side)).Concat(brushesCover.SelectMany(x => x.side)).Concat(brushesOverlap.SelectMany(x => x.side)).Concat(brushesDoor.SelectMany(x => x.side)).Concat(brushesLadder.SelectMany(x => x.side)).Concat(brushesDanger.SelectMany(x => x.side))
                    .ToList()
                );
            }

            var brushesSidesIgnoreUnordered = brushesIgnore.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.IgnoreTextureName)).ToList();
            var brushesSidesRemoveUnordered = brushesRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var brushesSidesPathUnordered = brushesPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var brushesSidesCoverUnordered = brushesCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var brushesSidesOverlapUnordered = brushesOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();
            var brushesSidesDoorUnordered = brushesDoor.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DoorTextureName)).ToList();
            var brushesSidesLadderUnordered = brushesLadder.SelectMany(x => x.side.Where(y => TextureNames.LadderTextureNames.Any(z => z == y.material.ToLower()))).ToList();
            var brushesSidesDangerUnordered = brushesDanger.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DangerTextureName)).ToList();

            /*var brushesSidesBuyzoneUnordered = brushesBuyzone.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BuyzoneTextureName)).ToList();
            var brushesSidesBombsiteAUnordered = brushesBombsiteA.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BombsiteATextureName)).ToList();
            var brushesSidesBombsiteBUnordered = brushesBombsiteB.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BombsiteBTextureName)).ToList();
            var brushesSidesRescueZoneUnordered = brushesRescueZone.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RescueZoneTextureName)).ToList();

            var brushesSidesHostageUnordered = brushesHostage.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.HostageTextureName)).ToList();
            var brushesSidesTSpawnUnordered = brushesTSpawn.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.TSpawnTextureName)).ToList();
            var brushesSidesCTSpawnUnordered = brushesCTSpawn.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CTSpawnTextureName)).ToList();*/

            // remove all of a brush's sides when there is a displacement side on the brush
            brushesSidesIgnoreUnordered.RemoveAll(x => brushesIgnore.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesRemoveUnordered.RemoveAll(x => brushesRemove.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesPathUnordered.RemoveAll(x => brushesPath.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesCoverUnordered.RemoveAll(x => brushesCover.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesOverlapUnordered.RemoveAll(x => brushesOverlap.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesDoorUnordered.RemoveAll(x => brushesDoor.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesLadderUnordered.RemoveAll(x => brushesLadder.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesDangerUnordered.RemoveAll(x => brushesDanger.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));

            /*brushesSidesBuyzoneUnordered.RemoveAll(x => brushesBuyzone.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesBombsiteAUnordered.RemoveAll(x => brushesBombsiteA.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesBombsiteBUnordered.RemoveAll(x => brushesBombsiteB.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesRescueZoneUnordered.RemoveAll(x => brushesRescueZone.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));

            brushesSidesHostageUnordered.RemoveAll(x => brushesHostage.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesTSpawnUnordered.RemoveAll(x => brushesTSpawn.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));
            brushesSidesCTSpawnUnordered.RemoveAll(x => brushesCTSpawn.FirstOrDefault(y => y.id == x.brushId).side.Any(y => y.isDisplacement));*/
            //

            brushesSidesIgnore = OrderListOfSidesByVerticesMin(brushesSidesIgnoreUnordered, true);
            brushesSidesRemove = OrderListOfSidesByVerticesMin(brushesSidesRemoveUnordered);
            brushesSidesPath = OrderListOfSidesByVerticesMin(brushesSidesPathUnordered);
            brushesSidesCover = OrderListOfSidesByVerticesMin(brushesSidesCoverUnordered);
            brushesSidesOverlap = OrderListOfSidesByVerticesMin(brushesSidesOverlapUnordered);
            brushesSidesDoor = OrderListOfSidesByVerticesMin(brushesSidesDoorUnordered);
            brushesSidesLadder = OrderListOfSidesByVerticesMin(brushesSidesLadderUnordered);
            brushesSidesDanger = OrderListOfSidesByVerticesMin(brushesSidesDangerUnordered);

            /*brushesSidesBuyzone = OrderListOfSidesByVerticesMin(brushesSidesBuyzoneUnordered);
            brushesSidesBombsiteA = OrderListOfSidesByVerticesMin(brushesSidesBombsiteAUnordered);
            brushesSidesBombsiteB = OrderListOfSidesByVerticesMin(brushesSidesBombsiteBUnordered);
            brushesSidesRescueZone = OrderListOfSidesByVerticesMin(brushesSidesRescueZoneUnordered);

            brushesSidesHostage = OrderListOfSidesByVerticesMin(brushesSidesHostageUnordered);
            brushesSidesTSpawn = OrderListOfSidesByVerticesMin(brushesSidesTSpawnUnordered);
            brushesSidesCTSpawn = OrderListOfSidesByVerticesMin(brushesSidesCTSpawnUnordered);*/


            // displacements
            displacementsIgnore = displacementsIgnoreIVNodes.Any() ? displacementsIgnoreIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsRemove = displacementsRemoveIVNodes.Any() ? displacementsRemoveIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsPath = displacementsPathIVNodes.Any() ? displacementsPathIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsCover = displacementsCoverIVNodes.Any() ? displacementsCoverIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsOverlap = displacementsOverlapIVNodes.Any() ? displacementsOverlapIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsDoor = displacementsDoorIVNodes.Any() ? displacementsDoorIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsLadder = displacementsLadderIVNodes.Any() ? displacementsLadderIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsDanger = displacementsDangerIVNodes.Any() ? displacementsDangerIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            displacementsBuyzone = displacementsBuyzoneIVNodes.Any() ? displacementsBuyzoneIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsBombsiteA = displacementsBombsiteAIVNodes.Any() ? displacementsBombsiteAIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsBombsiteB = displacementsBombsiteBIVNodes.Any() ? displacementsBombsiteBIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsRescueZone = displacementsRescueZoneIVNodes.Any() ? displacementsRescueZoneIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsHostage = displacementsHostageIVNodes.Any() ? displacementsHostageIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsTSpawn = displacementsTSpawnIVNodes.Any() ? displacementsTSpawnIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();
            displacementsCTSpawn = displacementsCTSpawnIVNodes.Any() ? displacementsCTSpawnIVNodes.Select(x => new Brush(x)).ToList() : new List<Brush>();

            // calculate vertices_plus for every brush side for vanilla hammer vmfs, as hammer++ adds vertices itself when saving a vmf
            if (GameConfigurationValues.isVanillaHammer)
            {
                VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushSides(
                    displacementsIgnore.SelectMany(x => x.side).Concat(displacementsRemove.SelectMany(x => x.side)).Concat(displacementsPath.SelectMany(x => x.side)).Concat(displacementsCover.SelectMany(x => x.side)).Concat(displacementsOverlap.SelectMany(x => x.side)).Concat(displacementsDoor.SelectMany(x => x.side)).Concat(displacementsLadder.SelectMany(x => x.side)).Concat(displacementsDanger.SelectMany(x => x.side))
                    .ToList()
                );
            }

            var displacementsSidesIgnoreUnordered = displacementsIgnore.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.IgnoreTextureName)).ToList();
            var displacementsSidesRemoveUnordered = displacementsRemove.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RemoveTextureName)).ToList();
            var displacementsSidesPathUnordered = displacementsPath.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.PathTextureName)).ToList();
            var displacementsSidesCoverUnordered = displacementsCover.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            var displacementsSidesOverlapUnordered = displacementsOverlap.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();
            var displacementsSidesDoorUnordered = displacementsDoor.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DoorTextureName)).ToList();
            var displacementsSidesLadderUnordered = displacementsLadder.SelectMany(x => x.side.Where(y => TextureNames.LadderTextureNames.Any(z => z == y.material.ToLower()))).ToList();
            var displacementsSidesDangerUnordered = displacementsDanger.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.DangerTextureName)).ToList();

            /*var displacementsSidesBuyzoneUnordered = displacementsBuyzone.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BuyzoneTextureName)).ToList();
            var displacementsSidesBombsiteAUnordered = displacementsBombsiteA.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BombsiteATextureName)).ToList();
            var displacementsSidesBombsiteBUnordered = displacementsBombsiteB.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.BombsiteBTextureName)).ToList();
            var displacementsSidesRescueZoneUnordered = displacementsRescueZone.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.RescueZoneTextureName)).ToList();

            var displacementsSidesHostageUnordered = displacementsHostage.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.HostageTextureName)).ToList();
            var displacementsSidesTSpawnUnordered = displacementsTSpawn.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.TSpawnTextureName)).ToList();
            var displacementsSidesCTSpawnUnordered = displacementsCTSpawn.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CTSpawnTextureName)).ToList();*/

            // remove all non displacement sides on the brush
            displacementsSidesIgnoreUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesRemoveUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesPathUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesCoverUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesOverlapUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesDoorUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesLadderUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesDangerUnordered.RemoveAll(x => !x.isDisplacement);

            /*displacementsSidesBuyzoneUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesBombsiteAUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesBombsiteBUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesRescueZoneUnordered.RemoveAll(x => !x.isDisplacement);

            displacementsSidesHostageUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesTSpawnUnordered.RemoveAll(x => !x.isDisplacement);
            displacementsSidesCTSpawnUnordered.RemoveAll(x => !x.isDisplacement);*/
            //

            displacementsSidesIgnore = OrderListOfSidesByVerticesMin(displacementsSidesIgnoreUnordered, true);
            displacementsSidesRemove = OrderListOfSidesByVerticesMin(displacementsSidesRemoveUnordered);
            displacementsSidesPath = OrderListOfSidesByVerticesMin(displacementsSidesPathUnordered);
            displacementsSidesCover = OrderListOfSidesByVerticesMin(displacementsSidesCoverUnordered);
            displacementsSidesOverlap = OrderListOfSidesByVerticesMin(displacementsSidesOverlapUnordered);
            displacementsSidesDoor = OrderListOfSidesByVerticesMin(displacementsSidesDoorUnordered);
            displacementsSidesLadder = OrderListOfSidesByVerticesMin(displacementsSidesLadderUnordered);
            displacementsSidesDanger = OrderListOfSidesByVerticesMin(displacementsSidesDangerUnordered);

            /*displacementsSidesBuyzone = OrderListOfSidesByVerticesMin(displacementsSidesBuyzoneUnordered);
            displacementsSidesBombsiteA = OrderListOfSidesByVerticesMin(displacementsSidesBombsiteAUnordered);
            displacementsSidesBombsiteB = OrderListOfSidesByVerticesMin(displacementsSidesBombsiteBUnordered);
            displacementsSidesRescueZone = OrderListOfSidesByVerticesMin(displacementsSidesRescueZoneUnordered);

            displacementsSidesHostage = OrderListOfSidesByVerticesMin(displacementsSidesHostageUnordered);
            displacementsSidesTSpawn = OrderListOfSidesByVerticesMin(displacementsSidesTSpawnUnordered);
            displacementsSidesCTSpawn = OrderListOfSidesByVerticesMin(displacementsSidesCTSpawnUnordered);*/


            // entities
            buyzoneBrushEntities = buyzoneBrushEntitiesIVNodes.Any() ? buyzoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            bombsiteBrushEntities = bombsiteBrushEntitiesIVNodes.Any() ? bombsiteBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            rescueZoneBrushEntities = rescueZoneBrushEntitiesIVNodes.Any() ? rescueZoneBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            hostageEntities = hostageEntitiesIVNodes.Any() ? hostageEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            ctSpawnEntities = ctSpawnEntitiesIVNodes.Any() ? ctSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            tSpawnEntities = tSpawnEntitiesIVNodes.Any() ? tSpawnEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();

            // calculate vertices_plus for every brush side for vanilla hammer vmfs, as hammer++ adds vertices itself when saving a vmf
            if (GameConfigurationValues.isVanillaHammer)
            {
                VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushSides(
                    buyzoneBrushEntities.SelectMany(x => x.brushes.SelectMany(y => y.side))
                        .Concat(bombsiteBrushEntities.SelectMany(x => x.brushes.SelectMany(y => y.side)))
                        .Concat(rescueZoneBrushEntities.SelectMany(x => x.brushes.SelectMany(y => y.side)))
                        .Concat(hostageEntities.SelectMany(x => x.brushes.SelectMany(y => y.side)))
                        .Concat(ctSpawnEntities.SelectMany(x => x.brushes.SelectMany(y => y.side)))
                        .Concat(tSpawnEntities.SelectMany(x => x.brushes.SelectMany(y => y.side)))
                    .ToList()
                );
            }

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


            // brush entities (JERC)
            jercBoxBrushEntities = jercBoxBrushEntitiesIVNodes.Any() ? jercBoxBrushEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();

            foreach (var entity in jercBoxBrushEntities)
            {
                /////////////////////////////////////////////////////////// TODO: This is a dreadful way to handle different instaces containing the same IDs. They should be handled sepearately somehow, NOT added together
                if (entitiesSidesByEntityJercBoxId.ContainsKey(entity.id))
                {
                    entitiesSidesByEntityJercBoxId[entity.id].AddRange(entity.brushes.SelectMany(x => x.side).ToList());

                    var newKeyTrying = entity.id + 1000;
                    var foundUnusedId = false;
                    while (!foundUnusedId)
                    {
                        if (!jercBoxByEntityJercBoxId.ContainsKey(newKeyTrying))
                        {
                            jercBoxByEntityJercBoxId.Add(newKeyTrying, new JercBox(entity)); /////////////////////////////////////////////////////////// The ID changes, so it will be different in jercBoxByEntityJercBoxId than in entitiesSidesByEntityJercBoxId
                            foundUnusedId = true;
                        }
                        else
                        {
                            newKeyTrying++;
                        }
                    }
                }
                else
                {
                    entitiesSidesByEntityJercBoxId.Add(entity.id, entity.brushes.SelectMany(x => x.side).ToList());
                    jercBoxByEntityJercBoxId.Add(entity.id, new JercBox(entity));
                }
            }


            // entities (JERC)
            jercConfigEntities = jercConfigEntitiesIVNodes.Any() ? jercConfigEntitiesIVNodes.Select(x => new Entity(x)).ToList() : new List<Entity>();
            jercDividerEntities = jercDividerEntitiesIVNodes.Any() ? jercDividerEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
            jercFloorEntities = jercFloorEntitiesIVNodes.Any() ? jercFloorEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
            jercCeilingEntities = jercCeilingEntitiesIVNodes.Any() ? jercCeilingEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>(); // order by lowest height first
            jercDispRotationEntities = jercDispRotationEntitiesIVNodes.Any() ? jercDispRotationEntitiesIVNodes.Select(x => new Entity(x)).OrderBy(x => new Vertices(x.origin).z).ToList() : new List<Entity>();


            Logger.LogMessage("Finished sorting the data");
        }


        // Orders by descending, then uses distinct to ensure that it gets the MAX value first for each side and ignores the rest.
        // Then, it reverses, so it is ascending (MIN value first)
        private static List<Side> OrderListOfSidesByVerticesMin(List<Side> sides, bool calculateVerticesPlus = false)
        {
            if (sides == null || !sides.Any())
                return sides;

            if (GameConfigurationValues.isVanillaHammer && calculateVerticesPlus)
            {
                VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushSides(sides);
            }

            var sidesNew = (from x in sides
                            from y in x?.vertices_plus
                            orderby y?.z descending
                            select x).Distinct().ToList();

            sidesNew.Reverse();

            return sidesNew;
        }


        public float GetLowestVerticesZ()
        {
            var allDisplayedBrushSides = GetAllDisplayedBrushSides();

            return allDisplayedBrushSides.SelectMany(x => x.vertices_plus.Where(y => y.z != null).Select(y => (float)y.z)).Min();
        }


        public float GetHighestVerticesZ()
        {
            var allDisplayedBrushSides = GetAllDisplayedBrushSides();

            return allDisplayedBrushSides.SelectMany(x => x.vertices_plus.Where(y => y.z != null).Select(y => (float)y.z)).Max();
        }


        // brushes
        public List<Brush> GetAllDisplayedBrushes()
        {
            return GetAllDisplayedBrushesBrush()
                .Concat(GetAllDisplayedBrushesDisplacement())
                .Concat(GetAllDisplayedBrushesEntity())
                .ToList();
        }

        public List<Brush> GetAllDisplayedBrushesBrush()
        {
            return brushesPath
                .Concat(brushesCover)
                .Concat(brushesOverlap)
                .Concat(brushesDoor)
                .Concat(brushesLadder)
                .Concat(brushesDanger)
                .Concat(brushesBuyzone)
                .Concat(brushesBombsiteA)
                .Concat(brushesBombsiteB)
                .Concat(brushesRescueZone)
                .ToList();
        }

        public List<Brush> GetAllDisplayedBrushesDisplacement()
        {
            return displacementsPath
                .Concat(displacementsCover)
                .Concat(displacementsOverlap)
                .Concat(displacementsDoor)
                .Concat(displacementsLadder)
                .Concat(displacementsDanger)
                .Concat(displacementsBuyzone)
                .Concat(displacementsBombsiteA)
                .Concat(displacementsBombsiteB)
                .Concat(displacementsRescueZone)
                .ToList();
        }

        public List<Brush> GetAllDisplayedBrushesEntity()
        {
            return bombsiteBrushEntities.SelectMany(x => x.brushes)
                .Concat(buyzoneBrushEntities.SelectMany(x => x.brushes))
                .Concat(rescueZoneBrushEntities.SelectMany(x => x.brushes))
                .Concat(jercBoxBrushEntities.SelectMany(x => x.brushes))
                .ToList();
        }
        //


        // brush sides
        public List<Side> GetAllDisplayedBrushSides()
        {
            return GetAllDisplayedBrushSidesBrush()
                .Concat(GetAllDisplayedBrushSidesDisplacement())
                .Concat(GetAllDisplayedBrushSidesEntity())
                .ToList();
        }

        public List<Side> GetAllDisplayedBrushSidesBrush()
        {
            return brushesSidesPath
                .Concat(brushesSidesCover)
                .Concat(brushesSidesOverlap)
                .Concat(brushesSidesDoor)
                .Concat(brushesSidesLadder)
                .Concat(brushesSidesDanger)
                .ToList();
        }

        public List<Side> GetAllDisplayedBrushSidesDisplacement()
        {
            return displacementsSidesPath
                .Concat(displacementsSidesCover)
                .Concat(displacementsSidesOverlap)
                .Concat(displacementsSidesDoor)
                .Concat(displacementsSidesLadder)
                .Concat(displacementsSidesDanger)
                .ToList();
        }

        public List<Side> GetAllDisplayedBrushSidesEntity()
        {
            return entitiesSidesByEntityBuyzoneId.SelectMany(x => x.Value)
                .Concat(entitiesSidesByEntityBombsiteId.SelectMany(x => x.Value))
                .Concat(entitiesSidesByEntityRescueZoneId.SelectMany(x => x.Value))
                .Concat(entitiesSidesByEntityJercBoxId.SelectMany(x => x.Value))
                .ToList();
        }
        //
    }
}
