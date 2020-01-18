using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DYMO.Label.Framework;
using Zen.Barcode;
using System.IO;

namespace LabelPrinting
{
    public class LabelPrinter
    {
        ILabel label;

        public LabelPrinter(string file_name = "two_up.label")
        {
            label = Framework.Open(file_name);
        }

        public void PrintLabel(string barcode_str)
        {
            //generate barcode image
            var metrics = new BarcodeMetrics1d(1, 4096, 200);
            metrics.Scale = 100;
            var image = BarcodeDrawFactory.Code128WithChecksum.Draw(barcode_str, metrics);

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                label.SetImagePngData("GRAPHIC", ms);
            }

            label.SetObjectText("TEXT", barcode_str);

            LabelWriterPrintParams printParams = new LabelWriterPrintParams();
            printParams.PrintQuality = LabelWriterPrintQuality.BarcodeAndGraphics;

            try
            {
                label.Print(Framework.GetLabelWriterPrinters().ToArray()[0], printParams);
            }
            catch (Exception ex)
            { }
        }

        public void PrintTwoLabel(string barcode_str_a, string barcode_str_b)
        {
            //generate barcode image
            var metrics = new BarcodeMetrics1d(1, 4096, 200);
            metrics.Scale = 100;
            var image = BarcodeDrawFactory.Code128WithChecksum.Draw(barcode_str_a, metrics);

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                label.SetImagePngData("GRAPHIC", ms);
            }

            var metrics_b = new BarcodeMetrics1d(1, 4096, 200);
            metrics.Scale = 100;
            var image_b = BarcodeDrawFactory.Code128WithChecksum.Draw(barcode_str_b, metrics);

            using (MemoryStream ms = new MemoryStream())
            {
                image_b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                label.SetImagePngData("GRAPHIC_1", ms);
            }

            label.SetObjectText("TEXT", barcode_str_a);
            label.SetObjectText("TEXT_1", barcode_str_b);

            LabelWriterPrintParams printParams = new LabelWriterPrintParams();
            printParams.PrintQuality = LabelWriterPrintQuality.BarcodeAndGraphics;

            try
            {
                label.Print(Framework.GetLabelWriterPrinters().ToArray()[0], printParams);
            }
            catch (Exception ex)
            { }
        }
    }
}
