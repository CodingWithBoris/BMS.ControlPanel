using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using BMS.ControlPanel.Models;
using BMS.Shared.Models;

namespace BMS.ControlPanel.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    // For local development, use http://localhost:8080/api/v1/
    // For production, use https://bms-production-f22e.up.railway.app/api/v1/
    private const string BaseUrl = "https://bms-production-f22e.up.railway.app/api/v1/";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Initialized with base URL: {BaseUrl}");
    }

    public void SetAuthToken(string? jwtToken)
    {
        if (jwtToken == null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Auth token cleared");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Auth token set: {jwtToken[..Math.Min(20, jwtToken.Length)]}...");
        }
    }

    // ──────────────────────────────────────────
    // Generic response parsing
    // ──────────────────────────────────────────
    private async Task<T?> ParseApiResponse<T>(HttpResponseMessage response) where T : class
    {
        var json = await response.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLine($"API Response [{response.StatusCode}]: {json[..Math.Min(json.Length, 500)]}");

        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<T>>(json, _jsonOptions);
            return apiResponse?.Success == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Parse error: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return response.StatusCode.ToString();
    }

    // ──────────────────────────────────────────
    // Auth Endpoints
    // ──────────────────────────────────────────
    public async Task<Models.AuthResult?> LoginAsync(string username, string password)
    {
        try
        {
            var request = new { username, password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("auth/login", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Login Error ({response.StatusCode}): {responseText}");
                return new Models.AuthResult { Success = false, Message = $"Server: {response.StatusCode}" };
            }
            
            return ParseAuthResponse(responseText);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login Error: {ex}");
            return new Models.AuthResult { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task<Models.AuthResult?> RegisterAsync(string username, string password)
    {
        try
        {
            var request = new { username, password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("auth/register", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Register Error ({response.StatusCode}): {responseText}");
                return new Models.AuthResult { Success = false, Message = $"Server: {response.StatusCode}" };
            }
            
            return ParseAuthResponse(responseText);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register Error: {ex}");
            return new Models.AuthResult { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task<string?> GetDiscordOAuthUrlAsync()
    {
        try
        {
            var redirectUri = "http://localhost:3000/oauth/callback";
            var requestUrl = $"auth/discord?redirectUri={Uri.EscapeDataString(redirectUri)}";
            var response = await _httpClient.GetAsync(requestUrl);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"API returned {response.StatusCode}: {responseText}");

            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String)
                return data.GetString();

            throw new Exception("Invalid API response format");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Discord OAuth URL Error: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.AuthResult?> DiscordCallbackAsync(string code)
    {
        try
        {
            var request = new { code, redirectUri = "http://localhost:3000/oauth/callback" };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("auth/discord/callback", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
                return new Models.AuthResult { Success = false, Message = $"Server: {response.StatusCode}" };

            return ParseAuthResponse(responseText);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Discord Callback Error: {ex}");
            return new Models.AuthResult { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    // ──────────────────────────────────────────
    // User Endpoints
    // ──────────────────────────────────────────
    public async Task<User?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("users/me");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<User>>(json, _jsonOptions);
            return apiResponse?.Success == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Current User Error: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string Message)> TestAuthAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Testing auth - Authorization header present: {_httpClient.DefaultRequestHeaders.Authorization != null}");
            
            var response = await _httpClient.GetAsync("auth/me");
            var responseBody = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] TestAuth response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] TestAuth response body: {responseBody}");
            
            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Auth test failed: {response.StatusCode} - {responseBody}");
            }

            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<User>>(responseBody, _jsonOptions);
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                return (true, $"Auth working! Logged in as: {apiResponse.Data.Username}");
            }
            
            return (false, "Auth test failed: Could not parse response");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] TestAuth exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<List<Faction>> GetUserFactionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("users/me/factions");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<Faction>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get User Factions Error: {ex.Message}");
            return new();
        }
    }

    // ──────────────────────────────────────────
    // Faction Endpoints
    // ──────────────────────────────────────────
    public async Task<List<Faction>> GetFactionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("factions");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<Faction>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Factions Error: {ex.Message}");
            return new();
        }
    }

    public async Task<Faction?> CreateFactionAsync(string title, string officerPassword, string viewPassword, List<CreateRoleModel>? roles = null, string? discordServerId = null)
    {
        try
        {
            var request = new
            {
                title,
                officerPassword,
                viewPassword,
                discordServerId = string.IsNullOrWhiteSpace(discordServerId) ? null : discordServerId.Trim(),
                roles = roles ?? new List<CreateRoleModel>()
            };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("factions", content);
            return await ParseApiResponse<Faction>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create Faction Error: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string Message)> JoinFactionAsync(string factionId, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Joining faction {factionId}");
            
            // Check if auth token is set
            var hasAuth = _httpClient.DefaultRequestHeaders.Authorization != null;
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Authorization header present: {hasAuth}");
            if (hasAuth)
            {
                System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Auth scheme: {_httpClient.DefaultRequestHeaders.Authorization?.Scheme}");
            }
            
            var request = new { password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"factions/{factionId}/join", content);
            var responseText = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Response body length: {responseText?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Response body: {responseText?[..Math.Min(responseText.Length, 500)]}");

            if (string.IsNullOrWhiteSpace(responseText))
            {
                return (false, $"Server returned empty response with status {response.StatusCode}");
            }

            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<Faction>>(responseText, _jsonOptions);
            if (apiResponse?.Success == true)
                return (true, apiResponse.Message);
            return (false, apiResponse?.Message ?? "Failed to join faction");
        }
        catch (JsonException jex)
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] JSON Parse Error: {jex.Message}");
            return (false, $"Invalid server response: {jex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] Join Faction Error: {ex.Message}");
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<Faction?> GetFactionAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}");
            return await ParseApiResponse<Faction>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Faction Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteFactionAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"factions/{factionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete Faction Error: {ex.Message}");
            return false;
        }
    }

    public async Task<Faction?> UpdateFactionAsync(string factionId, string? title = null, string? discordServerId = null)
    {
        try
        {
            var request = new
            {
                title,
                discordServerId = discordServerId?.Trim()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"factions/{factionId}", content);
            return await ParseApiResponse<Faction>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update Faction Error: {ex.Message}");
            return null;
        }
    }

    // ──────────────────────────────────────────
    // BMS Orders Endpoints
    // ──────────────────────────────────────────
    public async Task<List<BmsOrder>> GetAllOrdersAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}/orders/all");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<BmsOrder>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get All Orders Error: {ex.Message}");
            return new();
        }
    }

    public async Task<List<BmsOrder>> GetPublishedOrdersAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}/orders");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<BmsOrder>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Published Orders Error: {ex.Message}");
            return new();
        }
    }

    public async Task<BmsOrder?> CreateOrderAsync(string factionId, int orderIndex, string title, string content, string? roleId = null, List<OrderSection>? sections = null)
    {
        try
        {
            var request = new { orderIndex, title, content, roleId, sections };
            var json = JsonSerializer.Serialize(request);
            var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"factions/{factionId}/orders", body);
            return await ParseApiResponse<BmsOrder>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create Order Error: {ex.Message}");
            return null;
        }
    }

    public async Task<BmsOrder?> UpdateOrderAsync(string factionId, string orderId, string? title = null, string? content = null, bool? isPublished = null, int? orderIndex = null, string? roleId = null, string? mapImageUrl = null, List<OrderSection>? sections = null)
    {
        try
        {
            var request = new { title, content, isPublished, orderIndex, roleId, mapImageUrl, sections };
            var json = JsonSerializer.Serialize(request);
            var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"factions/{factionId}/orders/{orderId}", body);
            return await ParseApiResponse<BmsOrder>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update Order Error: {ex.Message}");
            return null;
        }
    }

    public async Task<BmsOrder?> PublishOrderAsync(string factionId, string orderId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"factions/{factionId}/orders/{orderId}/publish", null);
            return await ParseApiResponse<BmsOrder>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Publish Order Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteOrderAsync(string factionId, string orderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"factions/{factionId}/orders/{orderId}");
            if (!response.IsSuccessStatusCode) return false;
            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<string>>(json, _jsonOptions);
            return apiResponse?.Success == true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete Order Error: {ex.Message}");
            return false;
        }
    }

    // ──────────────────────────────────────────
    // Personnel Endpoints
    // ──────────────────────────────────────────
    public async Task<List<FactionOfficer>> GetPersonnelAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}/personnel");
            if (!response.IsSuccessStatusCode) return new();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<FactionOfficer>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Personnel Error: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> UpdateOfficerAsync(string factionId, string userId, bool? isActive = null, string? roleId = null)
    {
        try
        {
            var request = new { isActive, roleId };
            var json = JsonSerializer.Serialize(request);
            var body = new HttpRequestMessage(new HttpMethod("PATCH"), $"factions/{factionId}/personnel/{userId}")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            var response = await _httpClient.SendAsync(body);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update Officer Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveOfficerAsync(string factionId, string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"factions/{factionId}/personnel/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Remove Officer Error: {ex.Message}");
            return false;
        }
    }

    // ──────────────────────────────────────────
    // Roles Management
    // ──────────────────────────────────────────
    public async Task<List<Role>> GetRolesAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}/roles");
            if (!response.IsSuccessStatusCode) return new List<Role>();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<Role>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new List<Role>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get Roles Error: {ex.Message}");
            return new List<Role>();
        }
    }

    public async Task<Role?> CreateRoleAsync(string factionId, string name, string? password, bool isDefault)
    {
        try
        {
            var request = new { name, password, isDefault };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"factions/{factionId}/roles", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<Role>>(json, _jsonOptions);
            return apiResponse?.Success == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create Role Error: {ex.Message}");
            return null;
        }
    }

    public async Task<Role?> UpdateRoleAsync(string factionId, string roleId, string? name = null, string? password = null, bool? isDefault = null)
    {
        try
        {
            var request = new { name, password, isDefault };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync($"factions/{factionId}/roles/{roleId}", content);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<Role>>(json, _jsonOptions);
            return apiResponse?.Success == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update Role Error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteRoleAsync(string factionId, string roleId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"factions/{factionId}/roles/{roleId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete Role Error: {ex.Message}");
            return false;
        }
    }

    // ──────────────────────────────────────────
    // VC Roster
    // ──────────────────────────────────────────
    public async Task<List<VcRosterModel>> GetVcRosterAsync(string factionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"factions/{factionId}/vc-roster");
            if (!response.IsSuccessStatusCode) return new List<VcRosterModel>();

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<Shared.Models.ApiResponse<List<VcRosterModel>>>(json, _jsonOptions);
            return apiResponse?.Success == true && apiResponse.Data != null ? apiResponse.Data : new List<VcRosterModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get VC Roster Error: {ex.Message}");
            return new List<VcRosterModel>();
        }
    }

    public async Task<bool> UpdateVcMemberAsync(string factionId, string discordUserId,
        string? team = null, string? callsign = null, string? role = null, bool? isHidden = null)
    {
        try
        {
            var request = new { team, callsign, role, isHidden };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync($"factions/{factionId}/vc-roster/{discordUserId}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update VC Member Error: {ex.Message}");
            return false;
        }
    }

    // ──────────────────────────────────────────
    // Helper Methods
    // ──────────────────────────────────────────
    private Models.AuthResult? ParseAuthResponse(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("success", out var successProp) && root.TryGetProperty("data", out var dataProp))
            {
                var success = successProp.GetBoolean();
                var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                
                var authResult = JsonSerializer.Deserialize<Models.AuthResult>(dataProp.GetRawText(), _jsonOptions);
                if (authResult != null)
                {
                    authResult.Success = success;
                    authResult.Message = message;
                    return authResult;
                }
            }
            else
            {
                return JsonSerializer.Deserialize<Models.AuthResult>(responseText, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing auth response: {ex.Message}");
        }
        
        return null;
    }

    // ──────────────────────────────────────────
    // Objectives
    // ──────────────────────────────────────────
    public async Task<bool> ReplaceObjectivesAsync(string factionId, string orderId, List<MissionObjective> objectives)
    {
        try
        {
            var requests = objectives.Select(o => new { text = o.Text }).ToList();
            var json = JsonSerializer.Serialize(requests);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"factions/{factionId}/orders/{orderId}/objectives", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ControlPanel API] ReplaceObjectives error: {ex.Message}");
            return false;
        }
    }
}

// Helper model for API requests
public class CreateRoleModel
{
    public string Name { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsDefault { get; set; }
}
