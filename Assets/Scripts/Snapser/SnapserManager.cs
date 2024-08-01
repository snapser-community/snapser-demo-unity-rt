using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snapser.Api;
using Snapser.Model;
using UnityEngine;
using UnityEngine.Events;
using Utilities;
using Object = System.Object;

#region Supporting Structures

public enum SnapserService
{
    auth,
    profiles,
    storage,
    leaderboards,
    statistics
}

public class SnapserProfile
{
    public string gamertag;

    public SnapserProfile(string gt)
    {
        gamertag = gt;
    }
}

public class SnapserServiceResponse
{
    public bool Success { get; }
    public string ResponseMessage { get; }
    public Object Data { get; }

    public SnapserServiceResponse(bool success, string responseMessage, Object data = null)
    {
        Success = success;
        ResponseMessage = responseMessage;
        Data = data;
    }
}

#endregion

public class SnapserManager
{
    private AuthServiceApi _authService;
    private ProfilesServiceApi _profilesService;
    private LeaderboardsServiceApi _leaderboardsService;
    private StorageServiceApi _storageService;
    private StatisticsServiceApi _statisticsService;

    private static SnapserManager _instance;
    public static SnapserManager Instance => _instance;

    readonly Dictionary<SnapserService, bool> _snapserServicesState = new Dictionary<SnapserService, bool>();
    readonly Dictionary<SnapserService, bool> _snapserServicesChangedState = new Dictionary<SnapserService, bool>();

    public bool IsUsingCustomSnapend => !ClusterURL.IsEmptyOrNull();
    public bool HasProfilesEnabled => IsAuthenticated && _snapserServicesState[SnapserService.profiles];
    public bool HasStorageEnabled => IsAuthenticated && _snapserServicesState[SnapserService.storage];
    public bool HasLeaderboardsEnabled => IsAuthenticated && _snapserServicesState[SnapserService.leaderboards];
    public bool HasStatisticsEnabled => IsAuthenticated && _snapserServicesState[SnapserService.statistics];

    public event UnityAction<bool> OnSnapendCreated;
    public event UnityAction<Dictionary<SnapserService, bool>, Dictionary<SnapserService, bool>> OnSnapendEdited;
    public event UnityAction OnAuthenticationSuccessful;

    #region Snapser Properties

    private bool IsAuthenticated { get; set; } = false;

    private string ClusterURL { get; set; }

    public string _userId;
    public string UserId
    {
        get => _userId ?? PlayerPrefs.GetString(GameConstants.UId);
        set
        {
            _userId = value;
            PlayerPrefs.SetString(GameConstants.UId, _userId);
        }
    }

    private string _sessionToken;
    string SessionToken
    {
        get => _sessionToken ?? PlayerPrefs.GetString(GameConstants.SToken);
        set
        {
            _sessionToken = value;
            PlayerPrefs.SetString(GameConstants.SToken, _sessionToken);
        }
    }

    #endregion

    #region Snapser Manager Functions

    public SnapserManager(string cUrl)
    {
        _instance = this;

        ClusterURL = cUrl;
        PlayerPrefs.SetString(GameConstants.CAS, null);

        if (IsUsingCustomSnapend)
            _authService = new AuthServiceApi(ClusterURL);

        foreach (SnapserService service in (SnapserService[]) Enum.GetValues(typeof(SnapserService)))
        {
            _snapserServicesState.Add(service, true);
        }
    }

    public Dictionary<SnapserService, bool> UpdateSnapserServicesState(SnapserService service, bool state = true)
    {
        _snapserServicesChangedState[service] = true;
        _snapserServicesState[service] = state;
        PlayerPrefs.SetInt(service.ToString(), state ? 1 : 0);
        return _snapserServicesState;
    }

    void InstantiateRequiredSnapserServices()
    {
        string cURL = ClusterURL;

        if (_statisticsService == null && HasStatisticsEnabled)
        {
            _statisticsService = new StatisticsServiceApi(cURL);
            Debug.Log("Creating stats services for Cluster URL -  " + cURL);
        }

        if (_profilesService == null && HasProfilesEnabled)
        {
            _profilesService = new ProfilesServiceApi(cURL);
            Debug.Log("Creating profile services for  Cluster URL -  " + cURL);
        }

        if (_leaderboardsService == null && HasLeaderboardsEnabled)
        {
            _leaderboardsService = new LeaderboardsServiceApi(cURL);
            Debug.Log("Creating leaderboards services for Cluster URL -  " + cURL);
        }

        if (_storageService == null && HasStorageEnabled)
        {
            _storageService = new StorageServiceApi(cURL);
            Debug.Log("Creating storage services for Cluster URL -  " + cURL);
        }
    }

