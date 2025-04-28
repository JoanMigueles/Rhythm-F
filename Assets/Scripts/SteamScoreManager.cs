using Steamworks;
using System;
using UnityEngine;

public static class SteamScoreManager
{
    private static CallResult<LeaderboardFindResult_t> m_LeaderboardFindResult;
    private static CallResult<LeaderboardScoreUploaded_t> m_LeaderboardUploadResult;
    private static CallResult<LeaderboardScoresDownloaded_t> m_LeaderboardDownloadResult;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        m_LeaderboardFindResult = CallResult<LeaderboardFindResult_t>.Create();
        m_LeaderboardUploadResult = CallResult<LeaderboardScoreUploaded_t>.Create();
        m_LeaderboardDownloadResult = CallResult<LeaderboardScoresDownloaded_t>.Create();
    }

    public static void UploadScore(string leaderboardName, int score, Action<bool> callback = null)
    {
        if (!SteamManager.Initialized) {
            Debug.LogWarning("Steamworks not initialized - score not uploaded");
            callback?.Invoke(false);
            return;
        }

        var apiCall = SteamUserStats.FindLeaderboard(leaderboardName);
        m_LeaderboardFindResult.Set(apiCall, (result, failure) => {
            if (failure || result.m_bLeaderboardFound == 0) {
                Debug.LogError($"Leaderboard {leaderboardName} not found");
                callback?.Invoke(false);
                return;
            }

            var uploadCall = SteamUserStats.UploadLeaderboardScore(
                result.m_hSteamLeaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                null,
                0
            );

            m_LeaderboardUploadResult.Set(uploadCall, (uploadResult, uploadFailure) => {
                if (uploadFailure || uploadResult.m_bSuccess == 0) {
                    Debug.LogError($"Failed to upload score to {leaderboardName}");
                    callback?.Invoke(false);
                }
                else {
                    Debug.Log($"Successfully uploaded score of {score} to {leaderboardName}");
                    callback?.Invoke(true);
                }
            });
        });
    }

    public static void DownloadUserScore(string leaderboardName, Action<int, bool> callback)
    {
        if (!SteamManager.Initialized) {
            Debug.LogWarning("Steamworks not initialized - cannot download scores");
            callback?.Invoke(0, false);
            return;
        }

        var apiCall = SteamUserStats.FindLeaderboard(leaderboardName);
        m_LeaderboardFindResult.Set(apiCall, (result, failure) => {
            if (failure || result.m_bLeaderboardFound == 0) {
                Debug.LogError($"Leaderboard {leaderboardName} not found");
                callback?.Invoke(0, false);
                return;
            }

            var downloadCall = SteamUserStats.DownloadLeaderboardEntriesForUsers(
                result.m_hSteamLeaderboard,
                new CSteamID[] { SteamUser.GetSteamID() },
                1
            );

            m_LeaderboardDownloadResult.Set(downloadCall, (downloadResult, downloadFailure) => {
                if (downloadFailure || downloadResult.m_hSteamLeaderboardEntries.m_SteamLeaderboardEntries == 0) {
                    Debug.LogError($"Failed to download scores from {leaderboardName}");
                    callback?.Invoke(0, false);
                    return;
                }

                LeaderboardEntry_t entry;
                if (SteamUserStats.GetDownloadedLeaderboardEntry(
                    downloadResult.m_hSteamLeaderboardEntries,
                    0,
                    out entry,
                    null,
                    0)) {
                    callback?.Invoke(entry.m_nScore, true);
                }
                else {
                    Debug.Log($"No score found for user in {leaderboardName}");
                    callback?.Invoke(0, true);
                }
            });
        });
    }

    public static void DownloadTopScores(string leaderboardName, int count, Action<LeaderboardEntry_t[], bool> callback)
    {
        if (!SteamManager.Initialized) {
            Debug.LogWarning("Steamworks not initialized - cannot download scores");
            callback?.Invoke(null, false);
            return;
        }

        var apiCall = SteamUserStats.FindLeaderboard(leaderboardName);
        m_LeaderboardFindResult.Set(apiCall, (result, failure) => {
            if (failure || result.m_bLeaderboardFound == 0) {
                Debug.LogError($"Leaderboard {leaderboardName} not found");
                callback?.Invoke(null, false);
                return;
            }

            var downloadCall = SteamUserStats.DownloadLeaderboardEntries(
                result.m_hSteamLeaderboard,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                0,
                Mathf.Clamp(count, 1, 100)
            );

            m_LeaderboardDownloadResult.Set(downloadCall, (downloadResult, downloadFailure) => {
                if (downloadFailure || downloadResult.m_hSteamLeaderboardEntries.m_SteamLeaderboardEntries == 0) {
                    Debug.LogError($"Failed to download scores from {leaderboardName}");
                    callback?.Invoke(null, false);
                    return;
                }

                int entryCount = Mathf.Min(
                    SteamUserStats.GetLeaderboardEntryCount(result.m_hSteamLeaderboard),
                    count
                );

                LeaderboardEntry_t[] entries = new LeaderboardEntry_t[entryCount];
                for (int i = 0; i < entryCount; i++) {
                    SteamUserStats.GetDownloadedLeaderboardEntry(
                        downloadResult.m_hSteamLeaderboardEntries,
                        i,
                        out entries[i],
                        null,
                        0
                    );
                }

                callback?.Invoke(entries, true);
            });
        });
    }
}