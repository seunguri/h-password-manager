using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PasswordProtector
{
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer;

        public ToastWindow()
        {
            InitializeComponent();

            // 화면 오른쪽 아래에 위치 설정
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width;
            Top = workArea.Bottom - Height;

            // 초기 투명도 0으로 설정
            Opacity = 0;

            // 자동 닫기 타이머 설정 (3초 후)
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                FadeOutAndClose();
            };
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            
            // 슬라이드 업 + 페이드 인 애니메이션
            var slideAnimation = new DoubleAnimation
            {
                From = Top + 30,
                To = Top,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            BeginAnimation(TopProperty, slideAnimation);
            BeginAnimation(OpacityProperty, fadeAnimation);

            // 타이머 시작
            _closeTimer.Start();
        }

        private void FadeOutAndClose()
        {
            // 슬라이드 다운 + 페이드 아웃 애니메이션
            var slideAnimation = new DoubleAnimation
            {
                From = Top,
                To = Top + 30,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };
            fadeAnimation.Completed += (s, e) => Close();

            BeginAnimation(TopProperty, slideAnimation);
            BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// 토스트 알림을 표시합니다.
        /// </summary>
        public static void ShowToast()
        {
            var toast = new ToastWindow();
            toast.Show();
        }
    }
}
