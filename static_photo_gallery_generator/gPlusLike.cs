using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;

namespace static_photo_gallery_generator
{
    class gPlusLikeAlbum
    {
        /// <summary>
        /// path to album (string)
        /// </summary>
        public string album_path;

        /// <summary>
        /// album as DirectoryInfo instance
        /// </summary>
        public DirectoryInfo album_dir;

        /// <summary>
        /// Album folder contains descriptions.txt
        /// </summary>
        private bool hasDescriptions;

        /// <summary>
        /// photos in album as FileInfo instances
        /// </summary>
        public IEnumerable<FileInfo> photos;

        public gPlusLikeAlbum(DirectoryInfo album)
        {
            album_dir = album;
            album_path = album.FullName;
            photos = album_dir.GetFiles("*.*").Where(s => s.Name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || s.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

            hasDescriptions = album.EnumerateFiles("description.txt").Any();
            if (!hasDescriptions)
                Console.WriteLine(album.Name + "has no description.txt, captions of images will be empty!");
        }

        /// <summary>
        /// Lets say we have pictures and description.txt
        /// this will generate thmbs 
        /// </summary>
        public void generateAlbum()
        {
            ///copy template dir to album dir
            var source = new DirectoryInfo(Program.template_path);
            source.CopyTo(album_path, true);

            var t_index = Path.Combine(album_path, "index.html");
            var t_ShowPic = Path.Combine(album_path, "ShowPicture.html");

            ///Create thumbnails
            //Console.WriteLine("Generating thmbs for " + album_dir.Name);
            CreateThmbs();            

            ///index.html
            //Console.WriteLine("Writing index for " + album_dir.Name);
            var index_content = File.ReadAllText(t_index);
            File.WriteAllText(Path.Combine(album_path,"index.html"), index_content.Replace("###pictures", ContentForIndex()));

            ///ShowPicture.html
            //Console.WriteLine("Writing ShowPicture.....");
            var showPicContent = File.ReadAllText(t_ShowPic);
            File.WriteAllText(Path.Combine(album_path, "ShowPicture.html"), showPicContent.Replace("###fotkae", GenDataForShowPicture()));

        }

        
        /// <summary>
        /// Generate content of the "gallery" div in index.html
        /// Take all photo files and wrap them in div/a/img with requiered atributes
        /// </summary>
        public string ContentForIndex()
        {
            string ret = string.Empty;

            foreach (var photo in photos)
            {
                int height;
                int width;

                using (var img = Image.FromFile(photo.FullName))
                {
                    height = img.Height;
                    width = img.Width;
                }

                string filename_withoutThmb = photo.Name;
                string filename_thmb = photo.Name.Replace(photo.Extension, "_thmb" + photo.Extension);
                ret += (
                string.Format(
                   "<div class=\"Image_Wrapper\" data-caption=\"{3}\"><a href=\"ShowPicture.html?file={4}\"><img src=\"./thmbs/{0}\" width=\"{1}\" height=\"{2}\"></a></div>"
                    , filename_thmb, width.ToString(), height.ToString(), GetCaption(filename_withoutThmb), filename_withoutThmb)

                );
            }

            return ret;
        }

        /// <summary>
        /// Generate data for ShowPicture.html (to make it work even on localhost the data must be embedded directly in ShowPicture.html file, because javascript cannot read external files via file:///)
        /// </summary>
        public string GenDataForShowPicture()
        {
            string ret = string.Empty;
        
            ret += ("var fotkae = [ ");

            var photos_list = photos.ToList(); //because I will need to acces via idnex

            for (int i = 0; i < photos_list.Count(); i++ )
            {
                var photo = photos_list[i];
                var caption = "";
                FileInfo nextImage;
                FileInfo prevImage;
                string nCapt = "";
                string pCapt= "";
                bool panorama;
                int height = 0;
                int width = 0;

                
                using (var img = Image.FromFile(photo.FullName))
                {
                    height = img.Height;
                    width = img.Width;
                }

                if (photo.Name.Contains("pano") || (width / height) > 2)
                    panorama = true;
                else
                    panorama = false;

                if (i == 0)
                {
                    nextImage = photos_list[1];
                    prevImage = photos_list.Last();
                }
                else if (i == photos_list.Count()-1)
                {
                    nextImage = photos_list.First();
                    prevImage = photos_list[i-1];
                }
                else
                {
                    nextImage = photos_list[i+1];
                    prevImage = photos_list[i-1];
                }

                caption = GetCaption(photo.Name);
                nCapt = GetCaption(nextImage.Name);
                pCapt = GetCaption(prevImage.Name);

                //{ file:"L1070514.JPG", caption: "L1070514", nextImage: "L1070518.JPG", prevImage: "L1070530.JPG", nCaption: "next caption", pCaption: "prev caption", panorama: false  },
                //Debug.WriteLine(
                ret += string.Format(
                        "{{ file:\"{0}\", caption: \"{1}\", nextImage: \"{2}\", prevImage: \"{3}\", nCaption: \"{4}\", pCaption: \"{5}\", panorama: {6}, height: {7}, width: {8}  }},",
                        photo.Name,caption,nextImage.Name,prevImage.Name,nCapt,pCapt,panorama.ToString().ToLower(),height,width);
            }

            //Debug.WriteLine("   ];");
            ret += ("   ];");

            return ret;
        }

        /// <summary>
        /// Get caption (photo desctiption) from description.txt
        /// </summary>
        /// <param name="photoFileName">Photo that has description in description.txt file</param>
        /// <returns>Caption for given photo</returns>
        public string GetCaption(string photoFileName)
        {
            ///missing description.txt
            if (!hasDescriptions)
                return ""; 

            var lines = File.ReadAllLines(Path.Combine(album_path,"description.txt"))
                .Select(line => line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(fileNameAndDescription => fileNameAndDescription.Length == 2) //tam kde je jak nazev souboru tak nejaky popisek, zbytek ignoruj
                .ToList();

            var finename_and_description = lines.Where(line => line[0] == photoFileName).FirstOrDefault();

            if (finename_and_description != null)
                return finename_and_description[1];
            else
                return "";

        }


        public void CreateThmbs()
        {
            // 1. create dictionary
            if (!album_dir.GetDirectories("thmbs").Any())
                album_dir.CreateSubdirectory("thmbs");

            // 2. for each photo in album generate thumbnail
            foreach (var photo in photos)
            {
                var extension = photo.Extension;
                var newPath = Path.Combine(album_dir.FullName, "thmbs", photo.Name.Replace(extension, "_thmb" + extension));
                Program.createThumbnailImage(photo.FullName, Program.thmb_height, newPath);
            }
        }

    }
}
