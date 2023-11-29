// ReSharper disable UnusedMember.Global

using API.Contexts.Objects;
using API.Services;
using DotNetBungieAPI.Models;
using DotNetBungieAPI.Models.Authorization;
using DotNetBungieAPI.Models.Destiny;
using DotNetBungieAPI.Models.User;
using DotNetBungieAPI.Service.Abstractions;

namespace API.Util;

public static class UserExtensions
{
    public static long SignId(ulong unsignedValue)
    {
        var bytes = BitConverter.GetBytes(unsignedValue);
        return BitConverter.ToInt64(bytes, 0);
    }

    public static ulong UnSignId(long signedValue)
    {
        var bytes = BitConverter.GetBytes(signedValue);
        return BitConverter.ToUInt64(bytes, 0);
    }

    public static async Task UpdateMembership(this BungieProfile user, IBungieClient bungieClient)
    {
        var logger = LogService.CreateLogger("UpdateMembership");

        try
        {
            var latestProfile = new DestinyProfileUserInfoCard();

            var linkedProfiles =
                await bungieClient.ApiAccess.Destiny2.GetLinkedProfiles(BungieMembershipType.BungieNext,
                    user.MembershipId, true);

            foreach (var potentialProfile in linkedProfiles.Response.Profiles)
                if (potentialProfile.DateLastPlayed > latestProfile.DateLastPlayed)
                    latestProfile = potentialProfile;

            user.DestinyMembershipId = latestProfile.MembershipId;
            user.DestinyMembershipType = latestProfile.MembershipType;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update membership for {id}", user.MembershipId);
        }
    }

    public static bool NeedsRefresh(this BungieProfile user)
    {
        return user.TokenExpires < DateTime.UtcNow &&
               user.RefreshExpires > DateTime.UtcNow;
    }

    public static async Task RefreshToken(
        this BungieProfile user,
        IBungieClient bungieClient,
        DateTime nowTime)
    {
        var logger = LogService.CreateLogger("RefreshToken");

        try
        {
            var refreshedUser = await bungieClient.Authorization.RenewToken(user.GetTokenData());

            user.OauthToken = refreshedUser.AccessToken;
            user.TokenExpires = nowTime.AddSeconds(refreshedUser.ExpiresIn);
            user.RefreshToken = refreshedUser.RefreshToken;
            user.RefreshExpires = nowTime.AddSeconds(refreshedUser.RefreshExpiresIn);

            if (user.DestinyMembershipId == 0) await user.UpdateMembership(bungieClient);

            logger.LogDebug("Refreshed token for {id}", user.MembershipId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to refresh token for {id}", user.MembershipId);
        }
    }

    public static async Task<long> GetLatestCharacter(
        this BungieProfile user, IBungieClient bungieClient)
    {
        var logger = LogService.CreateLogger("GetLatestCharacter");

        try
        {
            var characterQuery = await bungieClient.ApiAccess.Destiny2.GetProfile(
                user.DestinyMembershipType,
                user.DestinyMembershipId,
                [DestinyComponentType.Characters],
                user.GetTokenData());

            var latestCharacter = characterQuery.Response.Characters.Data.Values.MaxBy(x => x.DateLastPlayed);

            if (latestCharacter != null)
                return latestCharacter.CharacterId;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get latest character for {id}", user.MembershipId);
        }

        return 0;
    }

    public static AuthorizationTokenData GetTokenData(this BungieProfile user)
    {
        return new AuthorizationTokenData
        {
            AccessToken = user.OauthToken,
            RefreshToken = user.RefreshToken,
            ExpiresIn = (int)(user.TokenExpires - DateTime.UtcNow).TotalSeconds,
            MembershipId = user.MembershipId,
            RefreshExpiresIn = (int)(user.RefreshExpires - DateTime.UtcNow).TotalSeconds,
            TokenType = "Bearer"
        };
    }
}
