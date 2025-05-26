using Microsoft.Extensions.Configuration;
using Npgsql;
using Pirater.Tabeller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pirater.Repositories
{
    public class DbRepository
    {
        private readonly string _connectionString;
        public DbRepository()
        {
            var config = new ConfigurationBuilder()
                             .AddUserSecrets<DbRepository>()
                             .Build();
            _connectionString = config.GetConnectionString("DefaultConnection");
        }
        public async Task<List<Rank>> GetAllRanks() //Metod som hämtar alla ranks från databasen.
        {
            // Lista för att lagra alla ranker
            List<Rank> ranks = new List<Rank>();
            
            // Anslut till databasen
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Hämta ranker från databasen
            using var command = new NpgsqlCommand("SELECT id, name FROM rank", conn);

            // Kör kommandot och läser resultatet
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Skapa en instans 
                    Rank rank = new Rank()
                    {
                        Id = (int)reader["id"], // Hämta Id
                        Name = (string)reader["name"].ToString() // Hämta namn

                    };
                    // Lägger till ranken i listan
                    ranks.Add(rank);
                }
            }
            // Returnerar listan 
            return ranks;
        }

        public async Task RegNewPirate(Pirate pirate) // Metod för att skapa pirater
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                // Lägger till ny pirat
                using var command = new NpgsqlCommand("INSERT INTO pirate(name, rank_id) " +
                                                      "VALUES(@pirate_name, @rank_id)", conn);
                // Parametrar för kommandot
                command.Parameters.AddWithValue("pirate_name", pirate.Name); // Lägger till namn
                command.Parameters.AddWithValue("rank_id", pirate.RankId); // Lägger till id

                // kör och lägg till i databasen
                var result = await command.ExecuteNonQueryAsync();
            }
            // Hantera undantag
            catch (Exception ex)
            {
                // Felmeddelande
                MessageBox.Show($"Något blev fel: {ex.Message}");
            }
        }

        public async Task<List<Pirate>> GetAllPirates() // Metod för att hämta befintliga pirater
        {
            //Lista för att lagra alla pirater från databsen
            List<Pirate> pirates = new List<Pirate>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Hämta pirater från databasen
            using var command = new NpgsqlCommand("SELECT p.id, p.name, pa.id as parrot_id, pa.name as parrot_name FROM pirate p " +
                                                  "LEFT JOIN parrot pa ON pa.pirate_id = p.id", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Pirate pirate = new Pirate()
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString()
                    };

                    string details;
                    // https://stackoverflow.com/questions/9801649/inserting-null-to-sql-db-from-c-sharp-dbcommand om det är null i databasen 
                    if (reader["parrot_name"] != DBNull.Value)
                    {
                        Parrot parrot = new Parrot()
                        {
                            Id = (int)reader["parrot_id"],
                            PirateId = (int)reader["id"],
                            Name = reader["parrot_name"].ToString()
                        };
                        pirate.Parrots = new List<Parrot>() { parrot };
                    }

                    pirates.Add(pirate);
                }
            }
            return pirates;
        }
        
        public async Task<List<Ship>> GetAllShips() // Metod för att hämta befintliga skepp
        {
            //Lista för att lagra alla skepp från databsen
            List<Ship> ships = new List<Ship>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Hämta skepp från databasen
            using var command = new NpgsqlCommand("SELECT s.id, s.name, s.ship_type_id, s.is_sunk, st.crew_size, st.type as ship_type FROM ship s " +
                                                  "LEFT JOIN ship_type st ON s.ship_type_id = st.id", conn);
            // using var command = new NpgsqlCommand("SELECT id, name, ship_type_id, is_sunk FROM ship", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    //string details = reader["name"].ToString() + " - " + reader["ship_type"].ToString();
                    Ship ship = new Ship()
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString(),
                        //ShipTypeId = (int)reader["ship_type_id"],
                        IsSunk = (bool)reader["is_sunk"] //Tillagd efter för att kunna få skeppet att visas som sänkt
                    };
                    ShipType shipType = new ShipType()
                    {
                        Id = (int)reader["id"],
                        Type = reader["ship_type"].ToString(),
                        CrewSize = (int)reader["crew_size"]
                    };
                    ship.ShipType = shipType;
                    ships.Add(ship);
                    
                }
            }
            return ships;
        }

        public async Task<bool> RecruitPirate (int pirateId, int shipId)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                int crewLimit = await GetCrewMaxSizeAsync(shipId);

                int currentCrewCount = await CountCurrentCrewAsync(shipId);

                if (currentCrewCount >= crewLimit)
                {
                    MessageBox.Show("Skeppet har inte plats för fler pirater");
                    return false;
                }

                using var updateCommand = new NpgsqlCommand("UPDATE pirate SET ship_id = @ship_id WHERE id = @pirate_id", conn);
                updateCommand.Parameters.AddWithValue("ship_id", shipId); // 
                updateCommand.Parameters.AddWithValue("pirate_id", pirateId); // 

                var result = await updateCommand.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
                {
                    MessageBox.Show($"Något blev fel: {ex.Message}");
                    return false;
                }
        }

        public async Task<int> GetCrewMaxSizeAsync(int shipId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            int crewLimit = 0;
            using var shipCommand = new NpgsqlCommand("SELECT crew_size FROM ship s JOIN ship_type st ON st.id = s.ship_type_id WHERE s.id = @ship_id", conn); //sätter @ship_id då in-värdet kan variera Föreläsning 9 kring 49:45
            shipCommand.Parameters.AddWithValue("ship_id", shipId);

            var maxCrewSize = await shipCommand.ExecuteScalarAsync();
            if (maxCrewSize != null)
            {
                crewLimit = (int)maxCrewSize;
            }

            return crewLimit;
        }

        public async Task<int> CountCurrentCrewAsync(int shipId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            //int currentCrew = 0;
            using var crewCommand = new NpgsqlCommand("SELECT COUNT(*) ::int FROM pirate WHERE ship_id = @ship_id", conn);
            crewCommand.Parameters.AddWithValue("ship_id", shipId);


            // Hämtar pirater på skeppet
            var currentCrewCount = (int)await crewCommand.ExecuteScalarAsync();
           
            return currentCrewCount;
        }

        public async Task<Pirate> GetPirateDetailsByIdAsync(int pirateId) //Detaljhämtande funktion likt den vi hade i SkiDatabase
        {

            using var connection = new NpgsqlConnection(_connectionString);
            {
                await connection.OpenAsync();
                using var command = new NpgsqlCommand("SELECT p.id, p.name, p.rank_id, p.ship_id, r.name AS rank_name, s.name AS ship_name " +
                                                        "FROM pirate p " +
                                                        "LEFT JOIN rank r ON p.rank_id = r.id " +
                                                        "LEFT JOIN ship s ON p.ship_id = s.id " +
                                                        "WHERE p.id = @pirate_id", connection);
                
                command.Parameters.AddWithValue("pirate_id", pirateId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Pirate
                        {
                            Id = (int)reader["id"], //Rätt format. Föreläsning 8 ca 50min in
                            Name = reader["name"].ToString(),
                            RankId = (int)reader["rank_id"],
                            ShipId = (int)reader["ship_id"],
                        };
                    }
                }
            }
            return null; // Om ingen pirat hittades
        }


        public async Task<Pirate> SearchPirateAsync(string pirateName)
        {
            Pirate pirate = null;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            // https://www.w3schools.com/mysql/mysql_like.asp fixat sökningen så man kan söka på pirat eller papegoja
            using var command = new NpgsqlCommand("SELECT p.id, p.name, p.rank_id, p.ship_id, pa.name as parrot_name FROM pirate p " +
                                                  "LEFT JOIN parrot pa ON pa.pirate_id = p.id " +
                                                  "WHERE p.name LIKE @pirate_name OR pa.name LIKE @parrot_name", connection);

            command.Parameters.AddWithValue("pirate_name", pirateName);
            command.Parameters.AddWithValue("parrot_name", pirateName);

            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string details;
                        if (reader["parrot_name"] == DBNull.Value)
                        {
                            details = (string)reader["name"].ToString();
                        }
                        else
                        {
                            details = (string)reader["name"].ToString() + " - " + (string)reader["parrot_name"].ToString();
                        }
                        pirate = new Pirate
                        {
                            Id = (int)reader["id"], //Rätt format. Föreläsning 8 ca 50min in
                            Name = details,
                            RankId = (int)reader["rank_id"],
                            ShipId = (int)reader["ship_id"],
                        };
                    }
                }
            }
            return pirate;
        }

        public async Task<int> GetCrewCountByShipIdAsync(int shipId)
        {
            int crewCount = 0;

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("SELECT COUNT(*) FROM pirate WHERE ship_id = @ship_id", conn); //count(*) räknar antalet rader i en tabell https://www.w3schools.com/sql/sql_count.asp
                command.Parameters.AddWithValue("ship_id", shipId);

                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    crewCount = Convert.ToInt32(result); //VI hade ett konstigt felmeddelande med castingen mellan
                                                         //system.int64 till system int32.vi googlade och hittade denna lösning
                                                         //verkade vara ett problem med serial i databasen som krockade med en vanlig int i VS https://stackoverflow.com/questions/4947562/why-use-convert-toint32-over-casting
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid hämtning av besättningsantal: {ex.Message}");
            }

            return crewCount;
        }

        public async Task MarkShipAsSunk(int shipId) //metod för att markera att skeppet är sänkt. Inte fullt fungerande då tanken var att comboboxen med skepp skulle visa vilket skepp som  var sänkt oich vilket som flöt
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Update ship status to sunk
                using var command = new NpgsqlCommand("UPDATE ship SET is_sunk = true WHERE id = @ship_id", conn); //Uppdaterar tabllen https://www.w3schools.com/sql/sql_ref_update.asp
                command.Parameters.AddWithValue("ship_id", shipId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid sänkning av skepp: {ex.Message}");
            }
        }

        public async Task<List<Pirate>> GetPiratesByShipId(int shipId) //metod för att hämta pirater med ett skeppsid
        {
            List<Pirate> pirates = new List<Pirate>();

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("SELECT id, name, rank_id FROM pirate WHERE ship_id = @ship_id", conn);
                command.Parameters.AddWithValue("ship_id", shipId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Pirate pirate = new Pirate()
                        {
                            Id = (int)reader["id"],
                            Name = reader["name"].ToString(),
                            RankId = (int)reader["rank_id"],
                            ShipId = shipId
                        };
                        pirates.Add(pirate);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid hämtning av pirater: {ex.Message}");
            }

            return pirates;
        }

        public async Task DeletePirate(int pirateId) // metod för att ta bort pirater
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("DELETE FROM pirate WHERE id = @pirate_id", conn);
                command.Parameters.AddWithValue("pirate_id", pirateId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid borttagning av pirat: {ex.Message}");
            }
        }

        public async Task RemovePirateFromShip(int pirateId) // metod för att ta bort pirater från skepp
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("UPDATE pirate SET ship_id = NULL WHERE id = @pirate_id", conn);
                command.Parameters.AddWithValue("pirate_id", pirateId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel vid avlägsnande av pirat från skepp: {ex.Message}");
            }
        }

        public async Task<string> GetShipNameById(int shipId) // metod för att också hämta skeppets namn från databasen. 
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("SELECT name FROM ship WHERE id = @ship_id", conn);
                command.Parameters.AddWithValue("ship_id", shipId);
                
                var answerShip = await command.ExecuteScalarAsync();
                return answerShip != null ? answerShip.ToString() : "Okänt skepp";
            }
            catch (Exception ex)
            {
                return "Fel vid hämtning av skeppsnamn";
            }
        }

        public async Task<string> GetRankNameById(int rankId)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString); //copy-paste jag gjorde från getshipnamebyid ovan för att hämta rank-namnet i databasen
                await conn.OpenAsync();

                using var command = new NpgsqlCommand("SELECT name FROM rank WHERE id = @rank_id", conn);
                command.Parameters.AddWithValue("rank_id", rankId);

                var answerRank = await command.ExecuteScalarAsync();
                return answerRank != null ? answerRank.ToString() : "Okänd rank";
            }
            catch (Exception ex)
            {
                return "Fel vid hämtning av rank";
            }
        }

        //public async Task PiratesAfterSunkenShip()

    }
}
