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
        public List<Prop> propsLayout;
        public List<Prop> propsCover;
        public List<Prop> propsNegative;
        public List<Prop> propsOverlap;

        public List<Side> brushesSidesLayout;
        public List<Side> brushesSidesCover;
        public List<Side> brushesSidesNegative;
        public List<Side> brushesSidesOverlap;

        public VmfRequiredData(IEnumerable<IVNode> propsLayout, IEnumerable<IVNode> propsCover, IEnumerable<IVNode> propsNegative, IEnumerable<IVNode> propsOverlap, IEnumerable<IVNode> brushesLayout, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesOverlap)
        {
            this.propsLayout = propsLayout.Select(x => new Prop(x)).ToList();
            this.propsCover = propsCover.Select(x => new Prop(x)).ToList();
            this.propsNegative = propsNegative.Select(x => new Prop(x)).ToList();
            this.propsOverlap = propsOverlap.Select(x => new Prop(x)).ToList();

            var brushesLayoutModelled = brushesLayout.Select(x => new Brush(x)).ToList();
            var brushesCoverModelled = brushesCover.Select(x => new Brush(x)).ToList();
            var brushesNegativeModelled = brushesNegative.Select(x => new Brush(x)).ToList();
            var brushesOverlapModelled = brushesOverlap.Select(x => new Brush(x)).ToList();

            brushesSidesLayout = brushesLayoutModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.LayoutTextureName)).ToList();
            brushesSidesCover = brushesCoverModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.CoverTextureName)).ToList();
            brushesSidesNegative = brushesNegativeModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.NegativeTextureName)).ToList();
            brushesSidesOverlap = brushesOverlapModelled.SelectMany(x => x.side.Where(y => y.material.ToLower() == TextureNames.OverlapTextureName)).ToList();
        }
    }
}
