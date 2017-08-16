using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealIdolSharp.ViewModel {

    class MainViewModel {
        public ReactiveProperty<string> TestMovieSourcePath { get; set; } = new ReactiveProperty<string>("TestResource/ankira-masterplus.mp4");
        public ReactiveCommand PlayTestMovieCommand { get; set; }

        public ReactiveProperty<int> CurrentFrame { get; set; } = new ReactiveProperty<int>(0);
        /// <summary>
        /// 処理するタスクに設定値を読ませる
        /// </summary>
        public ReactiveCollection<ProcessParameter> ProcessParameters { get; set; } = new ReactiveCollection<ProcessParameter>();
        public ReactiveProperty<ProcessParameter> ProcessParam { get; set; } = new ReactiveProperty<ProcessParameter>();

        public MainViewModel() {
            PlayTestMovieCommand =
                TestMovieSourcePath.Select(x => File.Exists(x))
                                   .CombineLatest(CurrentFrame.Select(x => x == 0), (a, b) => a & b)
                                   .ToReactiveCommand(false);

            PlayTestMovieCommand.Subscribe(async _ => await playTestMovie(TestMovieSourcePath.Value));

            var param = new ProcessParameter();
            ProcessParameters.Add(param);
            ProcessParam.Value = param;

            PlayTestMovieCommand.Execute();
        }


        /// <summary>
        /// 非同期で動画を流して処理する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private Task playTestMovie(string path) {
            return Task.Run(() => {
                using (var cap = new VideoCapture(path))
                using (var inputMat = new Mat()) {
                    //データフォーマットを確定
                    cap.Read(inputMat);
                    using (var diffMat = new Mat(inputMat.Rows, inputMat.Cols, MatType.CV_8UC3))
                    using (var prevMat = new Mat()) {
                        //初回データのコピー
                        inputMat.CopyTo(prevMat);

                        /* 本命 */
                        for (int i = 0; ; ++i) {
                            //read
                            cap.Read(inputMat);
                            if (inputMat.Empty()) break;
                            //差分画像から前回と今回のノート位置を推定
                            Cv2.Absdiff(inputMat, prevMat, diffMat);
                            var diffGrayMat = diffMat.CvtColor(OpenCvSharp.ColorConversion.BgrToGray);
                            var diffBinMat = diffGrayMat.Threshold(ProcessParam.Value.DiffGrayThreash, 255, OpenCvSharp.ThresholdType.Binary);
                            var circles = Cv2.HoughCircles(diffBinMat, OpenCvSharp.HoughCirclesMethod.Gradient,
                                ProcessParam.Value.HoughCircleDp,
                                ProcessParam.Value.HoughCircleMinDist,
                                ProcessParam.Value.HoughCircleParam1,
                                ProcessParam.Value.HoughCircleParam2,
                                ProcessParam.Value.HoughCircleMinRadius,
                                ProcessParam.Value.HoughCircleMaxRadius);
                            inputMat.CopyTo(prevMat);

                            //元画像を微分して、ホールドノートの推定をする（真縦のホールドは差分画像では追従不可)
                            var inputGrayMat = inputMat.CvtColor(ColorConversion.BgrToGray);
                            var inputSobelMat = inputGrayMat.Sobel(MatType.CV_8UC1,
                               ProcessParam.Value.SobelXOrder,
                               ProcessParam.Value.SobelYOrder);

                            //円の図示
                            //foreach (var c in circles) {
                            //    inputMat.Circle((int)c.Center.X, (int)c.Center.Y, (int)c.Radius, CvColor.Red, 3);
                            //}
                            //同じノートの推論
                            var circlesList = circles.ToList();
                            foreach (var c in circles) {
                                circlesList.Remove(c);//これから検索するので消す
                                var nearest = circlesList.Select(p => {
                                    var d = p.Center - c.Center;
                                    return new {
                                        Point = p,
                                        Distance = Math.Abs(d.X) + Math.Abs(d.Y)
                                    };
                                })
                                .Where(x => x.Distance < ProcessParam.Value.NearCircleDistance)
                                .OrderBy(p => p.Distance)
                                .FirstOrDefault();

                                if (nearest == null) continue;
                                circlesList.Remove(nearest.Point);//検索済なので消す

                                inputMat.Circle((int)c.Center.X, (int)c.Center.Y, (int)c.Radius, CvColor.Red, 3);
                                inputMat.Circle((int)nearest.Point.Center.X, (int)nearest.Point.Center.Y, (int)nearest.Point.Radius, CvColor.Blue, 3);
                                inputMat.Line((int)c.Center.X, (int)c.Center.Y, (int)nearest.Point.Center.X, (int)nearest.Point.Center.Y, CvColor.Purple, 3);
                            }


                            //show
                            Cv2.ImShow("input", inputMat);
                            Cv2.ImShow("diff", diffMat);
                            Cv2.ImShow("diffBin", diffBinMat);
                            Cv2.ImShow("sobel", inputSobelMat);
                            if (Cv2.WaitKey(1) == 27) break;//ESC
                            CurrentFrame.Value = i;
                        }
                    }
                }
                CurrentFrame.Value = 0;
            });
        }
    }
    public class ProcessParameter {
        public double DiffGrayThreash { get; set; } = 40;
        public double HoughCircleDp { get; set; } = 2;
        public double HoughCircleMinDist { get; set; } = 30;
        public double HoughCircleParam1 { get; set; } = 160;
        public double HoughCircleParam2 { get; set; } = 50;
        public int HoughCircleMinRadius { get; set; } = 10;
        public int HoughCircleMaxRadius { get; set; } = 40;
        public int SobelXOrder { get; internal set; } = 1;
        public int SobelYOrder { get; internal set; } = 0;
        public float NearCircleDistance { get; set; } = 100;
    }
}
