using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeRunner.Services
{
    public class BlobsProxy
    {
        private Config config;
        private ILogger logger;

        public BlobsProxy(Config config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public async Task SaveMaze(MazeDefinition maze)
        {
            if (maze == null) throw new ArgumentNullException(nameof(maze));
            var raw = JsonConvert.SerializeObject(maze);
            var bytes = Encoding.UTF8.GetBytes(raw);
            await this.UploadFile(bytes, ComposeMazeFileName(maze.MazeUid));
        }

        public async Task<MazeDefinition> GetMaze(Guid uid)
        {
            var fileName = ComposeMazeFileName(uid);
            if (!ExitsFile(fileName)) return null;
            var bytes = await DownloadFile(fileName);
            var raw = Encoding.UTF8.GetString(bytes);
            var result = JsonConvert.DeserializeObject<MazeDefinition>(raw);
            return result;
        }
        public async Task SaveGame(GameDefinition game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            var raw = JsonConvert.SerializeObject(game);
            var bytes = Encoding.UTF8.GetBytes(raw);
            await this.UploadFile(bytes, ComposeGameFileName(game.MazeUid, game.GameUid));
        }
        public async Task<GameDefinition> GetGame(Guid mazeUid, Guid gameUid)
        {
            var fileName = ComposeGameFileName(mazeUid, gameUid);
            if (!ExitsFile(fileName)) return null;
            var bytes = await DownloadFile(fileName);
            var raw = Encoding.UTF8.GetString(bytes);
            var result = JsonConvert.DeserializeObject<GameDefinition>(raw);
            return result;
        }

        private string ComposeMazeFileName(Guid mazeUid) => "MAZE" + mazeUid.ToString() + ".json";
        private string ComposeGameFileName(Guid mazeUid, Guid gameUid) => $"GAME{mazeUid}_{gameUid}.json";

        public async Task UploadFile(byte[] data, string fileName)
        {
            try
            {
                var container = GetContainer();
                var client = container.GetBlobClient(fileName);

                MemoryStream stream = new MemoryStream(data);
                await client.UploadAsync(stream, true);
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error uploading the blob");
                throw;
            }
        }

        public async Task<byte[]> DownloadFile(string fileName)
        {
            try
            {
                var container = GetContainer();
                var client = container.GetBlobClient(fileName);

                MemoryStream stream = new MemoryStream();
                var response = await client.DownloadAsync();
                response.Value.Content.CopyTo(stream);
                var result = stream.ToArray();
                return result;
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error downloading the blob");
                throw;
            }
        }

        public bool ExitsFile(string fileName)
        {
            try
            {
                var container = GetContainer();
                var client = container.GetBlobClient(fileName);
                return client.Exists();
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error checking if the blob exits");
                throw;
            }
        }

        public async Task RenameFile(string oldName, string newName)
        {
            var container = GetContainer();
            var oldFile = container.GetBlobClient(oldName);
            var oldFileStream = await oldFile.OpenReadAsync();
            var newFile = container.GetBlobClient(newName);
            await newFile.UploadAsync(oldFileStream);
            oldFile.Delete();
        }

        public async Task DeleteFile(string fileName)
        {
            var container = GetContainer();
            var file = container.GetBlobClient(fileName);
            await file.DeleteAsync();
        }

        public IEnumerable<string> ListFiles(string prefix)
        {
            var container = GetContainer();
            var searchResult = container.GetBlobs(prefix: prefix).Select(x => x.Name).ToArray();
            return searchResult;
        }

        public async Task DeleteFilesOlderThan(int days)
        {
            List<Task> result = new List<Task>();
            var container = GetContainer();
            var searchResult = container.GetBlobs();
            foreach (var item in searchResult)
            {
                if (item.Properties?.CreatedOn != null
                    && item.Properties.CreatedOn.HasValue
                    && item.Properties.CreatedOn.Value.DateTime < DateTime.Now.AddDays(-1 * days))
                {
                    result.Add(DeleteFile(item.Name));
                }
            }

            await Task.WhenAll(result.ToArray());
        }

        private BlobContainerClient GetContainer()
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(config.BlobConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(config.BlobContainer);
            return containerClient;
        }
    }
}
