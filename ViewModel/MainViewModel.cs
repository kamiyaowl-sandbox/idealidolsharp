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

        public MainViewModel() {
            PlayTestMovieCommand =
                TestMovieSourcePath.Select(x => File.Exists(x))
                                   .CombineLatest(CurrentFrame.Select(x => x == 0), (a, b) => a & b)
                                   .ToReactiveCommand(false);

            PlayTestMovieCommand.Subscribe(async _ => await playTestMovie(TestMovieSourcePath.Value));

            PlayTestMovieCommand.Execute();
        }


        /// <summary>
        /// 非同期で動画を流して処理する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private Task playTestMovie(string path) {
            return Task.Run(() => {
                using (var cap = new VideoCapture(path) {

                })
                using (var inputWin = new Window("input")) {
                    var inputMat = new Mat();
                    for (int i = 0; ; ++i) {
                        cap.Read(inputMat);
                        if (inputMat.Empty()) break;





                        inputWin.ShowImage(inputMat);
                        if (Cv2.WaitKey(1) == 27) break;//ESC
                        CurrentFrame.Value = i;
                    }
                }
                CurrentFrame.Value = 0;
            });
        }
    }
}
