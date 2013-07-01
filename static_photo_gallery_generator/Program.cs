using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace static_photo_gallery_generator
{
    class Program
    {
        static void Main(string[] args)
        {
            //static photo-gallery generator
            //or re-inventing the wheel for the x-th time :)

            string dir = string.Empty;

            if (args != null && args.Length > 0)
                dir = args[0];
            else
                dir = Environment.CurrentDirectory;

            generateAlbums(dir);

        }

        public static void generateAlbums(string pathToGallery)
        {
            string directory = @pathToGallery;
            double thmb_height = 170;

            //the "gallery" directory
            System.IO.DirectoryInfo postsDir = new System.IO.DirectoryInfo(directory);

            //albums without index.html
            IEnumerable<System.IO.DirectoryInfo> albums = postsDir.GetDirectories().Where(d => !(d.GetFiles().Select(fi => fi.Name.ToLower()).Contains("index.html")));

            var albms_count = albums.Count();

            foreach (var album in albums)
            {
                Console.Write("Generating index for " + album.Name);

                //mark that this album is gnerated
                File.WriteAllText(Path.Combine(album.FullName, "generated"), "generated " + DateTime.Now.ToString());

                //get the pictures
                IEnumerable<FileInfo> pictures = album.GetFiles("*.*").Where(s => s.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || s.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

                //create thmbnails
                foreach (var picture in pictures)
                {
                    var extension = picture.Extension;
                    var newPath = picture.FullName.Replace(extension,"_thmb"+extension);
                    createThumbnailImage(picture.FullName, thmb_height, newPath);
                }

                //create img html tags
                List<string> imgTags = new List<string>();
                foreach (var picture in pictures)
                {
                    var extension = picture.Extension;
                    var thmbName = picture.Name.Replace(extension, "_thmb" + extension);
                    imgTags.Add( imgTag(thmbName,picture.Name) );
                }

                //put images int html template
                string htmlIndexCOntent = template().Replace("IMAGES_HERE", string.Join(" ", imgTags));

                //write index.html file
                var index_path = Path.Combine(@album.FullName, "index.html");
                File.WriteAllText(index_path,htmlIndexCOntent);

                Console.WriteLine(" ...OK");
            }

            Console.WriteLine("Succesfully generated indexes for " + albms_count.ToString() + " albums. Have a nice day!");
            Console.ReadKey();
        }

        static void createThumbnailImage(string imagePath, double desiredHeight, string thumbPath)
        {
            Image imgOriginal = null;
            Image imgnew = null;

            try
            {
                imgOriginal = Bitmap.FromFile(imagePath);
                double width = (imgOriginal.Width * desiredHeight) / imgOriginal.Height;
                imgnew = new Bitmap((int)width, (int)desiredHeight, PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(imgnew);
                g.DrawImage(imgOriginal, new Point[] { new Point(0, 0), new Point((int)width, 0), new Point(0, (int)desiredHeight) },
                    new Rectangle(0, 0, imgOriginal.Width, imgOriginal.Height), GraphicsUnit.Pixel);

                imgnew.Save(thumbPath);
            }
            finally
            {
                imgOriginal.Dispose();
                imgnew.Dispose();
            }


        }


        static string imgTag(string imgThumb, string imgFull)
        {
            return @"<a href=""" + imgFull + @"""><img src="""+imgThumb+@"""></a>";
        }

        static string template()
        {
            return @"
                    <!DOCTYPE HTML>
                    <html>
                    <body bgcolor=""black"">

                    IMAGES_HERE

                    </body>
                    </html>
                    
                "   ;    
        }
    }
}
