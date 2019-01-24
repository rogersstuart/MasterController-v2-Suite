using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using System.Numerics;

namespace MasterControllerDotNet_Server
{
    public partial class ApplyAverageToForm : Form
    {
        public ApplyAverageToForm()
        {
            InitializeComponent();
        }

        private void ApplyAverageToForm_Load(object sender, EventArgs e)
        {
            
        }

        public void RefreshData(Queue<float> queue0_i, Queue<float> queue1_i, Queue<float> queue2_i, Queue<float> queue3_i, Queue<float> queue4_i, Queue<int> queue5_i)
        {
            new Thread(delegate() {

            float[] queue0 = queue0_i.ToArray();
            float[] queue1 = queue1_i.ToArray();
            float[] queue2 = queue2_i.ToArray();
            float[] queue3 = queue3_i.ToArray();
            float[] queue4 = queue4_i.ToArray();
            int[] queue5 = queue5_i.ToArray();


            this.Invoke(new Action(() => chart1.Series[0].Points.DataBindY(queue0)));
            this.Invoke(new Action(() => chart2.Series[0].Points.DataBindY(queue1)));
            this.Invoke(new Action(() => chart3.Series[0].Points.DataBindY(queue2)));
            //this.Invoke(new Action(() => chart4.Series[0].Points.DataBindY(queue3)));
            this.Invoke(new Action(() => chart5.Series[0].Points.DataBindY(queue4)));
            this.Invoke(new Action(() => chart6.Series[0].Points.DataBindY(queue5)));

            this.Invoke(new Action(() => chart1.Series[1].Points.DataBindY(new float[] { queue0.Average(), queue0.Average(), queue0.Average(), queue0.Average() })));
            this.Invoke(new Action(() => chart2.Series[1].Points.DataBindY(new float[] { queue1.Average(), queue1.Average(), queue1.Average(), queue1.Average() })));
            this.Invoke(new Action(() => chart3.Series[1].Points.DataBindY(new float[] { queue2.Average(), queue2.Average(), queue2.Average(), queue2.Average() })));
            //this.Invoke(new Action(() => chart4.Series[1].Points.DataBindY(new float[] { queue3.Average(), queue3.Average(), queue3.Average(), queue3.Average() })));
            this.Invoke(new Action(() => chart5.Series[1].Points.DataBindY(new float[] { queue4.Average(), queue4.Average(), queue4.Average(), queue4.Average() })));
            this.Invoke(new Action(() => chart6.Series[1].Points.DataBindY(new int[] { (int)queue5.Average(), (int)queue5.Average(), (int)queue5.Average(), (int)queue5.Average() })));

            Complex[] complex3 = new Complex[queue3.Length];
            for (int index_counter = 0; index_counter < queue3.Length; index_counter++)
                complex3[index_counter] = new Complex(queue3[index_counter], 0);

            Fourier.Forward(complex3);

            float[] vals = new float[queue3.Length / 2];
            for (int index_counter = 9; index_counter < queue3.Length / 2; index_counter++)
                vals[index_counter] = (float)complex3[index_counter].Magnitude;

            this.Invoke(new Action(() => chart4.Series[0].Points.DataBindY(vals)));

            }).Start();
            //chart1.
            //this.Invoke(new Action(() => chart1.ChartAreas[0].AxisY.Maximum = queue0.Max()));
            //this.Invoke(new Action(() => chart1.ChartAreas[0].AxisY.Minimum = queue0.Min()));
        }
    }
}
