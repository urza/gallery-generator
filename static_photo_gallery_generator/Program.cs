using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        #region fields
        /// size of thumbnails, width is calculated
        public static double thmb_height = 400;
       
        /// path to "template" directory, which is copied to every album, and index and ShowPicture are used with replace
        public static string template_path;

        public static string gallery_path;
        #endregion

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(@"Usage: usgg.exe C:\path\to\album(s) [C:\path\to\template]");
                return;
            }

            if (args.Length == 1)
            {
                //if path to template is not provided, try to find it in app execution directory /template
                //https://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-a-net-console-application#comment9462946_837501
                template_path = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase), "template");

                //check it exists
                if (!Directory.Exists(template_path))
                {
                    Console.WriteLine("please provide gallery template, not found in " + template_path);
                    return;
                }
            }
            else
            {
                template_path = args[1];
            }

            gallery_path = args[0];

            generateAlbums(gallery_path);
        }

        public static void generateAlbums(string pathToGallery)
        {
            string directory = @pathToGallery;

            ///the "gallery" directory
            System.IO.DirectoryInfo galleryDir = new System.IO.DirectoryInfo(directory);

            ///albums without index.html
            var albums = galleryDir.GetDirectories().Where(d => !(d.GetFiles().Select(fi => fi.Name.ToLower()).Contains("index.html"))).ToList();

            ///add the gallery folder itlsef also as album (if it has no files, it will be ommited, like any other empty album)
            albums.Add(galleryDir);

            var albms_count = albums.Count();

            Stopwatch s = new Stopwatch();
            s.Start();

            ///generating multiple albums in parallel, it is up to 50% faster in my tests
            Parallel.ForEach(albums, album =>
            {
                Console.WriteLine("Generating " + album.Name);

                ///mark that this album is gnerated
                File.WriteAllText(Path.Combine(album.FullName, "generated"), "generated " + DateTime.Now.ToString());

                gPlusLikeAlbum album_gPlusLike = new gPlusLikeAlbum(album);
                album_gPlusLike.generateAlbum();
            });

            s.Stop();

            Console.WriteLine("Succesfully generated indexes for " + albms_count.ToString() + " albums in " + s.ElapsedMilliseconds  + " ms. Have a nice day!");
            Console.ReadKey();
        }

        #region helper methods
        /// <summary>
        /// Create new smaller image and save it to destination path
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="desiredHeight"></param>
        /// <param name="thumbPath"></param>
        public static void createThumbnailImage(string imagePath, double desiredHeight, string thumbPath)
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
        #endregion

    }
}
