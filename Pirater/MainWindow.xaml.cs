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
            FillBoxes();
        }
       
        private async void FillBoxes()
        {
            List<Rank> ranks = await _dbRepo.GetAllRanks();
            ComboBoxPirateRank<Rank>(cboxRanks, ranks);

            List<Pirate> pirates = await _dbRepo.GetAllPirates();
            ListPirates<Pirate>(lstboxPirate, pirates);

            List<Ship> ships = await _dbRepo.GetAllShips();
            ComboBoxShips<Ship>(cboxShips, ships);
        }

        private async void ComboBoxPirateRank<T>(ComboBox cb, List<T> list) // Visar rankerna i combobox
        {
            // https://elearn20.miun.se/moodle/mod/kalvidres/view.php?id=1292192 källa på hur vi fyllde comboboxen
            cb.ItemsSource = list;
            // Det som visas
            cb.DisplayMemberPath = "Name";
            // Osynliga värdet
            cb.SelectedValuePath = "Id";
        } 
        
        private async void btnCreatePirate_Click(object sender, RoutedEventArgs e) // Knapp för att skapa pirat
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

                    // https://stackoverflow.com/questions/27376887/wpf-updating-the-itemssource-in-a-listbox-with-an-async-method källa för att uppdatera listan när en pirat har skapats
                    lstboxPirate.ItemsSource = await _dbRepo.GetAllPirates();

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

        private async void ComboBoxShips<T>(ComboBox cbs, List<T> list) // Visar skeppen i Combobox
        {
            cbs.ItemsSource = list;
            cbs.DisplayMemberPath = "Name";
            cbs.SelectedValuePath = "Id";
        }

        private async void btnRecruitPirate_Click(object sender, RoutedEventArgs e) // Knapp för att bemanna skepp
        {
            // Hämta den valda piraten
            var selectedPirate = (Pirate)lstboxPirate.SelectedItem;
            if (selectedPirate == null)
            {
                MessageBox.Show("Välj en pirat");
                return;
            }
            // Kolla om piraten redan är kopplad till ett skepp
            if (selectedPirate.ShipId > 0)
            {
                MessageBox.Show("Piraten är bemannad på ett annat skepp");
                return;
            }
            // Kolla om ett skepp är valt
            var selectedShip = (Ship)cboxShips.SelectedItem;
            if ( selectedShip == null )
            {
                MessageBox.Show("Välj ett skepp");
                return;
            }
            
            bool success = await _dbRepo.RecruitPirate(selectedPirate.Id, selectedShip.Id);

            if (success)  //Hittade denna if (success) här https://stackoverflow.com/questions/32569860/checking-if-httpstatuscode-represents-success-or-failure/34772513
            {
                MessageBox.Show($"Piraten {selectedPirate.Name} har rekryterats till {selectedShip.Name}!");
            }
           
        }



        public async void btnSearchPirate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string searchPirateName = txtPirateName.Text;
                //string searchParrotName = txtPirateName.Text;
                
                // Rensar listan vid en ny sökning
                lstSearchPirates.ItemsSource = null;

                // Hämta piraten från databasen
                var pirate = await _dbRepo.SearchPirateAsync(searchPirateName);

                if (pirate != null)
                {
                    // Hämta detaljer om piratens skepp och antal besättningsmedlemmar
                    var pirateDetails = await _dbRepo.GetPirateDetailsByIdAsync(pirate.Id);
                    int crewCount = await _dbRepo.GetCrewCountByShipIdAsync(pirateDetails.ShipId);

                    // Visa piratens information i labels
                    lblPirateName.Content = $"Namn: {pirateDetails.Name}";
                    lblShip.Content = $"Skepp: {pirateDetails.ShipId}";
                    lblPirateCount.Content = $"Antal pirater på skeppet: {crewCount}";
                    lblRank.Content = $"Rank: {pirateDetails.RankId}";

                    // Lägg till piraten i listan för visning
                    List<Pirate> pirateList = new List<Pirate> { pirate };
                    lstSearchPirates.ItemsSource = pirateList;
                    lstSearchPirates.DisplayMemberPath = "Name";
                    lstSearchPirates.SelectedValuePath = "Id";
                }
                else
                {
                    MessageBox.Show("Ingen pirat med det namnet hittades.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}");
            }
        }

        private async void lstSearchSkiers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstSearchPirates.SelectedItem == null)
                return;

            var selectedPirate = (Pirate)lstSearchPirates.SelectedItem;
            try
            {
                // hämta infon
                var pirateDetails = await _dbRepo.GetPirateDetailsByIdAsync(selectedPirate.Id);

                if (pirateDetails != null)
                {
                    // Visa informationen i labels
                    lblPirateName.Content = $"Namn: {pirateDetails.Name}";
                    lblShip.Content = $"Skepp: {pirateDetails.ShipId}";
                    //lblPirateCount.Content = $"Hur många pirater på skepp: {pirateDetails.PirateCount}";
                    lblRank.Content = $"Rank: {pirateDetails.RankId}";
                }
                else
                {
                    MessageBox.Show("Ingen information kundes hittas");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett fel inträffade: {ex.Message}");
            }
        }
    }
}