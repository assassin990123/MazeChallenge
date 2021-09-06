using System;
using System.IO;
using System.Net;
using System.Text;
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
    public static class MazeManager
    {
        [FunctionName("MazeManager")]
        //[OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        //[OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Maze/{mazeUid?}")] HttpRequest req,
            string mazeUid,
            ILogger log)
        {
            var config = Config.Read();
            var blobProxy = new BlobsProxy(config, log);

            if (req.Method == "POST")
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                MazePostRequest data = JsonConvert.DeserializeObject<MazePostRequest>(requestBody);
                if (data.Height<1 || data.Height>150) return new BadRequestObjectResult("Height property is out of range (0,150)");
                if (data.Width < 1 || data.Width > 150) return new BadRequestObjectResult("Width property is out of range (0,150)");

                MazeGenerator g = new MazeGenerator();
                var maze = g.Generate(data.Width, data.Height);

                await blobProxy.SaveMaze(maze);
                return new OkObjectResult(new MazePostResponse()
                {
                    MazeUid = maze.MazeUid,
                    Height = maze.Height,
                    Width = maze.Width
                });
            }
            else if (req.Method=="GET")
            {
                if (!Guid.TryParse(mazeUid, out Guid mazeUidParsed)) { return new BadRequestObjectResult("Error parsing maze uid"); }
                var maze = await blobProxy.GetMaze(mazeUidParsed);
                return new OkObjectResult(maze);
            }

            return new BadRequestObjectResult("Method not available");
        }
    }
}

