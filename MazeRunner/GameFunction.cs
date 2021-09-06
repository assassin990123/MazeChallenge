using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MazeRunner.Dtos;
using MazeRunner.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace MazeRunner
{
    public static class GameFunction
    {
        [FunctionName("Game")]
        //[OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        //[OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "game/{mazeUid}/{gameUid?}")] HttpRequest req,
            string mazeUid,
            string gameUid,
            ILogger log)
        {
            var blobs = new BlobsProxy(Config.Read(), log);
            // check maze
            if (!Guid.TryParse(mazeUid, out Guid mazeUidParsed)) { return new BadRequestObjectResult("The maze uid is not valid"); }
            var maze = await blobs.GetMaze(mazeUidParsed);
            if (maze == null) { return new BadRequestObjectResult("The maze uid has not been generated"); }

            if (req.Method == "POST")
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<GamePostRequest>(requestBody);
                if (data == null) { return new BadRequestObjectResult("The body is invalid"); }

                if (string.IsNullOrWhiteSpace(gameUid) && data.Operation == PlayerOperation.Start)
                {
                    // create new game
                    GameDefinition game = new GameDefinition()
                    {
                        MazeUid = mazeUidParsed,
                        GameUid = Guid.NewGuid(),
                        Completed = false,
                        CurrentPositionX = 0,
                        CurrentPositionY = 0
                    };

                    await blobs.SaveGame(game);
                    return new OkObjectResult(game);
                }
                else
                {
                    if (!Guid.TryParse(gameUid, out Guid gameUidParsed)) { return new BadRequestObjectResult("The gameUid is invalid"); }
                    var game = await blobs.GetGame(mazeUidParsed, gameUidParsed);
                    if (game == null) { return new BadRequestObjectResult("The gameUid doesn't exit"); }

                    switch (data.Operation)
                    {
                        case PlayerOperation.Start:
                            game.CurrentPositionX = 0;
                            game.CurrentPositionY = 0;
                            game.Completed = false;
                            await blobs.SaveGame(game);
                            return new OkObjectResult(GetGameResponse(game, maze));
                        case PlayerOperation.NotSet:
                            return new BadRequestObjectResult("Operation is not set");
                        default:
                            if (EvaluateMovement(maze, game, data.Operation))
                            {
                                if (game.CurrentPositionX==maze.Width && game.CurrentPositionY==maze.Height)
                                {
                                    game.Completed = true;
                                }
                                await blobs.SaveGame(game);
                                return new OkObjectResult(GetGameResponse(game, maze));
                            }
                            else
                            {
                                return new BadRequestObjectResult(" -You shall not pass- Gandalf said. (There is a wall in that direction");
                            }
                    }
                }
            }
            else if (req.Method=="GET")
            {
                if (!Guid.TryParse(gameUid, out Guid gameUidParsed)) { return new BadRequestObjectResult("The gameUid is invalid"); }
                var game = await blobs.GetGame(mazeUidParsed, gameUidParsed);
                if (game == null) { return new BadRequestObjectResult("The gameUid doesn't exit"); }
                return new OkObjectResult(GetGameResponse(game, maze));
            }


            return new BadRequestObjectResult("Method not defined");
        }

        private static GameResponse GetGameResponse(GameDefinition game, MazeDefinition maze)
        {
            GameResponse response = new GameResponse()
            {
                Game = game
            };

            var node = maze.Blocks.Where(item => item.CoordX == game.CurrentPositionX && item.CoordY == game.CurrentPositionY).FirstOrDefault();
            response.MazeBlockView = node;
            return response;
        }

        private static bool EvaluateMovement(MazeDefinition maze, GameDefinition game, PlayerOperation operation)
        {
            if (game.Completed) return false; // a completed game doesnt allow more movements
            var node = maze.Blocks.Where(item => item.CoordX == game.CurrentPositionX && item.CoordY == game.CurrentPositionY).FirstOrDefault();
            if (node == null) return false; // something strange happens
            switch (operation)
            {
                case PlayerOperation.GoNorth:
                    if (node.NorthBlocked) return false;
                    else game.CurrentPositionY = game.CurrentPositionY - 1;
                    return true;
                case PlayerOperation.GoSouth:
                    if (node.SouthBlocked) return false;
                    else game.CurrentPositionY = game.CurrentPositionY + 1;
                    return true;
                case PlayerOperation.GoEast:
                    if (node.EastBlocked) return false;
                    else game.CurrentPositionX = game.CurrentPositionX + 1;
                    return true;
                case PlayerOperation.GoWest:
                    if (node.WestBlocked) return false;
                    else game.CurrentPositionX = game.CurrentPositionX - 1;
                    return true;
            }

            return false;
        }
    }
}

