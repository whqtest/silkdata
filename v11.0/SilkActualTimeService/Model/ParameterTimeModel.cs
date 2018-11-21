﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkActualTimeService.Model
{
    public class ParameterTimeModel
    {
        public string ProductLineCode { get; set; }

        public string LineCode { get; set; }

        public string ProductCode { get; set; }

        public string StageCode { get; set; }

        public string ProcessCode { get; set; }

        public string ParameterCode { get; set; }

        public string HisTag { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public string SteadyStartTime { get; set; }

        public string SteadyEndTime { get; set; }
    }
}
