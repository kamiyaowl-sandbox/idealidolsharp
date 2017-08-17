using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealIdolSharp.Model {
    public class ProcessParameter {

        public double DiffGrayThreash { get; set; } = 100;

        public double HoughCircleDp { get; set; } = 2;
        public double HoughCircleMinDist { get; set; } = 30;
        public double HoughCircleParam1 { get; set; } = 160;
        public double HoughCircleParam2 { get; set; } = 50;
        public int HoughCircleMinRadius { get; set; } = 10;
        public int HoughCircleMaxRadius { get; set; } = 40;
        public int SobelXOrder { get; internal set; } = 1;
        public int SobelYOrder { get; internal set; } = 0;

        public double NearCircleDistanceLimit { get; set; } = 100;
        public double NearCircleRadiusErrorRange { get; set; } = 0.3;
        public double AbsDeltaMin { get; set; } = 1;

        public int UserTapY { get; set; } = 610;
        public int UserTapX0 { get; set; } = 130;
        public int UserTapX1 { get; set; } = 305;
        public int UserTapX2 { get; set; } = 480;
        public int UserTapX3 { get; set; } = 655;
        public int UserTapX4 { get; set; } = 830;
        public int UserTapXClearance { get; set; } = 100;
    }
}
