using System;
using System.Collections.Generic;
using System.Text;

namespace MazeRunner
{
    public class Config
    {
        public string BlobConnectionString { get; set; }
        public string BlobContainer { get; set; }

        public static Config Read()
        {
            Config config = new Config();
            config.BlobConnectionString = System.Environment.GetEnvironmentVariable("BlobConnectionString");
            config.BlobContainer = System.Environment.GetEnvironmentVariable("BlobContainer");
            return config;
        }
    }
}
