using JERC.Constants;
using System.Collections.Generic;
using System.Linq;
using VMFParser;

namespace JERC.Models
{
    public class DisplacementSideDistancesRow
    {
        public int rowNum;
        public List<float> valuesList = new List<float>();

        public DisplacementSideDistancesRow(int rowNum, int numOfPoints)
        {
            this.rowNum = rowNum;

            for (int i = 0; i < numOfPoints; i++)
                valuesList.Add(0);
        }

        public DisplacementSideDistancesRow(int rowNum, IVNode valuesIVNode)
        {
            this.rowNum = rowNum;

            var values = valuesIVNode.Value;
            var valuesSplit = values.Split(" ").ToList();

            for (int i = 0; i < valuesSplit.Count(); i++)
            {
                var parsedCorrectly = float.TryParse(valuesSplit[i], Globalization.Style, Globalization.Culture, out float value);

                // if it doesn't parse correctly, just set to 0, even though that might be way off
                if (!parsedCorrectly)
                    value = 0;

                valuesList.Add(value);
            }
        }
    }
}
