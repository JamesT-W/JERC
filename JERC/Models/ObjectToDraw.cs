using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class ObjectToDraw
    {
        public List<VerticesToDraw> verticesToDraw;


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw)
        {
            this.verticesToDraw = verticesToDraw;
        }


        public static ObjectToDraw Clone(ObjectToDraw objectToDraw)
        {
            var verticesToDrawNew = new List<VerticesToDraw>();

            foreach (var verticesToDraw in objectToDraw.verticesToDraw)
            {
                verticesToDrawNew.Add(new VerticesToDraw(verticesToDraw.vertices, verticesToDraw.colour));
            }

            return new ObjectToDraw(verticesToDrawNew);
        }
    }
}
