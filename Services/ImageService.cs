using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Okafor_.NET.Services
{
    public interface IImageService
    {
        string GetRandomHospitalImage();
        List<string> GetRandomHospitalImages(int count);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageService> _logger;
        private readonly Random _random = new Random();
        private readonly string _imageDirectory;
        private List<string> _cachedImages = new List<string>();

        public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
        {
            _env = env;
            _logger = logger;
            _imageDirectory = Path.Combine(env.WebRootPath, "images", "placeholders", "Hospital");
            LoadImages();
        }

        private void LoadImages()
        {
            _cachedImages = new List<string>();

            if (Directory.Exists(_imageDirectory))
            {
                var webpFiles = Directory.GetFiles(_imageDirectory, "*.webp")
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                _cachedImages = webpFiles;
                _logger.LogInformation("Loaded {ImageCount} hospital placeholder images from {ImageDirectory}.", _cachedImages.Count, _imageDirectory);
                return;
            }

            _logger.LogWarning("Hospital placeholder image directory was not found at {ImageDirectory}.", _imageDirectory);
        }

        public string GetRandomHospitalImage()
        {
            if (_cachedImages.Count == 0)
            {
                _logger.LogInformation("Falling back to the default hospital placeholder image because no local images were found.");
                return "/images/placeholders/placeholder.svg";
            }

            var randomImage = _cachedImages[_random.Next(_cachedImages.Count)];
            return $"/images/placeholders/Hospital/{randomImage}";
        }

        public List<string> GetRandomHospitalImages(int count)
        {
            if (_cachedImages.Count == 0)
            {
                _logger.LogInformation("Falling back to the default hospital placeholder image list because no local images were found.");
                return new List<string> { "/images/placeholders/placeholder.svg" };
            }

            var shuffled = _cachedImages.OrderBy(_ => _random.Next()).Take(count).ToList();
            return shuffled.Select(img => $"/images/placeholders/Hospital/{img}").ToList();
        }
    }
}
