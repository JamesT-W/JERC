using JERC.Constants;
using System.Collections.Generic;
using System.Linq;
using VMFParser;

namespace JERC.Models
{
    public class DisplacementSideNormalsRow
    {
        public int rowNum;
        public List<Vertices> valuesList = new List<Vertices>();

        public DisplacementSideNormalsRow(int rowNum, int numOfPoints)
        {
            this.rowNum = rowNum;

            for (int i = 0; i < numOfPoints; i++)
                valuesList.Add(new Vertices(0, 0, 0));
        }

        public DisplacementSideNormalsRow(int rowNum, IVNode valuesIVNode)
        {
            this.rowNum = rowNum;

            var values = valuesIVNode.Value;
            var valuesSplit = values.Split(" ").ToList();

            if (valuesSplit.Count() % 3 != 0)
            {
                Logger.LogWarning("Found an incorrect number of values in 'normals' for a displacement. Skipping.");
                return;
            }

            for (int i = 0; i < valuesSplit.Count(); i += 3)
            {
                var parsedCorrectlyX = float.TryParse(valuesSplit[i], Globalization.Style, Globalization.Culture, out float x);
                var parsedCorrectlyY = float.TryParse(valuesSplit[i+1], Globalization.Style, Globalization.Culture, out float y);
                var parsedCorrectlyZ = float.TryParse(valuesSplit[i+2], Globalization.Style, Globalization.Culture, out float z);

                // if any don't parse correctly, just set to 0, even though that might be way off
                if (!parsedCorrectlyX)
                    x = 0;
                if (!parsedCorrectlyY)
                    y = 0;
                if (!parsedCorrectlyZ)
                    z = 0;

                valuesList.Add(new Vertices(x, y, z));
            }
        }
    }
}
