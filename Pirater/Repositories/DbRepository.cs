using Microsoft.Extensions.Configuration;
using Npgsql;
using Pirater.Tabeller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

            // Skapa SQL - kommando för att hämta ranker från databasen
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
    }
}
