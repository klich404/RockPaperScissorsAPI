using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using RockPaperScissorAPI.Models;
using System.Numerics;

namespace RockPaperScissorAPI.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class RockPaperScissorController : ControllerBase
    {
        private IConfiguration _configuration;

        private (Player player1, Player player2) GetGameInfo(SqlConnection connection, int gameID)
        {
            string getPlayerInfoQuery = @"
        SELECT p1.PlayerID, p1.FullName, p2.PlayerID, p2.FullName
        FROM Games g
        JOIN Players p1 ON g.Player1ID = p1.PlayerID
        JOIN Players p2 ON g.Player2ID = p2.PlayerID
        WHERE g.GameID = @gameID";

            using (SqlCommand command = new SqlCommand(getPlayerInfoQuery, connection))
            {
                command.Parameters.AddWithValue("@gameID", gameID);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    Player player1 = new Player
                    {
                        PlayerID = reader.GetInt32(0),
                        FullName = reader.GetString(1)
                    };
                    Player player2 = new Player
                    {
                        PlayerID = reader.GetInt32(2),
                        FullName = reader.GetString(3)
                    };
                    return (player1, player2);
                }
            }
        }

        private string DetermineRoundWinner(string player1Move, string player2Move)
        {
            if (player1Move == player2Move)
            {
                return "Empate";
            }
            else if ((player1Move == "Piedra" && player2Move == "Tijeras") ||
                     (player1Move == "Tijeras" && player2Move == "Papel") ||
                     (player1Move == "Papel" && player2Move == "Piedra"))
            {
                return "Player1";
            }
            else
            {
                return "Player2";
            }
        }

        private int GetPlayerID(SqlConnection connection, int gameID, string playerColumn)
        {
            string getPlayerIDQuery = $"SELECT {playerColumn} FROM Games WHERE GameID = @gameID";
            using (SqlCommand command = new SqlCommand(getPlayerIDQuery, connection))
            {
                command.Parameters.AddWithValue("@gameID", gameID);
                return (int)command.ExecuteScalar();
            }
        }

        public RockPaperScissorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPost]
        [Route("Player")]
        public IActionResult Player([FromForm] string name)
        {
            string sqlDataSource = _configuration.GetConnectionString("RockPaperScissorsCon");
            string checkQuery = "SELECT PlayerID FROM Players WHERE FullName = @name";
            string insertQuery = "INSERT INTO Players (FullName) VALUES (@name); SELECT SCOPE_IDENTITY();";  // Devuelve el ID recién insertado

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();

                // Verificar si el jugador ya existe
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, myCon))
                {
                    checkCommand.Parameters.AddWithValue("@name", name);
                    var result = checkCommand.ExecuteScalar();

                    // Si el jugador ya existe, devolvemos su ID y mensaje de bienvenida
                    if (result != null)
                    {
                        int existingPlayerID = Convert.ToInt32(result);
                        return Ok(new { message = $"Bienvenido de nuevo {name}!", playerID = existingPlayerID });
                    }
                }

                // Si no existe, lo insertamos y devolvemos el nuevo PlayerID
                using (SqlCommand insertCommand = new SqlCommand(insertQuery, myCon))
                {
                    insertCommand.Parameters.AddWithValue("@name", name);
                    int newPlayerID = Convert.ToInt32(insertCommand.ExecuteScalar());  // Obtener el PlayerID generado
                    myCon.Close();

                    return StatusCode(201, new { message = $"Nuevo jugador creado: {name}!", playerID = newPlayerID });
                }
            }
        }



        [HttpPost]
        [Route("Game")]
        public IActionResult CreateGame([FromForm] int player1ID, [FromForm] int player2ID)
        {
            if (player1ID == player2ID)
            {
                return BadRequest(new { message = "Los jugadores no pueden ser el mismo!" });
            }

            using (SqlConnection myCon = new SqlConnection(_configuration.GetConnectionString("RockPaperScissorsCon")))
            {
                myCon.Open();

                string checkPlayerQuery = "SELECT COUNT(1) FROM Players WHERE PlayerID = @playerID";
                bool player1Exists = false;
                bool player2Exists = false;

                using (SqlCommand checkCommand = new SqlCommand(checkPlayerQuery, myCon))
                {
                    checkCommand.Parameters.AddWithValue("@playerID", player1ID);
                    player1Exists = (int)checkCommand.ExecuteScalar() > 0;

                    checkCommand.Parameters["@playerID"].Value = player2ID;
                    player2Exists = (int)checkCommand.ExecuteScalar() > 0;
                }

                if (!player1Exists && !player2Exists)
                {
                    return NotFound(new { message = "Ninguno de los jugadores existe." });
                }
                else if (!player1Exists)
                {
                    return NotFound(new { message = $"El jugador 1 con ID {player1ID} no existe." });
                }
                else if (!player2Exists)
                {
                    return NotFound(new { message = $"El jugador 2 con ID {player2ID} no existe." });
                }

                string insertGameQuery = "INSERT INTO Games (Player1ID, Player2ID) OUTPUT INSERTED.GameID VALUES (@player1ID, @player2ID)";
                int newGameID;

                using (SqlCommand insertCommand = new SqlCommand(insertGameQuery, myCon))
                {
                    insertCommand.Parameters.AddWithValue("@player1ID", player1ID);
                    insertCommand.Parameters.AddWithValue("@player2ID", player2ID);
                    newGameID = (int)insertCommand.ExecuteScalar();
                }

                string getPlayerInfoQuery = "SELECT PlayerID, FullName FROM Players WHERE PlayerID = @playerID";
                Player player1Info;
                Player player2Info;

                using (SqlCommand getPlayerInfoCommand = new SqlCommand(getPlayerInfoQuery, myCon))
                {
                    getPlayerInfoCommand.Parameters.AddWithValue("@playerID", player1ID);
                    using (SqlDataReader reader = getPlayerInfoCommand.ExecuteReader())
                    {
                        reader.Read();
                        player1Info = new Player
                        {
                            PlayerID = reader.GetInt32(0),
                            FullName = reader.GetString(1)
                        };
                    }

                    getPlayerInfoCommand.Parameters["@playerID"].Value = player2ID;
                    using (SqlDataReader reader = getPlayerInfoCommand.ExecuteReader())
                    {
                        reader.Read();
                        player2Info = new Player
                        {
                            PlayerID = reader.GetInt32(0),
                            FullName = reader.GetString(1)
                        };
                    }
                }

                myCon.Close();

                return StatusCode(201, new
                {
                    message = "Nuevo juego creado!",
                    gameID = newGameID,
                    player1 = player1Info,
                    player2 = player2Info
                });
            }
        }


        [HttpPost]
        [Route("Round")]
        public IActionResult PlayRound([FromForm] int gameID, [FromForm] string player1Move, [FromForm] string player2Move)
        {
            // Validar que las jugadas sean válidas (Piedra, Papel, Tijeras)
            string[] validMoves = { "Piedra", "Papel", "Tijeras" };
            if (!validMoves.Contains(player1Move) || !validMoves.Contains(player2Move))
            {
                return BadRequest(new { message = "Movimientos inválidos. Use: Piedra, Papel o Tijeras." });
            }

            string sqlDataSource = _configuration.GetConnectionString("RockPaperScissorsCon");

            using (SqlConnection myCon = new SqlConnection(sqlDataSource))
            {
                myCon.Open();

                // Verificar que el juego existe
                string checkGameExistsQuery = "SELECT COUNT(*) FROM Games WHERE GameID = @gameID";
                int gameExists = 0;

                using (SqlCommand checkGameExistsCommand = new SqlCommand(checkGameExistsQuery, myCon))
                {
                    checkGameExistsCommand.Parameters.AddWithValue("@gameID", gameID);
                    gameExists = (int)checkGameExistsCommand.ExecuteScalar();
                }

                if (gameExists == 0)
                {
                    return NotFound(new { message = "El juego no existe." });
                }

                // Verificar si el juego ya tiene un ganador
                string checkWinnerQuery = "SELECT WinnerID FROM Games WHERE GameID = @gameID";
                int? winnerID = null;

                using (SqlCommand checkWinnerCommand = new SqlCommand(checkWinnerQuery, myCon))
                {
                    checkWinnerCommand.Parameters.AddWithValue("@gameID", gameID);
                    winnerID = checkWinnerCommand.ExecuteScalar() as int?;
                }

                // Si ya hay un ganador, no permitimos más rondas
                if (winnerID != null && winnerID > 0)
                {
                    return BadRequest(new { message = "El juego ya ha terminado. No se pueden registrar más rondas." });
                }

                // Continuar el juego si no hay ganador
                string roundResult = DetermineRoundWinner(player1Move, player2Move);

                // Obtener el número de la próxima ronda
                string roundNumberQuery = "SELECT ISNULL(MAX(RoundNumber), 0) + 1 FROM Rounds WHERE GameID = @gameID";
                int roundNumber;

                using (SqlCommand roundNumberCommand = new SqlCommand(roundNumberQuery, myCon))
                {
                    roundNumberCommand.Parameters.AddWithValue("@gameID", gameID);
                    roundNumber = (int)roundNumberCommand.ExecuteScalar();
                }

                // Insertar la nueva ronda
                string insertRoundQuery = "INSERT INTO Rounds (GameID, RoundNumber, Player1Move, Player2Move, RoundResult) " +
                                          "VALUES (@gameID, @roundNumber, @player1Move, @player2Move, @roundResult)";

                using (SqlCommand insertCommand = new SqlCommand(insertRoundQuery, myCon))
                {
                    insertCommand.Parameters.AddWithValue("@gameID", gameID);
                    insertCommand.Parameters.AddWithValue("@roundNumber", roundNumber);
                    insertCommand.Parameters.AddWithValue("@player1Move", player1Move);
                    insertCommand.Parameters.AddWithValue("@player2Move", player2Move);
                    insertCommand.Parameters.AddWithValue("@roundResult", roundResult);
                    insertCommand.ExecuteNonQuery();
                }

                // Contar las victorias de cada jugador
                string countWinsQuery = @"
    SELECT 
        SUM(CASE WHEN RoundResult = 'Player1' THEN 1 ELSE 0 END) AS Player1Wins,
        SUM(CASE WHEN RoundResult = 'Player2' THEN 1 ELSE 0 END) AS Player2Wins
    FROM Rounds
    WHERE GameID = @gameID";

                int player1Wins = 0;
                int player2Wins = 0;

                using (SqlCommand countWinsCommand = new SqlCommand(countWinsQuery, myCon))
                {
                    countWinsCommand.Parameters.AddWithValue("@gameID", gameID);
                    using (SqlDataReader reader = countWinsCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            player1Wins = reader.GetInt32(0);
                            player2Wins = reader.GetInt32(1);
                        }
                    }
                }

                // Obtener la información del juego y los jugadores
                var gameInfo = GetGameInfo(myCon, gameID);

                // Determinar si hay un ganador de la ronda
                string roundWinnerName = roundResult == "Player1" ? gameInfo.player1.FullName : gameInfo.player2.FullName;

                // Determinar si ya hay un ganador del juego (alguien alcanzó 3 victorias)
                if (player1Wins >= 3 || player2Wins >= 3)
                {
                    // Identificar el ganador del juego
                    int finalWinnerID = player1Wins >= 3 ? gameInfo.player1.PlayerID : gameInfo.player2.PlayerID;

                    // Actualizar la tabla Games con el ganador
                    string updateWinnerQuery = "UPDATE Games SET WinnerID = @winnerID WHERE GameID = @gameID";
                    using (SqlCommand updateWinnerCommand = new SqlCommand(updateWinnerQuery, myCon))
                    {
                        updateWinnerCommand.Parameters.AddWithValue("@winnerID", finalWinnerID);
                        updateWinnerCommand.Parameters.AddWithValue("@gameID", gameID);
                        updateWinnerCommand.ExecuteNonQuery();
                    }

                    // Retornar la información final del juego
                    string gameWinnerName = player1Wins >= 3 ? gameInfo.player1.FullName : gameInfo.player2.FullName;

                    return Ok(new
                    {
                        message = $"Juego terminado, {gameWinnerName} es el ganador.",
                        player1Wins,
                        player2Wins,
                        player1 = gameInfo.player1,
                        player2 = gameInfo.player2
                    });
                }

                // Si no hay ganador del juego, retornamos el ganador de la ronda
                return Ok(new
                {
                    message = $"{roundWinnerName} ha ganado la ronda.",
                    player1Wins,
                    player2Wins,
                    player1 = gameInfo.player1,
                    player2 = gameInfo.player2
                });
            }
        }


    }
}
