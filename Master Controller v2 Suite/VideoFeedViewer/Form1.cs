using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace VideoFeedViewer
{
    public partial class Form1 : Form
    {
        VideoCapture grabber;

        public Form1()
        {
            InitializeComponent();
        }

        private void FrameGrabbed()
        {
            Mat frame = new Mat();
            grabber.Retrieve(frame);

            Invoke((MethodInvoker)(() =>
            {
                pictureBox1.Image = frame.Bitmap;
                Refresh();
            }));

            frame.Dispose();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            //Task.Run(() =>
            //{
            grabber = new VideoCapture("http://mccsrv1:9090");
            grabber.ImageGrabbed += (a, b) =>
            {
                FrameGrabbed();
            };
            grabber.Start();
               // grabber.Grab();
            //});
        }
    }
}
