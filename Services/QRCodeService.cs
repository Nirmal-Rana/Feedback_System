using QRCoder;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CollegeIssueManagement.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;

        public QRCodeService(IWebHostEnvironment hostEnvironment, IConfiguration configuration)
        {
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
        }

        public string GenerateQRCode(string data)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    // Create QR code data with error correction level Q (25%)
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

                    // Generate QR code as bitmap
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        // Get PNG bytes (20 pixels per module)
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);

                        // Convert to base64 string for inline display
                        return Convert.ToBase64String(qrCodeBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate QR code: {ex.Message}", ex);
            }
        }

        public string GenerateIssueFormQRCode(int issueId)
        {
            // Get base URL from configuration
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var formUrl = $"{baseUrl}/Home/IssueDetails/{issueId}";
            return GenerateQRCode(formUrl);
        }

        // Alternative method to save QR code to file
        public string GenerateQRCodeAndSaveToFile(string data, string fileName)
        {
            try
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "qrcodes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, $"{fileName}.png");

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        System.IO.File.WriteAllBytes(filePath, qrCodeBytes);
                    }
                }

                return $"/qrcodes/{fileName}.png";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save QR code: {ex.Message}", ex);
            }
        }

        // Generate QR code for different issue types
        public string GenerateIssueTypeQRCode(string issueType)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
            var formUrl = $"{baseUrl}/Home/CreateIssue?type={issueType}";
            return GenerateQRCode(formUrl);
        }

        // Generate multiple QR codes for all issue types
        public Dictionary<string, string> GenerateAllIssueTypeQRCodes()
        {
            var qrCodes = new Dictionary<string, string>();
            var issueTypes = new[] {
                "Internet", "IDCard", "Internship",
                "JobOpportunity", "FacultyFeedback",
                "Counselling", "ManagementAttention"
            };

            foreach (var issueType in issueTypes)
            {
                qrCodes[issueType] = GenerateIssueTypeQRCode(issueType);
            }

            return qrCodes;
        }
    }
}
