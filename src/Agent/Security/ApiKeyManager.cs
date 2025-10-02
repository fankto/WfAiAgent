using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace WorkflowPlus.AIAgent.Security;

/// <summary>
/// Manages secure storage and retrieval of API keys using Windows DPAPI.
/// </summary>
public class ApiKeyManager
{
    private readonly ILogger _logger;
    private readonly string _keyFilePath;

    public ApiKeyManager(ILogger logger, string? keyFilePath = null)
    {
        _logger = logger;
        _keyFilePath = keyFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkflowPlus", "AIAgent", "api_key.dat");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_keyFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Save an API key securely using DPAPI encryption.
    /// </summary>
    public void SaveApiKey(string apiKey)
    {
        try
        {
            // Encrypt using DPAPI (Windows only)
            var plainBytes = Encoding.UTF8.GetBytes(apiKey);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null, // No additional entropy
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_keyFilePath, encryptedBytes);
            _logger.Information("API key saved securely");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save API key");
            throw new InvalidOperationException("Failed to save API key securely", ex);
        }
    }

    /// <summary>
    /// Retrieve and decrypt the stored API key.
    /// </summary>
    public string? GetApiKey()
    {
        try
        {
            if (!File.Exists(_keyFilePath))
            {
                _logger.Debug("No stored API key found");
                return null;
            }

            var encryptedBytes = File.ReadAllBytes(_keyFilePath);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

            var apiKey = Encoding.UTF8.GetString(plainBytes);
            _logger.Debug("API key retrieved successfully");
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve API key");
            return null;
        }
    }

    /// <summary>
    /// Delete the stored API key.
    /// </summary>
    public void DeleteApiKey()
    {
        try
        {
            if (File.Exists(_keyFilePath))
            {
                File.Delete(_keyFilePath);
                _logger.Information("API key deleted");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete API key");
        }
    }

    /// <summary>
    /// Check if an API key is stored.
    /// </summary>
    public bool HasApiKey()
    {
        return File.Exists(_keyFilePath);
    }

    /// <summary>
    /// Get a masked version of the API key for display (e.g., "sk-...xyz").
    /// </summary>
    public string? GetMaskedApiKey()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 10)
            return null;

        return $"{apiKey.Substring(0, 7)}...{apiKey.Substring(apiKey.Length - 4)}";
    }
}

/// <summary>
/// Scrubs PII (Personally Identifiable Information) from text before logging.
/// </summary>
public class PiiScrubber
{
    private static readonly System.Text.RegularExpressions.Regex EmailRegex = 
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex PhoneRegex = 
        new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex SsnRegex = 
        new(@"\b\d{3}-\d{2}-\d{4}\b", 
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex ApiKeyRegex = 
        new(@"\b(sk-[a-zA-Z0-9]{20,})\b", 
            System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Scrub PII from text, replacing with placeholders.
    /// </summary>
    public static string Scrub(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        text = EmailRegex.Replace(text, "[EMAIL]");
        text = PhoneRegex.Replace(text, "[PHONE]");
        text = SsnRegex.Replace(text, "[SSN]");
        text = ApiKeyRegex.Replace(text, "[API_KEY]");

        return text;
    }

    /// <summary>
    /// Check if text contains potential PII.
    /// </summary>
    public static bool ContainsPii(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return EmailRegex.IsMatch(text) ||
               PhoneRegex.IsMatch(text) ||
               SsnRegex.IsMatch(text) ||
               ApiKeyRegex.IsMatch(text);
    }
}
