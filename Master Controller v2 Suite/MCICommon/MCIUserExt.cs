using MCICommon.MCIWeb;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;

namespace MCICommon
{
    public class MCIUserExt
    {
        private int[] home_floors;
        private Image user_photo;
        private string[] tags;
        private UserCredentials UserCredentials { get; set; }

        public MCIUserExt()
        {
            
        }

        public int[] HomeFloors
        {
            get { return home_floors; }
            set { home_floors = value; }
        }

        [JsonConverter(typeof(ImageConverter))]
        public Image UserPhoto
        {
            get { return user_photo; }
            set { user_photo = value; }
        }

        public string[] Tags
        {
            get { return tags; }
            set { tags = value; }
        }
    }

    public class ImageConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var base64 = (string)reader.Value;
            // convert base64 to byte array, put that into memory stream and feed to image
            if (base64 != null)
                return Image.FromStream(new MemoryStream(Convert.FromBase64String(base64)));
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var image = (Image)value;
            // save to memory stream in original format
            var ms = new MemoryStream();
            image.Save(ms, image.RawFormat);
            byte[] imageBytes = ms.ToArray();
            // write byte array, will be converted to base64 by JSON.NET
            writer.WriteValue(imageBytes);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Image);
        }
    }
}
