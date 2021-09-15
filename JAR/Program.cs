﻿using JAR.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VMFParser;

namespace JAR
{
    class Program
    {
        private static readonly string visgroupIdJarLayoutName = "jar_layout";
        private static readonly string visgroupIdJarCoverName = "jar_cover";
        private static readonly string visgroupIdJarNegativeName = "jar_negative";
        private static readonly string visgroupIdJarOverlapName = "jar_overlap";

        private static string visgroupIdJarLayoutId;
        private static string visgroupIdJarCoverId;
        private static string visgroupIdJarNegativeId;
        private static string visgroupIdJarOverlapId;

        private static VMF vmf;


        static void Main(string[] args)
        {
            /*
            if (args.Count() != 2 || args[0] != "filepath" || File.Exists(args[1]))
                return;

            var lines = File.ReadAllLines(args[1]);
            */
            var lines = File.ReadAllLines(@"G:\Dropbox\JAR\jar_test_map.vmf");

            vmf = new VMF(lines);

            SetVisgroupIds();

            var vmfRequiredData = GetVmfRequiredData();
        }


        public static void SetVisgroupIds()
        {
            //var visgroupLayout = vmf.VisGroups.Body.Where(x => x.Body.Any(y => y.Name == "name" && y.Value == visgroupIdJarLayoutName));

            visgroupIdJarLayoutId = (from x in vmf.VisGroups.Body
                                     from y in x.Body
                                     where y.Name == "name"
                                     where y.Value == visgroupIdJarLayoutName
                                     select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                     .FirstOrDefault();

            visgroupIdJarCoverId = (from x in vmf.VisGroups.Body
                                    from y in x.Body
                                    where y.Name == "name"
                                    where y.Value == visgroupIdJarCoverName
                                    select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                    .FirstOrDefault();

            visgroupIdJarNegativeId = (from x in vmf.VisGroups.Body
                                       from y in x.Body
                                       where y.Name == "name"
                                       where y.Value == visgroupIdJarNegativeName
                                       select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                       .FirstOrDefault();

            visgroupIdJarOverlapId = (from x in vmf.VisGroups.Body
                                      from y in x.Body
                                      where y.Name == "name"
                                      where y.Value == visgroupIdJarOverlapName
                                      select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                      .FirstOrDefault();
        }


        public static VmfRequiredData GetVmfRequiredData()
        {
            var allProps = vmf.Body.Where(x => x.Name == "entity");
            var allBrushes = vmf.World.Body.Where(x => x.Name == "solid");

            var propsLayout = GetPropsLayout(allProps);
            var propsCover = GetPropsCover(allProps);
            var propsNegative = GetPropsNegative(allProps);
            var propsOverlap = GetPropsOverlap(allProps);

            var brushesLayout = GetBrushesLayout(allBrushes);
            var brushesCover = GetBrushesCover(allBrushes);
            var brushesNegative = GetBrushesNegative(allBrushes);
            var brushesOverlap = GetBrushesOverlap(allBrushes);

            return new VmfRequiredData(propsLayout, propsCover, propsNegative, propsOverlap, brushesLayout, brushesCover, brushesNegative, brushesOverlap);
        }


        public static IEnumerable<IVNode> GetPropsLayout(IEnumerable<IVNode> allProps)
        {
            return from x in allProps
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarLayoutId
                   select x;
        }


        public static IEnumerable<IVNode> GetPropsCover(IEnumerable<IVNode> allProps)
        {
            return from x in allProps
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarCoverId
                   select x;
        }


        public static IEnumerable<IVNode> GetPropsNegative(IEnumerable<IVNode> allProps)
        {
            return from x in allProps
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarNegativeId
                   select x;
        }


        public static IEnumerable<IVNode> GetPropsOverlap(IEnumerable<IVNode> allProps)
        {
            return from x in allProps
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarOverlapId
                   select x;
        }


        public static IEnumerable<IVNode> GetBrushesLayout(IEnumerable<IVNode> allBrushes)
        {
            return from x in allBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarLayoutId
                   select x;
        }


        public static IEnumerable<IVNode> GetBrushesCover(IEnumerable<IVNode> allBrushes)
        {
            return from x in allBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarCoverId
                   select x;
        }


        public static IEnumerable<IVNode> GetBrushesNegative(IEnumerable<IVNode> allBrushes)
        {
            return from x in allBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarNegativeId
                   select x;
        }


        public static IEnumerable<IVNode> GetBrushesOverlap(IEnumerable<IVNode> allBrushes)
        {
            return from x in allBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarOverlapId
                   select x;
        }
    }
}
