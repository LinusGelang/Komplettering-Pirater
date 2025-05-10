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
            FillComboBox();
        }
       
        private async void FillComboBox()
        {
            List<Rank> ranks = await _dbRepo.GetAllRanks();
            ComboBoxPirateRank<Rank>(cboxRanks, ranks);

            List<Pirate> pirates = await _dbRepo.GetAllPirates();
            ListPirates<Pirate>(lstboxPirate, pirates);

            List<Ship> ships = await _dbRepo.GetAllShips();
            ListShips<Ship>(lstboxShip, ships);
        }

        private async void ComboBoxPirateRank<T>(ComboBox cb, List<T> list)
        {
            // https://elearn20.miun.se/moodle/mod/kalvidres/view.php?id=1292192 källa på hur vi fyllde comboboxen
            cb.ItemsSource = list;
            // Det som visas
            cb.DisplayMemberPath = "Name";
            // Osynliga värdet
            cb.SelectedValuePath = "Id";
        }
        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Om textboxen inte är tom och en rank är vald
                if (txtPirateName.Text.Length > 0 && cboxRanks.SelectedValue != null)
                {
                    // Ny instans för att tilldela namn och rank
                    Pirate pirate = new Pirate
                    { 
                        Name = txtPirateName.Text, // Namnet för piraten från textbox.
                        RankId = (int)cboxRanks.SelectedValue // Castar om värdet till en integer och hämta rank id från combox
                    };
                    // Lägg till i databasen
                    await _dbRepo.RegNewPirate(pirate);
                    // Bekräftelse på att piraten har lagts till
                    MessageBox.Show($"Piraten {pirate.Name} har lagts till! Yaaar!");
                }
                txtPirateName.Clear(); // Rensar Textboxen
                // Källa på hur man återställer comboboxen
                // https://stackoverflow.com/questions/46115862/how-to-reset-combobox
                cboxRanks.SelectedIndex = -1; // Rensar comboboxen
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}"); //om något blir fel så visas vilket felmeddelande som gäller
            }
        }

        private async void ListPirates<T>(ListBox lbp, List<T> list) // Visar piraterna i listbox
        {
            lbp.ItemsSource = list;
            lbp.DisplayMemberPath = "Name";
            lbp.SelectedValuePath = "Id";
        }

        private async void ListShips<T>(ListBox lbs, List<T> list) // Visar skeppen i listbox
        {
            lbs.ItemsSource = list;
            lbs.DisplayMemberPath = "Name";
            lbs.SelectedValuePath = "Id";
        }
    }
}