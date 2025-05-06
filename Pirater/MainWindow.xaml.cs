using Pirater.Repositories;
using Pirater.Tabeller;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pirater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DbRepository _dbRepo = new DbRepository();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtPirateName.Text.Length > 0)
                {
                    Pirate pirate = new Pirate { Name = txtPirateName.Text };
                    await _dbRepo.RegNewPirate(pirate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}");
            }
        }
    }
}