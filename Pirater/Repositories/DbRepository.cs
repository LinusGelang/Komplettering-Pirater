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
                                                        "VALUES(@pirate_name,  @rank_id)", conn);
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
            using var command = new NpgsqlCommand("SELECT id, name FROM pirate", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Pirate pirate = new Pirate()
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"].ToString()
                    };
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
            using var command = new NpgsqlCommand("SELECT id, name, ship_type_id FROM ship", conn);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Ship ship = new Ship()
                    {
                        Id = (int)reader["id"],
                        Name = (string)reader["name"].ToString(),
                        ShipTypeId = (int)reader["ship_type_Id"]
                    };
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

                // Hämta skepps typ för att kolla besättning
                int shipTypeId = 0;
                using var shipCommand = new NpgsqlCommand("SELECT ship_type_id FROM ship WHERE id = @ship_id", conn);
                shipCommand.Parameters.AddWithValue("ship_id", shipId);

                int currentCrew = 0;
                using var crewCommand = new NpgsqlCommand("SELECT id FROM pirate WHERE ship_id = @ship_id", conn);
                crewCommand.Parameters.AddWithValue("ship_id", shipId);


                // Hämtar piratare på skeppet
                using (var reader = crewCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        currentCrew++; // Öka currentCrew för varje pirat som hittas
                    }
                }

                int maxCrew = 0;
                if (shipTypeId == 1)
                {
                    maxCrew = 2;
                }
                else if (shipTypeId == 2)
                {
                    maxCrew = 3;
                }
                else if(shipTypeId == 3)
                {
                    maxCrew = 4;
                }
                else
                {
                    maxCrew = 0;
                }

                if (currentCrew >= maxCrew)
                {
                    MessageBox.Show("Skeppet har inte plats för fler pirater");
                    return false;
                }

                using var updateCommand = new NpgsqlCommand("UPDATE pirate SET ship_id = @ship_id WHERE id = @pirate_id", conn);
                updateCommand.Parameters.AddWithValue("ship_id", shipId); // 
                updateCommand.Parameters.AddWithValue("pirate_id", pirateId); // 

                var result = await updateCommand.ExecuteNonQueryAsync();
                MessageBox.Show("Piraten har blivit tilldelad ett skepp");
                return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Något blev fel: {ex.Message}");
                    return false;
                }
        }





        public async Task<Pirate> SearchPirateAsync(string pirateName)
        {
            Pirate pirate = null;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand("SELECT id, name, rank_id, ship_id FROM pirate", connection);
            command.Parameters.AddWithValue("id", pirateName);
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pirate = new Pirate
                        {
                            Id = (int)reader["id"], //Rätt format. Föreläsning 8 ca 50min in
                            Name = reader["name"].ToString(),
                            RankId = (int)reader["rank_id"],
                            ShipId = (int)reader["ship_id"],
                        };

                    }
                }

            }
            return null;
        }

    }
}
