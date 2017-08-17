using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealIdolSharp.Model {
    public class NoteCircleSegment {
        public CvCircleSegment OldPoint { get; internal set; }
        public CvCircleSegment NewPoint { get; internal set; }

        public override string ToString() =>
            $"{OldPoint} -> {NewPoint}";

        public NoteCircleSegment() {
        }
        public NoteCircleSegment(CvCircleSegment p1, CvCircleSegment p2) {
            if (p1.Center.Y < p2.Center.Y) {
                OldPoint = p1;
                NewPoint = p2;
            } else {
                OldPoint = p2;
                NewPoint = p1;
            }
        }

        public double Distance {
            get {
                var d = NewPoint.Center - OldPoint.Center;
                return Math.Abs(d.X) + Math.Abs(d.Y);
            }
        }
        public double RadiusRatio => NewPoint.Radius / OldPoint.Radius;

        public double DeltaX => NewPoint.Center.X - OldPoint.Center.X;
        public double DeltaY => NewPoint.Center.Y - OldPoint.Center.Y;
        public double Delta => DeltaY / DeltaX;




        /// <summary>
        /// 場所を推論する
        /// </summary>
        /// <param name="clearanceX"></param>
        /// <param name="xs"></param>
        /// <returns></returns>
        public int? Inference(double lineY, double clearanceX, params double[] xs) {
            Debug.Assert(xs.Length > 0);
            InferenceX = NewPoint.Center.X + (1.0 / Delta) * (lineY - NewPoint.Center.Y);
            InferenceLabel = null;

            if (InferenceX < xs[0] - clearanceX) return null;
            for (int i = 0; i < xs.Length; ++i) {
                if (InferenceX < xs[i] - clearanceX) continue;
                if (InferenceX > xs[i] + clearanceX) continue;
                InferenceLabel = i;
                return i;
            }
            return null;
        }

        /// <summary>
        /// 到達予測するX座標
        /// </summary>
        /// <returns></returns>
        public double InferenceX { get; internal set; }
        public int? InferenceLabel { get; internal set; }
    }
}
