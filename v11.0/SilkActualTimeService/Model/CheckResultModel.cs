using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class CheckResultModel:ParameterModel
    {
        public ArrayList array { get; set; }

        public CheckResultModel()
        {
            array = new ArrayList();
        }

    }
}
