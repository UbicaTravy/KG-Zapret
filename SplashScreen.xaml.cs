using System;
using System.Threading.Tasks;
using System.Windows;

namespace KG_Zapret {
    public partial class SplashScreen : Window {
        public SplashScreen() {
            InitializeComponent();
        }

        /// <summary>
        /// паказывает заставку на минимальную продолжительность
        /// </summary>
        /// <param name="minimumDuration">Minimum time to show splash screen in milliseconds</param>
        public async Task ShowSplashAsync(int minimumDuration = 2000) {
            Show();
            await Task.Delay(minimumDuration);
        }

        /// <summary>
        /// заставка капут
        /// </summary>
        public new void Close() {
            Dispatcher.Invoke(() => {
                base.Close();
            });
        }
    }
}