    public void ResetSnapserServices()
    {
        ClusterURL  = SessionToken = UserId = string.Empty;
        PlayerPrefs.SetString(GameConstants.CAS, string.Empty);
        _profilesService = null;
        _statisticsService = null;
        _leaderboardsService = null;
        _authService = null;
        _storageService = null;
    }

    #endregion

    #region Authentication

    public async void AuthenticationAnonLoginAsync(Action<SnapserServiceResponse> callback = null, string username = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to start authentication from user/auth services.");
            return;
        }

        if (_authService == null)
        {
            Debug.LogError("Auth service is not instantiated. This indicates there is a flow issue between cluster creation and the authentication call. Auth call can't be initiated.");
            return;
        }

        AuthAnonLoginResponse response;
        SnapserServiceResponse authResponse;
        try
        {
            string anonUsername = username.IsEmptyOrNull() ? GameConstants.DefaultPlayerName + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") : username;

            response = await _authService.AnonLoginAsync(new AuthAnonLoginRequest(true, anonUsername));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            authResponse = new SnapserServiceResponse(false, e.Message, e);
            callback?.Invoke(authResponse);
            Debug.Log("Authentication Call Complete - " + authResponse.Success + " - " + authResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            UserId = response.User.Id;
            SessionToken = response.User.SessionToken;
            authResponse = new SnapserServiceResponse(true, "Ok", response);
            Debug.Log("Authentication Call Complete - " + authResponse.Success + " - " + authResponse.ResponseMessage + ". UserID - " + response.User.Id + ". SessionToken - " + response.User.SessionToken);
            IsAuthenticated = true;
            InstantiateRequiredSnapserServices();
            callback?.Invoke(authResponse);
            OnAuthenticationSuccessful?.Invoke();
        }
    }
    
    public void AuthenticationAnonLogin(Action<SnapserServiceResponse> callback = null, string username = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to start authentication from user/auth services.");
            return;
        }

        if (_authService == null)
        {
            Debug.LogError("Auth service is not instantiated. This indicates there is a flow issue between cluster creation and the authentication call. Auth call can't be initiated.");
            return;
        }

        AuthAnonLoginResponse response;
        SnapserServiceResponse authResponse;
        try
        {
            string anonUserName = username.IsEmptyOrNull() ? GameConstants.DefaultPlayerName + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") : username;
                
            response = _authService.AnonLogin(new AuthAnonLoginRequest(true, anonUserName));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            authResponse = new SnapserServiceResponse(false, e.Message, e);
            callback?.Invoke(authResponse);
            Debug.Log("Authentication Call Complete - " + authResponse.Success + " - " + authResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            UserId = response.User.Id;
            SessionToken = response.User.SessionToken;
            authResponse = new SnapserServiceResponse(true, "Ok", response);
            Debug.Log("Authentication Call Complete - " + authResponse.Success + " - " + authResponse.ResponseMessage + ". UserID - " + response.User.Id + ". SessionToken - " + response.User.SessionToken);
            IsAuthenticated = true;
            InstantiateRequiredSnapserServices();
            callback?.Invoke(authResponse);
            OnAuthenticationSuccessful?.Invoke();
        }
    }

    #endregion

    #region Profiles

    public async void UpsertProfileAsync(string gamertag, Action<SnapserServiceResponse> callback = null)
    {

        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get the profile from profile services.");
            return;
        }


        if (_profilesService == null || !HasProfilesEnabled)
        {
            Debug.LogError("Profile service is not instantiated. Upsert Profile call can't be initiated");
            return;
        }

        Object response;
        SnapserServiceResponse upsertProfileResponse;
        try
        {
            string jsonObject = JsonConvert.SerializeObject(new { gamertag });
            response = await _profilesService.UpsertProfileAsync(UserId, SessionToken, new UpsertProfileRequest(new SnapserProfile(gamertag)));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            upsertProfileResponse = new SnapserServiceResponse(false, e.Message, e);
            callback?.Invoke(upsertProfileResponse);
            Debug.Log("Upsert Profile Call Complete - " + upsertProfileResponse.Success + " - " + upsertProfileResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            upsertProfileResponse = new SnapserServiceResponse(true, "Ok");
            callback?.Invoke(upsertProfileResponse);
            Debug.Log("Upsert Profile Call Complete - " + upsertProfileResponse.Success + " - " + upsertProfileResponse.ResponseMessage);
        }
    }

    public async void GetUserProfileAsync(Action<SnapserServiceResponse> callback = null, string userId = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get the profile from profile services.");
            return;
        }

        if (_profilesService == null || !HasProfilesEnabled)
        {
            Debug.LogError("Profile service is not instantiated. Get Profile call can't be initiated");
            return;
        }

