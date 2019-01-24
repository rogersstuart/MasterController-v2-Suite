using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCICommon
{
    public interface ProgressInterface
    {
        void Show();
        void Hide();

        void SetTitle(string title);
        void SetLabel(string label);

        void SetMaximum(int maximum);

        void Step();

        void Dispose();

        void InitManagedStep(int num_steps);
        void ManagedStep();

        void Reset();
    }
}
