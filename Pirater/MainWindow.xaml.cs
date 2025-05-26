using Npgsql;
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
                    MessageBox.Show($"Piraten {pirate.Name} har lagts till! Yaaarr!");

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
            lbp.DisplayMemberPath = "GetName";
            lbp.SelectedValuePath = "Id";
        }

        private async void ComboBoxShips<T>(ComboBox cbs, List<T> list) // Visar skeppen i Combobox
        {
            cbs.ItemsSource = list;
            cbs.DisplayMemberPath = "GetName";
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
                string searchParrotName = txtPirateName.Text;

                if (int.TryParse(searchPirateName, out int pirateId)) //https://stackoverflow.com/questions/45642091/int-tryparse-passing-only-boolean-value-to-database Dock använde vi inte Boolean här
                {
                    MessageBox.Show("Ange ett giltigt pirat namn");
                    return;
                }

                // Rensar listan vid en ny sökning
                lstSearchPirates.ItemsSource = null;

                // Hämta piraten från databasen
                var pirate = await _dbRepo.SearchPirateAsync(searchPirateName);

                if (pirate != null)
                {
                    // Hämta detaljer om piratens skepp och antal besättningsmedlemmar
                    var pirateDetails = await _dbRepo.GetPirateDetailsByIdAsync(pirate.Id);
                    //int crewCount = await _dbRepo.GetCrewCountByShipIdAsync(pirateDetails.ShipId);
                    int currentCrewCount = await _dbRepo.CountCurrentCrewAsync(pirateDetails.ShipId);
                    int maxCrewSize = await _dbRepo.GetCrewMaxSizeAsync(pirateDetails.ShipId);

                    // Hämta skeppets namn från databasen
                    string shipName = "Inte bemannad";
                    if (pirateDetails.ShipId > 0)
                    {
                        // Hämta skeppnamn via repository
                        shipName = await _dbRepo.GetShipNameById(pirateDetails.ShipId);
                    }

                    string rankName = "Ingen rank"; //Skapar en variabel som kallar på metoden som använder rank-id men returnerar rank-namnet.
                    if (pirateDetails.RankId > -1) //Eftersom att Orankad börjar på noll så satte jag ranked behöver vara mer än -1
                    {
                        // Hämta ranknamn via repository, kopierad från koden ovan
                        rankName = await _dbRepo.GetRankNameById(pirateDetails.RankId);
                    }

                    // Visa piratens information i labels
                    lblShip.Content = $"Skepp: {shipName}"; //Ändrade från pirateDetails.ShipId till shipName så visas skeppets namn i lblShip
                    lblPirateCount.Content = $"Antal pirater på skeppet: {currentCrewCount} / {maxCrewSize} av maxbesättning";
                    lblRank.Content = $"Rank: {rankName}"; //Samma som med skepp två rader upp så ändrade jag det till en string variabel

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

        private async void lstSearchPirates_SelectionChanged(object sender, SelectionChangedEventArgs e) //metod för att lista upp datan kring piraten i olika laables
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
                    //lblShip.Content = $"Skepp: {pirateDetails.ShipId}";
                    //lblPirateCount.Content = $"Hur många pirater på skepp: {pirateDetails.PirateCount}"; //Tidig del som skulle räkna antal pirater per båt
                    //lblRank.Content = $"Rank: {pirateDetails.RankId}";
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

        private async void btnSinkShip_Click(object sender, RoutedEventArgs e) 
        {
            // Kräver att ett skepp är valt i rullistanb
            var selectedShip = (Ship)cboxShips.SelectedItem;
            if (selectedShip == null)
            {
                MessageBox.Show("Välj ett skepp att sänka");
                return;
            }

            
            try
            {
                List<Pirate> shipCrew = await _dbRepo.GetPiratesByShipId(selectedShip.Id);

                // Skapar en slumpmäsig överlevnadschans för piraterna //https://www.tutorialsteacher.com/articles/generate-random-numbers-in-csharp
                Random random = new Random();
                int survivorCount = 0;
                

                // Lista för att hålla koll på vilka pirater som dog
                List<string> deadPirates = new List<string>();

                // Piraterna ges en 50% chans att överleva
                foreach (var pirate in shipCrew)
                {
                    bool survives = random.Next(2) == 1; // 50% chans

                    if (survives)
                    {
                        //Plussar på de överlevande piraterna
                        await _dbRepo.RemovePirateFromShip(pirate.Id);
                        survivorCount++;
                    }
                    else
                    {
                        //Om piraten dör så raderas den. 4ever-ever. Instängda i Davy Jones-kista. RIP
                        deadPirates.Add(pirate.Name);
                        await _dbRepo.DeletePirate(pirate.Id);
                    }
                }

                //Hänvisar till metoden för att markera ett skepp som sänkt i vår databas
                await _dbRepo.MarkShipAsSunk(selectedShip.Id);

                string deadPiratesNames = "";
                bool isFirst = true; // kollar om det är första namnet i listan
                
                foreach (var pirate in deadPirates)
                {
                    if (!isFirst)
                    {
                        deadPiratesNames += ", "; // om det är fler än en
                    }

                    deadPiratesNames += pirate; // lägger till pirat namnet
                    isFirst = false;
                }
                // kollar om listan med döda pirater är tom
                if (deadPiratesNames == "")
                {
                    deadPiratesNames = "Inga";
                }
                
                //En liten messagebox som bekräftar att skeppet sjunkit och hur många som överlevde.
                MessageBox.Show($"Skeppet {selectedShip.Name} har sjunkit! " +
                     $"{survivorCount} av {shipCrew.Count} pirater överlevde katastrofen.\n\n" + //Två st \n gör att det blir en tom rad till namnen på de döda piraterna
                     $"Döda pirater: {deadPiratesNames} " );

                //Uppdaterar listboxen så man kan se vilka pirater som fortfarande lever
                lstboxPirate.ItemsSource = await _dbRepo.GetAllPirates();

                //Uppdaterar skeppens combobox och ger en markering till vilka skepp som sjunkit då vi fortfarande vill kunna se dem.
                List<Ship> ships = await _dbRepo.GetAllShips();
                ComboBoxShips<Ship>(cboxShips, ships);
                cboxShips.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett fel inträffade: {ex.Message}");
            }
        }
    }
}