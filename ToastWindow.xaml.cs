using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PasswordProtector.Models;
using PasswordProtector.Services;

namespace PasswordProtector
{
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer;

        public ToastWindow(List<Account> expiredAccounts, List<Account> expiringAccounts)
        {
            InitializeComponent();

            // 만료된 계정 표시
            if (expiredAccounts.Any())
            {
                ExpiredSection.Visibility = Visibility.Visible;
                ExpiredList.ItemsSource = expiredAccounts;
            }

            // 만료 임박 계정 표시
            if (expiringAccounts.Any())
            {
                ExpiringSection.Visibility = Visibility.Visible;
                ExpiringList.ItemsSource = expiringAccounts;
                
                // 만료된 계정이 없으면 상단 마진 제거
                if (!expiredAccounts.Any())
                {
                    ExpiringSection.Margin = new Thickness(0);
                }
            }

            // 헤더 텍스트 설정
            var totalCount = expiredAccounts.Count + expiringAccounts.Count;
            HeaderText.Text = $"만료 알림 ({totalCount}개)";

            // 초기 투명도 0으로 설정
            Opacity = 0;

            // 자동 닫기 타이머 설정 (5초 후)
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
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

            // 화면 오른쪽 아래에 위치 설정 (콘텐츠 렌더링 후 실제 높이로 계산)
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth;
            Top = workArea.Bottom - ActualHeight;

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
        /// 만료/만료 임박 계정이 있으면 토스트 알림을 표시합니다.
        /// </summary>
        public static void ShowToast()
        {
            try
            {
                var iniFileService = new IniFileService();
                var accounts = iniFileService.LoadAccounts();

                // 만료된 계정 (D+1 이상)
                var expiredAccounts = accounts
                    .Where(a => a.DaysUntilExpiry.HasValue && a.DaysUntilExpiry < 0)
                    .OrderBy(a => a.DaysUntilExpiry)
                    .ToList();

                // 만료 임박 계정 (D-7 이하, D-Day 포함)
                var expiringAccounts = accounts
                    .Where(a => a.DaysUntilExpiry.HasValue && a.DaysUntilExpiry >= 0 && a.DaysUntilExpiry <= 7)
                    .OrderBy(a => a.DaysUntilExpiry)
                    .ToList();

                // 표시할 계정이 있을 때만 토스트 표시
                if (expiredAccounts.Any() || expiringAccounts.Any())
                {
                    var toast = new ToastWindow(expiredAccounts, expiringAccounts);
                    // ToastWindow가 Application.MainWindow로 설정되지 않도록 방지
                    var currentMain = Application.Current.MainWindow;
                    toast.Show();
                    if (currentMain != null)
                    {
                        Application.Current.MainWindow = currentMain;
                    }
                }
            }
            catch
            {
                // 오류 발생 시 조용히 무시
            }
        }
    }
}
