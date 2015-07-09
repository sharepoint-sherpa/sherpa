using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Installer.Model
{
    public class ShSetup
    {
        public List<ShEnvironment> Environments { get; set; }

        public ShSetup()
        {
            Environments = new List<ShEnvironment>();
        }
    }
}
