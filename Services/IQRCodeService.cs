using System.Collections.Generic;

namespace CollegeIssueManagement.Services
{
    public interface IQRCodeService
    {
        string GenerateQRCode(string data);
        string GenerateIssueFormQRCode(int issueId);
        string GenerateQRCodeAndSaveToFile(string data, string fileName);
        string GenerateIssueTypeQRCode(string issueType);
        Dictionary<string, string> GenerateAllIssueTypeQRCodes();
    }
}