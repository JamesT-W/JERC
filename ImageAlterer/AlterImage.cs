using ImageProcessor;
using System.Drawing;

namespace ImageAlterer
{
    public class ImageProcessorExtender
    {
        public ImageProcessorExtender()
        { }


        public ImageFactory GetBlurredImage(Image image, int blurValue, bool flipY = true, bool dispose = false)
        {
            if (dispose)
            {
                using (var imageFactory = new ImageFactory())
                {
                    return imageFactory.Load(image).Flip(flipY, false).GaussianBlur(blurValue);
                }
            }
            else
            {
                var imageFactory = new ImageFactory();
                return imageFactory.Load(image).Flip(flipY, false).GaussianBlur(blurValue);
            }
        }


        public ImageFactory GetBlurredImage(string imageFilepath, int blurValue, bool flipY = true, bool dispose = false)
        {
            if (dispose)
            {
                using (var imageFactory = new ImageFactory())
                {
                    return imageFactory.Load(imageFilepath).Flip(flipY, false).GaussianBlur(blurValue);
                }
            }
            else
            {
                var imageFactory = new ImageFactory();
                return imageFactory.Load(imageFilepath).Flip(flipY, false).GaussianBlur(blurValue);
            }
        }



        //ImageProcessor.Imaging.ImageLayer over = new ImageProcessor.Imaging.ImageLayer();
        //over.Image = new Bitmap(tempPath);

        //imageFactory.Load(background)
        //    .Overlay(over)
        //    .Save(Environment.CurrentDirectory + "/temp/test2.png");

        //Debug.Log("Saved final map to {0}", path);
    }
}