        ProfilesGetProfileResponse response;
        SnapserServiceResponse getProfileResponse;
        string uId = userId.IsEmptyOrNull() ? UserId : userId;

        try
        {
            response = await _profilesService.GetProfileAsync(uId, SessionToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            getProfileResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(getProfileResponse);
            Debug.Log("Get Profile Call Complete - " + getProfileResponse.Success + " - " + getProfileResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            JObject jObjectProfile = (JObject) response.Profile;
            SnapserProfile profile = jObjectProfile.ToObject<SnapserProfile>();
            getProfileResponse = new SnapserServiceResponse(true, "Ok", profile);
            callback?.Invoke(getProfileResponse);
            Debug.Log("Get Profile Call Complete - " + getProfileResponse.Success + " - " + getProfileResponse.ResponseMessage +". GamerTag - " + profile.gamertag);
        }
    }

    #endregion

    #region Leaderboard
    
    public async void IncrementLeaderboardScoreAsync(string leaderboardName, int score, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get the leaderboard info from leaderboard services.");
            return;
        }

        if (_leaderboardsService == null || !HasLeaderboardsEnabled)
        {
            Debug.LogError("Leaderboard service is not instantiated. Get Leaderboard call can't be initiated");
            return;
        }

        object response;
        SnapserServiceResponse incrementLeaderboardScoreResponse;
        try
        {
            response = await _leaderboardsService.IncrementScoreAsync(leaderboardName, UserId, SessionToken, new IncrementScoreRequest(score));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            incrementLeaderboardScoreResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(incrementLeaderboardScoreResponse);
            Debug.Log("Increment Leaderboard Call Complete - " + incrementLeaderboardScoreResponse.Success + " - " + incrementLeaderboardScoreResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            incrementLeaderboardScoreResponse = new SnapserServiceResponse(true, "Ok");
            callback?.Invoke(incrementLeaderboardScoreResponse);
            Debug.Log("Increment Leaderboard Call Complete - " + incrementLeaderboardScoreResponse.Success + " - " + incrementLeaderboardScoreResponse.ResponseMessage);
        }
    }
    
    public async void SetLeaderboardScoreAsync(string leaderboardName, int score, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get the leaderboard info from leaderboard services.");
            return;
        }

        if (_leaderboardsService == null || !HasLeaderboardsEnabled)
        {
            Debug.LogError("Leaderboard service is not instantiated. Get Leaderboard call can't be initiated");
            return;
        }

        object response;
        SnapserServiceResponse setLeaderboardScoreResponse;
        try
        {
            response = await _leaderboardsService.SetScoreAsync(leaderboardName, UserId, SessionToken, new SetScoreRequest(score));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            setLeaderboardScoreResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(setLeaderboardScoreResponse);
            Debug.Log("Set Leaderboard Call Complete - " + setLeaderboardScoreResponse.Success + " - " + setLeaderboardScoreResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            setLeaderboardScoreResponse = new SnapserServiceResponse(true, "Ok");
            callback?.Invoke(setLeaderboardScoreResponse);
            Debug.Log("Set Leaderboard Call Complete - " + setLeaderboardScoreResponse.Success + " - " + setLeaderboardScoreResponse.ResponseMessage);
        }
    }
    
    public async void GetLeaderboardAsync(string leaderboardName, string range, long count, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get the leaderboard info from leaderboard services.");
            return;
        }

        if (_leaderboardsService == null || !HasLeaderboardsEnabled)
        {
            Debug.LogError("Leaderboard service is not instantiated. Get Leaderboard call can't be initiated");
            return;
        }

        LeaderboardsGetScoresResponse response;
        SnapserServiceResponse getLeaderboardResponse;
        try
        {
            //string leaderboardName, string range, long count, string token, string userId = default(string), int? offset = default(int?
            response = await _leaderboardsService.GetScoresAsync(leaderboardName, range, count, SessionToken, UserId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            getLeaderboardResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(getLeaderboardResponse);
            Debug.Log("Get Leaderboard Call Complete - " + getLeaderboardResponse.Success + " - " + getLeaderboardResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            getLeaderboardResponse = new SnapserServiceResponse(true, "Ok", response.UserScores);
            callback?.Invoke(getLeaderboardResponse);
            Debug.Log("Get Leaderboard Call Complete - " + getLeaderboardResponse.Success + " - " + getLeaderboardResponse.ResponseMessage + ". Total entries - " + response.UserScores.Count);
        }
    }

    #endregion

    #region Storage

    public async void GetStorageBlob(string key, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get storage data from storage services.");
            return;
        }


        if (_storageService == null || !HasStorageEnabled)
        {
            Debug.LogError("Storage service is not instantiated. Get storage call can't be initiated");
            return;
        }

        StorageGetBlobResponse response;
        SnapserServiceResponse getStorageBlobResponse;
        try
        {
            string cas = PlayerPrefs.GetString(GameConstants.CAS, null);
            response = await _storageService.GetBlobAsync(UserId, "protected", key, SessionToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            getStorageBlobResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(getStorageBlobResponse);
            Debug.Log("Get Storage Blob Call Complete - " + getStorageBlobResponse.Success + " - " + getStorageBlobResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            string cas = response.Cas;
            PlayerPrefs.SetString(GameConstants.CAS, cas);
            getStorageBlobResponse = new SnapserServiceResponse(true, "Ok", response.Value);
            callback?.Invoke(getStorageBlobResponse);
            Debug.Log("Get Storage Blob Call Complete - " + getStorageBlobResponse.Success + " - " + getStorageBlobResponse.ResponseMessage +". CAS - " + cas);
        }
    }

    public async void ReplaceStorageBlob(string key, string value, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get storage data from storage services.");
            return;
        }


        if (_storageService == null || !HasStorageEnabled)
        {
            Debug.LogError("Storage service is not instantiated. Replace storage call can't be initiated");
            return;
        }

        StorageReplaceBlobResponse response;
        SnapserServiceResponse replaceStorageBlobResponse;
        try
        {
            string cas = PlayerPrefs.GetString(GameConstants.CAS, null);
            response = await _storageService.ReplaceBlobAsync(UserId, "protected", key, SessionToken, new ReplaceBlobRequest(cas, cas.IsEmptyOrNull(), 0L, value));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            replaceStorageBlobResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(replaceStorageBlobResponse);
            Debug.Log("Replace Storage Blob Call Complete - " + replaceStorageBlobResponse.Success + " - " + replaceStorageBlobResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            string cas = response.Cas;
            PlayerPrefs.SetString(GameConstants.CAS, cas);
            replaceStorageBlobResponse = new SnapserServiceResponse(true, "Ok", value);
            callback?.Invoke(replaceStorageBlobResponse);
            Debug.Log("Replace Storage Blob Call Complete - " + replaceStorageBlobResponse.Success + " - " + replaceStorageBlobResponse.ResponseMessage +". CAS - " + cas);
        }
    }

    #endregion

    #region Statistics

    public async void GetStatistic(string key, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get storage data from storage services.");
            return;
        }


        if (_statisticsService == null || !HasStatisticsEnabled)
        {
            Debug.LogError("Stats service is not instantiated. Increment stat call can't be initiated");
            return;
        }

        StatisticsUserStatistic response;
        SnapserServiceResponse incrementStatResponse;
        try
        {
            response = await _statisticsService.GetUserStatisticAsync(UserId, key, SessionToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            incrementStatResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(incrementStatResponse);
            Debug.Log("Get Stats Call Complete - " + incrementStatResponse.Success + " - " + incrementStatResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            incrementStatResponse = new SnapserServiceResponse(true, "Ok", response);
            callback?.Invoke(incrementStatResponse);
            Debug.Log("Get Stats Call Complete - " + incrementStatResponse.Success + " - " + incrementStatResponse.ResponseMessage + ". Stat for key - " + response.Key + " is " + response.Value + ". User Id - " + response.UserId);
        }
    }

    public async void IncrementStatistic(string key, int value, Action<SnapserServiceResponse> callback = null)
    {
        if (ClusterURL.IsEmptyOrNull())
        {
            Debug.LogError("No cluster url available to get storage data from storage services.");
            return;
        }


        if (_statisticsService == null || !HasStatisticsEnabled)
        {
            Debug.LogError("Stats service is not instantiated. Increment stat call can't be initiated");
            return;
        }

        StatisticsUserStatistic response;
        SnapserServiceResponse incrementStatResponse;
        try
        {
            response = await _statisticsService.IncrementUserStatisticAsync(UserId, key, SessionToken, new IncrementUserStatisticRequest(value));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            incrementStatResponse = new SnapserServiceResponse(false, e.Message);
            callback?.Invoke(incrementStatResponse);
            Debug.Log("Increment Stats Call Complete - " + incrementStatResponse.Success + " - " + incrementStatResponse.ResponseMessage);
            return;
        }

        if (response != null)
        {
            incrementStatResponse = new SnapserServiceResponse(true, "Ok", response);
            callback?.Invoke(incrementStatResponse);
            Debug.Log("Increment Stats Call Complete - " + incrementStatResponse.Success + " - " + incrementStatResponse.ResponseMessage + ". Stat for key - " + response.Key + " is " + response.Value + ". User Id - " + response.UserId);
        }
    }

    #endregion
}
