using IdealIdolSharp.Model;
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

        public ReactiveCollection<NoteCircleSegment> InferenceNotes { get; set; } = new ReactiveCollection<NoteCircleSegment>();

        public MainViewModel() {
            PlayTestMovieCommand =
                TestMovieSourcePath.Select(x => File.Exists(x))
                                   .CombineLatest(CurrentFrame.Select(x => x == 0), (a, b) => a & b)
                                   .ToReactiveCommand(false);

            PlayTestMovieCommand.Subscribe(async _ => await playTestMovie(TestMovieSourcePath.Value));

            var param = new ProcessParameter();
            ProcessParameters.Add(param);
            ProcessParameters.Add(new ProcessParameter());
            ProcessParameters.Add(new ProcessParameter());
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
                    var width = inputMat.Width;
                    var height = inputMat.Height;
                    var rows = inputMat.Rows;
                    var cols = inputMat.Cols;

                    using (var diffMat = new Mat(rows, cols, MatType.CV_8UC3))
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

                            //同じノートの推論
                            var notes = findPairNotes(inputMat, circles);
                            foreach (var n in notes) {
                                inputMat.Circle((int)n.OldPoint.Center.X, (int)n.OldPoint.Center.Y, (int)n.OldPoint.Radius, CvColor.Blue, 3);
                                inputMat.Circle((int)n.NewPoint.Center.X, (int)n.NewPoint.Center.Y, (int)n.NewPoint.Radius, CvColor.Red, 3);
                                inputMat.Line((int)n.OldPoint.Center.X, (int)n.OldPoint.Center.Y, (int)n.NewPoint.Center.X, (int)n.NewPoint.Center.Y, CvColor.Green, 3);
                                inputMat.Line((int)n.NewPoint.Center.X, (int)n.NewPoint.Center.Y, (int)n.InferenceX, ProcessParam.Value.UserTapY, CvColor.LightGreen, 3);
                                if (n.InferenceLabel.HasValue) inputMat.PutText($"{n.InferenceLabel.Value}", new Point(n.NewPoint.Center.X + ProcessParam.Value.UserTapXClearance / 2, n.NewPoint.Center.Y), FontFace.Italic, 2, CvColor.Cyan, 4);
                            }
                            InferenceNotes.ClearOnScheduler();
                            InferenceNotes.AddRangeOnScheduler(notes);

                            //情報の描画
                            inputMat.Circle(ProcessParam.Value.UserTapX0, ProcessParam.Value.UserTapY, ProcessParam.Value.HoughCircleMaxRadius, CvColor.Purple, 3);
                            inputMat.Circle(ProcessParam.Value.UserTapX1, ProcessParam.Value.UserTapY, ProcessParam.Value.HoughCircleMaxRadius, CvColor.Purple, 3);
                            inputMat.Circle(ProcessParam.Value.UserTapX2, ProcessParam.Value.UserTapY, ProcessParam.Value.HoughCircleMaxRadius, CvColor.Purple, 3);
                            inputMat.Circle(ProcessParam.Value.UserTapX3, ProcessParam.Value.UserTapY, ProcessParam.Value.HoughCircleMaxRadius, CvColor.Purple, 3);
                            inputMat.Circle(ProcessParam.Value.UserTapX4, ProcessParam.Value.UserTapY, ProcessParam.Value.HoughCircleMaxRadius, CvColor.Purple, 3);
                            inputMat.Line(0, ProcessParam.Value.UserTapY, width, ProcessParam.Value.UserTapY, CvColor.Purple, 3);

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

        /// <summary>
        /// 隣接するノードを抽出
        /// </summary>
        /// <param name="inputMat"></param>
        /// <param name="circles"></param>
        /// <returns></returns>
        private IEnumerable<NoteCircleSegment> findPairNotes(Mat inputMat, CvCircleSegment[] circles) {
            var circlesList = circles.OrderBy(p => p.Center.Y).ToList();
            foreach (var c in circles) {
                circlesList.Remove(c);//これから検索するので消す
                var nearest = circlesList.Select(p => new NoteCircleSegment(c, p))
                .Where(x => x.Distance < ProcessParam.Value.NearCircleDistanceLimit)
                .Where(x => (x.RadiusRatio < ProcessParam.Value.NearCircleRadiusErrorRange + 1) &&
                            (1 - ProcessParam.Value.NearCircleRadiusErrorRange < x.RadiusRatio)
                )
                .Where(x => Math.Abs(x.Delta) > ProcessParam.Value.AbsDeltaMin)
                .OrderBy(p => p.Distance)
                .FirstOrDefault();

                if (nearest == null) continue;
                circlesList.Remove(nearest.OldPoint);//検索済なので消す
                circlesList.Remove(nearest.NewPoint);//検索済なので消す

                nearest.Inference(
                    ProcessParam.Value.UserTapY,
                    ProcessParam.Value.UserTapXClearance,
                    ProcessParam.Value.UserTapX0,
                    ProcessParam.Value.UserTapX1,
                    ProcessParam.Value.UserTapX2,
                    ProcessParam.Value.UserTapX3,
                    ProcessParam.Value.UserTapX4
                    );
                yield return nearest;
            }
        }
    }

}
